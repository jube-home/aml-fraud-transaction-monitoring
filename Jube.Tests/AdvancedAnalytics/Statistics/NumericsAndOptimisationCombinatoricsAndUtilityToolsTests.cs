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
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.NumericsAndOptimisation;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class NumericsAndOptimisationCombinatoricsAndUtilityToolsTests
    {
        private readonly CombinatoricsAndUtilityTools tools = new();

        private static string Key(double[] row)
        {
            return string.Join(",", row.Select(v => v.ToString("R")));
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.EnumeratePermutations")]
        public void EnumeratePermutationsListsEveryOrdering()
        {
            var result = tools.EnumeratePermutations([1.0, 2.0, 3.0]);
            result.TotalCount.Should().Be(6.0);
            result.Returned.Should().Be(6);
            result.Truncated.Should().BeFalse();

            result.Items.Select(Key).Distinct().Should()
                .BeEquivalentTo("1,2,3", "1,3,2", "2,1,3", "2,3,1", "3,1,2", "3,2,1");

            var truncated = tools.EnumeratePermutations([1.0, 2.0, 3.0, 4.0], 5);
            truncated.Returned.Should().Be(5);
            truncated.Truncated.Should().BeTrue();
            truncated.TotalCount.Should().Be(24.0);

            tools.EnumeratePermutations([9.0]).Items.Should().HaveCount(1);
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumeratePermutations(new double[11]), "values");
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumeratePermutations([1.0], 0), "maxResults");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.EnumerateCombinations")]
        public void EnumerateCombinationsListsEverySelection()
        {
            var result = tools.EnumerateCombinations([1.0, 2.0, 3.0, 4.0], 2);
            result.TotalCount.Should().Be(6.0);

            result.Items.Select(r => Key([.. r.OrderBy(v => v)])).Should()
                .BeEquivalentTo("1,2", "1,3", "1,4", "2,3", "2,4", "3,4");

            tools.EnumerateCombinations([1.0, 2.0, 3.0, 4.0], 4).Items.Should().HaveCount(1);
            var cut = tools.EnumerateCombinations([.. Enumerable.Range(1, 25).Select(i => (double)i)], 12, 100);
            cut.Truncated.Should().BeTrue();
            cut.Returned.Should().Be(100);
            cut.TotalCount.Should().Be(5200300.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumerateCombinations([1.0, 2.0], 3), "k");
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumerateCombinations(new double[26], 2), "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.EnumerateSubsets")]
        public void EnumerateSubsetsListsPowerSet()
        {
            var result = tools.EnumerateSubsets([1.0, 2.0, 3.0]);
            result.TotalCount.Should().Be(7.0);
            result.Returned.Should().Be(7);
            result.Items.Select(r => r.Length).OrderBy(v => v).Should().Equal(1, 1, 1, 2, 2, 2, 3);

            tools.EnumerateSubsets([.. Enumerable.Range(1, 20).Select(i => (double)i)], 50).Truncated.Should()
                .BeTrue();

            NumericsTestSupport.ShouldRejectWith(() => tools.EnumerateSubsets(new double[21]), "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.EnumerateSequences")]
        public void EnumerateSequencesListsCartesianPower()
        {
            var result = tools.EnumerateSequences(2, 3);
            result.TotalCount.Should().Be(8.0);

            result.Items.Select(r => string.Join("", r)).Should()
                .BeEquivalentTo("000", "001", "010", "011", "100", "101", "110", "111");

            var ternary = tools.EnumerateSequences(3, 2);
            ternary.Items.Should().HaveCount(9);
            tools.EnumerateSequences(10, 6, 10).Truncated.Should().BeTrue();
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumerateSequences(1, 3), "symbols");
            NumericsTestSupport.ShouldRejectWith(() => tools.EnumerateSequences(2, 13), "length");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GenerateTruthTable")]
        public void GenerateTruthTableEnumeratesAllBooleanCombinations()
        {
            var table = tools.GenerateTruthTable(2);
            table.Items.Select(r => string.Join("", r)).Should().BeEquivalentTo("00", "01", "10", "11");
            tools.GenerateTruthTable(4).Items.Should().HaveCount(16);
            tools.GenerateTruthTable(3).Items.Select(r => string.Join("", r)).Distinct().Should().HaveCount(8);
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateTruthTable(0), "inputs");
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateTruthTable(15), "inputs");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.CountCombinatorialObjects")]
        public void CountCombinatorialObjectsKnownCounts()
        {
            tools.CountCombinatorialObjects(NumericsCountKind.Permutations, 5).Count.Should().Be(120.0);
            tools.CountCombinatorialObjects(NumericsCountKind.PartialPermutations, 10, 3).Count.Should().Be(720.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 52, 5).Count.Should().Be(2598960.0);
            tools.CountCombinatorialObjects(NumericsCountKind.CombinationsWithRepetition, 3, 2).Count.Should().Be(6.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Sequences, 2, 10).Count.Should().Be(1024.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 4, 5).Count.Should().Be(0.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Permutations, 0).Count.Should().Be(1.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 10).Count.Should().Be(1.0);
            tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 52, 5).IsExact.Should().BeTrue();

            var huge = tools.CountCombinatorialObjects(NumericsCountKind.Permutations, 1000);
            huge.Count.Should().Be(double.PositiveInfinity);
            huge.NaturalLogOfCount.Should().BeApproximately(5912.128178488163, 1e-6);
            huge.IsExact.Should().BeFalse();

            var large = tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 100, 50);
            (large.Count / 1.0089134454556419e29).Should().BeApproximately(1.0, 1e-9);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.CountCombinatorialObjects(NumericsCountKind.Combinations, -1), "n");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.CountCombinatorialObjects(NumericsCountKind.Combinations, 5, 1000001), "k");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GreatestCommonDivisorAndLeastCommonMultiple")]
        public void GreatestCommonDivisorAndLeastCommonMultipleKnownValues()
        {
            var result =
                tools.GreatestCommonDivisorAndLeastCommonMultiple([12, 17, 0, -12, 21],
                    [18, 5, 9, 18, 6]);

            result.Values.Take(5).Should().Equal(6.0, 1.0, 9.0, 6.0, 3.0);
            result.Values.Skip(5).Should().Equal(36.0, 85.0, 0.0, 36.0, 42.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.GreatestCommonDivisorAndLeastCommonMultiple([1], [1, 2]), "b");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.GreatestCommonDivisorAndLeastCommonMultiple([2000000000], [1]), "a");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.EuclideanModulo")]
        public void EuclideanModuloWrapsIntoRange()
        {
            NumericsTestSupport.VectorShouldMatch(
                tools.EuclideanModulo([-1.0, 25.0, 24.0, 370.5, -0.5], 24.0).Values,
                [23.0, 1.0, 0.0, 10.5, 23.5], 1e-12);

            NumericsTestSupport.ShouldRejectWith(() => tools.EuclideanModulo([1.0], 0.0), "modulus");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.PowerOfTwoProperties")]
        public void PowerOfTwoPropertiesKnownValues()
        {
            var values = tools.PowerOfTwoProperties([1, 5, 8, 1000]).Values;
            values.Take(4).Should().Equal(1.0, 8.0, 8.0, 1024.0);
            values.Skip(4).Take(4).Should().Equal(1.0, 4.0, 8.0, 512.0);
            values.Skip(8).Take(4).Should().Equal(0.0, 3.0, 3.0, 10.0);
            values.Skip(12).Should().Equal(1.0, 0.0, 1.0, 0.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.PowerOfTwoProperties([0]), "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.FindIntegerByBinarySearch")]
        public void FindIntegerByBinarySearchFindsSquareRoot()
        {
            var result = tools.FindIntegerByBinarySearch(NumericsUnivariateFunction.Polynomial,
                [0.0, 0.0, 1.0], 0, 100, 49.0);
            result.Solution.Should().Be(7.0);
            result.FunctionValue.Should().Be(49.0);

            tools.FindIntegerByBinarySearch(NumericsUnivariateFunction.Polynomial, [100.0, -1.0], 0, 200, 60.0)
                .Solution.Should().Be(40.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.FindIntegerByBinarySearch(NumericsUnivariateFunction.Polynomial, [0.0, 1.0], 5, 5,
                    3.0), "upper");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.SortValuesWithOriginalIndices")]
        public void SortValuesWithOriginalIndicesIsStableAndReportsOrigin()
        {
            var ascending = tools.SortValuesWithOriginalIndices([3.0, 1.0, 2.0, 1.0]);
            ascending.Values.Should().Equal(1.0, 1.0, 2.0, 3.0);
            ascending.OriginalIndices.Should().Equal(1, 3, 2, 0);
            var descending = tools.SortValuesWithOriginalIndices([3.0, 1.0, 2.0, 3.0], true);
            descending.Values.Should().Equal(3.0, 3.0, 2.0, 1.0);
            descending.OriginalIndices.Should().Equal(0, 3, 2, 1);
            tools.SortValuesWithOriginalIndices([5.0]).Values.Should().Equal(5.0);

            NumericsTestSupport.ShouldRejectWith(() => tools.SortValuesWithOriginalIndices([]),
                "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.SortValuesByKeys")]
        public void SortValuesByKeysReordersInParallel()
        {
            tools.SortValuesByKeys([3.0, 1.0, 2.0, 1.0], [30.0, 10.0, 20.0, 11.0]).Values.Should()
                .Equal(10.0, 11.0, 20.0, 30.0);

            NumericsTestSupport.ShouldRejectWith(() => tools.SortValuesByKeys([1.0], [1.0, 2.0]),
                "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.SelectNthSmallestValue")]
        public void SelectNthSmallestValueKnownRanks()
        {
            var values = new[] { 9.0, 4.0, 7.0, 1.0, 8.0 };
            tools.SelectNthSmallestValue(values, 0).Should().Be(1.0);
            tools.SelectNthSmallestValue(values, 2).Should().Be(7.0);
            tools.SelectNthSmallestValue(values, 4).Should().Be(9.0);
            tools.SelectNthSmallestValue(values, 0, true).Should().Be(9.0);
            values.Should().Equal(9.0, 4.0, 7.0, 1.0, 8.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.SelectNthSmallestValue(values, 5), "n");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.SelectSmallestValuesSorted")]
        public void SelectSmallestValuesSortedKnownTopN()
        {
            var values = new[] { 9.0, 4.0, 7.0, 1.0, 8.0, 3.0 };
            tools.SelectSmallestValuesSorted(values, 3).Values.Should().Equal(1.0, 3.0, 4.0);
            tools.SelectSmallestValuesSorted(values, 2, true).Values.Should().Equal(9.0, 8.0);
            tools.SelectSmallestValuesSorted(values, 6).Values.Should().Equal(1.0, 3.0, 4.0, 7.0, 8.0, 9.0);
            NumericsTestSupport.ShouldRejectWith(() => tools.SelectSmallestValuesSorted(values, 7), "count");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GenerateUniformRandomNumbers")]
        public void GenerateUniformRandomNumbersIsReproducibleAndInRange()
        {
            var first = tools.GenerateUniformRandomNumbers(10000, 2.0, 5.0, 7).Values;
            var second = tools.GenerateUniformRandomNumbers(10000, 2.0, 5.0, 7).Values;
            first.Should().Equal(second);
            first.Should().OnlyContain(v => v >= 2.0 && v <= 5.0);
            first.Average().Should().BeApproximately(3.5, 5.0 * Math.Sqrt(9.0 / 12.0 / 10000.0));

            tools.GenerateUniformRandomNumbers(10, 2.0, 5.0, 8).Values.Should()
                .NotEqual(tools.GenerateUniformRandomNumbers(10, 2.0, 5.0, 7).Values);

            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateUniformRandomNumbers(10, 5.0, 2.0), "maximum");
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateUniformRandomNumbers(1000001, 0.0, 1.0), "count");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GenerateNormalRandomNumbers")]
        public void GenerateNormalRandomNumbersHaveRequestedMoments()
        {
            var values = tools.GenerateNormalRandomNumbers(200000, 10.0, 2.0, 3).Values;
            var mean = values.Average();
            var deviation = Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
            mean.Should().BeApproximately(10.0, 5.0 * 2.0 / Math.Sqrt(200000.0));
            deviation.Should().BeApproximately(2.0, 0.02);

            tools.GenerateNormalRandomNumbers(100, 0, 1, 3).Values.Should()
                .Equal(tools.GenerateNormalRandomNumbers(100, 0, 1, 3).Values);

            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateNormalRandomNumbers(10, 0.0, 0.0),
                "standardDeviation");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GenerateExponentialRandomNumbers")]
        public void GenerateExponentialRandomNumbersHaveRequestedMean()
        {
            var values = tools.GenerateExponentialRandomNumbers(200000, 4.0, 5).Values;
            values.Should().OnlyContain(v => v >= 0.0);
            values.Average().Should().BeApproximately(0.25, 5.0 * 0.25 / Math.Sqrt(200000.0));
            NumericsTestSupport.ShouldRejectWith(() => tools.GenerateExponentialRandomNumbers(10, -1.0), "rate");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.MonteCarloExpectationOfNamedFunction")]
        public void MonteCarloExpectationOfNamedFunctionMatchesClosedForms()
        {
            var uniform = tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Uniform,
                [0.0, 1.0], NumericsUnivariateFunction.Polynomial, [0.0, 0.0, 1.0], 200000, 11);

            Math.Abs(uniform.Estimate - 1.0 / 3.0).Should().BeLessThan(5.0 * uniform.StandardError);

            uniform.StandardError.Should()
                .BeApproximately(Math.Sqrt(1.0 / 5.0 - 1.0 / 9.0) / Math.Sqrt(200000.0), 3e-4);

            uniform.ConfidenceLower95.Should().BeLessThan(uniform.Estimate);
            uniform.ConfidenceUpper95.Should().BeGreaterThan(uniform.Estimate);
            uniform.Samples.Should().Be(200000);

            var normal = tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Normal,
                [1.0, 2.0], NumericsUnivariateFunction.Polynomial, [0.0, 0.0, 1.0], 300000, 12);

            Math.Abs(normal.Estimate - 5.0).Should().BeLessThan(5.0 * normal.StandardError);

            var exponential = tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Exponential,
                [2.0], NumericsUnivariateFunction.Polynomial, [0.0, 1.0], 200000, 13);

            Math.Abs(exponential.Estimate - 0.5).Should().BeLessThan(5.0 * exponential.StandardError);

            var again = tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Uniform,
                [0.0, 1.0], NumericsUnivariateFunction.Polynomial, [0.0, 0.0, 1.0], 200000, 11);

            again.Estimate.Should().Be(uniform.Estimate);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Uniform,
                    [1.0, 0.0], NumericsUnivariateFunction.Sine, null), "distributionParameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Normal, [0.0],
                    NumericsUnivariateFunction.Sine, null), "distributionParameters");

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MonteCarloExpectationOfNamedFunction(NumericsSamplingDistribution.Uniform,
                    [0.0, 1.0], NumericsUnivariateFunction.Sine, null, 5), "samples");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.MonteCarloProbabilityOfNamedFunction")]
        public void MonteCarloProbabilityOfNamedFunctionMatchesNormalTables()
        {
            var within = tools.MonteCarloProbabilityOfNamedFunction(NumericsSamplingDistribution.Normal,
                [0.0, 1.0], NumericsUnivariateFunction.Polynomial, [0.0, 1.0], -1.96, 1.96, 400000, 21);

            Math.Abs(within.Estimate - 0.950004209703559).Should().BeLessThan(5.0 * within.StandardError);
            within.ConfidenceLower95.Should().BeGreaterThan(0.0);
            within.ConfidenceUpper95.Should().BeLessThan(1.0);

            var upperTail = tools.MonteCarloProbabilityOfNamedFunction(NumericsSamplingDistribution.Normal,
                [0.0, 1.0], NumericsUnivariateFunction.Polynomial, [0.0, 1.0], 1.0, null, 400000, 22);

            Math.Abs(upperTail.Estimate - 0.15865525393145707).Should().BeLessThan(5.0 * upperTail.StandardError);

            var exponential = tools.MonteCarloProbabilityOfNamedFunction(NumericsSamplingDistribution.Exponential,
                [1.0], NumericsUnivariateFunction.Polynomial, [0.0, 1.0], null, 1.0, 400000, 23);

            Math.Abs(exponential.Estimate - (1.0 - Math.Exp(-1.0))).Should()
                .BeLessThan(5.0 * exponential.StandardError);

            var certain = tools.MonteCarloProbabilityOfNamedFunction(NumericsSamplingDistribution.Uniform,
                [0.0, 1.0], NumericsUnivariateFunction.Polynomial, [0.0, 1.0]);
            certain.Estimate.Should().Be(1.0);
            certain.StandardError.Should().Be(0.0);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.MonteCarloProbabilityOfNamedFunction(NumericsSamplingDistribution.Uniform,
                    [0.0, 1.0], NumericsUnivariateFunction.Sine, null, 2.0, 1.0), "lower");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.GetMathematicalConstants")]
        public void GetMathematicalConstantsHasKnownValues()
        {
            var constants = tools.GetMathematicalConstants().ToDictionary(c => c.Name, c => c.Value);
            constants["Pi"].Should().Be(Math.PI);
            constants["E"].Should().Be(Math.E);
            constants["EulerGamma"].Should().BeApproximately(0.5772156649015329, 1e-15);
            constants["Catalan"].Should().BeApproximately(0.915965594177219, 1e-15);
            constants["Sqrt2"].Should().BeApproximately(Math.Sqrt(2.0), 1e-15);
            constants["Sqrt2Pi"].Should().BeApproximately(Math.Sqrt(2.0 * Math.PI), 1e-14);
            constants["Log2"].Should().BeApproximately(Math.Log(2.0), 1e-15);
            constants["LogSqrt2Pi"].Should().BeApproximately(0.5 * Math.Log(2.0 * Math.PI), 1e-15);
            constants["DoubleEpsilon"].Should().BeApproximately(1.1102230246251565e-16, 1e-30);
            tools.GetMathematicalConstants().Should().OnlyContain(c => c.Meaning.Length > 10);
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsFastFourierTransform")]
        public void NumericsFastFourierTransformKnownTransformAndRoundTrip()
        {
            var forward = tools.NumericsFastFourierTransform([1.0, 2.0, 3.0, 4.0]);
            NumericsTestSupport.VectorShouldMatch(forward.Real, [10.0, -2.0, -2.0, -2.0], 1e-10);
            NumericsTestSupport.VectorShouldMatch(forward.Imaginary, [0.0, 2.0, 0.0, -2.0], 1e-10);
            forward.Magnitude[1].Should().BeApproximately(Math.Sqrt(8.0), 1e-10);

            var back = tools.NumericsFastFourierTransform(forward.Real, forward.Imaginary,
                NumericsTransformDirection.Inverse);

            NumericsTestSupport.VectorShouldMatch(back.Real, [1.0, 2.0, 3.0, 4.0], 1e-10);
            NumericsTestSupport.VectorShouldMatch(back.Imaginary, [0.0, 0.0, 0.0, 0.0], 1e-10);

            var cosine = Enumerable.Range(0, 8).Select(n => Math.Cos(2.0 * Math.PI * 2.0 * n / 8.0)).ToArray();
            var spectrum = tools.NumericsFastFourierTransform(cosine);
            spectrum.Magnitude[2].Should().BeApproximately(4.0, 1e-9);
            spectrum.Magnitude[6].Should().BeApproximately(4.0, 1e-9);
            spectrum.Magnitude[1].Should().BeApproximately(0.0, 1e-9);

            var odd = tools.NumericsFastFourierTransform([1.0, 1.0, 1.0]);
            NumericsTestSupport.VectorShouldMatch(odd.Real, [3.0, 0.0, 0.0], 1e-9);

            NumericsTestSupport.ShouldRejectWith(
                () => tools.NumericsFastFourierTransform([1.0, 2.0], [1.0]), "imaginary");

            NumericsTestSupport.ShouldRejectWith(() => tools.NumericsFastFourierTransform([]),
                "real");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsDiscreteCosineTransform")]
        public void NumericsDiscreteCosineTransformKnownValuesAndRoundTrip()
        {
            var constant = tools.NumericsDiscreteCosineTransform([1.0, 1.0, 1.0, 1.0]).Values;
            constant[0].Should().BeApproximately(2.0, 1e-10);
            constant.Skip(1).Should().OnlyContain(v => Math.Abs(v) < 1e-10);
            var signal = new[] { 4.0, -2.0, 7.0, 1.0, 0.5 };
            var forward = tools.NumericsDiscreteCosineTransform(signal).Values;

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericsDiscreteCosineTransform(forward, NumericsTransformDirection.Inverse).Values, signal,
                1e-10);

            forward.Sum(v => v * v).Should().BeApproximately(signal.Sum(v => v * v), 1e-9);
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsDiscreteSineTransform")]
        public void NumericsDiscreteSineTransformRoundTrip()
        {
            var signal = new[] { 4.0, -2.0, 7.0, 1.0, 0.5, 3.0 };
            var forward = tools.NumericsDiscreteSineTransform(signal).Values;
            forward.Should().NotEqual(signal);

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericsDiscreteSineTransform(forward, NumericsTransformDirection.Inverse).Values, signal, 1e-9);

            NumericsTestSupport.ShouldRejectWith(() => tools.NumericsDiscreteSineTransform([]),
                "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsHartleyTransform")]
        public void NumericsHartleyTransformKnownValuesAndSelfInverse()
        {
            NumericsTestSupport.VectorShouldMatch(tools.NumericsHartleyTransform([1.0, 0.0, 0.0, 0.0]).Values,
                [0.5, 0.5, 0.5, 0.5], 1e-10);

            var signal = new[] { 4.0, -2.0, 7.0, 1.0, 0.5 };
            var twice = tools.NumericsHartleyTransform(tools.NumericsHartleyTransform(signal).Values).Values;
            NumericsTestSupport.VectorShouldMatch(twice, signal, 1e-9);
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsHilbertTransform")]
        public void NumericsHilbertTransformOfCosineIsSine()
        {
            var n = 16;
            var cosine = Enumerable.Range(0, n).Select(i => Math.Cos(2.0 * Math.PI * 3.0 * i / n)).ToArray();
            var sine = Enumerable.Range(0, n).Select(i => Math.Sin(2.0 * Math.PI * 3.0 * i / n)).ToArray();
            NumericsTestSupport.VectorShouldMatch(tools.NumericsHilbertTransform(cosine).Values, sine, 1e-9);
            NumericsTestSupport.ShouldRejectWith(() => tools.NumericsHilbertTransform(new double[12]), "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsHaarWaveletTransform")]
        public void NumericsHaarWaveletTransformKnownValuesAndRoundTrip()
        {
            var forward = tools.NumericsHaarWaveletTransform([4.0, 2.0, 6.0, 6.0]).Values;
            NumericsTestSupport.VectorShouldMatch(forward, [3.0, 6.0, 1.0, 0.0], 1e-10);

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericsHaarWaveletTransform(forward, NumericsTransformDirection.Inverse).Values,
                [4.0, 2.0, 6.0, 6.0], 1e-10);

            NumericsTestSupport.ShouldRejectWith(() => tools.NumericsHaarWaveletTransform([1.0, 2.0, 3.0]),
                "values");
        }

        [Fact]
        [Trait("Covers", "CombinatoricsAndUtilityTools.NumericsCdf97WaveletTransform")]
        public void NumericsCdf97WaveletTransformRoundTripAndSmoothness()
        {
            var signal = Enumerable.Range(0, 16).Select(i => Math.Sin(i / 4.0) * 10.0).ToArray();
            var forward = tools.NumericsCdf97WaveletTransform(signal).Values;

            NumericsTestSupport.VectorShouldMatch(
                tools.NumericsCdf97WaveletTransform(forward, NumericsTransformDirection.Inverse).Values, signal, 1e-8);

            forward.Skip(8).Select(Math.Abs).Max().Should().BeLessThan(forward.Take(8).Select(Math.Abs).Max());
            NumericsTestSupport.ShouldRejectWith(() => tools.NumericsCdf97WaveletTransform(new double[5]), "values");
        }
    }
}