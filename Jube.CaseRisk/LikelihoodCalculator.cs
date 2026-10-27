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

using Jube.Dto.Options;

namespace Jube.CaseRisk
{
    public static class LikelihoodCalculator
    {
        public static double Logit(double probability) => Math.Log(probability / (1 - probability));

        public static double Sigmoid(double logOdds) => 1 / (1 + Math.Exp(-logOdds));

        public static double EstimateBaseRate(int confirmed, int closed, CaseRiskSettings settings)
        {
            var estimate = (confirmed + settings.PriorStrength * settings.BaseRatePrior)
                           / (closed + settings.PriorStrength);

            return Clamp(estimate, settings);
        }

        public static CaseLikelihoodBand BandOf(double probability) => probability switch
        {
            < 0.25 => CaseLikelihoodBand.Unlikely,
            < 0.5 => CaseLikelihoodBand.Possible,
            < 0.75 => CaseLikelihoodBand.Likely,
            _ => CaseLikelihoodBand.VeryLikely
        };

        public static LikelihoodAssessment Compute(double baseRate, int closedBehindBaseRate,
            IReadOnlyCollection<EvidenceItem> evidence, CaseRiskSettings settings)
        {
            var clampedBase = Clamp(baseRate, settings);
            var baseLogit = Logit(clampedBase);

            var scored = evidence
                .Where(e => e.Closed > 0)
                .Select((e, index) =>
                {
                    var precision = (e.Confirmed + settings.PriorStrength * clampedBase)
                                    / (e.Closed + settings.PriorStrength);
                    var llr = Logit(Clamp(precision, settings)) - baseLogit;
                    var decay = e.HalfLifeDays > 0 ? Math.Pow(0.5, e.AgeDays / e.HalfLifeDays) : 1;

                    return (Item: e, Index: index, Precision: precision, Llr: llr, Decay: decay);
                })
                .ToList();

            var contributions = new List<Contribution>();

            foreach (var group in scored.GroupBy(s => s.Item.DependenceGroup ?? "#" + s.Index))
            {
                var ordered = group.OrderByDescending(s => Math.Abs(s.Llr * s.Decay)).ToList();

                for (var position = 0; position < ordered.Count; position++)
                {
                    var s = ordered[position];
                    var weight = position == 0 ? 1.0 : settings.Rho;

                    contributions.Add(new Contribution(s.Item.Family, s.Item.Label, s.Item.Source,
                        s.Item.DependenceGroup, s.Item.Confirmed, s.Item.Closed, s.Precision, s.Llr, s.Decay,
                        weight, settings.Kappa * s.Llr * s.Decay * weight, position > 0));
                }
            }

            contributions = contributions.OrderByDescending(c => Math.Abs(c.LogOdds)).ToList();

            var probability = Clamp(Sigmoid(baseLogit + contributions.Sum(c => c.LogOdds)), settings);

            return new LikelihoodAssessment(probability, BandOf(probability), clampedBase, closedBehindBaseRate,
                CaseRiskMethod.ColdStart, contributions);
        }

        private static double Clamp(double probability, CaseRiskSettings settings) =>
            Math.Min(1 - settings.ProbabilityFloor, Math.Max(settings.ProbabilityFloor, probability));
    }
}
