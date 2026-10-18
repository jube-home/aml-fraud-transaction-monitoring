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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Charts;
using Jube.Service.Exceptions;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.ModelDiagrams;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveTopologyChartServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> modelIds = [];
        private readonly List<int> promotedIds = [];
        private readonly List<int> searchIds = [];
        private readonly List<int> sensitivityIds = [];
        private readonly List<int> trialIds = [];
        private readonly List<int> trialVariableIds = [];
        private readonly List<int> variableIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>()
                .Where(w => sensitivityIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>()
                .Where(w => trialVariableIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceVariable>()
                .Where(w => variableIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstance>()
                .Where(w => promotedIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<ExhaustiveSearchInstanceTrialInstance>()
                .Where(w => trialIds.Contains(w.Id)).DeleteAsync();

            foreach (var id in searchIds)
            {
                await dbContext.GetTable<ExhaustiveSearchInstanceVersion>()
                    .Where(w => w.ExhaustiveSearchInstanceId == id).DeleteAsync();

                await dbContext.GetTable<Data.Poco.ExhaustiveSearchInstance>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in modelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();

                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private async Task<int> SeedSearchAsync(DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            modelIds.Add(model.Id);

            var search = await new ExhaustiveSearchInstanceRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.ExhaustiveSearchInstance
                {
                    Name = $"{DatabaseFixture.Prefix}Search{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    EntityAnalysisModelId = model.Id,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    StatusId = 0
                });
            searchIds.Add(search.Id);

            return search.Id;
        }

        private async Task<(int TrialId, int PromotedId)> SeedPromotedAsync(DbContext dbContext, int searchId,
            string? json, byte active = 1, int complexity = 6, double score = 0.75)
        {
            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchId,
                    CreatedDate = DateTime.UtcNow
                });
            trialIds.Add(trial.Id);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstancePromotedTrialInstance
                {
                    ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                    Active = active,
                    Score = score,
                    TopologyComplexity = complexity,
                    Json = json,
                    CreatedDate = DateTime.UtcNow
                });
            promotedIds.Add(promoted.Id);

            return (trial.Id, promoted.Id);
        }

        private async Task SeedVariablesAsync(DbContext dbContext, int searchId, int trialId,
            IReadOnlyList<string> names, IReadOnlyDictionary<string, double> sensitivities)
        {
            for (var i = 0; i < names.Count; i++)
            {
                var variableId = await dbContext.InsertWithInt32IdentityAsync(new ExhaustiveSearchInstanceVariable
                {
                    ExhaustiveSearchInstanceId = searchId,
                    Name = names[i],
                    Mean = 1,
                    StandardDeviation = 2,
                    Maximum = 5,
                    Minimum = 0,
                    VariableSequence = i + 1
                });
                variableIds.Add(variableId);

                var trialVariableId = await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstanceTrialInstanceVariable
                    {
                        ExhaustiveSearchInstanceVariableId = variableId,
                        ExhaustiveSearchInstanceTrialInstanceId = trialId,
                        Removed = 0,
                        VariableSequence = i + 1
                    });
                trialVariableIds.Add(trialVariableId);

                if (sensitivities.TryGetValue(names[i], out var value))
                {
                    sensitivityIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                        new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                        {
                            ExhaustiveSearchInstanceTrialInstanceVariableId = trialVariableId,
                            Sensitivity = value
                        }));
                }
            }
        }

        private static string Json()
        {
            return ModelDiagramFixtures.Read("topology:Sigmoid2");
        }

        private static List<string> NamesOf(string json)
        {
            return [.. JObject.Parse(json)["annotations"]!.Select(a => a["name"]!.Value<string>()!)];
        }

        private static XmlDocument AssertSafeSvg(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.DocumentElement!.LocalName.Should().Be("svg");
            document.GetElementsByTagName("script").Count.Should().Be(0);
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");
            Regex.IsMatch(svg, @"\son[a-z]+\s*=", RegexOptions.IgnoreCase).Should().BeFalse();
            svg.Should().NotContain("<style");
            svg.Should().NotContain("style=");
            document.DocumentElement!.GetAttribute("class").Split(' ').Should().Contain("jc");

            return document;
        }

        private static List<string> TextOf(string svg)
        {
            var document = AssertSafeSvg(svg);

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private Task<ExhaustiveTopologyChartService> ServiceAsync(DbContext dbContext, string? user)
        {
            return ExhaustiveTopologyChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());
        }

        [Fact]
        public async Task RefusesAnUnauthenticatedCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var userName in new[] { null, "", "   ", fx.Seed.UnknownUser, fx.Seed.UserNoTenant })
            {
                var ex = await Assert.ThrowsAnyAsync<ServiceException>(() => ServiceAsync(dbContext, userName));
                ex.Code.Should().Be("NotAuthenticated");
            }
        }

        [Fact]
        public async Task DeniesACallerWithoutThePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var ex = await Assert.ThrowsAnyAsync<ServiceException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission)).GetSvgAsync(1));

            ex.Code.Should().Be("PermissionDenied");
        }

        [Fact]
        public async Task DrawsTheStoredNetworkWithRealVariableNamesAndSensitivityShadingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var json = Json();
            var names = NamesOf(json);
            var (trialId, _) = await SeedPromotedAsync(dbContext, searchId, json, complexity: 6, score: 0.8123);

            await SeedVariablesAsync(dbContext, searchId, trialId, names,
                new Dictionary<string, double> { [names[0]] = 0.4, [names[1]] = 0.1, [names[2]] = 0.2 });

            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var svg = await service.GetSvgAsync(searchId);
            var text = TextOf(svg);

            text.Should().Contain(ExhaustiveTopologyRenderer.Title);
            text.Should().Contain(names[0]);

            text.Should().Contain(t => t.StartsWith("26 inputs, 5 hidden neurons, 1 output") &&
                                       t.Contains("topology complexity 6") && t.Contains("score 0.812"));

            text.Should().Contain(ExhaustiveTopologyRenderer.SensitivityTitle);
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.4"));
            svg.Should().Contain("nq8");
            XmlDocumentId(svg).Should().Be("ex-tp-" + searchId);
        }

        [Fact]
        public async Task StillDrawsTheNetworkWhenThereIsNoSensitivityDataAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var json = Json();
            var (trialId, _) = await SeedPromotedAsync(dbContext, searchId, json);
            await SeedVariablesAsync(dbContext, searchId, trialId, NamesOf(json), new Dictionary<string, double>());
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var svg = await service.GetSvgAsync(searchId);

            TextOf(svg).Should().NotContain(ExhaustiveTopologyRenderer.SensitivityTitle);
            svg.Should().NotContain("nq");
        }

        [Fact]
        public async Task ForeignTenantUnknownAndNotTrainedIdsAreAllTheSameEmptyStateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var owned = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedPromotedAsync(dbContext, owned, Json());
            var untrained = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var inactive = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedPromotedAsync(dbContext, inactive, Json(), 0);
            var owner = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var other = await ServiceAsync(dbContext, fx.Seed.UserTenantB);

            var own = await owner.GetSvgAsync(owned);
            var foreign = await other.GetSvgAsync(owned);
            var unknown = await other.GetSvgAsync(int.MaxValue);
            var notTrained = await owner.GetSvgAsync(untrained);
            var notActive = await owner.GetSvgAsync(inactive);

            TextOf(own).Should().NotContain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));

            foreach (var empty in new[] { foreign, unknown, notTrained, notActive })
            {
                TextOf(empty).Should().Contain(t => t.Contains("has not been trained yet"));
            }

            foreign.Replace("-" + owned, "-ID").Should().Be(unknown.Replace("-" + int.MaxValue, "-ID"));
            notTrained.Replace("-" + untrained, "-ID").Should().Be(unknown.Replace("-" + int.MaxValue, "-ID"));
            notActive.Replace("-" + inactive, "-ID").Should().Be(unknown.Replace("-" + int.MaxValue, "-ID"));
        }

        [Fact]
        public async Task AnUnreadableStoredTopologyIsAMessageNotAnErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedPromotedAsync(dbContext, searchId, "{}");
            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var text = TextOf(await service.GetSvgAsync(searchId));

            text.Should().Contain(ExhaustiveTopologyRenderer.UnreadableMessage);
        }

        [Fact]
        public async Task AMismatchBetweenVariableCountAndInputsFallsBackToTheStoredNamesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            var (trialId, _) = await SeedPromotedAsync(dbContext, searchId, Json());

            await SeedVariablesAsync(dbContext, searchId, trialId, ["only", "two"],
                new Dictionary<string, double> { ["only"] = 1 });

            var service = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var text = TextOf(await service.GetSvgAsync(searchId));

            text.Should().Contain(t => t.StartsWith("26 inputs"));
            text.Should().NotContain("only");
        }

        private static string XmlDocumentId(string svg)
        {
            var label = Regex.Match(svg, "aria-labelledby=\"([^\"]+)\"").Groups[1].Value;

            return Regex.Match(label, "ex-tp-\\d+").Value;
        }
    }
}