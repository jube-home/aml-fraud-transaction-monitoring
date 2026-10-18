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
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed record RegressorConfig
    {
        public RegressionFamily Family { get; init; } = RegressionFamily.LinearRegression;
        public LinearPenalty Penalty { get; init; } = LinearPenalty.None;
        public double Lambda { get; init; } = 0.1;
        public double L1Ratio { get; init; } = 0.5;
        public int MaxDepth { get; init; } = 6;
        public int MinLeaf { get; init; } = 5;
        public int Trees { get; init; } = 100;
        public double FeatureFraction { get; init; }
        public double SampleFraction { get; init; } = 1d;
        public double LearningRate { get; init; } = 0.1;
        public int K { get; init; } = 5;
        public double Complexity { get; init; } = 1d;
        public SvmKernel Kernel { get; init; } = SvmKernel.Gaussian;
        public double Gamma { get; init; }
        public int[] Hidden { get; init; } = [8];
        public int Epochs { get; init; } = 200;
        public int Patience { get; init; }
        public TreePruning Pruning { get; init; } = TreePruning.None;

        public bool NeedsScaling => Family is not (RegressionFamily.DecisionTree or RegressionFamily.RandomForest
            or RegressionFamily.GradientBoosting);

        public string Name => Family.ToString();

        public static HyperParameter[] Supported(RegressionFamily family)
        {
            return family switch
            {
                RegressionFamily.LinearRegression => [HyperParameter.Regularisation],
                RegressionFamily.DecisionTree => [HyperParameter.MaxDepth, HyperParameter.MinLeafSize],
                RegressionFamily.RandomForest =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.FeatureFraction, HyperParameter.SampleFraction
                ],
                RegressionFamily.GradientBoosting =>
                [
                    HyperParameter.TreeCount, HyperParameter.MaxDepth, HyperParameter.MinLeafSize,
                    HyperParameter.LearningRate, HyperParameter.SampleFraction
                ],
                RegressionFamily.KNearestNeighbours => [HyperParameter.NeighbourCount],
                RegressionFamily.KernelRidge => [HyperParameter.Complexity, HyperParameter.KernelWidth],
                RegressionFamily.NeuralNetwork => [HyperParameter.HiddenUnits, HyperParameter.Epochs],
                _ => []
            };
        }

        public RegressorConfig With(HyperParameter parameter, double value, string argumentName)
        {
            if (!Supported(Family).Contains(parameter))
            {
                throw new ArgumentException(
                    $"Hyper-parameter {parameter} does not apply to {Family}; the supported ones are {string.Join(", ", Supported(Family))}.",
                    argumentName);
            }

            switch (parameter)
            {
                case HyperParameter.Regularisation:
                    RangeIn(value, 0, 1e6, parameter, argumentName);

                    return this with { Lambda = value };
                case HyperParameter.MaxDepth:
                    return this with
                    {
                        MaxDepth = WholeIn(value, 1, ModelEvaluationLimits.MaximumTreeDepth, parameter, argumentName)
                    };
                case HyperParameter.MinLeafSize:
                    return this with { MinLeaf = WholeIn(value, 1, 1000, parameter, argumentName) };
                case HyperParameter.TreeCount:
                    return this with
                    {
                        Trees = WholeIn(value, 1, ModelEvaluationLimits.MaximumTrees, parameter, argumentName)
                    };
                case HyperParameter.FeatureFraction:
                    RangeIn(value, 0.01, 1, parameter, argumentName);

                    return this with { FeatureFraction = value };
                case HyperParameter.SampleFraction:
                    RangeIn(value, 0.05, 1, parameter, argumentName);

                    return this with { SampleFraction = value };
                case HyperParameter.LearningRate:
                    RangeIn(value, 0.001, 1, parameter, argumentName);

                    return this with { LearningRate = value };
                case HyperParameter.NeighbourCount:
                    return this with { K = WholeIn(value, 1, 500, parameter, argumentName) };
                case HyperParameter.Complexity:
                    RangeIn(value, 1e-4, 1e4, parameter, argumentName);

                    return this with { Complexity = value };
                case HyperParameter.KernelWidth:
                    RangeIn(value, 1e-5, 1e3, parameter, argumentName);

                    return this with { Gamma = value };
                case HyperParameter.HiddenUnits:
                    return this with
                    {
                        Hidden = [WholeIn(value, 1, ModelEvaluationLimits.MaximumHiddenUnits, parameter, argumentName)]
                    };
                case HyperParameter.Epochs:
                    return this with
                    {
                        Epochs = WholeIn(value, 1, ModelEvaluationLimits.MaximumEpochs, parameter, argumentName)
                    };
                default:
                    throw new ArgumentException($"Unsupported hyper-parameter {parameter}.", argumentName);
            }
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
                HyperParameter.Regularisation => Lambda,
                _ => 0d
            };
        }

        public string Label(IEnumerable<HyperParameter> parameters)
        {
            var parts = parameters.Select(p => $"{p}={LearnerConfig.Format(ValueOf(p))}").ToList();

            return parts.Count == 0 ? Name : string.Join(", ", parts);
        }

        public EvaluationSetting[] Settings()
        {
            var list = new List<EvaluationSetting> { new("Family", Family.ToString()) };

            string F(double v)
            {
                return LearnerConfig.Format(v);
            }

            switch (Family)
            {
                case RegressionFamily.LinearRegression:
                    list.Add(new EvaluationSetting("Penalty", Penalty.ToString()));

                    if (Penalty != LinearPenalty.None)
                    {
                        list.Add(new EvaluationSetting("Regularisation", F(Lambda)));
                    }

                    if (Penalty == LinearPenalty.ElasticNet)
                    {
                        list.Add(new EvaluationSetting("L1Ratio", F(L1Ratio)));
                    }

                    break;
                case RegressionFamily.DecisionTree:
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("Pruning", Pruning.ToString()));
                    break;
                case RegressionFamily.RandomForest:
                    list.Add(new EvaluationSetting("TreeCount", Trees.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));

                    list.Add(new EvaluationSetting("FeatureFraction",
                        FeatureFraction > 0 ? F(FeatureFraction) : "one third"));

                    list.Add(new EvaluationSetting("SampleFraction", F(SampleFraction)));
                    break;
                case RegressionFamily.GradientBoosting:
                    list.Add(new EvaluationSetting("TreeCount", Trees.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MaxDepth", MaxDepth.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("MinLeafSize", MinLeaf.ToString(CultureInfo.InvariantCulture)));
                    list.Add(new EvaluationSetting("LearningRate", F(LearningRate)));
                    list.Add(new EvaluationSetting("SampleFraction", F(SampleFraction)));
                    break;
                case RegressionFamily.KNearestNeighbours:
                    list.Add(new EvaluationSetting("NeighbourCount", K.ToString(CultureInfo.InvariantCulture)));
                    break;
                case RegressionFamily.KernelRidge:
                    list.Add(new EvaluationSetting("Kernel", Kernel.ToString()));
                    list.Add(new EvaluationSetting("Complexity", F(Complexity)));
                    list.Add(new EvaluationSetting("KernelWidth", Gamma > 0 ? F(Gamma) : "1/features"));
                    break;
                case RegressionFamily.NeuralNetwork:
                    list.Add(new EvaluationSetting("HiddenLayers", string.Join("-", Hidden)));
                    list.Add(new EvaluationSetting("Epochs", Epochs.ToString(CultureInfo.InvariantCulture)));
                    break;
            }

            return list.ToArray();
        }

        private static int WholeIn(double value, int minimum, int maximum, HyperParameter parameter, string name)
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum ||
                Math.Abs(value - Math.Round(value)) > 1e-9)
            {
                throw new ArgumentException($"{parameter} must be a whole number between {minimum} and {maximum}.",
                    name);
            }

            return (int)value;
        }

        private static void RangeIn(double value, double minimum, double maximum, HyperParameter parameter, string name)
        {
            if (!double.IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentException(
                    $"{parameter} must be between {LearnerConfig.Format(minimum)} and {LearnerConfig.Format(maximum)}.",
                    name);
            }
        }
    }

    internal sealed record RegressionFitContext(
        string[] Names,
        int Seed,
        double[][]? ValidationX,
        double[]? ValidationY,
        FeatureTransform Transform,
        CancellationToken Token);

    internal sealed class TreeRegressionModel : IRegressionModel
    {
        private readonly double baseValue;
        private readonly string kind;
        private readonly double rate;
        private readonly CartTree[] trees;

        public TreeRegressionModel(CartTree[] trees, double baseValue, double rate, string kind)
        {
            this.trees = trees;
            this.baseValue = baseValue;
            this.rate = rate;
            this.kind = kind;
        }

        public double[] Predict(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                var sum = 0d;

                foreach (var tree in trees)
                {
                    sum += tree.Predict(rows[i]);
                }

                result[i] = kind == "gradient boosted trees" ? baseValue + rate * sum : sum / trees.Length;
            }

            return result;
        }

        public EvaluationNumber[] Coefficients(string[] names)
        {
            return [];
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = new double[names.Length];

            foreach (var tree in trees)
            {
                var one = tree.Importance();

                for (var j = 0; j < importance.Length; j++)
                {
                    importance[j] += one[j] / trees.Length;
                }
            }

            var single = trees.Length == 1;

            return new ModelDescription(
                $"A {kind} with variance-reduction splits on the target.",
                [
                    ModelText.Fact("Trees", trees.Length),
                    ModelText.Fact("MeanDepth", trees.Average(t => t.Height)),
                    ModelText.Fact("MeanLeaves", trees.Average(t => t.LeafCount))
                ],
                single ? trees[0].Rules(names, v => "predict " + LearnerConfig.Format(v[0]), 25) : [],
                single ? trees[0].ToText(names, v => "predict " + LearnerConfig.Format(v[0])) : null,
                ModelText.Rank(names, importance));
        }

        public static TreeRegressionModel Fit(RegressorConfig c, double[][] x, double[] y, RegressionFitContext ctx)
        {
            var n = x.Length;
            var p = x[0].Length;

            switch (c.Family)
            {
                case RegressionFamily.DecisionTree:
                {
                    var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, Math.Max(2, 2 * c.MinLeaf), p);

                    var tree = CartTree.Fit(x, y, null, Enumerable.Range(0, n).ToArray(), options,
                        new Random(ctx.Seed));

                    if (c.Pruning == TreePruning.ReducedError && ctx.ValidationX is { Length: > 0 } &&
                        ctx.ValidationY != null)
                    {
                        tree.PruneReducedError(ctx.ValidationX, ctx.ValidationY);
                    }

                    return new TreeRegressionModel([tree], 0d, 1d, "regression tree");
                }
                case RegressionFamily.RandomForest:
                {
                    var perSplit = c.FeatureFraction > 0
                        ? Math.Max(1, (int)Math.Ceiling(c.FeatureFraction * p))
                        : Math.Max(1, p / 3);

                    var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, Math.Max(2, 2 * c.MinLeaf), perSplit);
                    var sample = Math.Max(1, (int)Math.Round(c.SampleFraction * n));
                    var trees = new CartTree[c.Trees];

                    Parallel.For(0, c.Trees, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 8)),
                        CancellationToken = ctx.Token
                    }, t =>
                    {
                        var random = new Random(SeededLibrary.Derive(ctx.Seed, t));
                        var rows = new int[sample];

                        for (var i = 0; i < sample; i++)
                        {
                            rows[i] = random.Next(n);
                        }

                        trees[t] = CartTree.Fit(x, y, null, rows, options, random);
                    });

                    return new TreeRegressionModel(trees, 0d, 1d, "random forest");
                }
                default:
                {
                    var mean = y.Average();
                    var prediction = Enumerable.Repeat(mean, n).ToArray();
                    var residual = new double[n];
                    var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, Math.Max(2, 2 * c.MinLeaf), p);
                    var sampleSize = Math.Max(2, (int)Math.Round(c.SampleFraction * n));
                    var trees = new List<CartTree>();

                    for (var round = 0; round < c.Trees; round++)
                    {
                        ctx.Token.ThrowIfCancellationRequested();
                        var random = new Random(SeededLibrary.Derive(ctx.Seed, round));

                        for (var i = 0; i < n; i++)
                        {
                            residual[i] = y[i] - prediction[i];
                        }

                        var rows = sampleSize >= n
                            ? Enumerable.Range(0, n).ToArray()
                            : Enumerable.Range(0, n).OrderBy(_ => random.Next()).Take(sampleSize).ToArray();

                        var tree = CartTree.Fit(x, residual, null, rows, options, random);
                        trees.Add(tree);

                        for (var i = 0; i < n; i++)
                        {
                            prediction[i] += c.LearningRate * tree.Predict(x[i]);
                        }
                    }

                    return new TreeRegressionModel(trees.ToArray(), mean, c.LearningRate, "gradient boosted trees");
                }
            }
        }
    }
}