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
using System.Text.Json;
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Security.PenTest;
using LinqToDB;
using Xunit.Abstractions;

namespace Jube.Test.Security.DynamicSql;

public abstract class DynamicSqlTestBase(DatabaseFixture fx, ITestOutputHelper output) : PenTestBase(fx, output)
{
    private static readonly JsonSerializerOptions jsonOptions = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal DynamicSqlData Data
    {
        get => field.Required();
        private set;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Data = await DynamicSqlData.CreateAsync(Fx, Host);
    }

    public override Task DisposeAsync()
    {
        return Data.DisposeAsync();
    }

    protected static string Unique(string label = "Ds") => $"{DatabaseFixture.Prefix}W12{label}{Guid.NewGuid():N}";

    protected static string Json(object value) => JsonSerializer.Serialize(value, jsonOptions);

    protected static Dictionary<string, object?> DatasourceBody(int registryId, string command, string? name = null,
        int id = 0)
    {
        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["visualisationRegistryId"] = registryId,
            ["name"] = name ?? Unique(),
            ["active"] = true,
            ["locked"] = false,
            ["command"] = command,
            ["priority"] = 1,
            ["rowSpan"] = 1,
            ["columnSpan"] = 1,
            ["includeGrid"] = true,
            ["includeDisplay"] = false,
            ["visualisationTypeId"] = 1
        };
    }

    protected async Task<int> CountExecutionLogsAsync(IReadOnlyCollection<int> datasourceIds, bool withError)
    {
        await using var db = Fx.GetDbContext();
        return await db.GetTable<Data.Poco.VisualisationRegistryDatasourceExecutionLog>()
            .CountAsync(w => datasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0) &&
                             (withError ? w.Error != null : w.Error == null));
    }
}