/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.Case;

using Jube.Case.Outbound;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Service.CaseDispatch.Models;
using Jube.Service.Charts.Datasource;
using Jube.Service.Charts.Datasource.Template;
using log4net;

namespace Jube.Service.CaseDispatch
{
    public sealed class CaseDispatcher(
        DbContext dbContext,
        string userName,
        int tenantRegistryId,
        DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
        ILog log,
        bool tenantScoped = false)
    {
        private readonly CaseWorkflowDispatchRepository repository =
            new(dbContext, tenantRegistryId, userName);

        public async Task<CaseRows> DispatchAsync(DispatchSource source, DispatchCase target,
            IReadOnlyDictionary<string, string?>? values, LegacyRender? legacy, CaseRows? preloaded,
            CancellationToken token = default)
        {
            var plan = await PrepareAsync(source, target, values, legacy, preloaded, token).ConfigureAwait(false);

            await plan.NotifyAsync(token).ConfigureAwait(false);
            await plan.CallAsync(token).ConfigureAwait(false);

            return plan.Rows;
        }

        public async Task<DispatchPlan> PrepareAsync(DispatchSource source, DispatchCase target,
            IReadOnlyDictionary<string, string?>? values, LegacyRender? legacy, CaseRows? preloaded,
            CancellationToken token = default)
        {
            if (!source.Wanted)
            {
                return new DispatchPlan(this, source, target, values, legacy, preloaded ?? CaseRows.None, null, false);
            }

            if (!source.UsesSql)
            {
                return new DispatchPlan(this, source, target, values, legacy, CaseRows.None, null, false);
            }

            CaseRows rows;

            try
            {
                rows = preloaded ?? await Runner().RunAsync(source.Sql, target.Id, target.KeyValue, token)
                    .ConfigureAwait(false);
            }
            catch (CaseSqlException e)
            {
                await RefuseAllAsync(source, target, null, e.Message, token).ConfigureAwait(false);

                return new DispatchPlan(this, source, target, values, legacy, CaseRows.None, null, true);
            }

            var roots = new Dictionary<string, object?>(CaseRowsContext.Roots(rows), StringComparer.OrdinalIgnoreCase)
            {
                ["Form"] = new Dictionary<string, object?>((values ?? new Dictionary<string, string?>())
                    .Select(v => new KeyValuePair<string, object?>(v.Key, v.Value)), StringComparer.OrdinalIgnoreCase)
            };

            return new DispatchPlan(this, source, target, values, legacy, rows, roots, false);
        }

        private CaseSqlRunner Runner()
        {
            var connection = dynamicEnvironment.AppSettings("ReportConnectionString") ??
                             dynamicEnvironment.AppSettings("ConnectionString");

            return tenantScoped
                ? CaseSqlRunner.ForTenant(connection, tenantRegistryId, log)
                : new CaseSqlRunner(connection, userName, log);
        }

        internal async Task<string?> NotifyPlanAsync(DispatchPlan plan, CancellationToken token)
        {
            var source = plan.Source;

            if (!source.EnableNotification || plan.Refused)
            {
                return null;
            }

            if (!source.UsesSql)
            {
                return plan.Legacy == null
                    ? null
                    : await LegacyNotifyAsync(source, plan.Target, plan.Legacy, token).ConfigureAwait(false);
            }

            return await NotifyAsync(source, plan.Target, plan.Rows, plan.Roots!, token).ConfigureAwait(false);
        }

        internal async Task<string?> CallPlanAsync(DispatchPlan plan, CancellationToken token)
        {
            var source = plan.Source;

            if (!source.EnableHttp || plan.Refused)
            {
                return null;
            }

            if (!source.UsesSql)
            {
                return plan.Legacy == null
                    ? null
                    : await LegacyCallAsync(source, plan.Target, plan.Legacy, token).ConfigureAwait(false);
            }

            return await CallAsync(source, plan.Target, plan.Rows, plan.Roots!, plan.Values, token)
                .ConfigureAwait(false);
        }

        private async Task<string?> NotifyAsync(DispatchSource source, DispatchCase target, CaseRows rows,
            IReadOnlyDictionary<string, object?> roots, CancellationToken token)
        {
            var sms = source.NotificationTypeId != 1;
            var channel = sms ? CaseDispatchChannel.Sms : CaseDispatchChannel.Email;
            string destination;
            string subject;
            string body;

            try
            {
                destination = TemplateText.Render(source.Destination, roots, TemplateText.Plain).Trim();
                subject = EmailTarget.Subject(TemplateText.Render(source.Subject, roots, TemplateText.Plain));
                body = TemplateText.Render(source.Body, roots, sms ? TemplateText.Plain : TemplateText.Html);
            }
            catch (DatasourceChartException e)
            {
                await RecordAsync(source, target, channel, CaseDispatchMode.Sql, source.Destination, source.Subject,
                    source.Body, CaseDispatchOutcome.Refused, e.Message, null, Guid.Empty, rows, token)
                    .ConfigureAwait(false);

                return null;
            }

            var recipients = sms ? null : EmailTarget.Recipients(destination);
            var number = sms ? EmailTarget.Number(destination) : null;

            if (recipients == null && number == null)
            {
                await RecordAsync(source, target, channel, CaseDispatchMode.Sql, destination, subject, body,
                    CaseDispatchOutcome.Refused, "The rendered destination is not " + (sms ? "a telephone number." : "a list of email addresses."),
                    null, Guid.Empty, rows, token).ConfigureAwait(false);

                return null;
            }

            var result = await new Notification(log, dynamicEnvironment).TrySendAsync(source.NotificationTypeId,
                recipients != null ? string.Join(",", recipients) : number!, subject, body, false, token)
                .ConfigureAwait(false);

            await RecordAsync(source, target, channel, CaseDispatchMode.Sql, recipients != null
                    ? string.Join(", ", recipients)
                    : number, subject, body, result.Ok ? CaseDispatchOutcome.Sent : CaseDispatchOutcome.Failed,
                result.Error, result.Status, result.CorrelationId, rows, token).ConfigureAwait(false);

            return destination;
        }

        private async Task<string?> CallAsync(DispatchSource source, DispatchCase target, CaseRows rows,
            IReadOnlyDictionary<string, object?> roots, IReadOnlyDictionary<string, string?>? values,
            CancellationToken token)
        {
            var post = source.HttpEndpointTypeId == 1;
            var channel = post ? CaseDispatchChannel.HttpPost : CaseDispatchChannel.HttpGet;
            var (url, refusal) = HttpTarget.Resolve(source.HttpEndpoint, roots);

            if (url == null)
            {
                await RecordAsync(source, target, channel, CaseDispatchMode.Sql, source.HttpEndpoint, null, null,
                    CaseDispatchOutcome.Refused, $"The endpoint was refused: {refusal}.", null, Guid.Empty, rows,
                    token).ConfigureAwait(false);

                return source.HttpEndpoint;
            }

            var body = post
                ? DispatchBody.Build(source.Kind.ToString().ToLowerInvariant(), source.Name, target.Id, target.Key,
                    target.KeyValue, userName, rows.Rows, values)
                : null;

            var guarded = GuardedHttpEndpoint.For(dynamicEnvironment);

            var result = post
                ? await guarded.PostAsync(url, body!, log, token: token).ConfigureAwait(false)
                : await guarded.GetAsync(url, log, token: token).ConfigureAwait(false);

            await RecordAsync(source, target, channel, CaseDispatchMode.Sql, url, null, body,
                result.Ok
                    ? CaseDispatchOutcome.Sent
                    : result.Refusal != null
                        ? CaseDispatchOutcome.Refused
                        : CaseDispatchOutcome.Failed, result.Error, result.Status,
                result.CorrelationId, rows, token).ConfigureAwait(false);

            return url;
        }

        private async Task<string?> LegacyNotifyAsync(DispatchSource source, DispatchCase target, LegacyRender legacy,
            CancellationToken token)
        {
            var sms = source.NotificationTypeId != 1;
            var destination = legacy.Replace(source.Destination) ?? string.Empty;
            var subject = legacy.Replace(source.Subject) ?? string.Empty;
            var body = legacy.Replace(source.Body) ?? string.Empty;

            var result = await new Notification(log, dynamicEnvironment).TrySendAsync(source.NotificationTypeId,
                destination, subject, body, true, token).ConfigureAwait(false);

            await RecordAsync(source, target, sms ? CaseDispatchChannel.Sms : CaseDispatchChannel.Email,
                CaseDispatchMode.Payload, destination, subject, body,
                result.Ok ? CaseDispatchOutcome.Sent : CaseDispatchOutcome.Failed, result.Error, result.Status,
                result.CorrelationId, null, token).ConfigureAwait(false);

            return destination;
        }

        private async Task<string?> LegacyCallAsync(DispatchSource source, DispatchCase target, LegacyRender legacy,
            CancellationToken token)
        {
            var post = source.HttpEndpointTypeId == 1;
            var endpoint = legacy.Replace(source.HttpEndpoint) ?? string.Empty;
            var body = post ? legacy.PostBody() : null;

            var result = post
                ? await SendHttpEndpoint.PostAsync(endpoint, body!, log).ConfigureAwait(false)
                : await SendHttpEndpoint.GetAsync(endpoint, log).ConfigureAwait(false);

            await RecordAsync(source, target, post ? CaseDispatchChannel.HttpPost : CaseDispatchChannel.HttpGet,
                CaseDispatchMode.Payload, endpoint, null, body,
                result.Ok ? CaseDispatchOutcome.Sent : CaseDispatchOutcome.Failed, result.Error, result.Status,
                result.CorrelationId, null, token).ConfigureAwait(false);

            return endpoint;
        }

        private async Task RefuseAllAsync(DispatchSource source, DispatchCase target, CaseRows? rows, string detail,
            CancellationToken token)
        {
            if (source.EnableNotification)
            {
                await RecordAsync(source, target,
                    source.NotificationTypeId != 1 ? CaseDispatchChannel.Sms : CaseDispatchChannel.Email,
                    CaseDispatchMode.Sql, source.Destination, source.Subject, source.Body,
                    CaseDispatchOutcome.Refused, detail, null, Guid.Empty, rows, token).ConfigureAwait(false);
            }

            if (source.EnableHttp)
            {
                await RecordAsync(source, target,
                    source.HttpEndpointTypeId == 1 ? CaseDispatchChannel.HttpPost : CaseDispatchChannel.HttpGet,
                    CaseDispatchMode.Sql, source.HttpEndpoint, null, null, CaseDispatchOutcome.Refused, detail, null,
                    Guid.Empty, rows, token).ConfigureAwait(false);
            }
        }

        private async Task RecordAsync(DispatchSource source, DispatchCase target, CaseDispatchChannel channel,
            CaseDispatchMode mode, string? destination, string? subject, string? body, CaseDispatchOutcome outcome,
            string? detail, int? status, Guid correlationId, CaseRows? rows, CancellationToken token)
        {
            try
            {
                await repository.InsertAsync(new CaseWorkflowDispatch
                {
                    CaseId = target.Id,
                    CaseKey = target.Key,
                    CaseKeyValue = target.KeyValue,
                    SourceKindId = (byte)source.Kind,
                    SourceId = source.Id,
                    SourceName = source.Name,
                    ChannelId = (byte)channel,
                    ModeId = (byte)mode,
                    Destination = destination,
                    Subject = subject,
                    Body = body,
                    OutcomeId = (byte)outcome,
                    Detail = detail,
                    ResponseStatus = status,
                    CorrelationId = correlationId == Guid.Empty ? Guid.NewGuid() : correlationId,
                    Rows = rows is { Rows.Count: > 0 } ? CaseRowsContext.ToJson(rows) : null
                }, token).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.Error($"CaseDispatcher: the audit of a {channel} dispatch could not be written user={userName}", e);
            }
        }
    }
}
