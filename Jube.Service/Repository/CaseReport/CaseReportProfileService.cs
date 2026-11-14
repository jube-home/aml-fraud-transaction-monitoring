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

using System.Text.Json;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseReport;
using Jube.RegulatoryReport;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Service.Repository.CaseReport
{
    public sealed class CaseReportProfileService
    {
        public const int MaximumName = 100;
        public const int MaximumDescription = 500;
        public const int MaximumDocuments = 20;
        public const int MaximumDocumentCharacters = 2_000_000;
        private static readonly int[] listPermissions = [75, 76, 77];
        private static readonly int[] importPermissions = [76];

        private readonly DbContext dbContext;
        private readonly string userName;
        private readonly int tenantRegistryId;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly ILog auditLog;
        private readonly IServiceChangeBus serviceChangeBus;

        private CaseReportProfileService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
        }

        public static async Task<CaseReportProfileService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var tenant = await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, userName, token)
                .ConfigureAwait(false);

            if (tenant is null)
            {
                throw new CaseExceptions.NotAuthenticatedException("Not authenticated.");
            }

            var validation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new CaseReportProfileService(dbContext, userName, tenant.Value, validation, log,
                LogManager.GetLogger("Jube.Audit"), serviceChangeBus);
        }

        private void Ensure(int[] required)
        {
            if (!permissionValidation.Validate(required))
            {
                throw new CaseExceptions.ForbiddenException("Permission denied.", required);
            }
        }

        public async Task<List<CaseReportProfileSummaryDto>> ListAsync(CancellationToken token = default)
        {
            Ensure(listPermissions);

            return
            [
                .. (await new CaseReportProfileRepository(dbContext).ListCurrentAsync(tenantRegistryId, token)
                    .ConfigureAwait(false))
                .Select(p => new CaseReportProfileSummaryDto
                    { Name = p.Name, Version = p.Version, Description = p.Description })
            ];
        }

        public async Task<CaseReportProfileDto> GetAsync(string? name, CancellationToken token = default)
        {
            Ensure(importPermissions);

            var row = string.IsNullOrWhiteSpace(name)
                ? null
                : await new CaseReportProfileRepository(dbContext).CurrentAsync(tenantRegistryId, name.Trim(), token)
                    .ConfigureAwait(false);

            if (row is null)
            {
                throw new CaseExceptions.NotFoundException("The profile was not found.");
            }

            return new CaseReportProfileDto
            {
                Name = row.Name,
                Version = row.Version,
                Description = row.Description,
                TemplateXml = row.TemplateXml,
                SchemaDocuments = JsonSerializer.Deserialize<Dictionary<string, string>>(row.SchemaDocumentsJson) ?? [],
                CodeLists = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                    row.CodeListsJson) ?? [],
                CreatedUser = row.CreatedUser,
                CreatedDate = CaseReportMapper.Utc(row.CreatedDate)
            };
        }

        public async Task<CaseReportProfileResultDto> ImportAsync(CaseReportProfileDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("CaseReportProfile", "Import", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                Ensure(importPermissions);

                if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length > MaximumName
                    || string.IsNullOrWhiteSpace(model.TemplateXml)
                    || model.Description is { Length: > MaximumDescription }
                    || model.SchemaDocuments.Count is 0 or > MaximumDocuments
                    || model.SchemaDocuments.Values.Any(v => v.Length > MaximumDocumentCharacters)
                    || model.TemplateXml.Length > ReportProfile.MaximumTemplateCharacters)
                {
                    throw new ArgumentException("The profile needs a name, a template and one to 20 schema files, within the size limits.");
                }

                var profile = new ReportProfile(model.Name.Trim(), model.TemplateXml,
                    model.SchemaDocuments.ToDictionary(k => k.Key, v => v.Value),
                    model.CodeLists.ToDictionary(k => k.Key, v => (IReadOnlyDictionary<string, string>)v.Value),
                    model.Description);

                var issues = ReportBuilder.CheckProfile(profile);

                if (issues.Count > 0)
                {
                    return new CaseReportProfileResultDto { Issues = [.. issues.Select(CaseReportMapper.ToDto)] };
                }

                var repository = new CaseReportProfileRepository(dbContext);
                var current = await repository.CurrentAsync(tenantRegistryId, profile.Name, token).ConfigureAwait(false);

                var row = new CaseReportProfile
                {
                    TenantRegistryId = tenantRegistryId,
                    Name = profile.Name,
                    Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                    TemplateXml = model.TemplateXml,
                    SchemaDocumentsJson = JsonSerializer.Serialize(model.SchemaDocuments),
                    CodeListsJson = JsonSerializer.Serialize(model.CodeLists),
                    CreatedUser = userName
                };

                if (!await repository.AppendAsync(row, current?.Version ?? 0, token).ConfigureAwait(false))
                {
                    throw new CaseExceptions.ConflictException("The profile was changed by someone else; try again.");
                }

                op.Created();

                return new CaseReportProfileResultDto { Valid = true, Version = row.Version };
            }
            catch (Exception ex)
            {
                op.Error(ex);

                throw;
            }
        }
    }
}
