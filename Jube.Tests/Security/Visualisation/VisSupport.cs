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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;

namespace Jube.Test.Security.Visualisation;

internal static class VisSupport
{
    public const string Prefix = DatabaseFixture.Prefix;

    public static string Unique(string label)
    {
        return $"{Prefix}{label}{Guid.NewGuid():N}"[..Math.Min(40, Prefix.Length + label.Length + 32)];
    }

    public static async Task<VisSeed> SeedAsync(DatabaseFixture fx, PenTestHost host)
    {
        await using var db = fx.GetDbContext();
        var userA = host.UserName(PenTestUser.WithPermission);
        var userB = host.UserName(PenTestUser.TenantB);
        var userA2 = host.UserName(PenTestUser.NoApproveByReview);

        var tenantA = await db.UserInTenant.Where(w => w.User == userA).Select(s => s.TenantRegistryId)
            .FirstAsync();
        var tenantB = await db.UserInTenant.Where(w => w.User == userB).Select(s => s.TenantRegistryId)
            .FirstAsync();
        var roleA = await db.UserRegistry.Where(w => w.Name == userA).Select(s => s.RoleRegistryGuid).FirstAsync();
        var roleB = await db.UserRegistry.Where(w => w.Name == userB).Select(s => s.RoleRegistryGuid).FirstAsync();
        var roleA2 = await db.UserRegistry.Where(w => w.Name == userA2).Select(s => s.RoleRegistryGuid).FirstAsync();
        var roleAId = await db.RoleRegistry.Where(w => w.Guid == roleA).Select(s => s.Id).FirstAsync();
        var roleBId = await db.RoleRegistry.Where(w => w.Guid == roleB).Select(s => s.Id).FirstAsync();

        var registryA = await InsertRegistryAsync(db, tenantA, "VisRegA", true, true);
        var registryB = await InsertRegistryAsync(db, tenantB, "VisRegB", true, true);
        var parameterA = await InsertParameterAsync(db, registryA.Id, "ParamA");
        var parameterB = await InsertParameterAsync(db, registryB.Id, "ParamB");
        var datasourceA = await InsertDatasourceAsync(db, registryA.Id, "DsA");
        var datasourceB = await InsertDatasourceAsync(db, registryB.Id, "DsB");
        var seriesA = Unique("SeriesA");
        var seriesB = Unique("SeriesB");
        await db.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasourceSeries
        {
            VisualisationRegistryDatasourceId = datasourceA.Id, Name = seriesA, DataTypeId = 2
        });
        await db.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasourceSeries
        {
            VisualisationRegistryDatasourceId = datasourceB.Id, Name = seriesB, DataTypeId = 2
        });

        return new VisSeed
        {
            TenantAId = tenantA,
            TenantBId = tenantB,
            RoleAGuid = roleA,
            RoleBGuid = roleB,
            RoleA2Guid = roleA2,
            RoleAId = roleAId,
            RoleBId = roleBId,
            RegistryA = registryA,
            RegistryB = registryB,
            ParameterA = parameterA,
            ParameterB = parameterB,
            DatasourceA = datasourceA,
            DatasourceB = datasourceB,
            SeriesNameA = seriesA,
            SeriesNameB = seriesB
        };
    }

    public static async Task<VisRegistryRow> InsertRegistryAsync(DbContext db, int tenantId, string label,
        bool active, bool showInDirectory, bool locked = false, bool deleted = false)
    {
        var guid = Guid.NewGuid();
        var name = Unique(label);
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistry
        {
            Name = name,
            Guid = guid,
            Active = (byte)(active ? 1 : 0),
            Locked = (byte)(locked ? 1 : 0),
            Deleted = (byte)(deleted ? 1 : 0),
            ShowInDirectory = (byte)(showInDirectory ? 1 : 0),
            Columns = 2,
            ColumnWidth = 100,
            RowHeight = 100,
            TenantRegistryId = tenantId,
            Version = 1,
            CreatedUser = Prefix,
            CreatedDate = DateTime.UtcNow
        });
        return new VisRegistryRow(id, guid, name);
    }

    public static async Task<VisParameterRow> InsertParameterAsync(DbContext db, int registryId, string label,
        bool active = true, bool locked = false, bool deleted = false, int dataType = 1, string defaultValue = "x")
    {
        var guid = Guid.NewGuid();
        var name = Unique(label);
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistryParameter
        {
            VisualisationRegistryId = registryId,
            Name = name,
            Guid = guid,
            Active = (byte)(active ? 1 : 0),
            Locked = (byte)(locked ? 1 : 0),
            Deleted = (byte)(deleted ? 1 : 0),
            DataTypeId = dataType,
            DefaultValue = defaultValue,
            Required = 0,
            Version = 1,
            CreatedUser = Prefix,
            CreatedDate = DateTime.UtcNow
        });
        return new VisParameterRow(id, guid, registryId, name);
    }

    public static async Task<VisDatasourceRow> InsertDatasourceAsync(DbContext db, int registryId, string label)
    {
        var guid = Guid.NewGuid();
        var name = Unique(label);
        var id = await db.InsertWithInt32IdentityAsync(new VisualisationRegistryDatasource
        {
            VisualisationRegistryId = registryId,
            Name = name,
            Guid = guid,
            Active = 1,
            Locked = 0,
            Deleted = 0,
            VisualisationTypeId = 1,
            Command = "select 1",
            Priority = 1,
            Version = 1,
            CreatedUser = Prefix,
            CreatedDate = DateTime.UtcNow
        });
        return new VisDatasourceRow(id, guid, registryId, name);
    }

    public static async Task CleanupAsync(DatabaseFixture fx, VisSeed? seed)
    {
        if (seed == null)
        {
            return;
        }

        await using var db = fx.GetDbContext();
        int? tenantA = seed.TenantAId;
        int? tenantB = seed.TenantBId;
        var registryIds = db.GetTable<VisualisationRegistry>()
            .Where(w => w.TenantRegistryId == tenantA || w.TenantRegistryId == tenantB).Select(w => (int?)w.Id);
        var registryGuids = db.GetTable<VisualisationRegistry>()
            .Where(w => w.TenantRegistryId == tenantA || w.TenantRegistryId == tenantB).Select(w => w.Guid);
        var datasourceIds = db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).Select(w => (int?)w.Id);
        var datasourceGuids = db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).Select(w => w.Guid);
        var parameterIds = db.GetTable<VisualisationRegistryParameter>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).Select(w => (int?)w.Id);
        var parameterGuids = db.GetTable<VisualisationRegistryParameter>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).Select(w => w.Guid);

        await db.GetTable<VisualisationRegistryDatasourceSeries>()
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceRole>()
            .Where(w => datasourceGuids.Contains(w.VisualisationRegistryDatasourceGuid)).DeleteAsync();
        await db.GetTable<VisualisationRegistryParameterRole>()
            .Where(w => parameterGuids.Contains(w.VisualisationRegistryParameterGuid)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasourceVersion>()
            .Where(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId)).DeleteAsync();
        await db.GetTable<VisualisationRegistryParameterVersion>()
            .Where(w => parameterIds.Contains(w.VisualisationRegistryParameterId)).DeleteAsync();
        await db.GetTable<VisualisationRegistryDatasource>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();
        await db.GetTable<VisualisationRegistryParameter>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();
        await db.GetTable<VisualisationRegistryRole>()
            .Where(w => registryGuids.Contains(w.VisualisationRegistryGuid)).DeleteAsync();
        await db.GetTable<VisualisationRegistryVersion>()
            .Where(w => registryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();
        await db.GetTable<VisualisationRegistry>()
            .Where(w => w.TenantRegistryId == tenantA || w.TenantRegistryId == tenantB).DeleteAsync();
    }

    public static Task<PenTestResponse> SendJsonAsync(PenTestClient client, string method, string target,
        object? body)
    {
        return body == null
            ? client.SendAsync(method, target)
            : client.SendAsync(method, target, JsonSerializer.SerializeToUtf8Bytes(body), "application/json");
    }

    public static Task<PenTestResponse> SendRawAsync(PenTestClient client, string method, string target,
        string raw, string contentType = "application/json")
    {
        return client.SendAsync(method, target, Encoding.UTF8.GetBytes(raw), contentType);
    }

    public static string Enc(string value)
    {
        return Uri.EscapeDataString(value);
    }

    public static bool SameOutcome(PenTestResponse a, PenTestResponse b)
    {
        return a.Status == b.Status && string.Equals(a.Body.Trim(), b.Body.Trim(), StringComparison.Ordinal);
    }

    public static JsonElement? Prop(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    public static int? IntProp(PenTestResponse response, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            var value = Prop(document.RootElement, name);
            return value is { ValueKind: JsonValueKind.Number } number ? number.GetInt32() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? StringProp(PenTestResponse response, string name)
    {
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            var value = Prop(document.RootElement, name);
            return value is { ValueKind: JsonValueKind.String } text ? text.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static Guid? GuidProp(PenTestResponse response, string name)
    {
        return Guid.TryParse(StringProp(response, name), out var guid) ? guid : null;
    }

    public static IEnumerable<(string Label, PenTestClient Client)> RefusedCallers(PenTestHost host)
    {
        yield return ("anonymous", host.As(PenTestUser.Anonymous));
        yield return ("garbage-bearer", host.As(PenTestUser.Anonymous).WithBearer("garbage.token.value"));
        yield return ("empty-bearer", host.As(PenTestUser.Anonymous).WithBearer(""));
        yield return ("truncated-jwt", host.As(PenTestUser.Anonymous)
            .WithBearer(host.TokenFor(PenTestUser.WithPermission)[..40]));
        yield return ("without-permission", host.As(PenTestUser.WithoutPermission));
        yield return ("no-tenant", host.As(PenTestUser.NoTenant));
        yield return ("unknown-user", host.As(PenTestUser.UnknownUser));
    }
}