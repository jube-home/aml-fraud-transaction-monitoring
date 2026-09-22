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
            public Flow<double> MatchDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> RejectDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> BreakDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> RequireDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDifferenceFromBelow(flow.Value, other, bound));
            }

            public Flow<double> EnsureDifferenceFromBelow(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDifferenceFromBelow(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchDifferenceFromBelow(double other, double bound)
            {
                return value.Start().MatchDifferenceFromBelow(other, bound);
            }

            public Flow<double> RejectDifferenceFromBelow(double other, double bound)
            {
                return value.Start().RejectDifferenceFromBelow(other, bound);
            }

            public Flow<double> BreakDifferenceFromBelow(double other, double bound)
            {
                return value.Start().BreakDifferenceFromBelow(other, bound);
            }

            public Flow<double> RequireDifferenceFromBelow(double other, double bound)
            {
                return value.Start().RequireDifferenceFromBelow(other, bound);
            }

            public Flow<double> EnsureDifferenceFromBelow(double other, double bound)
            {
                return value.Start().EnsureDifferenceFromBelow(other, bound);
            }
        }
    }
}
