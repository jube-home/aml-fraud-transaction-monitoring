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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Charts;
using Jube.Service.Exceptions.Query.ExhaustiveSearchInstanceVariableStatistics;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.ExhaustiveSearchInstanceVariableStatistics;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveStatisticsChartServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly StatisticsSeeder seeder = new(fx);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return seeder.DisposeAsync();
        }

        private static Task<ExhaustiveStatisticsChartService> BuildAsync(DbContext db, string? user)
        {
            return ExhaustiveStatisticsChartService.CreateAsync(db, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static string Normalised(string svg)
        {
            return Regex.Replace(svg, "ex-[a-z]+l?-\\d+-\\d+", "ex-x");
        }

        [Fact]
        public async Task ClassesChartHasTheTwoClassesForTheRequestedVariableOnlyAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("Cls"));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var svg = await service.GetVariableClassesSvgAsync(instance.Id, variable, false, false);

            svg.Should().Contain("Negative class").And.Contain("Positive class");
            svg.Should().Contain("id=\"ex-vc-" + instance.Id + "-" + variable + "-t\"");
            (await service.GetVariableClassesSvgAsync(instance.Id, variable, true, true)).Should().Contain("Frequency");
        }

        [Fact]
        public async Task MonteCarloAndSummaryChartsCarryTheModelProfileAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("Mc"));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            (await service.GetMonteCarloSvgAsync(instance.Id, variable, false, false)).Should()
                .Contain("Model profile (Monte Carlo)");

            (await service.GetMonteCarloSummarySvgAsync(instance.Id, variable, true)).Should()
                .Contain("Standard deviations");
        }

        [Fact]
        public async Task MonteCarloCountsModeShowsCountInsteadOfDensityAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("McC"));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var density = await service.GetMonteCarloSvgAsync(instance.Id, variable, false, true);
            var counts = await service.GetMonteCarloSvgAsync(instance.Id, variable, true, true);

            density.Should().Contain("Density").And.NotContain("Count");
            counts.Should().Contain("Count").And.NotContain("Density");

            var gridCounts = await service.GetGridAsync(instance.Id, ExhaustiveStatisticsChartService.MonteCarloView,
                true);
            gridCounts[0].Svg.Should().Contain("Count");
        }

        [Fact]
        public async Task GridReturnsOneChartPerVariableInOneCallForEveryViewAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, first) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("G1"));
            var second = await seeder.VariableAsync(db, instance, Nm("G2"));
            await seeder.PopulationAsync(db, second, (0, 1, 4), (1, 2, 6));
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var classes = await service.GetGridAsync(instance.Id, ExhaustiveStatisticsChartService.ClassesView, false);
            var counts = await service.GetGridAsync(instance.Id, ExhaustiveStatisticsChartService.ClassesView, true);

            var monteCarlo = await service.GetGridAsync(instance.Id, ExhaustiveStatisticsChartService.MonteCarloView,
                false);
            var summary = await service.GetGridAsync(instance.Id, ExhaustiveStatisticsChartService.SummaryView, false);

            classes.Select(i => i.VariableId).Should().Equal(first, second);
            classes[0].Svg.Should().Contain("Negative class");
            classes[1].Svg.Should().Contain("Density").And.Contain("All rows");
            counts[0].Svg.Should().Contain("Frequency");
            monteCarlo[0].Svg.Should().Contain("Model profile (Monte Carlo)");
            monteCarlo[1].Svg.Should().Contain("No Monte Carlo statistics");
            summary[1].Svg.Should().Contain("No Monte Carlo statistics");
        }

        [Fact]
        public async Task UnknownForeignAndEmptyIdsGiveTheSameEmptyStateAsync()
        {
            await using var db = fx.GetDbContext();
            var (foreign, foreignVariable) = await seeder.FullAsync(db, fx.Seed.UserTenantB, Nm("Fo"));
            var empty = await seeder.InstanceAsync(db, fx.Seed.UserWithPermission, false);
            var service = await BuildAsync(db, fx.Seed.UserWithPermission);

            var unknown =
                Normalised(await service.GetVariableClassesSvgAsync(int.MaxValue, int.MaxValue, false, false));

            var foreignSvg = Normalised(await service.GetVariableClassesSvgAsync(foreign.Id, foreignVariable, false,
                false));

            var emptySvg = Normalised(await service.GetVariableClassesSvgAsync(empty.Id, 1, false, false));

            unknown.Should().Contain("No data available");
            foreignSvg.Should().Be(unknown);
            emptySvg.Should().Be(unknown);

            var unknownMc = Normalised(await service.GetMonteCarloSvgAsync(int.MaxValue, int.MaxValue, false, false));

            Normalised(await service.GetMonteCarloSvgAsync(foreign.Id, foreignVariable, false, false)).Should()
                .Be(unknownMc);

            (await service.GetGridAsync(foreign.Id, "classes", false)).Should().BeEmpty();
        }

        [Fact]
        public async Task EveryPathRefusesACallerWithoutThePermissionAsync()
        {
            await using var db = fx.GetDbContext();
            var (instance, variable) = await seeder.FullAsync(db, fx.Seed.UserWithPermission, Nm("Np"));
            var service = await BuildAsync(db, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetVariableClassesSvgAsync(instance.Id, variable, false, false));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetMonteCarloSvgAsync(instance.Id, variable, false, false));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetMonteCarloSummarySvgAsync(instance.Id, variable, false));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetGridAsync(instance.Id, "classes", false));
        }

        [Fact]
        public async Task CreateWithoutAUserThrowsNotAuthenticatedAsync()
        {
            await using var db = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(db, null));
        }

        private static string Nm(string suffix)
        {
            return $"{DatabaseFixture.Prefix}Cs{suffix}{Guid.NewGuid():N}"[..30];
        }
    }
}