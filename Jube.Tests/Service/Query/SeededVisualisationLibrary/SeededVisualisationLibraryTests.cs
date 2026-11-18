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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Options;
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Query.VisualisationRegistryDatasourceCommandExecution;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.SeededVisualisationLibrary
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SeededVisualisationLibraryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly string[] prefixes =
        [
            "Executive: ", "Rules: ", "Monitoring: ", "Cases: ", "Screening: ", "Configuration: ", "Models: ",
            "Network: ", "Platform: ", "Case: Workflow Dashboard"
        ];

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private DbContext? dbContext;

        public Task InitializeAsync()
        {
            dbContext = fx.GetDbContext();

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (dbContext != null)
            {
                await dbContext.DisposeAsync();
            }
        }

        private async Task<List<Data.Poco.VisualisationRegistry>> LibraryRegistriesAsync()
        {
            var registries = await dbContext!.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(r => r.TenantRegistryId == 1 && (r.Deleted == 0 || r.Deleted == null))
                .ToListAsync();

            return
            [
                .. registries.Where(r =>
                    prefixes.Any(p => (r.Name ?? string.Empty).StartsWith(p, StringComparison.Ordinal)))
            ];
        }

        private static Task<VisualisationRegistryDatasourceCommandExecutionService> BuildServiceAsync(DbContext context)
        {
            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ReportConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                             ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty
            });

            return VisualisationRegistryDatasourceCommandExecutionService.CreateAsync(context, "Administrator",
                TestLog.NoOp, localizers, new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private static string ParameterValue(Data.Poco.VisualisationRegistryParameter parameter)
        {
            var text = parameter.DefaultValue ?? string.Empty;

            if (parameter.DataTypeId != 4)
            {
                return text;
            }

            var days = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;

            return DateTime.UtcNow.AddDays(-days).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        [Fact]
        public async Task EveryDatasourceOfTheSeededLibraryRendersWithoutErrorAsync()
        {
            var registries = await LibraryRegistriesAsync();

            if (registries.Count == 0)
            {
                return;
            }

            var context = dbContext!;

            var caseId = await context.GetTable<Data.Poco.CaseEvent>().Where(e => e.CaseId != null)
                .OrderBy(e => e.CaseId).Select(e => e.CaseId).FirstOrDefaultAsync();

            var service = await BuildServiceAsync(context);
            var failures = new List<string>();
            var rendered = 0;

            foreach (var registry in registries)
            {
                var parameters = await context.GetTable<Data.Poco.VisualisationRegistryParameter>()
                    .Where(p => p.VisualisationRegistryId == registry.Id && (p.Deleted == 0 || p.Deleted == null))
                    .ToListAsync();

                var datasources = await context.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                    .Where(d => d.VisualisationRegistryId == registry.Id && d.Active == 1 &&
                                (d.Deleted == 0 || d.Deleted == null))
                    .ToListAsync();

                foreach (var datasource in datasources)
                {
                    var request = new DatasourceCardRequestDto
                    {
                        Parameters =
                        [
                            .. parameters.Select(p => new DatasourceCardParameterDto
                            {
                                Id = p.Id,
                                Value = ParameterValue(p)
                            })
                        ],
                        ContextId = registry.ContextKindId == 2 && caseId > 0 ? caseId : null,
                        Width = 700,
                        Height = 240
                    };

                    var card = await service.CardAsync(datasource.Id, request);

                    if (card.StateId == (int)DatasourceCardState.Error)
                    {
                        failures.Add($"{registry.Name} / {datasource.Name}: {card.Message}");
                    }
                    else
                    {
                        rendered++;
                    }
                }
            }

            failures.Should().BeEmpty();
            rendered.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task EveryRegistryDatasourceAndParameterOfTheSeededLibraryIsGrantedToTheAdministratorRoleAsync()
        {
            var registries = await LibraryRegistriesAsync();

            if (registries.Count == 0)
            {
                return;
            }

            var context = dbContext!;
            var role = await context.GetTable<Data.Poco.RoleRegistry>().FirstAsync(r => r.Id == 1);
            var ungranted = new List<string>();

            foreach (var registry in registries)
            {
                var registryGranted = await context.GetTable<Data.Poco.VisualisationRegistryRole>().AnyAsync(g =>
                    g.VisualisationRegistryGuid == registry.Guid && g.RoleRegistryGuid == role.Guid &&
                    (g.Deleted == 0 || g.Deleted == null));

                if (!registryGranted)
                {
                    ungranted.Add($"registry {registry.Name}");
                }

                foreach (var datasource in await context.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                             .Where(d => d.VisualisationRegistryId == registry.Id && (d.Deleted == 0 || d.Deleted == null))
                             .ToListAsync())
                {
                    var granted = await context.GetTable<Data.Poco.VisualisationRegistryDatasourceRole>().AnyAsync(g =>
                        g.VisualisationRegistryDatasourceGuid == datasource.Guid && g.RoleRegistryGuid == role.Guid &&
                        (g.Deleted == 0 || g.Deleted == null));

                    if (!granted)
                    {
                        ungranted.Add($"datasource {registry.Name} / {datasource.Name}");
                    }
                }

                foreach (var parameter in await context.GetTable<Data.Poco.VisualisationRegistryParameter>()
                             .Where(p => p.VisualisationRegistryId == registry.Id && (p.Deleted == 0 || p.Deleted == null))
                             .ToListAsync())
                {
                    var granted = await context.GetTable<Data.Poco.VisualisationRegistryParameterRole>().AnyAsync(g =>
                        g.VisualisationRegistryParameterGuid == parameter.Guid && g.RoleRegistryGuid == role.Guid &&
                        (g.Deleted == 0 || g.Deleted == null));

                    if (!granted)
                    {
                        ungranted.Add($"parameter {registry.Name} / {parameter.Name}");
                    }
                }
            }

            ungranted.Should().BeEmpty();
        }

        [Fact]
        public async Task NoDatasourceOfTheSeededLibraryReadsAColumnThatCarriesFileBytesAsync()
        {
            var registries = await LibraryRegistriesAsync();
            var ids = registries.Select(r => r.Id).ToList();

            var commands = await dbContext!.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(d => ids.Contains(d.VisualisationRegistryId ?? 0)).Select(d => new { d.Name, d.Command })
                .ToListAsync();

            var offenders = commands.Where(c => (c.Command ?? string.Empty).Contains("\"Object\"", StringComparison.Ordinal) ||
                                                (c.Command ?? string.Empty).Contains("\"Bytes\"", StringComparison.Ordinal) ||
                                                (c.Command ?? string.Empty).Contains("\"EncryptedBytes\"", StringComparison.Ordinal))
                .Select(c => c.Name).ToList();

            offenders.Should().BeEmpty();
        }
    }
}
