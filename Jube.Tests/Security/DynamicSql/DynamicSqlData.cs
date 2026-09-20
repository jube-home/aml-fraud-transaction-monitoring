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
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;
using LinqToDB.Data;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Test.Security.DynamicSql;

internal sealed class DynamicSqlData
{
    public const string SafeCommand = "SELECT 1::integer AS \"Value\"";
    public const string MarkerA = "ZzW12TenantAMarker";
    public const string MarkerB = "ZzW12TenantBMarker";

    private readonly List<int> datasourceIds = [];
    private readonly List<int> registryIds = [];
    private readonly List<int> parameterIds = [];
    private readonly DatabaseFixture fx;

    private DynamicSqlData(DatabaseFixture fx)
    {
        this.fx = fx;
    }

    public string UserA { get; private set; } = string.Empty;
    public string UserB { get; private set; } = string.Empty;
    public int TenantA { get; private set; }
    public int TenantB { get; private set; }
    public Guid RoleA { get; private set; }
    public Guid RoleB { get; private set; }
    public int RegistryA { get; private set; }
    public int RegistryB { get; private set; }
    public Guid RegistryGuidA { get; private set; }
    public Guid RegistryGuidB { get; private set; }
    public int DatasourceA { get; private set; }
    public int DatasourceB { get; private set; }
    public int ParameterA { get; private set; }

    public static async Task<DynamicSqlData> CreateAsync(DatabaseFixture fx, PenTestHost host)
    {
        var data = new DynamicSqlData(fx)
        {
            UserA = host.UserName(PenTestUser.WithPermission),
            UserB = host.UserName(PenTestUser.TenantB)
        };

        await using var db = fx.GetDbContext();
        data.TenantA = (await db.UserInTenant.FirstAsync(w => w.User == data.UserA)).TenantRegistryId;
        data.TenantB = (await db.UserInTenant.FirstAsync(w => w.User == data.UserB)).TenantRegistryId;
        data.RoleA = (await db.UserRegistry.FirstAsync(w => w.Name == data.UserA)).RoleRegistryGuid;
        data.RoleB = (await db.UserRegistry.FirstAsync(w => w.Name == data.UserB)).RoleRegistryGuid;

        (data.RegistryA, data.RegistryGuidA) =
            await data.AddRegistryAsync(db, data.TenantA, data.RoleA, data.UserA, "A");
        (data.RegistryB, data.RegistryGuidB) =
            await data.AddRegistryAsync(db, data.TenantB, data.RoleB, data.UserB, "B");
        data.DatasourceA = await data.AddDatasourceAsync(db, data.RegistryA, data.RoleA, data.UserA,
            $"SELECT '{MarkerA}' AS \"Marker\"");
        data.DatasourceB = await data.AddDatasourceAsync(db, data.RegistryB, data.RoleB, data.UserB,
            $"SELECT '{MarkerB}' AS \"Marker\"");
        data.ParameterA = await data.AddParameterAsync(db, data.RegistryA, data.UserA, "Term", 1, "default");
        return data;
    }

    private async Task<(int Id, Guid Guid)> AddRegistryAsync(DbContext db, int tenantId, Guid role, string user,
        string label)
    {
        var guid = Guid.NewGuid();
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistry
        {
            Name = $"{DatabaseFixture.Prefix}W12Reg{label}{Guid.NewGuid():N}"[..40],
            Guid = guid,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            ShowInDirectory = 1,
            TenantRegistryId = tenantId,
            Version = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        registryIds.Add(id);
        await db.InsertAsync(new VisualisationRegistryRole
        {
            VisualisationRegistryGuid = guid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = role,
            CreatedDate = DateTime.UtcNow,
            CreatedUser = user,
            Deleted = 0,
            Version = 1
        });
        return (id, guid);
    }

    public async Task<int> AddDatasourceAsync(DbContext db, int registryId, Guid role, string user, string command,
        byte active = 1)
    {
        var guid = Guid.NewGuid();
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasource
        {
            VisualisationRegistryId = registryId,
            Name = $"{DatabaseFixture.Prefix}W12Ds{Guid.NewGuid():N}"[..40],
            Guid = guid,
            Active = active,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            VisualisationTypeId = 1,
            Command = command,
            Priority = 1,
            IncludeGrid = 1,
            IncludeDisplay = 0,
            ColumnSpan = 1,
            RowSpan = 1,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        // ReSharper disable once InconsistentlySynchronizedField
        datasourceIds.Add(id);
        await db.InsertAsync(new VisualisationRegistryDatasourceRole
        {
            VisualisationRegistryDatasourceGuid = guid,
            Guid = Guid.NewGuid(),
            RoleRegistryGuid = role,
            CreatedDate = DateTime.UtcNow,
            CreatedUser = user,
            Deleted = 0,
            Version = 1
        });
        return id;
    }

    public async Task<int> AddParameterAsync(DbContext db, int registryId, string user, string name, int dataTypeId,
        string defaultValue)
    {
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistryParameter
        {
            VisualisationRegistryId = registryId,
            Name = name,
            Guid = Guid.NewGuid(),
            Active = 1,
            Locked = 0,
            Deleted = 0,
            Version = 1,
            DataTypeId = dataTypeId,
            DefaultValue = defaultValue,
            Required = 0,
            CreatedUser = user,
            CreatedDate = DateTime.UtcNow
        });
        parameterIds.Add(id);
        return id;
    }

    public async Task<string> SnapshotAsync(bool includeDatasources = true)
    {
        await using var db = fx.GetDbContext();
        var registries = await db.GetTable<VisualisationRegistry>()
            .Where(w => registryIds.Contains(w.Id) ||
                        (w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix + "W12")))
            .OrderBy(w => w.Id).Select(w => w.Id + ":" + w.Name + ":" + w.Version + ":" + w.Deleted).ToListAsync();
        var datasources = await db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => includeDatasources && registryIds.Contains(w.VisualisationRegistryId ?? 0))
            .OrderBy(w => w.Id).Select(w =>
                w.Id + ":" + w.Name + ":" + w.Command + ":" + w.Version + ":" + w.Deleted + ":" + w.Active)
            .ToListAsync();
        var users = await db.GetTable<UserRegistry>().Where(w => w.Name == UserA || w.Name == UserB)
            .OrderBy(w => w.Name).Select(w => w.Name + ":" + w.Password + ":" + w.Email + ":" + w.Active).ToListAsync();
        var tenants = await db.GetTable<TenantRegistry>().Where(w => w.Id == TenantA || w.Id == TenantB)
            .OrderBy(w => w.Id).Select(w => w.Id + ":" + w.Name).ToListAsync();
        var sessionSearches = await db.GetTable<SessionCaseSearchCompiledSql>()
            .Where(w => w.CreatedUser == UserA || w.CreatedUser == UserB).CountAsync();
        var stray = await db.QueryToListAsync<string>(
            "select table_name::text from information_schema.tables where table_name ilike 'zzw12%' " +
            "union all select rolname::text from pg_roles where rolname ilike 'zzw12%'");
        return string.Join("|", registries) + "#" + string.Join("|", datasources) + "#" + string.Join("|", users) +
               "#" + string.Join("|", tenants) + "#" + sessionSearches + "#" + string.Join(",", stray);
    }

    public async Task DisposeAsync()
    {
        await RegisterAllDatasourcesOfRegistryAsync(0);
        await using var db = fx.GetDbContext();
        var datasourceGuids = await db.GetTable<VisualisationRegistryDatasource>()
            // ReSharper disable once InconsistentlySynchronizedField
            .Where(w => datasourceIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();
        var logIds = db.GetTable<VisualisationRegistryDatasourceExecutionLog>()
            // ReSharper disable once InconsistentlySynchronizedField
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).Select(w => w.Id);
        await db.GetTable<VisualisationRegistryDatasourceExecutionLogParameter>()
            .Where(w => logIds.Contains(w.VisualisationRegistryDatasourceExecutionLogId ?? 0)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceExecutionLog>()
            // ReSharper disable once InconsistentlySynchronizedField
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceSeries>()
            // ReSharper disable once InconsistentlySynchronizedField
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceRole>()
            .Where(w => datasourceGuids.Contains(w.VisualisationRegistryDatasourceGuid)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceVersion>()
            // ReSharper disable once InconsistentlySynchronizedField
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId ?? 0)).DeleteAsync();
        await db.GetTable<VisualisationRegistryParameter>().Where(w => parameterIds.Contains(w.Id)).DeleteAsync();
        var registryGuids = db.GetTable<VisualisationRegistry>().Where(w => registryIds.Contains(w.Id))
            .Select(w => w.Guid);
        await db.GetTable<VisualisationRegistryRole>()
            .Where(w => registryGuids.Contains(w.VisualisationRegistryGuid)).DeleteAsync();
        await db.GetTable<VisualisationRegistryVersion>().Where(w => registryIds.Contains(w.VisualisationRegistryId))
            .DeleteAsync();
        await db.GetTable<VisualisationRegistry>().Where(w => registryIds.Contains(w.Id)).DeleteAsync();
    }

    public Task RegisterCreatedDatasourceAsync(int id)
    {
        lock (datasourceIds)
        {
            datasourceIds.Add(id);
        }

        return Task.CompletedTask;
    }

    public async Task RegisterAllDatasourcesOfRegistryAsync(int registryId)
    {
        await using var db = fx.GetDbContext();
        var ids = await db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId ?? 0)).Select(w => w.Id).ToListAsync();
        lock (datasourceIds)
        {
            datasourceIds.AddRange(ids.Where(i => !datasourceIds.Contains(i)));
        }
    }

    public async Task<int> AddDatasourceInactiveAsync()
    {
        await using var db = fx.GetDbContext();
        return await AddDatasourceAsync(db, RegistryA, RoleA, UserA, SafeCommand, active: 0);
    }

    public async Task<int> DatasourceCountAsync(int registryId)
    {
        await using var db = fx.GetDbContext();
        return await db.GetTable<VisualisationRegistryDatasource>()
            .CountAsync(w => w.VisualisationRegistryId == registryId && (w.Deleted == 0 || w.Deleted == null));
    }

    public async Task<VisualisationRegistryDatasource?> DatasourceAsync(int id)
    {
        await using var db = fx.GetDbContext();
        return await db.GetTable<VisualisationRegistryDatasource>().FirstOrDefaultAsync(w => w.Id == id);
    }
}