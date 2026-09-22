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
            public Flow<double> MatchSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSlopeFromBelow(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> RejectSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSlopeFromBelow(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> BreakSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSlopeFromBelow(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> RequireSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSlopeFromBelow(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> EnsureSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSlopeFromBelow(flow.Value, previous, elapsed, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return value.Start().MatchSlopeFromBelow(previous, elapsed, bound);
            }

            public Flow<double> RejectSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return value.Start().RejectSlopeFromBelow(previous, elapsed, bound);
            }

            public Flow<double> BreakSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return value.Start().BreakSlopeFromBelow(previous, elapsed, bound);
            }

            public Flow<double> RequireSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return value.Start().RequireSlopeFromBelow(previous, elapsed, bound);
            }

            public Flow<double> EnsureSlopeFromBelow(double previous, double elapsed, double bound)
            {
                return value.Start().EnsureSlopeFromBelow(previous, elapsed, bound);
            }
        }
    }
}
