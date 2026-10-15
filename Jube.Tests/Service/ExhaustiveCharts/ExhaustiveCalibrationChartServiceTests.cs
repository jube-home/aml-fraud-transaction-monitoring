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
using System.Xml;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Calibration.Models;
using Jube.Service.Charts;
using Jube.Service.Exceptions;
using Jube.Service.Reactivity;
using Jube.Test.Exhaustive;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ExhaustiveCalibrationChartServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> modelIds = [];
        private readonly List<int> predictedActualIds = [];
        private readonly List<int> promotedIds = [];
        private readonly List<int> searchIds = [];
        private readonly List<int> trialIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>()
                .Where(w => predictedActualIds.Contains(w.Id)).DeleteAsync();
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

        private async Task<int> SeedPromotedAsync(DbContext dbContext, int searchId,
            Action<ExhaustiveSearchInstancePromotedTrialInstance>? calibration)
        {
            var trial = await new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext).InsertAsync(
                new ExhaustiveSearchInstanceTrialInstance
                {
                    ExhaustiveSearchInstanceId = searchId,
                    CreatedDate = DateTime.UtcNow
                });
            trialIds.Add(trial.Id);

            var row = new ExhaustiveSearchInstancePromotedTrialInstance
            {
                ExhaustiveSearchInstanceTrialInstanceId = trial.Id,
                Active = 1,
                Score = 0.9,
                Json = "{}",
                CreatedDate = DateTime.UtcNow
            };

            calibration?.Invoke(row);

            var promoted = await new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext)
                .InsertAsync(row);
            promotedIds.Add(promoted.Id);

            return trial.Id;
        }

        private async Task SeedRowsAsync(DbContext dbContext, int trialId)
        {
            var (scores, labels) = ExhaustiveCalibrationFitterTests.Generate(4243, 300);

            for (var i = 0; i < scores.Length; i++)
            {
                predictedActualIds.Add(await dbContext.InsertWithInt32IdentityAsync(
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    {
                        ExhaustiveSearchInstanceTrialInstanceId = trialId,
                        Predicted = scores[i],
                        Actual = labels[i]
                    }));
            }
        }

        private static void Platt(ExhaustiveSearchInstancePromotedTrialInstance row)
        {
            row.CalibrationMethodId = ExhaustiveCalibrationMethod.Platt;
            row.CalibrationInputId = ExhaustiveCalibrationInput.Logit;
            row.CalibrationSlope = 1.9138919318703835;
            row.CalibrationIntercept = 0.03593553619719747;
            row.CalibrationEpsilon = 1e-6;
            row.TrainingPrior = 0.5;
            row.CalibrationPrior = 0.535;
            row.BaseRate = 0.02;
            row.BaseRateSourceId = ExhaustiveBaseRateSource.PopulationCount;
            row.CalibrationSampleSize = 400;
            row.CalibrationStatusId = ExhaustiveCalibrationStatus.Calibrated;
        }

        private static List<string> TextOf(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);
            document.DocumentElement!.LocalName.Should().Be("svg");
            svg.Should().NotContainEquivalentOf("<script");
            svg.Should().NotContainEquivalentOf("foreignObject");

            return
            [
                .. document.GetElementsByTagName("text").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText),
                .. document.GetElementsByTagName("desc").Cast<XmlNode>().Select(n => n.InnerText)
            ];
        }

        private Task<ExhaustiveCalibrationChartService> ServiceAsync(DbContext dbContext, string? user)
        {
            return ExhaustiveCalibrationChartService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus());
        }

        [Fact]
        public async Task ACalibratedModel_DrawsTheRawAndTheCalibratedCurvesWithTheStatisticsInTheCaptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, Platt));

            var text = TextOf(await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetSvgAsync(searchId));

            text.Should().Contain("Calibration: raw versus calibrated");
            text.Should().Contain(t => t.Contains(ExhaustiveCalibrationRenderer.RawSeriesName));
            text.Should().Contain(t => t.Contains(ExhaustiveCalibrationRenderer.CalibratedSeriesName));
            text.Should().Contain(t => t.Contains("Perfectly calibrated"));

            text.Should().Contain(t => t.Contains("Platt scaling on the validation split") &&
                                       t.Contains("slope 1.914") && t.Contains("intercept 0.036") &&
                                       t.Contains("base rate 0.02 (PopulationCount)"));

            text.Should().NotContain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
        }

        [Fact]
        public async Task AHistoricalModel_DrawsOnlyTheRawCurveAndSaysItIsNotCalibratedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, null));

            var text = TextOf(await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetSvgAsync(searchId));

            text.Should().Contain(t => t.Contains("Not calibrated (promoted before calibration was introduced)"));
            text.Should().NotContain(t => t.Contains(ExhaustiveCalibrationRenderer.CalibratedSeriesName));
        }

        [Fact]
        public async Task ASkippedCalibration_ShowsTheRecordedReasonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);

            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, r =>
            {
                r.CalibrationMethodId = ExhaustiveCalibrationMethod.None;
                r.CalibrationStatusId = ExhaustiveCalibrationStatus.Skipped;
                r.CalibrationReason = "The validation split has a single class.";
            }));

            var text = TextOf(await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetSvgAsync(searchId));

            text.Should().Contain(t => t.Contains("Not calibrated: The validation split has a single class."));
        }

        [Fact]
        public async Task AnInvalidStoredCalibrator_IsReportedAndNotDrawnAsCalibratedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);

            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, r =>
            {
                Platt(r);
                r.CalibrationSlope = -1;
            }));

            var text = TextOf(await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission)).GetSvgAsync(searchId));

            text.Should().Contain(t => t.Contains("stored calibrator is invalid"));
            text.Should().NotContain(t => t.Contains(ExhaustiveCalibrationRenderer.CalibratedSeriesName));
        }

        [Fact]
        public async Task PermissionTenantAndUnknownIdBehaviourIsUnchangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, Platt));

            var denied = await Assert.ThrowsAnyAsync<ServiceException>(async () =>
                await (await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission)).GetSvgAsync(searchId));
            denied.Code.Should().Be("PermissionDenied");

            foreach (var user in new[] { null, "", fx.Seed.UnknownUser, fx.Seed.UserNoTenant })
            {
                var ex = await Assert.ThrowsAnyAsync<ServiceException>(() => ServiceAsync(dbContext, user));
                ex.Code.Should().Be("NotAuthenticated");
            }

            var other = await (await ServiceAsync(dbContext, fx.Seed.UserTenantB)).GetSvgAsync(searchId);

            var unknown = await (await ServiceAsync(dbContext, fx.Seed.UserWithPermission))
                .GetSvgAsync(int.MaxValue);

            TextOf(other).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            TextOf(unknown).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            other.Should().NotContain("Platt");
        }

        [Fact]
        public async Task ThePredictedActualChart_ShowsCalibratedProbabilitiesForACalibratedModelAndLabelsThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, Platt));
            var historicalId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, historicalId, null));
            var bus = new NullServiceChangeBus();

            var service = await ExhaustivePredictedActualChartService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, bus);

            var calibrated = TextOf(await service.GetSvgAsync(searchId));
            var historical = TextOf(await service.GetSvgAsync(historicalId));

            string.Join(" ", calibrated).Should().Contain("calibrated at the sample prior");
            historical.Should().NotContain(t => t.Contains("calibrated"));
        }

        private async Task<string> PerformanceAsync(DbContext dbContext, string user, int searchId,
            ExhaustivePerformanceChart chart)
        {
            var service = await ExhaustivePerformanceChartService.CreateAsync(dbContext, user, TestLog.NoOp,
                localizers, new NullServiceChangeBus());

            return await service.GetSvgAsync(searchId, chart);
        }

        [Fact]
        public async Task TheResultsPageMeasuresPanel_ForACalibratedModel_ShowsTheCalibrationStatisticsAndCaptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var searchId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);

            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, searchId, r =>
            {
                Platt(r);

                r.CalibrationQualityJson = new CalibrationQuality
                {
                    Test = new SplitQuality
                    {
                        Rows = 300,
                        Ece = { Raw = 0.0736, Calibrated = 0.0592 },
                        Brier = { Raw = 0.1762, Calibrated = 0.1706 },
                        LogLoss = { Raw = 0.5291, Calibrated = 0.5021 }
                    },
                    TestAtBaseRate = new BaseRateQuality
                    {
                        BaseRate = 0.02,
                        Brier = { Raw = 0.1719, Calibrated = 0.0172 },
                        LogLoss = { Raw = 0.5213, Calibrated = 0.0776 }
                    }
                }.ToJson();
            }));

            var text = TextOf(await PerformanceAsync(dbContext, fx.Seed.UserWithPermission, searchId,
                ExhaustivePerformanceChart.Measures));

            text.Should().Contain("Probability quality (calibrated)");
            text.Should().Contain("Calibration");
            text.Should().Contain("Calibrator slope");
            text.Should().Contain("Calibrator intercept");
            text.Should().Contain("Raw ECE");
            text.Should().Contain("Raw Brier score");
            text.Should().Contain("Raw log loss");
            text.Should().Contain("Training prior");
            text.Should().Contain("Base rate");
            text.Should().Contain("PopulationCount");
            text.Should().Contain("Brier at base rate");
            text.Should().Contain(t => t.Contains("Calibrated at the sample prior 0.535, recall base rate 0.02"));
            text.Should().Contain(t => t.Contains("consider the minimum-risk threshold"));
        }

        [Fact]
        public async Task
            TheResultsPageCalibrationChart_ForACalibratedModel_DrawsBothCurvesAndForAHistoricalModelSaysSoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var calibratedId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, calibratedId, Platt));
            var historicalId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, historicalId, null));

            var calibrated = TextOf(await PerformanceAsync(dbContext, fx.Seed.UserWithPermission, calibratedId,
                ExhaustivePerformanceChart.Calibration));
            var historical = TextOf(await PerformanceAsync(dbContext, fx.Seed.UserWithPermission, historicalId,
                ExhaustivePerformanceChart.Calibration));
            var historicalMeasures = TextOf(await PerformanceAsync(dbContext, fx.Seed.UserWithPermission,
                historicalId, ExhaustivePerformanceChart.Measures));

            calibrated.Should().Contain(t => t.Contains(ExhaustiveCalibrationRenderer.RawSeriesName));
            calibrated.Should().Contain(t => t.Contains(ExhaustiveCalibrationRenderer.CalibratedSeriesName));
            historical.Should().Contain(t => t.Contains("Not calibrated (promoted before calibration was introduced)"));
            historical.Should().NotContain(t => t.Contains(ExhaustiveCalibrationRenderer.CalibratedSeriesName));

            historicalMeasures.Should().Contain(t =>
                t.Contains("Not calibrated (promoted before calibration was introduced)"));

            historicalMeasures.Should().NotContain("Calibrator slope");
            historicalMeasures.Should().Contain("Probability quality");
        }

        [Fact]
        public async Task TheResultsPageCharts_RenderForBothKindsOfModelAndStayIsolatedByTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var calibratedId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, calibratedId, Platt));
            var historicalId = await SeedSearchAsync(dbContext, fx.Seed.UserWithPermission);
            await SeedRowsAsync(dbContext, await SeedPromotedAsync(dbContext, historicalId, null));

            var calibrated = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission, calibratedId,
                ExhaustivePerformanceChart.Roc);
            var historical = await PerformanceAsync(dbContext, fx.Seed.UserWithPermission, historicalId,
                ExhaustivePerformanceChart.Roc);
            var other = await PerformanceAsync(dbContext, fx.Seed.UserTenantB, calibratedId,
                ExhaustivePerformanceChart.Measures);

            TextOf(calibrated).Should().Contain("ROC");
            TextOf(historical).Should().Contain("ROC");
            TextOf(other).Should().Contain(t => t.Contains(ExhaustiveChartRenderer.EmptyMessage));
            other.Should().NotContain("Platt");
        }

        [Fact]
        public void TheRenderer_CalibratesRowsAtTheSamplePriorAndLeavesUncalibratedRowsAlone()
        {
            var rows = new List<ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto>
            {
                new() { Predicted = 0.1, Actual = 0 },
                new() { Predicted = 0.9, Actual = 1 }
            };

            var promoted = new ExhaustiveSearchInstancePromotedTrialInstanceQueryDto
            {
                Active = true,
                CalibrationMethod = "platt",
                CalibrationInput = "logit",
                CalibrationSlope = 1.9138919318703835,
                CalibrationIntercept = 0.03593553619719747,
                CalibrationEpsilon = 1e-6,
                CalibrationPrior = 0.535,
                BaseRate = 0.02
            };

            var calibrated = ExhaustiveCalibrationRenderer.Calibrate(rows,
                ExhaustiveCalibrationRenderer.ToCalibration(promoted));

            var untouched = ExhaustiveCalibrationRenderer.Calibrate(rows, ExhaustiveCalibration.Identity);

            calibrated[0].Predicted.Should().BeApproximately(0.015227391903322045, 1e-9);
            calibrated[1].Predicted.Should().BeApproximately(0.9858136335137764, 1e-9);
            calibrated[1].Error.Should().BeApproximately(1 - 0.9858136335137764, 1e-9);
            untouched.Select(r => r.Predicted).Should().Equal(0.1, 0.9);
            ExhaustiveCalibrationRenderer.Active([]).Should().BeNull();
            ExhaustiveCalibrationRenderer.Caption(null).Should().Contain("promoted before calibration");
        }
    }
}