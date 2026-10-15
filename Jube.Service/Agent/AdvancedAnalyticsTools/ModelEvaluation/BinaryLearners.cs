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

using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class BinaryLearners
    {
        public static IBinaryModel Fit(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx)
        {
            var minimumSplit = Math.Max(2, 2 * c.MinLeaf);

            switch (c.Family)
            {
                case EvaluationFamily.DecisionTree:
                    return FitTree(c, x, y, w, ctx, minimumSplit);
                case EvaluationFamily.RandomForest:
                case EvaluationFamily.Bagging:
                    return ForestBinaryModel.Fit(c, x, y, w, ctx);
                case EvaluationFamily.AdaBoost:
                    return AdaBoostBinaryModel.Fit(c, x, y, w, ctx);
                case EvaluationFamily.GradientBoosting:
                    return GradientBoostingBinaryModel.Fit(c, x, y, w, ctx);
                case EvaluationFamily.NaiveBayes:
                    return c.BayesKind switch
                    {
                        NaiveBayesKind.Categorical => CategoricalNaiveBayesModel.Fit(c, x, y, w, ctx.Names),
                        NaiveBayesKind.TreeAugmented => TreeAugmentedBayesModel.Fit(c, x, y, w, ctx),
                        _ => GaussianNaiveBayesModel.Fit(c, x, y, w)
                    };
                case EvaluationFamily.TreeAugmentedNaiveBayes:
                    return TreeAugmentedBayesModel.Fit(c, x, y, w, ctx);
                case EvaluationFamily.KNearestNeighbours:
                    return NearestNeighbourModel.Fit(c, x, y);
                case EvaluationFamily.LinearSupportVector:
                case EvaluationFamily.KernelSupportVector:
                    return SupportVectorBinaryModel.Fit(c, x, y, w, ctx);
                case EvaluationFamily.NeuralNetwork:
                    return NeuralBinaryModel.Fit(c, x, y, ctx);
                default:
                    throw new ArgumentException($"{c.Family} is fitted by its dedicated trainer, not here.");
            }
        }

        private static IBinaryModel FitTree(LearnerConfig c, double[][] x, int[] y, double[]? w, FitContext ctx,
            int minimumSplit)
        {
            var p = x[0].Length;

            if (c.Algorithm == TreeAlgorithm.Cart)
            {
                var target = y.Select(v => (double)v).ToArray();
                var options = new CartTree.Options(c.MaxDepth, c.MinLeaf, minimumSplit, p);

                var tree = CartTree.Fit(x, target, w, Enumerable.Range(0, x.Length).ToArray(), options,
                    new Random(ctx.Seed));

                if (c.Pruning == TreePruning.ReducedError && ctx.ValidationX is { Length: > 0 } &&
                    ctx.ValidationY != null)
                {
                    tree.PruneReducedError(ctx.ValidationX, ctx.ValidationY.Select(v => (double)v).ToArray());
                }

                return new CartBinaryModel(tree);
            }

            if (c.Algorithm == TreeAlgorithm.Id3)
            {
                var discretiser = Discretiser.Fit(x, ctx.Names, c.Bins);
                var codes = x.Select(r => discretiser.ApplyInt(r)).ToArray();
                var symbols = discretiser.Symbols;
                var variables = ctx.Names.Select((n, j) => DecisionVariable.Discrete(n, symbols[j])).ToArray();

                var learner = new ID3Learning(variables) { MaxHeight = c.MaxDepth };
                var tree = SeededLibrary.Run(ctx.Seed, () => learner.Learn(codes, y));
                var model = new LibraryTreeModel(tree, discretiser, x, y, null, "ID3");

                if (c.Pruning == TreePruning.ReducedError && ctx.ValidationX is { Length: > 0 } &&
                    ctx.ValidationY != null)
                {
                    model.Prune(ctx.ValidationX, ctx.ValidationY);
                }

                return model;
            }

            var continuous = ctx.Names.Select(DecisionVariable.Continuous).ToArray();

            var c45 = new C45Learning(continuous)
            {
                MaxHeight = c.MaxDepth,
                Join = 0,
                MinimumLeafSize = c.MinLeaf,
                MinimumSplitSize = minimumSplit
            };

            var fitted = SeededLibrary.Run(ctx.Seed, () => c45.Learn(x, y, w));
            var wrapped = new LibraryTreeModel(fitted, null, x, y, w, "C4.5");

            if (c.Pruning == TreePruning.ReducedError && ctx.ValidationX is { Length: > 0 } && ctx.ValidationY != null)
            {
                wrapped.Prune(ctx.ValidationX, ctx.ValidationY);
            }

            return wrapped;
        }
    }
}