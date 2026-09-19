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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using Npgsql;
using Fixture = Jube.Test.Infrastructure.DatabaseFixture.DatabaseFixture;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public sealed class ModelScaffold : IAsyncDisposable
    {
        public const int ExampleModelId = 1;

        public static readonly Guid ExampleModelGuid = new("81abd51c-0013-41c1-a4c7-4a6270eb5aa4");

        public const string DefaultExcludedTablePattern =
            "^(Archive.*|MockArchive|EntityAnalysisModelInstance.*|EntityAnalysisModelSampleExecutionLog|" +
            "EntityAnalysisModelReprocessingRuleInstance|.*Version|.*CounterHistory|EntityAnalysisModelProcessingCounter|" +
            "EntityAnalysisModelAsynchronousQueueBalance|Case|CaseEvent|CaseFile|CaseNote|CaseWorkflowFormEntry|" +
            "CaseWorkflowFormEntryValue)$";

        internal const string Root = "EntityAnalysisModel";

        private readonly object gate = new();
        private readonly List<int> scheduleIds = [];
        private int disposed;

        private ModelScaffold(ModelScaffoldOptions options)
        {
            Options = options;
        }

        public static string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;" +
            "Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public ModelScaffoldOptions Options { get; }
        public int ModelId { get; private set; }
        public Guid ModelGuid { get; private set; }
        public string ModelName { get; private set; } = string.Empty;
        public int TenantRegistryId { get; private set; }
        public string TenantName { get; private set; } = string.Empty;

        public bool OwnsTenant { get; private set; }

        public IReadOnlyDictionary<string, IReadOnlyDictionary<long, long>> IdMap { get; private set; } =
            new Dictionary<string, IReadOnlyDictionary<long, long>>();

        public IReadOnlyDictionary<string, int> CopiedRows =>
            IdMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        public Guid RoleGuid { get; private set; }

        public int RoleRegistryId { get; private set; }
        public int OtherRoleRegistryId { get; private set; }
        public int UserRegistryId { get; private set; }

        public string UserName { get; private set; } = string.Empty;

        public string OtherUserName { get; private set; } = string.Empty;

        public IReadOnlyList<int> ScheduleIds
        {
            get
            {
                lock (gate)
                {
                    return [.. scheduleIds];
                }
            }
        }

        public static async Task<ModelScaffold> CreateAsync(ModelScaffoldOptions? options = null)
        {
            options ??= new ModelScaffoldOptions();
            if (!options.NamePrefix.StartsWith(Fixture.Prefix, StringComparison.Ordinal))
            {
                // ReSharper disable LocalizableElement
                throw new ArgumentException(
                    $"NamePrefix '{options.NamePrefix}' must start with '{Fixture.Prefix}' so that the database " +
                    "fixture's clean-up still finds what a crashed run leaves behind.", nameof(options));
                // ReSharper restore LocalizableElement
            }

            var scaffold = new ModelScaffold(options);
            try
            {
                await scaffold.BuildAsync().ConfigureAwait(false);
            }
            catch
            {
                await scaffold.DisposeAsync().ConfigureAwait(false);
                throw;
            }

            return scaffold;
        }

        private async Task BuildAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var prefix = Options.NamePrefix;
            ModelGuid = Options.ModelGuid ?? Guid.NewGuid();

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            if (Options.TenantRegistryId is { } existing)
            {
                var tenant = await dbContext.TenantRegistry.Where(w => w.Id == existing).FirstOrDefaultAsync()
                    .ConfigureAwait(false) ?? throw new InvalidOperationException(
                    $"TenantRegistryId {existing} does not exist, so the model cannot be scaffolded into it.");
                TenantRegistryId = tenant.Id;
                TenantName = tenant.Name;
            }
            else
            {
                TenantName = $"{prefix}Tenant{suffix}";
                TenantRegistryId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
                {
                    Name = TenantName,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Landlord = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Fixture.Prefix
                }).ConfigureAwait(false);
                OwnsTenant = true;
            }

            if (Options.CreateCallers)
            {
                RoleGuid = Guid.NewGuid();
                RoleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
                {
                    Guid = RoleGuid,
                    Name = $"{prefix}Role{suffix}",
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    TenantRegistryId = TenantRegistryId,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Fixture.Prefix
                }).ConfigureAwait(false);

                (UserName, UserRegistryId) =
                    await InsertUserWithApiKeyAsync(dbContext, $"{prefix}User{suffix}", RoleGuid)
                        .ConfigureAwait(false);

                var otherRoleGuid = Guid.NewGuid();
                OtherRoleRegistryId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
                {
                    Guid = otherRoleGuid,
                    Name = $"{prefix}OtherRole{suffix}",
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    TenantRegistryId = TenantRegistryId,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Fixture.Prefix
                }).ConfigureAwait(false);
                (OtherUserName, _) = await InsertUserWithApiKeyAsync(dbContext, $"{prefix}OtherUser{suffix}",
                    otherRoleGuid).ConfigureAwait(false);
            }

            ModelName = $"{prefix}Model{suffix}";
            await using (var connection = new NpgsqlConnection(ConnectionString))
            {
                await connection.OpenAsync().ConfigureAwait(false);
                await using var transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);
                (ModelId, IdMap) = await ModelGraphCopier.CopyAsync(connection, transaction, Options,
                    TenantRegistryId, ModelGuid, ModelName).ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            if (Options.CreateCallers)
            {
                await dbContext.InsertAsync(new EntityAnalysisModelRole
                {
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelGuid = ModelGuid,
                    RoleRegistryGuid = RoleGuid,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = Fixture.Prefix
                }).ConfigureAwait(false);
            }
        }

        private async Task<(string Name, int Id)> InsertUserWithApiKeyAsync(DbContext dbContext, string userName,
            Guid roleGuid)
        {
            var userId = await dbContext.InsertWithInt32IdentityAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-the-scaffold",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Fixture.Prefix
            }).ConfigureAwait(false);

            await dbContext.InsertAsync(new UserRegistryApiKey
            {
                Guid = Guid.NewGuid(),
                Name = "scaffold-key",
                UserRegistryId = userId,
                ApiKey = "not-a-real-key",
                ApiKeyDisplay = "not-a-real-key",
                Deleted = 0,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = Fixture.Prefix
            }).ConfigureAwait(false);

            await dbContext.InsertAsync(new UserInTenant
            {
                User = userName,
                TenantRegistryId = TenantRegistryId
            }).ConfigureAwait(false);

            return (userName, userId);
        }

        internal void TrackSchedule(int id)
        {
            lock (gate)
            {
                scheduleIds.Add(id);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 1)
            {
                return;
            }

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            foreach (var id in ScheduleIds)
            {
                await ModelGraphCopier.ExecuteAsync(connection,
                        "DELETE FROM \"EntityAnalysisModelSynchronisationSchedule\" WHERE \"Id\" = @id", ("id", id))
                    .ConfigureAwait(false);
            }

            if (ModelId > 0)
            {
                await ModelGraphCopier.ExecuteAsync(connection,
                    "DELETE FROM \"EntityAnalysisModelRole\" WHERE \"EntityAnalysisModelGuid\" = @guid",
                    ("guid", ModelGuid)).ConfigureAwait(false);

                foreach (var table in await ModelGraphCopier.GuidLinkedTablesAsync(connection).ConfigureAwait(false))
                {
                    await ModelGraphCopier.CascadeDeleteAsync(connection, table, "\"EntityAnalysisModelGuid\" = @guid",
                        ("guid", ModelGuid)).ConfigureAwait(false);
                }

                await ModelGraphCopier.CascadeDeleteAsync(connection, Root, "\"Id\" = @id", ("id", ModelId))
                    .ConfigureAwait(false);
            }

            if (TenantRegistryId > 0)
            {
                foreach (var name in new[] { UserName, OtherUserName }.Where(n => n.Length > 0))
                {
                    await ModelGraphCopier.ExecuteAsync(connection,
                        "DELETE FROM \"UserInTenant\" WHERE \"User\" = @name",
                        ("name", name)).ConfigureAwait(false);
                    await ModelGraphCopier.CascadeDeleteAsync(connection, "UserRegistry", "\"Name\" = @name",
                        ("name", name)).ConfigureAwait(false);
                }

                foreach (var roleId in new[] { RoleRegistryId, OtherRoleRegistryId }.Where(r => r > 0))
                {
                    await ModelGraphCopier.CascadeDeleteAsync(connection, "RoleRegistry", "\"Id\" = @id",
                        ("id", roleId)).ConfigureAwait(false);
                }

                if (OwnsTenant)
                {
                    await ModelGraphCopier.CascadeDeleteAsync(connection, "TenantRegistry", "\"Id\" = @tenant",
                        ("tenant", TenantRegistryId)).ConfigureAwait(false);
                }
            }
        }
    }
}