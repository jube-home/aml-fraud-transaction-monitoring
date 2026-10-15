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
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class RelationshipsAndRegressionSelectionTests
    {
        private readonly LinearRegressionTools linear = new();
        private readonly RegressionModelSelectionTools tools = new();

        private static (double[][] Rows, double[] Y, string[] Names) Signal()
        {
            var rows = Enumerable.Range(0, 60).Select(i => new[]
            {
                3 * Math.Sin(i * 1.1),
                2 * Math.Cos(i * 0.7) + Math.Sin(i * 0.3),
                Math.Sin(i * 2.3 + 1),
                Math.Cos(i * 3.1 + 2)
            }).ToArray();

            var y = rows.Select((r, i) => 5 + 2 * r[0] - 3 * r[1] + 0.4 * Math.Sin(i * 5.7)).ToArray();

            return (rows, y, ["signalA", "signalB", "noiseC", "noiseD"]);
        }

        private double Aic(double[][] rows, double[] y, string[] names, IEnumerable<int> subset)
        {
            var columns = subset.ToArray();

            if (columns.Length == 0)
            {
                return linear
                    .FitMultipleLinearRegression([.. rows.Select(_ => new[] { 1.0 })], y, ["c"], false)
                    .Aic;
            }

            return linear.FitMultipleLinearRegression([.. rows.Select(r => columns.Select(j => r[j]).ToArray())],
                y,
                [.. columns.Select(j => names[j])]).Aic;
        }

        [Fact]
        [Trait("Covers", "StepwiseLinearRegression")]
        public void EveryDirectionSelectsTheTrueSignalsUnderAic()
        {
            var (rows, y, names) = Signal();

            foreach (var direction in new[]
                         { StepwiseDirection.Forward, StepwiseDirection.Backward, StepwiseDirection.Both })
            {
                var result = tools.StepwiseLinearRegression(rows, y, names, direction);

                result.SelectedVariables.Should().Contain(["signalA", "signalB"]);
                result.Criterion.Should().Be("Aic");
                result.FinalModel.Coefficients.Select(c => c.Term).Should().Contain("signalA");

                result.FinalModel.Coefficients.First(c => c.Term == "signalA").Estimate.Should()
                    .BeApproximately(2, 0.1);

                result.FinalModel.Coefficients.First(c => c.Term == "signalB").Estimate.Should()
                    .BeApproximately(-3, 0.1);

                result.SelectedVariables.Concat(result.RemovedVariables).Should().BeEquivalentTo(names);
            }
        }

        [Fact]
        [Trait("Covers", "StepwiseLinearRegression")]
        public void TheSelectedModelIsALocalAicOptimumAndStepsAreRecorded()
        {
            var (rows, y, names) = Signal();
            var forward = tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Forward);

            var chosen = forward.SelectedVariables.Select(n => Array.IndexOf(names, n)).ToList();
            var chosenAic = Aic(rows, y, names, chosen);
            forward.FinalModel.Aic.Should().BeApproximately(chosenAic, 1e-8);

            foreach (var j in Enumerable.Range(0, 4).Except(chosen))
            {
                Aic(rows, y, names, chosen.Append(j)).Should().BeGreaterOrEqualTo(chosenAic - 1e-9);
            }

            foreach (var j in chosen)
            {
                Aic(rows, y, names, chosen.Where(v => v != j)).Should().BeGreaterOrEqualTo(chosenAic - 1e-9);
            }

            forward.Steps[0].Action.Should().Be("Add");
            forward.Steps[0].Variable.Should().Be("signalB");
            forward.Steps.Select(s => s.Step).Should().Equal(Enumerable.Range(1, forward.Steps.Length));
            forward.Steps.Last().ModelAfterStep.Should().BeEquivalentTo(forward.SelectedVariables);
            forward.Steps.Should().OnlyContain(s => s.PValue >= 0 && s.PValue <= 1);
        }

        [Fact]
        [Trait("Covers", "StepwiseLinearRegression")]
        public void PValueAndBicCriteriaWork()
        {
            var (rows, y, names) = Signal();

            var byP = tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Both,
                ModelSelectionCriterion.PValue);
            byP.SelectedVariables.Should().BeEquivalentTo("signalA", "signalB");
            byP.Steps.Where(s => s.Action == "Add").Should().OnlyContain(s => s.CriterionValue < 0.05);

            var byBic = tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Backward,
                ModelSelectionCriterion.Bic);
            byBic.SelectedVariables.Should().BeEquivalentTo("signalA", "signalB");
            byBic.Steps.Should().OnlyContain(s => s.Action == "Remove");
            byBic.Steps.Should().HaveCount(2);

            var pure = Enumerable.Range(0, 30).Select(i => new[] { Math.Sin(i * 1.7), Math.Cos(i * 2.9) }).ToArray();
            var flat = Enumerable.Range(0, 30).Select(i => 4 + 0.001 * Math.Sin(i * 13.1)).ToArray();

            var none = tools.StepwiseLinearRegression(pure, flat, null, StepwiseDirection.Forward,
                ModelSelectionCriterion.PValue, 0.0001, 0.05);
            none.SelectedVariables.Should().BeEmpty();
            none.FinalModel.Coefficients.Should().ContainSingle(c => c.Term == "(Intercept)");
            none.FinalModel.Coefficients[0].Estimate.Should().BeApproximately(flat.Average(), 1e-9);
            none.Steps.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "StepwiseLinearRegression")]
        public void StepwiseValidatesItsInputs()
        {
            var (rows, y, names) = Signal();

            RelationshipsFixtures.Rejects(() => tools.StepwiseLinearRegression(rows, y, names, (StepwiseDirection)9),
                "direction");

            RelationshipsFixtures.Rejects(
                () => tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Both,
                    ModelSelectionCriterion.MallowsCp), "criterion");

            RelationshipsFixtures.Rejects(
                () => tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Both,
                    ModelSelectionCriterion.PValue, 0.2), "entryPValue");

            RelationshipsFixtures.Rejects(() => tools.StepwiseLinearRegression(rows, [.. y.Take(3)], names),
                "outputs");

            RelationshipsFixtures.Rejects(
                () => tools.StepwiseLinearRegression([.. rows.Take(4)], [.. y.Take(4)], names), "inputs");

            var collinear = rows.Select(r => new[] { r[0], r[1], r[0] + r[1] }).ToArray();

            RelationshipsFixtures.Rejects(
                () => tools.StepwiseLinearRegression(collinear, y, null, StepwiseDirection.Backward), "inputs");

            tools.StepwiseLinearRegression(collinear, y, null, StepwiseDirection.Forward).SelectedVariables.Should()
                .HaveCount(2);

            RelationshipsFixtures.Rejects(
                () => tools.StepwiseLinearRegression(rows, y, names, StepwiseDirection.Both,
                    ModelSelectionCriterion.Aic, 0.05, 0.1, 2), "confidenceLevel");
        }

        [Fact]
        [Trait("Covers", "BestSubsetLinearRegression")]
        public void BestSubsetMatchesABruteForceOverIndependentFits()
        {
            var (rows, y, names) = Signal();
            var three = rows.Select(r => new[] { r[0], r[1], r[2] }).ToArray();
            var threeNames = names.Take(3).ToArray();

            var result = tools.BestSubsetLinearRegression(three, y, threeNames);

            result.ModelsEvaluated.Should().Be(7);
            result.ModelsSkippedAsCollinear.Should().Be(0);

            var expected = new[]
                {
                    new[] { 0 }, new[] { 1 }, new[] { 2 }, new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 2 },
                    new[] { 0, 1, 2 }
                }
                .Select(s => (Set: s,
                    Fit: linear.FitMultipleLinearRegression([.. three.Select(r => s.Select(j => r[j]).ToArray())],
                        y, [.. s.Select(j => threeNames[j])])))
                .OrderBy(t => t.Fit.Bic).ToArray();

            result.TopModels[0].Variables.Should().Equal(expected[0].Set.Select(j => threeNames[j]));
            result.TopModels[0].Bic.Should().BeApproximately(expected[0].Fit.Bic, 1e-8);
            result.TopModels.Select(m => m.Bic).Should().BeInAscendingOrder();
            result.BestPerSize.Should().HaveCount(3);
            result.BestPerSize.Select(m => m.Size).Should().Equal(1, 2, 3);
            var full = result.BestPerSize[2];
            full.MallowsCp.Should().BeApproximately(4, 1e-8);
            full.RSquared.Should().BeApproximately(linear.FitMultipleLinearRegression(three, y).RSquared, 1e-12);
            result.Criterion.Should().Be("Bic");
        }

        [Fact]
        [Trait("Covers", "BestSubsetLinearRegression")]
        public void BestSubsetSupportsEveryCriterionAndValidates()
        {
            var (rows, y, names) = Signal();

            foreach (var criterion in new[]
                     {
                         ModelSelectionCriterion.Aic, ModelSelectionCriterion.AdjustedRSquared,
                         ModelSelectionCriterion.MallowsCp
                     })
            {
                var result = tools.BestSubsetLinearRegression(rows, y, names, criterion, 0, 3);
                result.TopModels.Should().HaveCount(3);
                result.TopModels[0].Variables.Should().Contain(["signalA", "signalB"]);
                result.ModelsEvaluated.Should().Be(15);
            }

            tools.BestSubsetLinearRegression(rows, y, names, ModelSelectionCriterion.Aic, 2).ModelsEvaluated.Should()
                .Be(10);

            RelationshipsFixtures.Rejects(
                () => tools.BestSubsetLinearRegression(rows, y, names, ModelSelectionCriterion.PValue), "criterion");

            RelationshipsFixtures.Rejects(
                () => tools.BestSubsetLinearRegression(rows, y, names, ModelSelectionCriterion.Aic, 9),
                "maximumSubsetSize");

            RelationshipsFixtures.Rejects(
                () => tools.BestSubsetLinearRegression(rows, y, names, ModelSelectionCriterion.Aic, 0, 0), "topModels");

            var wide = rows.Select(r => Enumerable.Range(0, 16).Select(j => r[j % 4] + j).ToArray()).ToArray();
            RelationshipsFixtures.Rejects(() => tools.BestSubsetLinearRegression(wide, y), "inputs");
            var collinear = rows.Select(r => new[] { r[0], r[1], r[0] - r[1] }).ToArray();
            RelationshipsFixtures.Rejects(() => tools.BestSubsetLinearRegression(collinear, y), "inputs");
        }

        [Fact]
        [Trait("Covers", "CompareModelsByInformationCriteria")]
        public void InformationCriteriaMatchTheClosedForms()
        {
            var result = tools.CompareModelsByInformationCriteria(["small", "medium", "large"],
                [-100.0, -95, -94.5], [2, 4, 5], 50);

            result.Models[0].Aic.Should().BeApproximately(204, 1e-12);
            result.Models[1].Aic.Should().BeApproximately(198, 1e-12);
            result.Models[2].Aic.Should().BeApproximately(199, 1e-12);
            result.Models[0].Bic.Should().BeApproximately(2 * Math.Log(50) + 200, 1e-12);
            result.Models[1].Aicc.Should().BeApproximately(198 + 2d * 4 * 5 / (50 - 4 - 1), 1e-12);
            result.Models[0].DeltaAic.Should().BeApproximately(6, 1e-12);
            result.Models[1].DeltaAic.Should().Be(0);
            var sum = Math.Exp(-3) + 1 + Math.Exp(-0.5);
            result.Models[0].AkaikeWeight.Should().BeApproximately(Math.Exp(-3) / sum, 1e-12);
            result.Models[1].AkaikeWeight.Should().BeApproximately(1 / sum, 1e-12);
            result.Models.Sum(m => m.BicWeight).Should().BeApproximately(1, 1e-12);
            result.BestByAic.Should().Be("medium");
            result.BestByBic.Should().Be("medium");
        }

        [Fact]
        [Trait("Covers", "CompareModelsByInformationCriteria")]
        public void InformationCriteriaValidateAndLinkToARegressionFit()
        {
            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria([], [1.0], [1], 10),
                "modelNames");

            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria(["a", "b"], [1.0], [1, 2], 10),
                "parameterCounts");

            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria(["a", "a"], [1.0, 2], [1, 2],
                    10), "modelNames");

            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria(["a"], [1.0], [0], 10),
                "parameterCounts");

            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria(["a"], [double.NaN], [1], 10),
                "logLikelihoods");

            RelationshipsFixtures.Rejects(
                () => tools.CompareModelsByInformationCriteria(["a"], [1.0], [1], 1),
                "observations");

            double.IsNaN(tools.CompareModelsByInformationCriteria(["a"], [-5.0], [3], 4)
                .Models[0].Aicc).Should().BeTrue();

            var fit = linear.FitSimpleLinearRegression(RelationshipsFixtures.AnscombeOneX,
                RelationshipsFixtures.AnscombeOneY);

            var compared = tools.CompareModelsByInformationCriteria(["line"], [fit.LogLikelihood],
                [3], fit.Observations);

            compared.Models[0].Aic.Should().BeApproximately(fit.Aic, 1e-9);
            compared.Models[0].Bic.Should().BeApproximately(fit.Bic, 1e-9);
            compared.Models[0].Aicc.Should().BeApproximately(fit.Aicc, 1e-9);
            compared.Models[0].AkaikeWeight.Should().Be(1);
        }
    }
}