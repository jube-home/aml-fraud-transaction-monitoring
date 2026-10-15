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

using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class CategoricalIntervals
    {
        public static CategoricalEstimate Proportion(double events, double trials, double level,
            ProportionIntervalMethod method)
        {
            if (trials <= 0d)
            {
                return new CategoricalEstimate { Estimate = double.NaN, Lower = double.NaN, Upper = double.NaN };
            }

            var p = events / trials;
            var alpha = 1d - level;
            var z = CategoricalGuards.TwoSidedZ(level);
            double lower;
            double upper;

            switch (method)
            {
                case ProportionIntervalMethod.Wald:
                {
                    var half = z * Math.Sqrt(p * (1d - p) / trials);
                    lower = p - half;
                    upper = p + half;
                    break;
                }
                case ProportionIntervalMethod.AgrestiCoull:
                {
                    var adjusted = trials + z * z;
                    var centre = (events + z * z / 2d) / adjusted;
                    var half = z * Math.Sqrt(centre * (1d - centre) / adjusted);
                    lower = centre - half;
                    upper = centre + half;
                    break;
                }
                case ProportionIntervalMethod.ClopperPearson:
                    lower = events <= 0d
                        ? 0d
                        : CategoricalLibrary.BetaQuantile(alpha / 2d, events, trials - events + 1d);

                    upper = events >= trials
                        ? 1d
                        : CategoricalLibrary.BetaQuantile(1d - alpha / 2d, events + 1d, trials - events);

                    break;
                case ProportionIntervalMethod.Jeffreys:
                    lower = events <= 0d
                        ? 0d
                        : CategoricalLibrary.BetaQuantile(alpha / 2d, events + 0.5, trials - events + 0.5);

                    upper = events >= trials
                        ? 1d
                        : CategoricalLibrary.BetaQuantile(1d - alpha / 2d, events + 0.5, trials - events + 0.5);

                    break;
                default:
                {
                    var denominator = 1d + z * z / trials;
                    var centre = (p + z * z / (2d * trials)) / denominator;
                    var half = z * Math.Sqrt(p * (1d - p) / trials + z * z / (4d * trials * trials)) / denominator;
                    lower = centre - half;
                    upper = centre + half;
                    break;
                }
            }

            return new CategoricalEstimate
            {
                Estimate = p,
                Lower = Math.Max(0d, lower),
                Upper = Math.Min(1d, upper)
            };
        }

        public static CategoricalEstimate PoissonRate(double events, double exposure, double level)
        {
            var alpha = 1d - level;
            var lower = events <= 0d ? 0d : CategoricalLibrary.ChiSquareQuantile(alpha / 2d, 2d * events) / 2d;
            var upper = CategoricalLibrary.ChiSquareQuantile(1d - alpha / 2d, 2d * (events + 1d)) / 2d;

            return new CategoricalEstimate
            {
                Estimate = events / exposure,
                Lower = lower / exposure,
                Upper = upper / exposure
            };
        }
    }
}