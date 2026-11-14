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

namespace Jube.Engine.CaseDigest
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto.Options;
    using CaseServiceLevel;

    public sealed record CaseDigestResult(int Notified, int Emailed);

    public static class CaseDigest
    {
        public static bool IsDue(DateTime now, int hourUtc, string days, DateTime? lastRunDate)
        {
            if (now.Hour != hourUtc)
            {
                return false;
            }

            if (lastRunDate.HasValue && lastRunDate.Value.Date == now.Date)
            {
                return false;
            }

            return days.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(d => int.TryParse(d, out var parsed) ? parsed : -1)
                .Contains((int)now.DayOfWeek);
        }

        public static async Task<CaseDigestResult> RunAsync(DbContext dbContext, DateTime now, bool email,
            Func<string, string, string, bool> send, CancellationToken token = default)
        {
            var digest = new CaseDigestRepository(dbContext);
            var breaches = new CaseServiceLevelBreachRepository(dbContext);
            var notifications = new UserNotificationRepository(dbContext);
            var notified = 0;
            var emailed = 0;

            foreach (var figures in await digest.ListWorkflowFiguresAsync(now.AddDays(-1), token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                var recipients = (await breaches.ListSupervisorsAsync(figures.TenantRegistryId, figures.WorkflowGuid,
                    CaseServiceLevelSweep.SupervisionPermission, token).ConfigureAwait(false)).Distinct().ToList();

                if (recipients.Count == 0)
                {
                    continue;
                }

                var emails = email
                    ? await digest.ListEmailsAsync(recipients, token).ConfigureAwait(false)
                    : [];

                var arguments = System.Text.Json.JsonSerializer.Serialize(new
                {
                    workflow = figures.WorkflowName, open = figures.Open, unassigned = figures.Unassigned,
                    overdue = figures.Overdue, closed = figures.Closed
                });

                foreach (var recipient in recipients)
                {
                    var inserted = await notifications.InsertIfNewAsync(new UserNotification
                    {
                        Guid = Guid.NewGuid(),
                        TenantRegistryId = figures.TenantRegistryId,
                        RecipientUser = recipient,
                        KindId = (short)NotificationKind.CaseDigest,
                        PriorityId = (short)(figures.Overdue > 0 ? NotificationPriority.High : NotificationPriority.Low),
                        ArgumentsJson = arguments,
                        DedupeKey = $"case-digest:{figures.WorkflowGuid:N}:{now:yyyyMMdd}",
                        CreatedDate = now
                    }, token).ConfigureAwait(false);

                    if (!inserted)
                    {
                        continue;
                    }

                    notified++;

                    if (emails.TryGetValue(recipient, out var address) && send(address,
                            $"Jube case digest: {figures.WorkflowName}", Body(figures)))
                    {
                        emailed++;
                    }
                }
            }

            return new CaseDigestResult(notified, emailed);
        }

        private static string Body(CaseDigestFigures figures)
        {
            return $"<p><strong>{WebUtility.HtmlEncode(figures.WorkflowName)}</strong></p>" +
                   $"<ul><li>Open cases: {figures.Open}</li>" +
                   $"<li>Open and nobody holds them: {figures.Unassigned}</li>" +
                   $"<li>Open and past a service level: {figures.Overdue}</li>" +
                   $"<li>Closed in the last day: {figures.Closed}</li></ul>" +
                   "<p>Counts only. Sign in to see the cases.</p>";
        }
    }
}
