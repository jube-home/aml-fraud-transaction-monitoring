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
            public Flow<double> MatchImbalanceWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImbalanceWithAbove(flow.Value, other, bound));
            }

            public Flow<double> RejectImbalanceWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImbalanceWithAbove(flow.Value, other, bound));
            }

            public Flow<double> BreakImbalanceWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImbalanceWithAbove(flow.Value, other, bound));
            }

            public Flow<double> RequireImbalanceWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImbalanceWithAbove(flow.Value, other, bound));
            }

            public Flow<double> EnsureImbalanceWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImbalanceWithAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchImbalanceWithAbove(double other, double bound)
            {
                return value.Start().MatchImbalanceWithAbove(other, bound);
            }

            public Flow<double> RejectImbalanceWithAbove(double other, double bound)
            {
                return value.Start().RejectImbalanceWithAbove(other, bound);
            }

            public Flow<double> BreakImbalanceWithAbove(double other, double bound)
            {
                return value.Start().BreakImbalanceWithAbove(other, bound);
            }

            public Flow<double> RequireImbalanceWithAbove(double other, double bound)
            {
                return value.Start().RequireImbalanceWithAbove(other, bound);
            }

            public Flow<double> EnsureImbalanceWithAbove(double other, double bound)
            {
                return value.Start().EnsureImbalanceWithAbove(other, bound);
            }
        }
    }
}
