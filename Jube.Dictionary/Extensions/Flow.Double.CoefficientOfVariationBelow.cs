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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        extension(Flow<double> flow)
        {
            public Flow<double> MatchCoefficientOfVariationBelow(double mean, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleCoefficientOfVariationBelow(flow.Value, mean, bound));
            }

            public Flow<double> RejectCoefficientOfVariationBelow(double mean, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleCoefficientOfVariationBelow(flow.Value, mean, bound));
            }

            public Flow<double> BreakCoefficientOfVariationBelow(double mean, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleCoefficientOfVariationBelow(flow.Value, mean, bound));
            }

            public Flow<double> RequireCoefficientOfVariationBelow(double mean, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleCoefficientOfVariationBelow(flow.Value, mean, bound));
            }

            public Flow<double> EnsureCoefficientOfVariationBelow(double mean, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleCoefficientOfVariationBelow(flow.Value, mean, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchCoefficientOfVariationBelow(double mean, double bound)
            {
                return value.Start().MatchCoefficientOfVariationBelow(mean, bound);
            }

            public Flow<double> RejectCoefficientOfVariationBelow(double mean, double bound)
            {
                return value.Start().RejectCoefficientOfVariationBelow(mean, bound);
            }

            public Flow<double> BreakCoefficientOfVariationBelow(double mean, double bound)
            {
                return value.Start().BreakCoefficientOfVariationBelow(mean, bound);
            }

            public Flow<double> RequireCoefficientOfVariationBelow(double mean, double bound)
            {
                return value.Start().RequireCoefficientOfVariationBelow(mean, bound);
            }

            public Flow<double> EnsureCoefficientOfVariationBelow(double mean, double bound)
            {
                return value.Start().EnsureCoefficientOfVariationBelow(mean, bound);
            }
        }
    }
}
