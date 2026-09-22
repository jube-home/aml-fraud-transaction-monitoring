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
            public Flow<double> MatchDifferenceFromAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDifferenceFromAbove(flow.Value, other, bound));
            }

            public Flow<double> RejectDifferenceFromAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDifferenceFromAbove(flow.Value, other, bound));
            }

            public Flow<double> BreakDifferenceFromAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDifferenceFromAbove(flow.Value, other, bound));
            }

            public Flow<double> RequireDifferenceFromAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDifferenceFromAbove(flow.Value, other, bound));
            }

            public Flow<double> EnsureDifferenceFromAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDifferenceFromAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchDifferenceFromAbove(double other, double bound)
            {
                return value.Start().MatchDifferenceFromAbove(other, bound);
            }

            public Flow<double> RejectDifferenceFromAbove(double other, double bound)
            {
                return value.Start().RejectDifferenceFromAbove(other, bound);
            }

            public Flow<double> BreakDifferenceFromAbove(double other, double bound)
            {
                return value.Start().BreakDifferenceFromAbove(other, bound);
            }

            public Flow<double> RequireDifferenceFromAbove(double other, double bound)
            {
                return value.Start().RequireDifferenceFromAbove(other, bound);
            }

            public Flow<double> EnsureDifferenceFromAbove(double other, double bound)
            {
                return value.Start().EnsureDifferenceFromAbove(other, bound);
            }
        }
    }
}
