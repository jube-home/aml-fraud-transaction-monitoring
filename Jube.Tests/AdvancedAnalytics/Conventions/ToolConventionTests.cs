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
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.Models;

namespace Jube.Test.AdvancedAnalytics.Conventions
{
    public class ToolConventionTests
    {
        private static readonly Assembly productionAssembly = typeof(AdvancedAnalyticsToolCatalogue).Assembly;

        private static IReadOnlyList<AdvancedAnalyticsToolDescriptor> Fixtures()
        {
            return
            [
                .. AdvancedAnalyticsToolCatalogue.Discover(typeof(ToolConventionTests).Assembly)
                    .Where(d => d.GroupType.Namespace == typeof(ToolConventionTests).Namespace)
            ];
        }

        [Fact]
        public void EveryProductionToolHasNoConventionViolations()
        {
            var descriptors = AdvancedAnalyticsToolCatalogue.Discover(productionAssembly);

            AdvancedAnalyticsToolCatalogue.Validate(descriptors).Should().BeEmpty();
        }

        [Fact]
        public void TheTrainerToolSetLivesInTheServiceAssemblyWithTheSamePermissions()
        {
            typeof(AdvancedAnalyticsToolCatalogue).Assembly.GetName().Name.Should().Be("Jube.Service");

            var found = AdvancedAnalyticsToolCatalogue.Discover(productionAssembly)
                .Where(d => d.GroupName is "LogisticRegression" or "LogisticRegressionTrainingRuns")
                .Select(d => d.ToolName + ":" + string.Join(",", d.Permissions))
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

            found.Should().HaveCount(7);

            found.Select(x => x.Split(':')[0]).Should().BeEquivalentTo(
                "TrainLogisticRegression", "RecallLogisticRegression", "AssessPredictorSignificance",
                "SubmitLogisticRegressionTraining", "GetTrainingRunStatus", "CancelTrainingRun",
                "GetTrainingRunResult");
        }

        [Fact]
        public void ProductionToolNamesAreGloballyUnique()
        {
            var names = AdvancedAnalyticsToolCatalogue.Discover(productionAssembly)
                .Select(d => d.ToolName)
                .ToList();

            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void ProductionToolsDeclareAtLeastOneKnownPermission()
        {
            foreach (var descriptor in AdvancedAnalyticsToolCatalogue.Discover(productionAssembly))
            {
                descriptor.Permissions.Should().NotBeEmpty();
                descriptor.Permissions.Should().OnlyContain(p => AdvancedAnalyticsPermissions.All.Contains(p));
            }
        }

        [Fact]
        public void EveryProductionToolMethodIsCoveredByATest()
        {
            var covered = CoverageScanner.CoveredNames(typeof(ToolConventionTests).Assembly);
            var descriptors = AdvancedAnalyticsToolCatalogue.Discover(productionAssembly);

            AdvancedAnalyticsToolCatalogue.FindUncovered(descriptors, covered).Should().BeEmpty();
        }

        [Fact]
        public void PermissionNamesAreTheFourAgreedNames()
        {
            AdvancedAnalyticsPermissions.All.Should().BeEquivalentTo(
                "Advanced Analytics",
                "Advanced Analytics Training",
                "Advanced Analytics Recall",
                "Advanced Analytics Promote Model");
        }

        [Fact]
        public void BadFixtureTripsEveryConvention()
        {
            var violations = AdvancedAnalyticsToolCatalogue.Validate(Fixtures());

            violations.Should().Contain(v => v.Contains("MissingEverything") && v.Contains("description is missing"));
            violations.Should().Contain(v => v.Contains("MissingEverything") && v.Contains("no required permission"));
            violations.Should().Contain(v =>
                v.Contains("MissingEverything") && v.Contains("parameter 'values' has no description"));
            violations.Should().Contain(v =>
                v.Contains("MissingEverything") && v.Contains("parameter 'count' has no description"));
            violations.Should().Contain(v =>
                v.Contains("LibraryNamedAndUnknownPermission") && v.Contains("names the underlying library"));
            violations.Should().Contain(v =>
                v.Contains("LibraryNamedAndUnknownPermission") && v.Contains("unknown permission"));
            violations.Should()
                .Contain(v => v.Contains("ShortDescription") && v.Contains("tool description is shorter"));
            violations.Should().Contain(v =>
                v.Contains("ShortDescription") && v.Contains("parameter 'values' description is shorter"));
            violations.Should().Contain(v => v.Contains("EmptyName") && v.Contains("tool name is empty"));
            violations.Should().Contain(v => v.Contains("'FixtureDuplicate' is used by"));
        }

        [Fact]
        public void GoodFixtureHasNoViolationsAndUnionsGroupPermissions()
        {
            var good = Fixtures().Where(d => d.GroupType == typeof(FixtureGoodToolGroup)).ToList();

            good.Should().HaveCount(2);
            AdvancedAnalyticsToolCatalogue.Validate(good).Should().BeEmpty();

            good.Single(d => d.ToolName == "FixtureMean").Permissions.Should()
                .BeEquivalentTo(AdvancedAnalyticsPermissions.Analytics);

            good.Single(d => d.ToolName == "FixtureTrain").Permissions.Should()
                .BeEquivalentTo(AdvancedAnalyticsPermissions.Training, AdvancedAnalyticsPermissions.Analytics);
        }

        [Fact]
        public void CancellationTokenParametersNeedNoDescription()
        {
            var mean = Fixtures().Single(d => d.ToolName == "FixtureMean");

            mean.Method.GetParameters().Should().Contain(p => p.ParameterType == typeof(CancellationToken));
            AdvancedAnalyticsToolCatalogue.Validate([mean]).Should().BeEmpty();
        }

        [Fact]
        public void CoverageMetaTestFailsForAnUncoveredFixtureTool()
        {
            var covered = CoverageScanner.CoveredNames(typeof(ToolConventionTests).Assembly);

            var uncovered = AdvancedAnalyticsToolCatalogue.FindUncovered(Fixtures(), covered);

            uncovered.Should().Contain(u => u.EndsWith("FixtureBadToolGroup.MissingEverything"));
            uncovered.Should().NotContain(u => u.EndsWith("FixtureGoodToolGroup.Mean"));
            uncovered.Should().NotContain(u => u.EndsWith("FixtureGoodToolGroup.Train"));
        }

        [Fact]
        [Trait("Covers", "Mean")]
        public void MeanFixtureToolWorks()
        {
            new FixtureGoodToolGroup().Mean([1.0, 3.0]).Should().Be(2.0);
        }

        [Fact]
        [Trait("Covers", "FixtureGoodToolGroup.Train")]
        public void TrainFixtureToolWorks()
        {
            new FixtureGoodToolGroup().Train(3).Should().Be(3);
        }
    }
}