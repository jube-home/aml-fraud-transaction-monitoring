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
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;
using Xunit.Abstractions;
using Jube.Test.Security.DynamicSql.Models;

namespace Jube.Test.Security.DynamicSql;

public abstract class CaseSearchTestBase(DatabaseFixture fx, ITestOutputHelper output) : PenTestBase(fx, output)
{
    private static readonly JsonSerializerOptions jsonOptions = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal CaseSeedData Seed
    {
        get => field.Required();
        private set;
    }

    internal Seeded A
    {
        get => field.Required();
        private set;
    }

    internal Seeded B
    {
        get => field.Required();
        private set;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Seed = new CaseSeedData(Fx);
        A = await Seed.AddCaseAsync(Host.UserName(PenTestUser.WithPermission));
        B = await Seed.AddCaseAsync(Host.UserName(PenTestUser.TenantB));
    }

    public override Task DisposeAsync()
    {
        return Seed.DisposeAsync();
    }

    protected static string Json(object value) => JsonSerializer.Serialize(value, jsonOptions);

    protected static string SelectJson(string id = "CaseKeyValue", string field = "\"Case\".\"CaseKeyValue\"",
        string value = "ASC", string condition = "AND")
    {
        return Json(new
        {
            condition,
            rules = new[] { new { id, field, type = "string", @operator = "order", value } }
        });
    }

    protected static string FilterJson(string caseKeyValue, string id = "CaseKeyValue", string @operator = "equal",
        string condition = "AND", string type = "string")
    {
        return Json(new
        {
            condition,
            rules = new[] { new { id, field = id, type, @operator, value = caseKeyValue } }
        });
    }

    protected static string Compile(Guid workflow, string selectJson, string filterJson, Guid? filterGuid = null)
    {
        return Json(new
        {
            caseWorkflowGuid = workflow,
            caseWorkflowFilterGuid = filterGuid,
            selectJson,
            filterJson
        });
    }

    protected static Guid? GuidOf(PenTestResponse response)
    {
        try
        {
            using var document = JsonDocument.Parse(response.Body);
            return document.RootElement.GetProperty("guid").GetGuid();
        }
        catch (Exception)
        {
            return null;
        }
    }

    protected async Task<string?> PasswordOfAsync(string user)
    {
        await using var db = Fx.GetDbContext();
        return (await db.UserRegistry.FirstAsync(u => u.Name == user)).Password;
    }
}