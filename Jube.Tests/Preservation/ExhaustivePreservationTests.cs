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
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustivePreservationTests(DatabaseFixture fx)
    {
        private readonly PreservationHarness harness = new(fx);

        private static int CountFor(Type child, int parentIndex)
        {
            return child == typeof(ExhaustiveSearchInstanceTrialInstance) ||
                   child == typeof(ExhaustiveSearchInstanceVariable)
                ? 3
                : 1 + parentIndex % 2;
        }

        private static async Task<Dictionary<Type, List<object>>> SeedTreeAsync(DbContext db, int modelId, int counter)
        {
            var rows = new Dictionary<Type, List<object>>();

            var instance = ExhaustiveTreeSpec.Fill(typeof(ExhaustiveSearchInstance), ref counter);
            ExhaustiveTreeSpec.Set(instance, "EntityAnalysisModelId", modelId);
            ((ExhaustiveSearchInstance)instance).ExhaustiveInstanceCsvId = null;
            ((ExhaustiveSearchInstance)instance).ExhaustiveInstanceJsonId = null;
            ExhaustiveTreeSpec.Set(instance, "Id", await ExhaustiveTreeSpec.InsertAsync(db, instance));
            rows[typeof(ExhaustiveSearchInstance)] = [instance];

            foreach (var primary in ExhaustiveTreeSpec.Links.Where(w => w.Primary))
            {
                var secondaries = ExhaustiveTreeSpec.Links.Where(w => !w.Primary && w.Child == primary.Child).ToList();
                var created = new List<object>();
                var parents = rows[primary.Parent];
                var sequence = 0;

                for (var parentIndex = 0; parentIndex < parents.Count; parentIndex++)
                {
                    for (var i = 0; i < CountFor(primary.Child, parentIndex); i++)
                    {
                        var row = ExhaustiveTreeSpec.Fill(primary.Child, ref counter);
                        ExhaustiveTreeSpec.Set(row, primary.ForeignKey, ExhaustiveTreeSpec.Id(parents[parentIndex]));

                        foreach (var secondary in secondaries)
                        {
                            var pool = rows[secondary.Parent];

                            var offset = secondary.ForeignKey.StartsWith("Test", StringComparison.Ordinal)
                                ? parentIndex + 1 + i
                                : sequence;

                            ExhaustiveTreeSpec.Set(row, secondary.ForeignKey,
                                ExhaustiveTreeSpec.Id(pool[offset % pool.Count]));
                        }

                        ExhaustiveTreeSpec.Set(row, "Id", await ExhaustiveTreeSpec.InsertAsync(db, row));
                        created.Add(row);
                        sequence++;
                    }
                }

                rows[primary.Child] = created;
            }

            return rows;
        }

        private static async Task<Dictionary<Type, List<object>>> CollectAsync(DbContext db, int modelId)
        {
            var rows = new Dictionary<Type, List<object>>();

            rows[typeof(ExhaustiveSearchInstance)] =
            [
                .. (await db.ExhaustiveSearchInstance
                    .Where(w => w.EntityAnalysisModelId == modelId && (w.Deleted == 0 || w.Deleted == null))
                    .OrderBy(o => o.Id).ToListAsync()).Cast<object>()
            ];

            foreach (var primary in ExhaustiveTreeSpec.Links.Where(w => w.Primary))
            {
                var parentIds = rows[primary.Parent].Select(ExhaustiveTreeSpec.Id).ToList();

                rows[primary.Child] =
                    await ExhaustiveTreeSpec.LoadAsync(db, primary.Child, primary.ForeignKey, parentIds);
            }

            return rows;
        }

        private static List<string> Signatures(Dictionary<Type, List<object>> rows, Type type)
        {
            return [.. rows[type].Select(ExhaustiveTreeSpec.Signature).Order(StringComparer.Ordinal)];
        }

        private static List<string> LinkSignatures(Dictionary<Type, List<object>> rows, TreeLink link)
        {
            var parents = rows[link.Parent].ToDictionary(ExhaustiveTreeSpec.Id);

            return
            [
                .. rows[link.Child].Select(child =>
                    {
                        var parentId = ExhaustiveTreeSpec.Get(child, link.ForeignKey);

                        var parent = parentId.HasValue && parents.TryGetValue(parentId.Value, out var found)
                            ? ExhaustiveTreeSpec.Signature(found)
                            : "MISSING";

                        return $"{ExhaustiveTreeSpec.Signature(child)} => {link.ForeignKey} => {parent}";
                    })
                    .Order(StringComparer.Ordinal)
            ];
        }

        private static void AssertSameTree(Dictionary<Type, List<object>> expected,
            Dictionary<Type, List<object>> actual)
        {
            foreach (var type in ExhaustiveTreeSpec.Types)
            {
                expected[type].Should().NotBeEmpty($"{type.Name} must be seeded so that it is exercised");
                Signatures(actual, type).Should().Equal(Signatures(expected, type), type.Name);
            }

            foreach (var link in ExhaustiveTreeSpec.Links)
            {
                LinkSignatures(actual, link).Should().Equal(LinkSignatures(expected, link),
                    $"{link.Child.Name}.{link.ForeignKey}");
            }
        }

        [Fact]
        public async Task EveryExhaustiveTableRoundTripsWithEveryColumnAndEveryForeignKeyRemappedAsync()
        {
            var decoy = await harness.CreateTenantAsync("ExD");
            var source = await harness.CreateTenantAsync("ExS");
            var target = await harness.CreateTenantAsync("ExT");
            var decoyModel = await harness.CreateModelAsync(decoy, "ExD");
            var sourceModel = await harness.CreateModelAsync(source, "ExS");
            var targetModel = await harness.CreateModelAsync(target, "ExT");
            await using var db = fx.GetDbContext();

            await SeedTreeAsync(db, decoyModel, 10000);
            await SeedTreeAsync(db, sourceModel, 0);
            var targetOld = await SeedTreeAsync(db, targetModel, 20000);
            var decoyBefore = await CollectAsync(db, decoyModel);
            var sourceBefore = await CollectAsync(db, sourceModel);

            var bytes = await harness.ExportAsync(source, PreservationHarness.ExportOptionsFor(true,
                false));

            await harness.ImportAsync(target, bytes);

            var importedModel = await db.EntityAnalysisModel.SingleAsync(w => w.TenantRegistryId == target.Id
                                                                              && (w.Deleted == 0 || w.Deleted == null));

            var imported = await CollectAsync(db, importedModel.Id);

            AssertSameTree(sourceBefore, imported);

            var sourceAfter = await CollectAsync(db, sourceModel);
            AssertSameTree(sourceBefore, sourceAfter);
            AssertSameTree(decoyBefore, await CollectAsync(db, decoyModel));

            (await db.ExhaustiveSearchInstance.SingleAsync(w => w.Id == ExhaustiveTreeSpec.Id(
                targetOld[typeof(ExhaustiveSearchInstance)][0]))).Deleted.Should().Be(1);

            imported[typeof(ExhaustiveSearchInstance)].Cast<ExhaustiveSearchInstance>().Single().EntityAnalysisModelId
                .Should().Be(importedModel.Id);
        }

        [Fact]
        public async Task PerParentChildrenAreNotMixedAcrossParentsOrLeakedFromOtherTenantsAsync()
        {
            var decoy = await harness.CreateTenantAsync("PpD");
            var source = await harness.CreateTenantAsync("PpS");
            var decoyModel = await harness.CreateModelAsync(decoy, "PpD");
            var sourceModel = await harness.CreateModelAsync(source, "PpS");
            await using var db = fx.GetDbContext();

            await SeedTreeAsync(db, decoyModel, 10000);
            var seeded = await SeedTreeAsync(db, sourceModel, 0);

            var wrapper = PreservationHarness.Decrypt(await harness.ExportAsync(source,
                PreservationHarness.ExportOptionsFor(true, false)));

            var instance = wrapper.Payload!.EntityAnalysisModel!.Single().ExhaustiveSearchInstance.Single();

            instance.ExhaustiveSearchInstanceTrialInstance
                .Select(s => s.ExhaustiveSearchInstancePromotedTrialInstanceRoc.Count())
                .Should().Equal(seeded[typeof(ExhaustiveSearchInstanceTrialInstance)]
                    .Select((_, i) => CountFor(typeof(ExhaustiveSearchInstancePromotedTrialInstanceRoc), i)));

            instance.ExhaustiveSearchInstanceTrialInstance
                .SelectMany(s => s.ExhaustiveSearchInstancePromotedTrialInstanceRoc)
                .Should().HaveCount(seeded[typeof(ExhaustiveSearchInstancePromotedTrialInstanceRoc)].Count);
            instance.ExhaustiveSearchInstanceVariable
                .SelectMany(s => s.ExhaustiveSearchInstanceVariableHistogramAnomaly)
                .Should().HaveCount(seeded[typeof(ExhaustiveSearchInstanceVariableHistogramAnomaly)].Count);
            instance.ExhaustiveSearchInstanceVariable
                .SelectMany(s => s.ExhaustiveSearchInstanceVariableHistogramClassification)
                .Should().HaveCount(seeded[typeof(ExhaustiveSearchInstanceVariableHistogramClassification)].Count);

            instance.ExhaustiveSearchInstanceTrialInstance
                .SelectMany(s => s.ExhaustiveSearchInstanceTrialInstanceVariable)
                .SelectMany(s => s.ExhaustiveSearchInstancePromotedTrialInstanceVariable)
                .SelectMany(s => s.ExhaustiveSearchInstancePrescriptionHistogram)
                .Should().HaveCount(seeded[typeof(ExhaustiveSearchInstancePrescriptionHistogram)]
                    .Count);
        }

        [Fact]
        public async Task AnExportMadeBeforeThePrescriptionHistogramWasCarriedStillImportsAsync()
        {
            var source = await harness.CreateTenantAsync("LxS");
            var target = await harness.CreateTenantAsync("LxT");
            var sourceModel = await harness.CreateModelAsync(source, "LxS");
            await using var db = fx.GetDbContext();
            var seeded = await SeedTreeAsync(db, sourceModel, 0);

            var wrapper = PreservationHarness.Decrypt(await harness.ExportAsync(source,
                PreservationHarness.ExportOptionsFor(true, false)));

            foreach (var promoted in wrapper.Payload!.EntityAnalysisModel!.Single().ExhaustiveSearchInstance.Single()
                         .ExhaustiveSearchInstanceTrialInstance
                         .SelectMany(s => s.ExhaustiveSearchInstanceTrialInstanceVariable)
                         .SelectMany(s => s.ExhaustiveSearchInstancePromotedTrialInstanceVariable))
            {
                promoted.ExhaustiveSearchInstancePrescriptionHistogram = null;
            }

            await harness.ImportAsync(target, PreservationHarness.Encrypt(wrapper));

            var importedModel = await db.EntityAnalysisModel.SingleAsync(w => w.TenantRegistryId == target.Id
                                                                              && (w.Deleted == 0 || w.Deleted == null));

            var imported = await CollectAsync(db, importedModel.Id);
            imported[typeof(ExhaustiveSearchInstancePrescriptionHistogram)].Should().BeEmpty();

            imported[typeof(ExhaustiveSearchInstanceTrialInstanceVariable)].Should()
                .HaveCount(seeded[typeof(ExhaustiveSearchInstanceTrialInstanceVariable)].Count);
            imported[typeof(ExhaustiveSearchInstanceVariableMultiCollinearity)].Should()
                .HaveCount(seeded[typeof(ExhaustiveSearchInstanceVariableMultiCollinearity)].Count);
        }

        [Fact]
        public async Task LivePayloadCarriesTheExhaustiveAndAnalyticsCollectionsAsync()
        {
            var tenant = await harness.CreateTenantAsync("Pk");
            var modelId = await harness.CreateModelAsync(tenant, "Pk");
            await using var db = fx.GetDbContext();
            await SeedTreeAsync(db, modelId, 0);
            await AdvancedAnalyticsSeed.SeedAsync(db, tenant.Id, modelId, "Pk");
            var json = await PreservationHarness.LiveJsonAsync(db, tenant.UserName, true, false, false, false,
                advancedAnalytics: true);
            var without = await PreservationHarness.LiveJsonAsync(db, tenant.UserName, true, false, false, false);

            json.Should().Contain("ExhaustiveSearchInstancePrescriptionHistogram");
            json.Should().Contain("TrainedModelVersions");
            json.Should().Contain("TrainingRunCurvePoints");
            without.Should().NotContain("TrainedModelVersions");
        }
    }
}