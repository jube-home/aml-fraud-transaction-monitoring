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
            public Flow<double> MatchRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            public Flow<double> RejectRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            public Flow<double> BreakRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            public Flow<double> RequireRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }

            public Flow<double> EnsureRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleRatioOfRatiosBelow(flow.Value, denominator, otherNumerator, otherDenominator, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return value.Start().MatchRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            public Flow<double> RejectRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return value.Start().RejectRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            public Flow<double> BreakRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return value.Start().BreakRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            public Flow<double> RequireRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return value.Start().RequireRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }

            public Flow<double> EnsureRatioOfRatiosBelow(double denominator, double otherNumerator, double otherDenominator, double bound)
            {
                return value.Start().EnsureRatioOfRatiosBelow(denominator, otherNumerator, otherDenominator, bound);
            }
        }
    }
}
