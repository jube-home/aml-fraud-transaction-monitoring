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

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Reporting;
using Jube.Dto.Query.EntityAnalysisModelSample;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.EntityAnalysisModelSample;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.Query.EntityAnalysisModelSample;
using log4net;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;

namespace Jube.Service.Query.EntityAnalysisModelSample
{
    using FieldQuery = global::Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;

    public sealed class EntityAnalysisModelSampleService
    {
        private static readonly int[] permissions = [40];

        private static readonly string[] staticColumns =
        [
            "EntityAnalysisModelInstanceEntryGuid", "ResponseElevation",
            "PrevailingEntityAnalysisModelActivationRuleId", "EntityAnalysisModelGuid",
            "EntityAnalysisModelActivationRuleCount", "CreatedDate", "ReferenceDate"
        ];

        private readonly ILog auditLog;
        private readonly DynamicEnvironment.DynamicEnvironment dynamicEnvironment;
        private readonly EntityAnalysisModelRepository entityAnalysisModelRepository;
        private readonly EntityAnalysisModelSampleExecutionLogRepository executionLogRepository;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly FieldQuery query;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelSampleOptionsDtoValidator validator;

        private EntityAnalysisModelSampleService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings, DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.dynamicEnvironment = dynamicEnvironment;
            validator = new EntityAnalysisModelSampleOptionsDtoValidator(strings);
            entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, userName);
            executionLogRepository = new EntityAnalysisModelSampleExecutionLogRepository(dbContext);
            query = new FieldQuery(dbContext, userName);
        }

        public static Task<EntityAnalysisModelSampleService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                dynamicEnvironment, LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelSampleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelSampleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("EntityAnalysisModelSample.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSampleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"EntityAnalysisModelSample.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityAnalysisModelSampleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelSampleService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings, dynamicEnvironment);
        }

        [Description("Samples archived transactions of an Entity Analysis Model over a ReferenceDate window, " +
                     "each archived row included with probability 'Sample', and returns them as a UTF-8 CSV file " +
                     "(static columns followed by one column per payload field of the model). Every execution, " +
                     "successful or failed, is recorded in the Entity Analysis Model Sample Execution Log. Reads " +
                     "the reporting database when one is configured.")]
        [ServiceOperation("EntityAnalysisModelSampleExecute", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelSampleFileDto> ExecuteAsync(
            [Description("Model guid, date window and sampling rate.")]
            EntityAnalysisModelSampleOptionsDto? model, CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelSample", "Execute", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
            {
                log.Debug($"EntityAnalysisModelSample.Execute: entry user={userName}");
            }

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelSample.Execute");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    throw new DtoValidationException(results);
                }

                var entityAnalysisModel = await entityAnalysisModelRepository
                    .GetByGuidAsync(model.EntityAnalysisModelGuid, token).ConfigureAwait(false);

                if (entityAnalysisModel == null)
                {
                    throw new NotFoundException(strings[EntityAnalysisModelSampleResources.ModelNotFound]);
                }

                var executionLog = new EntityAnalysisModelSampleExecutionLog
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = userName,
                    Sample = model.Sample,
                    DateFrom = model.DateFrom?.UtcDateTime,
                    DateTo = model.DateTo?.UtcDateTime,
                    EntityAnalysisModelId = entityAnalysisModel.Id
                };

                var sw = Stopwatch.StartNew();
                try
                {
                    var completions = (await query.ExecuteAsync(entityAnalysisModel.Id, 5, true, token)
                        .ConfigureAwait(false)).ToArray();

                    var connectionString = dynamicEnvironment.AppSettings("ReportConnectionString")
                                           ?? dynamicEnvironment.AppSettings("ConnectionString");

                    List<JObject> jsonObjects;
                    using (var postgres = new Postgres(connectionString, log,
                               dynamicEnvironment.ParserAssertSelectOnly()))
                    {
                        jsonObjects = await postgres.ExecuteReturnOnlyJsonFromArchiveSampleAsync(
                                entityAnalysisModel.Id, model.Sample, model.DateFrom?.UtcDateTime,
                                model.DateTo?.UtcDateTime, token)
                            .ConfigureAwait(false);
                    }

                    var csv = BuildCsv(completions, jsonObjects);
                    sw.Stop();

                    executionLog.RowCount = jsonObjects.Count;
                    executionLog.ResponseTime = sw.ElapsedMilliseconds;
                    executionLog.InError = 0;

                    await executionLogRepository.InsertAsync(executionLog, token).ConfigureAwait(false);

                    op.Rows(jsonObjects.Count);
                    return new EntityAnalysisModelSampleFileDto(
                        Encoding.UTF8.GetBytes(csv),
                        $"sample_{entityAnalysisModel.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
                        "text/csv");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    executionLog.InError = 1;
                    executionLog.ErrorStack = ex.ToString();

                    await executionLogRepository.InsertAsync(executionLog, token).ConfigureAwait(false);
                    throw;
                }
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("not_found");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelSample.Execute: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private string BuildCsv(FieldQuery.Dto[] completions, IEnumerable<JObject> jsonObjects)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Join(",",
                staticColumns.Select(QuoteCsvField)
                    .Concat(completions.Select(c => QuoteCsvField(c.Name)))));

            foreach (var jObject in jsonObjects)
            {
                var staticFields = new[]
                {
                    QuoteCsvField(jObject["entityAnalysisModelInstanceEntryGuid"]?.Value<string>() ?? string.Empty),
                    jObject["responseElevation"]?["value"]?.Value<double>()
                        .ToString("G", CultureInfo.InvariantCulture) ?? "0",
                    jObject["prevailingEntityAnalysisModelActivationRuleId"]?.Value<int>().ToString() ?? "0",
                    QuoteCsvField(jObject["entityAnalysisModelGuid"]?.Value<string>() ?? string.Empty),
                    jObject["entityAnalysisModelActivationRuleCount"]?.Value<int>().ToString() ?? "0",
                    jObject["createdDate"]?.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture) ??
                    string.Empty,
                    jObject["referenceDate"]?.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture) ??
                    string.Empty
                };

                var payloadFields = completions.Select(completion =>
                    ExtractValue(completion, jObject.SelectToken(completion.ValueJsonPath)));

                sb.AppendLine(string.Join(",", staticFields.Concat(payloadFields)));
            }

            return sb.ToString();
        }

        private string ExtractValue(FieldQuery.Dto completion, JToken? jToken)
        {
            if (jToken == null)
            {
                return completion.DataTypeId switch
                {
                    1 => QuoteCsvField(string.Empty),
                    2 or 3 or 6 or 7 => "0",
                    4 => string.Empty,
                    5 => "0",
                    _ => string.Empty
                };
            }

            try
            {
                if (!completion.Group.Equals("payload", StringComparison.OrdinalIgnoreCase))
                {
                    return jToken.Value<double>().ToString("G", CultureInfo.InvariantCulture);
                }

                return completion.DataTypeId switch
                {
                    1 => QuoteCsvField(jToken.Value<string>() ?? string.Empty),
                    2 => jToken.Value<int>().ToString(),
                    3 or 6 or 7 => jToken.Value<double>().ToString("G", CultureInfo.InvariantCulture),
                    4 => jToken.Value<DateTime>().ToString("O", CultureInfo.InvariantCulture),
                    5 => "1",
                    _ => QuoteCsvField(jToken.Value<string>() ?? string.Empty)
                };
            }
            catch (Exception e)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info($"Type coercion failed for field '{completion.Name}' " +
                             $"(DataTypeId={completion.DataTypeId}, token='{jToken}', " +
                             $"tokenType={jToken.Type}): {e.Message}");
                }

                return completion.DataTypeId == 1 ? QuoteCsvField(string.Empty) : "0";
            }
        }

        private static string QuoteCsvField(string value)
        {
            if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r' &&
                !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                value = "'" + value;
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityAnalysisModelSampleResources.PermissionDenied],
                permissions);
        }
    }
}