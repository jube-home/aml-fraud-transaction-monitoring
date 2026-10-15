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

using System.Globalization;
using Accord.Math.Random;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed record LearnerConfig
    {
        public EvaluationFamily Family { get; init; } = EvaluationFamily.DecisionTree;
        public TreeAlgorithm Algorithm { get; init; } = TreeAlgorithm.C45;
        public TreePruning Pruning { get; init; } = TreePruning.None;
        public int MaxDepth { get; init; } = 6;
        public int MinLeaf { get; init; } = 5;
        public int Trees { get; init; } = 100;
        public double FeatureFraction { get; init; }
        public double SampleFraction { get; init; } = 1d;
        public double LearningRate { get; init; } = 0.1;
        public int Bins { get; init; } = 8;
        public NaiveBayesKind BayesKind { get; init; } = NaiveBayesKind.Gaussian;
        public double Regularisation { get; init; } = -1d;
        public int K { get; init; } = 5;
        public double Complexity { get; init; } = 1d;
        public SvmKernel Kernel { get; init; } = SvmKernel.Gaussian;
        public double Gamma { get; init; }
        public int Degree { get; init; } = 3;
        public int[] Hidden { get; init; } = [8];
        public int Epochs { get; init; } = 100;
        public int Patience { get; init; }
        public ClassImbalanceHandling Imbalance { get; init; } = ClassImbalanceHandling.None;
        public string Name => Family.ToString();

        public bool SupportsWeights =>
            Family is not (EvaluationFamily.KNearestNeighbours or EvaluationFamily.NeuralNetwork)
            && !(Family == EvaluationFamily.DecisionTree && Algorithm == TreeAlgorithm.Id3);

        public bool NeedsScaling => Family is EvaluationFamily.KNearestNeighbours
            or EvaluationFamily.LinearSupportVector
            or EvaluationFamily.KernelSupportVector or EvaluationFamily.NeuralNetwork or EvaluationFamily.NaiveBayes
            or EvaluationFamily.TreeAugmentedNaiveBayes || Family == EvaluationFamily.LogisticRegression;

        public double ResolvedRegularisation(double fallback)
        {
            return Regularisation < 0 ? fallback : Regularisation;
        }

        public EvaluationSetting[] Settings()
        {
            var list = new List<EvaluationSetting> { new("Family", Family.ToString()) };

            switch (Family)
            {
                case EvaluationFamily.DecisionTree:
                    list.Add(new EvaluationSetting("Algorithm", Algorithm.ToString()));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("Pruning", Pruning.ToString()));

                    if (Algorithm == TreeAlgorithm.Id3)
                    {
                        list.Add(new EvaluationSetting("BinCount", Bins.ToString(CultureInfo.InvariantCulture)));
                    }

                    break;
                case EvaluationFamily.RandomForest:
                case EvaluationFamily.Bagging:
                    list.Add(new EvaluationSetting("TreeCount", Trees.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));

                    list.Add(new EvaluationSetting("FeatureFraction", Family == EvaluationFamily.Bagging
                        ? "1"
                        : FeatureFraction > 0
                            ? Format(FeatureFraction)
                            : "sqrt"));

                    list.Add(new EvaluationSetting("SampleFraction", Format(SampleFraction)));
                    break;
                case EvaluationFamily.AdaBoost:
                    list.Add(new EvaluationSetting("TreeCount", Trees.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("LearningRate", Format(LearningRate)));
                    break;
                case EvaluationFamily.GradientBoosting:
                    list.Add(new EvaluationSetting("TreeCount", Trees.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("LearningRate", Format(LearningRate)));
                    list.Add(new EvaluationSetting("SampleFraction", Format(SampleFraction)));

                    list.Add(
                        new EvaluationSetting("FeatureFraction",
                            FeatureFraction > 0 ? Format(FeatureFraction) : "all"));

                    break;
                case EvaluationFamily.NaiveBayes:
                    list.Add(new EvaluationSetting("Kind", BayesKind.ToString()));

                    if (BayesKind == NaiveBayesKind.Categorical)
                    {
                        list.Add(new EvaluationSetting("BinCount", Bins.ToString(CultureInfo.InvariantCulture)));
                    }

                    list.Add(new EvaluationSetting("Regularisation",
                        Format(ResolvedRegularisation(BayesKind == NaiveBayesKind.Categorical ? 1d : 1e-6))));

                    break;
                case EvaluationFamily.TreeAugmentedNaiveBayes:
                    list.Add(new EvaluationSetting("Regularisation", Format(ResolvedRegularisation(1d))));
                    break;
                case EvaluationFamily.KNearestNeighbours:
                    list.Add(new EvaluationSetting("NeighbourCount", K.ToString(CultureInfo.InvariantCulture)));
                    break;
                case EvaluationFamily.LinearSupportVector:
                    list.Add(new EvaluationSetting("Complexity", Format(Complexity)));
                    break;
                case EvaluationFamily.KernelSupportVector:
                    list.Add(new EvaluationSetting("Kernel", Kernel.ToString()));
                    list.Add(new EvaluationSetting("Complexity", Format(Complexity)));
                    list.Add(new EvaluationSetting("KernelWidth", Gamma > 0 ? Format(Gamma) : "1/features"));
                    break;
                case EvaluationFamily.NeuralNetwork:
                    list.Add(new EvaluationSetting("HiddenLayers", string.Join("-", Hidden)));
                    list.Add(new EvaluationSetting("Epochs", Epochs.ToString(CultureInfo.InvariantCulture)));
                    list.Add(
                        new EvaluationSetting("EarlyStoppingPatience",
                            Patience.ToString(CultureInfo.InvariantCulture)));

                    break;
                case EvaluationFamily.LogisticRegression:
                    list.Add(new EvaluationSetting("Regularisation", Format(ResolvedRegularisation(0d))));
                    break;
            }

            list.Add(new EvaluationSetting("ClassImbalance", Imbalance.ToString()));

            return list.ToArray();
        }

        public string Label(IEnumerable<HyperParameter> parameters)
        {
            var parts = new List<string>();

            foreach (var parameter in parameters)
            {
                parts.Add($"{parameter}={Format(ValueOf(parameter))}");
            }

            return parts.Count == 0 ? Name : string.Join(", ", parts);
        }

        public double ValueOf(HyperParameter parameter)
        {
            return parameter switch
            {
                HyperParameter.MaxDepth => MaxDepth,
                HyperParameter.MinLeafSize => MinLeaf,
                HyperParameter.TreeCount => Trees,
                HyperParameter.FeatureFraction => FeatureFraction,
                HyperParameter.SampleFraction => SampleFraction,
                HyperParameter.LearningRate => LearningRate,
                HyperParameter.NeighbourCount => K,
                HyperParameter.Complexity => Complexity,
                HyperParameter.KernelWidth => Gamma,
                HyperParameter.HiddenUnits => Hidden.Length == 0 ? 0 : Hidden[0],
                HyperParameter.Epochs => Epochs,
                HyperParameter.Regularisation => Regularisation,
                HyperParameter.BinCount => Bins,
                _ => 0d
            };
        }

        public static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        public static HyperParameter[] Supported(EvaluationFamily family)
        {
            return family switch
            {
                EvaluationFamily.DecisionTree =>
                    [HyperParameter.MaxDepth, HyperParameter.MinLeafSize, HyperParameter.BinCount],
                EvaluationFamily.RandomForest =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.FeatureFraction, HyperParameter.SampleFraction
                ],
                EvaluationFamily.Bagging =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.SampleFraction
                ],
                EvaluationFamily.AdaBoost =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.LearningRate
                ],
                EvaluationFamily.GradientBoosting =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.LearningRate, HyperParameter.SampleFraction, HyperParameter.FeatureFraction
                ],
                EvaluationFamily.NaiveBayes => [HyperParameter.Regularisation, HyperParameter.BinCount],
                EvaluationFamily.TreeAugmentedNaiveBayes => [HyperParameter.Regularisation],
                EvaluationFamily.KNearestNeighbours => [HyperParameter.NeighbourCount],
                EvaluationFamily.LinearSupportVector => [HyperParameter.Complexity],
                EvaluationFamily.KernelSupportVector => [HyperParameter.Complexity, HyperParameter.KernelWidth],
                EvaluationFamily.NeuralNetwork => [HyperParameter.HiddenUnits, HyperParameter.Epochs],
                EvaluationFamily.LogisticRegression => [HyperParameter.Regularisation],
                _ => []
            };
        }

        public LearnerConfig With(HyperParameter parameter, double value, string argumentName)
        {
            if (!Supported(Family).Contains(parameter))
            {
                throw new ArgumentException(
                    $"Hyper-parameter {parameter} does not apply to {Family}; the supported ones are {string.Join(", ", Supported(Family))}.",
                    argumentName);
            }

            if (!double.IsFinite(value))
            {
                throw new ArgumentException($"Value for {parameter} must be a finite number.", argumentName);
            }

            switch (parameter)
            {
                case HyperParameter.MaxDepth:
                    RequireWhole(value, 1, ModelEvaluationLimits.MaximumTreeDepth, parameter, argumentName);

                    return this with { MaxDepth = (int)value };
                case HyperParameter.MinLeafSize:
                    RequireWhole(value, 1, 1000, parameter, argumentName);

                    return this with { MinLeaf = (int)value };
                case HyperParameter.TreeCount:
                    RequireWhole(value, 1, ModelEvaluationLimits.MaximumTrees, parameter, argumentName);

                    return this with { Trees = (int)value };
                case HyperParameter.FeatureFraction:
                    Require(value, 0.01, 1, parameter, argumentName);

                    return this with { FeatureFraction = value };
                case HyperParameter.SampleFraction:
                    Require(value, 0.05, 1, parameter, argumentName);

                    return this with { SampleFraction = value };
                case HyperParameter.LearningRate:
                    Require(value, 0.001, 1, parameter, argumentName);

                    return this with { LearningRate = value };
                case HyperParameter.NeighbourCount:
                    RequireWhole(value, 1, 500, parameter, argumentName);

                    return this with { K = (int)value };
                case HyperParameter.Complexity:
                    Require(value, 1e-4, 1e4, parameter, argumentName);

                    return this with { Complexity = value };
                case HyperParameter.KernelWidth:
                    Require(value, 1e-5, 1e3, parameter, argumentName);

                    return this with { Gamma = value };
                case HyperParameter.HiddenUnits:
                    RequireWhole(value, 1, ModelEvaluationLimits.MaximumHiddenUnits, parameter, argumentName);

                    return this with { Hidden = [(int)value] };
                case HyperParameter.Epochs:
                    RequireWhole(value, 1, ModelEvaluationLimits.MaximumEpochs, parameter, argumentName);

                    return this with { Epochs = (int)value };
                case HyperParameter.Regularisation:
                    Require(value, 0, 1e6, parameter, argumentName);

                    return this with { Regularisation = value };
                case HyperParameter.BinCount:
                    RequireWhole(value, 2, ModelEvaluationLimits.MaximumBins, parameter, argumentName);

                    return this with { Bins = (int)value };
                default:
                    throw new ArgumentException($"Unsupported hyper-parameter {parameter}.", argumentName);
            }
        }

        private static void Require(double value, double minimum, double maximum, HyperParameter parameter, string name)
        {
            if (value < minimum || value > maximum)
            {
                throw new ArgumentException($"{parameter} must be between {Format(minimum)} and {Format(maximum)}.",
                    name);
            }
        }

        private static void RequireWhole(double value, int minimum, int maximum, HyperParameter parameter, string name)
        {
            if (value < minimum || value > maximum || Math.Abs(value - Math.Round(value)) > 1e-9)
            {
                throw new ArgumentException($"{parameter} must be a whole number between {minimum} and {maximum}.",
                    name);
            }
        }
    }

    internal sealed record FitContext(
        string[] Names,
        int Seed,
        double[][]? ValidationX,
        int[]? ValidationY,
        CancellationToken Token);

    internal static class SeededLibrary
    {
        public static T Run<T>(int seed, Func<T> body)
        {
            var previous = Generator.ThreadSeed;
            Generator.ThreadSeed = seed;

            try
            {
                return body();
            }
            finally
            {
                Generator.ThreadSeed = previous;
            }
        }

        public static int Derive(int seed, int index)
        {
            return unchecked(seed * 7919 + index * 104729 + 17);
        }
    }
}