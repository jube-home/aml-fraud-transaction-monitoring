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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using LinqToDB.Data;
using Xunit;
using Jube.Test.Infrastructure.DatabaseFixture.Models;

namespace Jube.Test.Infrastructure.DatabaseFixture
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        public const string Prefix = "ZzTest";

        private string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public SeedData Seed { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await using (var probe = GetDbContext())
            {
                try
                {
                    _ = await probe.TenantRegistry.Take(1).ToListAsync().ConfigureAwait(false);
                    _ = await probe.EntityAnalysisModel.Take(1).ToListAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "DatabaseFixture: could not read from a running, migrated Jube schema at the configured " +
                        "connection string. Point JubeTestConnectionString at a running, migrated Jube instance " +
                        "-- the service-layer suite does not provision one.", ex);
                }
            }

            await using var dbContext = GetDbContext();
            Seed = await SeedAsync(dbContext).ConfigureAwait(false);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = GetDbContext();

            var prefixedModelIds = dbContext.EntityAnalysisModel
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .Select(w => (int?)w.Id);

            var prefixedCaseWorkflowGuids = dbContext.CaseWorkflow
                .Where(w => (w.Name != null && w.Name.StartsWith(Prefix))
                            || prefixedModelIds.Contains(w.EntityAnalysisModelId))
                .Select(w => w.Guid);

            var prefixedCaseWorkflowIds = dbContext.CaseWorkflow
                .Where(c => (c.Name != null && c.Name.StartsWith(Prefix))
                            || prefixedModelIds.Contains(c.EntityAnalysisModelId))
                .Select(c => (int?)c.Id);

            await dbContext.GetTable<CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && prefixedCaseWorkflowIds.Contains(w.CaseWorkflowId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<CaseWorkflowRole>()
                .Where(w => prefixedCaseWorkflowGuids.Contains(w.CaseWorkflowGuid))
                .DeleteAsync().ConfigureAwait(false);

            await CascadeDeleteChildrenAsync(dbContext, "CaseWorkflow",
                "\"Name\" LIKE @prefix OR \"EntityAnalysisModelId\" IN " +
                "(SELECT \"Id\" FROM \"EntityAnalysisModel\" WHERE \"Name\" LIKE @prefix)").ConfigureAwait(false);

            await dbContext.CaseWorkflow
                .Where(w => (w.Name != null && w.Name.StartsWith(Prefix))
                            || prefixedModelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<EntityAnalysisModelRequestXpath>()
                .Where(w => prefixedModelIds.Contains(w.EntityAnalysisModelId))
                .DeleteAsync().ConfigureAwait(false);

            await CascadeDeleteChildrenAsync(dbContext, "EntityAnalysisModel", "\"Name\" LIKE @prefix")
                .ConfigureAwait(false);

            await dbContext.EntityAnalysisModel
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            var prefixedRoleRegistryIds = dbContext.RoleRegistry
                .Where(r => r.Name != null && r.Name.StartsWith(Prefix))
                .Select(r => (int?)r.Id);

            await dbContext.GetTable<RoleRegistryPermissionVersion>()
                .Where(w => prefixedRoleRegistryIds.Contains(w.RoleRegistryId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.RoleRegistryPermission
                .Where(w => prefixedRoleRegistryIds.Contains(w.RoleRegistryId))
                .DeleteAsync().ConfigureAwait(false);

            await CascadeDeleteChildrenAsync(dbContext, "UserRegistry", "\"Name\" LIKE @prefix")
                .ConfigureAwait(false);
            await CascadeDeleteChildrenAsync(dbContext, "RoleRegistry", "\"Name\" LIKE @prefix")
                .ConfigureAwait(false);

            await dbContext.RoleRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.UserInTenant
                .Where(w => w.User != null && w.User.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.UserRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            var prefixedVisualisationRegistryIds = dbContext.GetTable<VisualisationRegistry>()
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .Select(w => (int?)w.Id);
            var prefixedVisualisationRegistryGuids = dbContext.GetTable<VisualisationRegistry>()
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .Select(w => w.Guid);

            var prefixedDatasourceIds = dbContext.GetTable<VisualisationRegistryDatasource>()
                .Where(w => prefixedVisualisationRegistryIds.Contains(w.VisualisationRegistryId))
                .Select(w => (int?)w.Id);
            var prefixedDatasourceGuids = dbContext.GetTable<VisualisationRegistryDatasource>()
                .Where(w => prefixedVisualisationRegistryIds.Contains(w.VisualisationRegistryId))
                .Select(w => w.Guid);

            var prefixedParameterIds = dbContext.GetTable<VisualisationRegistryParameter>()
                .Where(w => prefixedVisualisationRegistryIds.Contains(w.VisualisationRegistryId))
                .Select(w => (int?)w.Id);
            var prefixedParameterGuids = dbContext.GetTable<VisualisationRegistryParameter>()
                .Where(w => prefixedVisualisationRegistryIds.Contains(w.VisualisationRegistryId))
                .Select(w => w.Guid);

            await dbContext.GetTable<VisualisationRegistryDatasourceSeries>()
                .Where(w => prefixedDatasourceIds.Contains(w.VisualisationRegistryDatasourceId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryDatasourceRole>()
                .Where(w => prefixedDatasourceGuids.Contains(w.VisualisationRegistryDatasourceGuid))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryParameterRole>()
                .Where(w => prefixedParameterGuids.Contains(w.VisualisationRegistryParameterGuid))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryDatasourceVersion>()
                .Where(w => prefixedDatasourceIds.Contains(w.VisualisationRegistryDatasourceId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryParameterVersion>()
                .Where(w => prefixedParameterIds.Contains(w.VisualisationRegistryParameterId))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryDatasource>()
                .Where(w => prefixedDatasourceIds.Contains(w.Id))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryParameter>()
                .Where(w => prefixedParameterIds.Contains(w.Id))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryRole>()
                .Where(w => prefixedVisualisationRegistryGuids.Contains(w.VisualisationRegistryGuid))
                .DeleteAsync().ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistryVersion>()
                .Where(w => prefixedVisualisationRegistryIds.Contains(w.VisualisationRegistryId))
                .DeleteAsync().ConfigureAwait(false);

            await CascadeDeleteChildrenAsync(dbContext, "VisualisationRegistry", "\"Name\" LIKE @prefix")
                .ConfigureAwait(false);

            await dbContext.GetTable<VisualisationRegistry>()
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);

            await CascadeDeleteChildrenAsync(dbContext, "TenantRegistry", "\"Name\" LIKE @prefix")
                .ConfigureAwait(false);

            await dbContext.TenantRegistry
                .Where(w => w.Name != null && w.Name.StartsWith(Prefix))
                .DeleteAsync().ConfigureAwait(false);
        }

        private static readonly HashSet<string> cascadeRootTables = new(StringComparer.Ordinal)
        {
            "CaseWorkflow", "EntityAnalysisModel", "UserRegistry", "RoleRegistry", "VisualisationRegistry",
            "TenantRegistry"
        };

        private const int CascadeMaxRows = 1_000_000;
        private const int CascadeMaxDepth = 8;

        private static string QuoteIdentifier(string identifier)
        {
            return "\"" + identifier.Replace("\"", "\"\"") + "\"";
        }

        private static async Task CascadeDeleteChildrenAsync(DbContext dbContext, string table, string whereSql)
        {
            if (!cascadeRootTables.Contains(table))
            {
                throw new ArgumentException(
                    // ReSharper disable once LocalizableElement
                    $"'{table}' is not an allowed cascade root ({string.Join(", ", cascadeRootTables)}).",
                    nameof(table));
            }

            if (!whereSql.Contains("@prefix", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    // ReSharper disable once LocalizableElement
                    "The root filter of a cascade must be restricted by the test prefix (@prefix).", nameof(whereSql));
            }

            var deleted = new Dictionary<string, long>(StringComparer.Ordinal);
            await dbContext.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                await CascadeAsync(dbContext, table, whereSql, 0, deleted).ConfigureAwait(false);

                var total = deleted.Values.Sum();
                if (total > CascadeMaxRows)
                {
                    throw new InvalidOperationException(
                        $"The fixture cascade from '{table}' would delete {total} rows (limit {CascadeMaxRows}); " +
                        $"rolled back. Per table: {string.Join(", ", deleted.Select(d => $"{d.Key}={d.Value}"))}");
                }

                await dbContext.CommitTransactionAsync().ConfigureAwait(false);

                if (total > 0)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"DatabaseFixture cascade from {table}: deleted {total} row(s): " +
                        string.Join(", ", deleted.Where(d => d.Value > 0).Select(d => $"{d.Key}={d.Value}")));
                }
            }
            catch
            {
                await dbContext.RollbackTransactionAsync().ConfigureAwait(false);
                throw;
            }
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static async Task CascadeAsync(DbContext dbContext, string table, string whereSql, int depth,
            Dictionary<string, long> deleted)
        {
            if (depth > CascadeMaxDepth)
            {
                throw new InvalidOperationException(
                    $"The fixture cascade from '{table}' is deeper than {CascadeMaxDepth} levels: a foreign key cycle?");
            }

            var children = await dbContext.QueryToListAsync<ForeignKeyRow>(
                "SELECT cl.relname AS \"Child\", " +
                "string_agg(quote_ident(a.attname), ', ' ORDER BY k.ord) AS \"ChildColumns\", " +
                "string_agg(quote_ident(af.attname), ', ' ORDER BY k.ord) AS \"ParentColumns\" " +
                "FROM pg_constraint c " +
                "JOIN pg_class cl ON cl.oid = c.conrelid " +
                "JOIN pg_namespace n ON n.oid = cl.relnamespace AND n.nspname = 'public' " +
                "JOIN unnest(c.conkey, c.confkey) WITH ORDINALITY AS k(attnum, fattnum, ord) ON true " +
                "JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum " +
                "JOIN pg_attribute af ON af.attrelid = c.confrelid AND af.attnum = k.fattnum " +
                "WHERE c.contype = 'f' AND c.conrelid <> c.confrelid " +
                "AND c.confrelid = (SELECT oid FROM pg_class WHERE relname = @parent " +
                "AND relnamespace = 'public'::regnamespace) " +
                "GROUP BY c.oid, cl.relname",
                new DataParameter("parent", table)).ConfigureAwait(false);

            foreach (var child in children)
            {
                var childWhere = $"({child.ChildColumns}) IN (SELECT {child.ParentColumns} " +
                                 $"FROM {QuoteIdentifier(table)} WHERE {whereSql})";

                await CascadeAsync(dbContext, child.Child, childWhere, depth + 1, deleted).ConfigureAwait(false);

                var count = await dbContext.ExecuteAsync(
                    $"DELETE FROM {QuoteIdentifier(child.Child)} WHERE {childWhere}",
                    new DataParameter("prefix", Prefix + "%")).ConfigureAwait(false);

                deleted[child.Child] = deleted.GetValueOrDefault(child.Child) + count;
            }
        }

        public DbContext GetDbContext()
        {
            return DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);
        }

        private static async Task<SeedData> SeedAsync(DbContext dbContext)
        {
            int[] readWriteSpecs =
            [
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 28,
                29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43
            ];
            var readWriteSpecsNoApproveByReview = readWriteSpecs.Where(s => s != 41).ToArray();

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var tenantAId = await InsertTenantAsync(dbContext, $"{Prefix}TenantA{suffix}", false)
                .ConfigureAwait(false);
            var tenantBId = await InsertTenantAsync(dbContext, $"{Prefix}TenantB{suffix}", false)
                .ConfigureAwait(false);
            var landlordTenantId = await InsertTenantAsync(dbContext, $"{Prefix}TenantL{suffix}", true)
                .ConfigureAwait(false);
            var roleWithPermissionAId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithPermissionA{suffix}", tenantAId,
                    readWriteSpecs).ConfigureAwait(false);
            var roleWithoutPermissionAId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithoutPermissionA{suffix}", tenantAId, [])
                    .ConfigureAwait(false);
            var roleWithPermissionNoApproveByReviewAId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithPermissionNoApproveByReviewA{suffix}", tenantAId,
                    readWriteSpecsNoApproveByReview).ConfigureAwait(false);
            var roleWithPermissionBId =
                await InsertRoleAsync(dbContext, $"{Prefix}RoleWithPermissionB{suffix}", tenantBId,
                    readWriteSpecs).ConfigureAwait(false);
            var landlordRoleId = await InsertRoleAsync(dbContext, $"{Prefix}RoleLandlord{suffix}", landlordTenantId, [])
                .ConfigureAwait(false);
            var userWithPermission = $"{Prefix}UserWithPermission{suffix}";
            var userWithoutPermission = $"{Prefix}UserWithoutPermission{suffix}";
            var userWithPermissionNoApproveByReview = $"{Prefix}UserWithPermissionNoApproveByReview{suffix}";
            var userTenantB = $"{Prefix}UserTenantB{suffix}";
            var landlordUser = $"{Prefix}UserLandlord{suffix}";
            var userNoTenant = $"{Prefix}UserNoTenant{suffix}";
            var userBothTenants = $"{Prefix}UserBothTenants{suffix}";
            var roleWithPermissionAGuid = await InsertUserAsync(dbContext, userWithPermission, roleWithPermissionAId)
                .ConfigureAwait(false);
            var roleWithoutPermissionAGuid =
                await InsertUserAsync(dbContext, userWithoutPermission, roleWithoutPermissionAId).ConfigureAwait(false);
            var roleWithPermissionNoApproveByReviewAGuid =
                await InsertUserAsync(dbContext, userWithPermissionNoApproveByReview,
                    roleWithPermissionNoApproveByReviewAId).ConfigureAwait(false);
            var roleWithPermissionBGuid =
                await InsertUserAsync(dbContext, userTenantB, roleWithPermissionBId).ConfigureAwait(false);
            var landlordRoleGuid = await InsertUserAsync(dbContext, landlordUser, landlordRoleId).ConfigureAwait(false);
            var noTenantRoleGuid =
                await InsertUserAsync(dbContext, userNoTenant, roleWithPermissionAId).ConfigureAwait(false);
            var bothTenantsRoleGuid = await InsertUserAsync(dbContext, userBothTenants, roleWithPermissionAId)
                .ConfigureAwait(false);

            await dbContext.InsertAsync(new UserInTenant { User = userWithPermission, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userWithoutPermission, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant
                { User = userWithPermissionNoApproveByReview, TenantRegistryId = tenantAId }).ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userTenantB, TenantRegistryId = tenantBId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = landlordUser, TenantRegistryId = landlordTenantId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userBothTenants, TenantRegistryId = tenantAId })
                .ConfigureAwait(false);
            await dbContext.InsertAsync(new UserInTenant { User = userBothTenants, TenantRegistryId = tenantBId })
                .ConfigureAwait(false);

            _ = roleWithPermissionAGuid;
            _ = roleWithoutPermissionAGuid;
            _ = roleWithPermissionNoApproveByReviewAGuid;
            _ = roleWithPermissionBGuid;
            _ = landlordRoleGuid;
            _ = noTenantRoleGuid;
            _ = bothTenantsRoleGuid;

            return new SeedData(
                userWithPermission, userWithoutPermission, userWithPermissionNoApproveByReview, userTenantB,
                landlordUser, userNoTenant, userBothTenants, $"{Prefix}UnknownUser{suffix}");
        }

        private static Task<int> InsertTenantAsync(DbContext dbContext, string name, bool landlord)
        {
            return dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = name,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = (byte)(landlord ? 1 : 0),
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            });
        }

        private static async Task<int> InsertRoleAsync(DbContext dbContext, string name, int tenantRegistryId,
            int[] specs)
        {
            var guid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = guid,
                Name = name,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            }).ConfigureAwait(false);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Prefix
                }).ConfigureAwait(false);
            }

            return roleId;
        }

        private static async Task<Guid> InsertUserAsync(DbContext dbContext, string userName, int roleRegistryId)
        {
            var role = await dbContext.RoleRegistry.FirstAsync(f => f.Id == roleRegistryId).ConfigureAwait(false);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = role.Guid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Prefix
            }).ConfigureAwait(false);

            return role.Guid;
        }
    }
}