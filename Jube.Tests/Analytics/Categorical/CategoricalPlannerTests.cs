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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Analytics.Categorical;
using Jube.Analytics.Categorical.Models;
using Xunit;

namespace Jube.Test.Analytics.Categorical
{
    using static CategoricalTestData;

    public class CategoricalPlannerTests
    {
        private static CategoricalFieldCandidate Candidate(string name, CategoricalCounter counter,
            bool declaredString = true, bool declaredOther = false)
        {
            return new CategoricalFieldCandidate(name, counter, declaredString, declaredOther);
        }

        [Fact]
        public void DeclaredStringIsCategorical()
        {
            var plan = CategoricalPlanner.Fit(
                [Candidate("Payload.Country", Count(("GB", 100), ("US", 50)))], Options());

            plan.Encoders.Should().ContainSingle();
            plan.Report.Single().Decision.Should().Be(CategoricalDecision.Categorical);
        }

        [Fact]
        public void DeclaredNumericIsNotCategoricalUnlessIncluded()
        {
            var counter = Count(("5411", 100), ("5812", 50));
            var not = CategoricalPlanner.Fit([Candidate("Payload.Mcc", counter, false, true)], Options());
            not.Encoders.Should().BeEmpty();
            not.Report.Single().Decision.Should().Be(CategoricalDecision.NotCategorical);

            var options = Options();
            options.Include.Add("Payload.Mcc");
            var included = CategoricalPlanner.Fit([Candidate("Payload.Mcc", counter, false, true)], options);
            included.Encoders.Should().ContainSingle();
        }

        [Fact]
        public void UndeclaredTypeIsInferredFromTheSample()
        {
            var words = Count(("GB", 100), ("US", 50));
            var numbers = Count(("1.5", 100), ("2", 50));
            var mixed = Count(("GB", 60), ("1", 90));

            var plan = CategoricalPlanner.Fit(
            [
                Candidate("Payload.A", words, false), Candidate("Payload.B", numbers, false),
                Candidate("Payload.C", mixed, false)
            ], Options());

            plan.Encoders.Select(s => s.SourceName).Should().Equal("Payload.A");

            plan.Report.Single(s => s.SourceName == "Payload.B").Decision.Should()
                .Be(CategoricalDecision.NotCategorical);

            plan.Report.Single(s => s.SourceName == "Payload.C").Reason.Should().Contain("numeric");
        }

        [Fact]
        public void IdentifierLikeFieldsAreSkippedAndReported()
        {
            var counter = new CategoricalCounter();

            for (var i = 0; i < 1000; i++)
            {
                counter.Add("ref" + i);
            }

            var plan = CategoricalPlanner.Fit([Candidate("Payload.Reference", counter)], Options());

            plan.Encoders.Should().BeEmpty();
            plan.Report.Single().Decision.Should().Be(CategoricalDecision.SkippedIdentifier);
        }

        [Fact]
        public void ExcludedFieldsAreSkipped()
        {
            var options = Options();
            options.Exclude.Add("Payload.Country");

            var plan = CategoricalPlanner.Fit([Candidate("Payload.Country", Count(("GB", 100), ("US", 50)))], options);

            plan.Encoders.Should().BeEmpty();
            plan.Report.Single().Decision.Should().Be(CategoricalDecision.SkippedExcluded);
        }

        [Fact]
        public void ConstantAndEmptyFieldsAreSkipped()
        {
            var plan = CategoricalPlanner.Fit(
                [Candidate("Payload.A", Count(("GB", 100))), Candidate("Payload.B", Count((null, 100)))], Options());

            plan.Encoders.Should().BeEmpty();

            plan.Report.Select(s => s.Decision).Should().BeEquivalentTo([
                CategoricalDecision.SkippedConstant, CategoricalDecision.SkippedNoValues
            ]);
        }

        [Fact]
        public void TotalIndicatorBudgetRemovesTheLeastFrequentLevelsAcrossFields()
        {
            var a = Count(("A1", 500), ("A2", 400), ("A3", 300), ("A4", 200), ("A5", 100));
            var b = Count(("B1", 500), ("B2", 50), ("B3", 45), ("B4", 40), ("B5", 35));
            var options = Options();
            options.MaxTotalIndicators = 6;

            var plan = CategoricalPlanner.Fit([Candidate("Payload.A", a), Candidate("Payload.B", b)], options);

            plan.IndicatorCount.Should().BeLessThanOrEqualTo(6);
            var fieldA = plan.Encoders.Single(s => s.SourceName == "Payload.A");
            var fieldB = plan.Encoders.Single(s => s.SourceName == "Payload.B");
            fieldA.Levels.Count.Should().BeGreaterThan(fieldB.Levels.Count);
        }

        [Fact]
        public void TotalIndicatorBudgetIsDeterministic()
        {
            List<CategoricalFieldCandidate> Build()
            {
                var list = new List<CategoricalFieldCandidate>();

                for (var f = 0; f < 6; f++)
                {
                    var counter = new CategoricalCounter();

                    for (var i = 0; i < 2000; i++)
                    {
                        counter.Add($"v{i * (f + 3) % 30}");
                    }

                    list.Add(Candidate($"Payload.F{f}", counter));
                }

                return list;
            }

            var options = Options();
            options.MaxTotalIndicators = 40;

            var first = CategoricalPlanner.Fit(Build(), options);
            var second = CategoricalPlanner.Fit(Build(), options);

            first.IndicatorCount.Should().BeLessThanOrEqualTo(40);
            first.Encoders.Select(s => s.ToJson()).Should().Equal(second.Encoders.Select(s => s.ToJson()));
        }

        [Fact]
        public void OptionsRoundTripAndSanitise()
        {
            var options = CategoricalOptions.FromJson(
                "{\"enabled\":true,\"maxLevels\":-4,\"minLevelCount\":0,\"coverageTarget\":7,\"exclude\":[\" a \",\"\"]}");

            options.Enabled.Should().BeTrue();
            options.MaxLevels.Should().Be(1);
            options.MinLevelCount.Should().Be(1);
            options.CoverageTarget.Should().Be(1);
            options.Exclude.Should().Equal("a");
            CategoricalOptions.FromJson(options.ToJson()).ToJson().Should().Be(options.ToJson());
            CategoricalOptions.FromJson(null).Enabled.Should().BeFalse();
            CategoricalOptions.FromJson("garbage").Enabled.Should().BeFalse();
        }
    }
}