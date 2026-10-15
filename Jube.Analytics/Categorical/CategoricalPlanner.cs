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

using Jube.Analytics.Categorical.Models;

namespace Jube.Analytics.Categorical
{
    public static class CategoricalPlanner
    {
        public static CategoricalPlan Fit(IReadOnlyList<CategoricalFieldCandidate> candidates,
            CategoricalOptions options)
        {
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentNullException.ThrowIfNull(options);

            var report = new List<CategoricalDiscoveryResult>();

            var fitted =
                new List<(CategoricalFieldCandidate Candidate, CategoricalFieldEncoder Encoder, int MaxLevels)>();

            foreach (var candidate in candidates)
            {
                var decision = Classify(candidate, options, out var reason);

                if (decision != CategoricalDecision.Categorical)
                {
                    report.Add(new CategoricalDiscoveryResult(candidate.SourceName, decision, reason));
                    continue;
                }

                var encoder = CategoricalFieldEncoder.Fit(candidate.SourceName, candidate.Counter, options);

                if (encoder == null)
                {
                    report.Add(new CategoricalDiscoveryResult(candidate.SourceName, CategoricalDecision.SkippedConstant,
                        "The field has a single level and nothing to distinguish it."));

                    continue;
                }

                fitted.Add((candidate, encoder, encoder.Levels.Count));
            }

            var total = fitted.Sum(s => s.Encoder.Indicators.Count);

            while (total > options.MaxTotalIndicators)
            {
                var target = -1;
                var smallest = long.MaxValue;

                for (var i = 0; i < fitted.Count; i++)
                {
                    var levels = fitted[i].Encoder.Levels;

                    if (levels.Count <= 1)
                    {
                        continue;
                    }

                    var count = levels[^1].Count;

                    if (count < smallest)
                    {
                        smallest = count;
                        target = i;
                    }
                }

                if (target >= 0)
                {
                    var entry = fitted[target];

                    var reduced = CategoricalFieldEncoder.Fit(entry.Candidate.SourceName, entry.Candidate.Counter,
                        options, entry.Encoder.Levels.Count - 1);

                    if (reduced == null)
                    {
                        report.Add(new CategoricalDiscoveryResult(entry.Candidate.SourceName,
                            CategoricalDecision.SkippedConstant,
                            "Reducing to the indicator budget left no indicator."));

                        fitted.RemoveAt(target);
                    }
                    else
                    {
                        fitted[target] = (entry.Candidate, reduced, reduced.Levels.Count);
                    }
                }
                else
                {
                    var drop = 0;

                    for (var i = 1; i < fitted.Count; i++)
                    {
                        if (fitted[i].Encoder.TotalCount < fitted[drop].Encoder.TotalCount)
                        {
                            drop = i;
                        }
                    }

                    report.Add(new CategoricalDiscoveryResult(fitted[drop].Candidate.SourceName,
                        CategoricalDecision.SkippedIndicatorBudget, "The total indicator budget was exhausted."));

                    fitted.RemoveAt(drop);
                }

                total = fitted.Sum(s => s.Encoder.Indicators.Count);
            }

            foreach (var entry in fitted)
            {
                report.Add(new CategoricalDiscoveryResult(entry.Candidate.SourceName, CategoricalDecision.Categorical,
                    $"{entry.Encoder.Levels.Count} levels kept, {entry.Encoder.Indicators.Count} indicators."));
            }

            return new CategoricalPlan(fitted.Select(s => s.Encoder).ToList(), report);
        }

        public static CategoricalDecision Classify(CategoricalFieldCandidate candidate, CategoricalOptions options,
            out string reason)
        {
            var counter = candidate.Counter;

            if (options.Exclude.Contains(candidate.SourceName, StringComparer.Ordinal))
            {
                reason = "Excluded by the instance options.";

                return CategoricalDecision.SkippedExcluded;
            }

            if (counter.NonMissing == 0)
            {
                reason = "No values in the sample.";

                return CategoricalDecision.SkippedNoValues;
            }

            var included = options.Include.Contains(candidate.SourceName, StringComparer.Ordinal);

            if (!candidate.DeclaredString && !included)
            {
                var numericShare = (double)counter.NumericLooking / counter.NonMissing;

                if (candidate.DeclaredOther)
                {
                    reason = "Declared as a non-string type.";

                    return CategoricalDecision.NotCategorical;
                }

                if (numericShare >= options.NumericLookingThreshold)
                {
                    reason = $"Undeclared type, {numericShare:P0} of the values look numeric.";

                    return CategoricalDecision.NotCategorical;
                }

                if (numericShare > 1d - options.NumericLookingThreshold)
                {
                    reason = $"Undeclared type and ambiguous, {numericShare:P0} of the values look numeric.";

                    return CategoricalDecision.NotCategorical;
                }

                reason = "Undeclared type, inferred categorical from the sample.";
            }
            else
            {
                reason = included ? "Included by the instance options." : "Declared as a string.";
            }

            var ratio = (double)counter.Distinct / counter.NonMissing;

            if (counter.Distinct > options.MaxLevels && ratio > options.IdentifierRatio)
            {
                reason = $"Looks like an identifier: {counter.Distinct} distinct values in {counter.NonMissing} rows.";

                return CategoricalDecision.SkippedIdentifier;
            }

            return CategoricalDecision.Categorical;
        }
    }
}