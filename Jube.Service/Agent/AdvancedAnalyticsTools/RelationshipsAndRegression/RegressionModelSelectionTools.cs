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

using System.ComponentModel;
using Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.RelationshipsAndRegression
{
    [AdvancedAnalyticsToolGroup("RelationshipsAndRegression")]
    [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
    public class RegressionModelSelectionTools
    {
        [AdvancedAnalyticsTool("StepwiseLinearRegression")]
        [Description(
            "Chooses which predictors to keep in a linear regression by stepwise search, then returns the final model with the full inference table. " +
            "Direction Forward starts from an intercept-only model and adds predictors, Backward starts from all predictors and removes them, Both starts empty and after each addition re-examines whether any earlier predictor should leave. " +
            "With criterion Aic or Bic each step makes the single move that most lowers the criterion and stops when no move helps (Bic penalises size more and gives smaller models); with PValue a predictor enters when its t-test p-value is below entryPValue and leaves when above exitPValue (exitPValue must be at least entryPValue). " +
            "The Steps list records every addition or removal with the criterion (or p-value) after the move. Stepwise selection is greedy and its p-values are optimistic because the same data chose the model; validate the result with CrossValidateLinearRegression. Predictors that are collinear with those already in the model are skipped. An intercept is always fitted.")]
        public StepwiseRegressionResult StepwiseLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Forward, Backward or Both. Default Both.")]
            StepwiseDirection direction = StepwiseDirection.Both,
            [Description("Aic (default), Bic or PValue decides which moves are made.")]
            ModelSelectionCriterion criterion = ModelSelectionCriterion.Aic,
            [Description("With criterion PValue, the p-value below which a predictor may enter. Default 0.05.")]
            double entryPValue = 0.05,
            [Description(
                "With criterion PValue, the p-value above which a predictor is removed; at least entryPValue. Default 0.10.")]
            double exitPValue = 0.10,
            [Description(RegressionText.Level)] double confidenceLevel = 0.95,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            RegressionInput.RequireLevel(confidenceLevel, nameof(confidenceLevel));

            if (!Enum.IsDefined(typeof(StepwiseDirection), direction))
            {
                throw new ArgumentException("Unknown direction. Use Forward, Backward or Both.", nameof(direction));
            }

            if (criterion != ModelSelectionCriterion.Aic && criterion != ModelSelectionCriterion.Bic &&
                criterion != ModelSelectionCriterion.PValue)
            {
                throw new ArgumentException("Stepwise selection supports the criteria Aic, Bic and PValue.",
                    nameof(criterion));
            }

            if (!(entryPValue > 0 && entryPValue < 1) || !(exitPValue > 0 && exitPValue <= 1) ||
                exitPValue < entryPValue)
            {
                throw new ArgumentException(
                    "entryPValue must be between 0 and 1 and exitPValue must be between entryPValue and 1.",
                    nameof(entryPValue));
            }

            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 5);

            if (data.P > 60)
            {
                throw new ArgumentException("Stepwise selection accepts at most 60 predictor columns.", nameof(inputs));
            }

            var current = direction == StepwiseDirection.Backward
                ? Enumerable.Range(0, data.P).ToList()
                : new List<int>();

            var fit = SubsetFitting.TryFit(data, current);

            if (fit == null)
            {
                throw new ArgumentException(
                    "The full set of predictors is collinear, so backward selection cannot start. Remove redundant columns or use Forward.",
                    nameof(inputs));
            }

            var steps = new List<StepwiseStep>();

            for (var guard = 0; guard < 4 * data.P + 4; guard++)
            {
                var move = criterion == ModelSelectionCriterion.PValue
                    ? PValueMove(data, current, fit, direction, entryPValue, exitPValue)
                    : CriterionMove(data, current, fit, direction, criterion);

                if (move == null)
                {
                    break;
                }

                double movedP;

                if (move.Value.Action == "Add")
                {
                    current.Add(move.Value.Variable);

                    movedP = SubsetFitting.SlopePValue(move.Value.Fit,
                        current.OrderBy(v => v).ToList().IndexOf(move.Value.Variable) + 1);
                }
                else
                {
                    movedP = SubsetFitting.SlopePValue(fit,
                        current.OrderBy(v => v).ToList().IndexOf(move.Value.Variable) + 1);

                    current.Remove(move.Value.Variable);
                }

                fit = move.Value.Fit;

                steps.Add(new StepwiseStep
                {
                    Step = steps.Count + 1,
                    Action = move.Value.Action,
                    Variable = data.Names[move.Value.Variable],
                    CriterionValue = move.Value.Value,
                    PValue = movedP,
                    ModelAfterStep = current.OrderBy(v => v).Select(v => data.Names[v]).ToArray()
                });
            }

            var ordered = current.OrderBy(v => v).ToList();
            var finalFit = SubsetFitting.TryFit(data, ordered)!;
            var finalRows = data.X.Select(r => ordered.Select(j => r[j]).ToArray()).ToArray();

            return new StepwiseRegressionResult
            {
                SelectedVariables = ordered.Select(v => data.Names[v]).ToArray(),
                RemovedVariables = Enumerable.Range(0, data.P).Where(v => !ordered.Contains(v))
                    .Select(v => data.Names[v]).ToArray(),
                Steps = steps.ToArray(),
                FinalModel = LinearReporting.Summarise(finalFit, ordered.Select(v => data.Names[v]).ToArray(),
                    finalRows,
                    confidenceLevel, false, data.Dropped),
                Criterion = criterion.ToString()
            };
        }

        [AdvancedAnalyticsTool("BestSubsetLinearRegression")]
        [Description(
            "Searches every combination of predictors (best subset selection, exhaustive rather than greedy) and ranks the resulting linear regressions. " +
            "criterion chooses the ranking: Aic or Bic (lower better), AdjustedRSquared (higher better) or MallowsCp (lower better, and a good model has Cp close to its number of parameters, computed against the full model's residual variance). " +
            "It returns the TopModels overall, and BestPerSize, the single best model for each number of predictors, which shows how much each extra predictor buys. Every subset reports R squared, adjusted R squared, AIC, BIC, Cp and residual standard error. " +
            "The search cost doubles with each predictor, so at most 15 predictors are accepted (default limit 12 unless maximumSubsetSize caps the size). Subsets whose columns are collinear are skipped and counted. An intercept is always fitted.")]
        public BestSubsetResult BestSubsetLinearRegression(
            [Description(RegressionText.Inputs)] double[][] inputs,
            [Description(RegressionText.Outputs)] double[] outputs,
            [Description(RegressionText.Names)] string[]? variableNames = null,
            [Description("Ranking criterion: Aic (default), Bic, AdjustedRSquared or MallowsCp.")]
            ModelSelectionCriterion criterion = ModelSelectionCriterion.Bic,
            [Description(
                "Largest number of predictors in any subset, from 1 to the predictor count. Leave 0 for no cap.")]
            int maximumSubsetSize = 0,
            [Description("How many top models to return, from 1 to 100. Default 10.")]
            int topModels = 10,
            [Description(RegressionText.Drop)] bool dropIncompleteRows = false)
        {
            if (!Enum.IsDefined(typeof(ModelSelectionCriterion), criterion) ||
                criterion == ModelSelectionCriterion.PValue)
            {
                throw new ArgumentException("Use Aic, Bic, AdjustedRSquared or MallowsCp as the criterion.",
                    nameof(criterion));
            }

            if (topModels < 1 || topModels > 100)
            {
                throw new ArgumentException("topModels must be between 1 and 100.", nameof(topModels));
            }

            var data = RegressionInput.Prepare(inputs, outputs, variableNames, null, dropIncompleteRows, 5);

            if (data.P > 15)
            {
                throw new ArgumentException(
                    $"Best subset search is exhaustive and accepts at most 15 predictors; {data.P} were supplied. Use StepwiseLinearRegression or filter the columns first.",
                    nameof(inputs));
            }

            if (maximumSubsetSize < 0 || maximumSubsetSize > data.P)
            {
                throw new ArgumentException($"maximumSubsetSize must be between 0 and {data.P}.",
                    nameof(maximumSubsetSize));
            }

            var cap = maximumSubsetSize == 0 ? data.P : maximumSubsetSize;
            var full = SubsetFitting.TryFit(data, Enumerable.Range(0, data.P).ToList());

            if (full == null)
            {
                throw new ArgumentException(
                    "The full set of predictors is collinear, so Mallows Cp cannot be computed. Remove redundant columns first.",
                    nameof(inputs));
            }

            var sigma2Full = full.Sigma2;
            var scores = new List<SubsetModelScore>();
            var skipped = 0;

            for (long mask = 1; mask < 1L << data.P; mask++)
            {
                var subset = Enumerable.Range(0, data.P).Where(j => (mask & (1L << j)) != 0).ToList();

                if (subset.Count > cap)
                {
                    continue;
                }

                var fit = SubsetFitting.TryFit(data, subset);

                if (fit == null)
                {
                    skipped++;
                    continue;
                }

                scores.Add(new SubsetModelScore
                {
                    Variables = subset.Select(j => data.Names[j]).ToArray(),
                    Size = subset.Count,
                    RSquared = fit.RSquared,
                    AdjustedRSquared = fit.AdjustedRSquared,
                    Aic = fit.Aic,
                    Bic = fit.Bic,
                    MallowsCp = fit.Rss / sigma2Full - (data.N - 2d * fit.K),
                    ResidualStandardError = fit.Sigma
                });
            }

            if (scores.Count == 0)
            {
                throw new ArgumentException("No subset could be fitted. Supply more rows or fewer collinear columns.",
                    nameof(inputs));
            }

            double Key(SubsetModelScore s)
            {
                return criterion switch
                {
                    ModelSelectionCriterion.Aic => s.Aic,
                    ModelSelectionCriterion.Bic => s.Bic,
                    ModelSelectionCriterion.AdjustedRSquared => -s.AdjustedRSquared,
                    _ => s.MallowsCp
                };
            }

            var ranked = scores.OrderBy(Key).ToList();

            return new BestSubsetResult
            {
                TopModels = ranked.Take(topModels).ToArray(),
                BestPerSize = scores.GroupBy(s => s.Size).OrderBy(g => g.Key).Select(g => g.OrderBy(Key).First())
                    .ToArray(),
                ModelsEvaluated = scores.Count,
                ModelsSkippedAsCollinear = skipped,
                Criterion = criterion.ToString()
            };
        }

        [AdvancedAnalyticsTool("CompareModelsByInformationCriteria")]
        [Description(
            "Compares candidate models from their log-likelihoods, using the Akaike information criterion AIC = 2k - 2 logL, its small-sample correction AICc = AIC + 2k(k+1)/(n-k-1), and the Bayesian information criterion BIC = k ln n - 2 logL, where k counts every fitted parameter (including any variance) and n is the number of observations. " +
            "For each model it returns the criteria, the differences from the best model (a DeltaAic above about 2 is meaningful evidence against a model, above 10 essentially rules it out) and Akaike and BIC weights that sum to 1 and read as the relative support for each model within the set. " +
            "All models must be fitted to the same n observations of the same outcome. Use it to compare fits from different tools, for example a linear and a nonlinear curve.")]
        public InformationCriterionComparison CompareModelsByInformationCriteria(
            [Description("A label for each model, unique, in the same order as the other arrays.")]
            string[] modelNames,
            [Description("The maximised log-likelihood of each model.")]
            double[] logLikelihoods,
            [Description(
                "The number of fitted parameters of each model, counting the error variance if the model has one; each at least 1.")]
            int[] parameterCounts,
            [Description("The number of observations every model was fitted to, at least 2.")]
            int observations)
        {
            if (modelNames == null || modelNames.Length == 0)
            {
                throw new ArgumentException("modelNames is required: at least one model label.", nameof(modelNames));
            }

            RegressionInput.RequireArray(logLikelihoods, nameof(logLikelihoods), 1);

            if (parameterCounts == null || parameterCounts.Length != modelNames.Length ||
                logLikelihoods.Length != modelNames.Length)
            {
                throw new ArgumentException(
                    "modelNames, logLikelihoods and parameterCounts must all have the same length, one entry per model.",
                    nameof(parameterCounts));
            }

            if (modelNames.Distinct(StringComparer.Ordinal).Count() != modelNames.Length ||
                modelNames.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("modelNames must be unique and not empty.", nameof(modelNames));
            }

            if (observations < 2)
            {
                throw new ArgumentException("observations must be at least 2.", nameof(observations));
            }

            if (parameterCounts.Any(k => k < 1))
            {
                throw new ArgumentException("Every parameter count must be at least 1.", nameof(parameterCounts));
            }

            if (logLikelihoods.Any(double.IsNaN))
            {
                throw new ArgumentException("logLikelihoods must not contain NaN.", nameof(logLikelihoods));
            }

            var aic = logLikelihoods.Select((l, i) => 2d * parameterCounts[i] - 2 * l).ToArray();

            var aicc = logLikelihoods.Select((_, i) => observations - parameterCounts[i] - 1 > 0
                ? aic[i] + 2d * parameterCounts[i] * (parameterCounts[i] + 1) / (observations - parameterCounts[i] - 1)
                : double.NaN).ToArray();

            var bic = logLikelihoods.Select((l, i) => parameterCounts[i] * Math.Log(observations) - 2 * l).ToArray();
            var bestAic = aic.Min();
            var bestBic = bic.Min();
            var akaike = aic.Select(a => Math.Exp(-(a - bestAic) / 2)).ToArray();
            var bayes = bic.Select(b => Math.Exp(-(b - bestBic) / 2)).ToArray();
            var akaikeSum = akaike.Sum();
            var bayesSum = bayes.Sum();

            return new InformationCriterionComparison
            {
                Models = modelNames.Select((n, i) => new InformationCriterionRow
                {
                    Model = n,
                    LogLikelihood = logLikelihoods[i],
                    Parameters = parameterCounts[i],
                    Aic = aic[i],
                    Aicc = aicc[i],
                    Bic = bic[i],
                    DeltaAic = aic[i] - bestAic,
                    DeltaBic = bic[i] - bestBic,
                    AkaikeWeight = akaike[i] / akaikeSum,
                    BicWeight = bayes[i] / bayesSum
                }).ToArray(),
                BestByAic = modelNames[Array.IndexOf(aic, bestAic)],
                BestByBic = modelNames[Array.IndexOf(bic, bestBic)],
                Observations = observations
            };
        }

        private static (string Action, int Variable, LinearFit Fit, double Value)? CriterionMove(
            PreparedRegressionData data, List<int> current, LinearFit currentFit, StepwiseDirection direction,
            ModelSelectionCriterion criterion)
        {
            var baseline = SubsetFitting.Score(currentFit, criterion);
            (string Action, int Variable, LinearFit Fit, double Value)? best = null;

            if (direction != StepwiseDirection.Backward)
            {
                foreach (var v in Enumerable.Range(0, data.P).Except(current))
                {
                    var trial = current.Append(v).OrderBy(j => j).ToList();
                    var fit = SubsetFitting.TryFit(data, trial);

                    if (fit != null && SubsetFitting.Score(fit, criterion) < baseline - 1e-9 &&
                        (best == null || SubsetFitting.Score(fit, criterion) < best.Value.Value))
                    {
                        best = ("Add", v, fit, SubsetFitting.Score(fit, criterion));
                    }
                }
            }

            if (direction != StepwiseDirection.Forward)
            {
                foreach (var v in current)
                {
                    var trial = current.Where(j => j != v).OrderBy(j => j).ToList();
                    var fit = SubsetFitting.TryFit(data, trial);

                    if (fit != null && SubsetFitting.Score(fit, criterion) < baseline - 1e-9 &&
                        (best == null || SubsetFitting.Score(fit, criterion) < best.Value.Value))
                    {
                        best = ("Remove", v, fit, SubsetFitting.Score(fit, criterion));
                    }
                }
            }

            return best;
        }

        private static (string Action, int Variable, LinearFit Fit, double Value)? PValueMove(
            PreparedRegressionData data, List<int> current, LinearFit currentFit, StepwiseDirection direction,
            double entry, double exit)
        {
            (string Action, int Variable, LinearFit Fit, double Value)? best = null;

            if (direction != StepwiseDirection.Backward)
            {
                foreach (var v in Enumerable.Range(0, data.P).Except(current))
                {
                    var trial = current.Append(v).OrderBy(j => j).ToList();
                    var fit = SubsetFitting.TryFit(data, trial);

                    if (fit == null)
                    {
                        continue;
                    }

                    var p = SubsetFitting.SlopePValue(fit, trial.IndexOf(v) + 1);

                    if (p < entry && (best == null || p < best.Value.Value))
                    {
                        best = ("Add", v, fit, p);
                    }
                }

                if (best != null)
                {
                    return best;
                }
            }

            if (direction != StepwiseDirection.Forward && current.Count > 0)
            {
                var ordered = current.OrderBy(j => j).ToList();
                var worst = -1;
                var worstP = exit;

                for (var i = 0; i < ordered.Count; i++)
                {
                    var p = SubsetFitting.SlopePValue(currentFit, i + 1);

                    if (p > worstP)
                    {
                        worstP = p;
                        worst = ordered[i];
                    }
                }

                if (worst >= 0)
                {
                    var trial = ordered.Where(j => j != worst).ToList();
                    var fit = SubsetFitting.TryFit(data, trial);

                    if (fit != null)
                    {
                        return ("Remove", worst, fit, worstP);
                    }
                }
            }

            return null;
        }
    }
}