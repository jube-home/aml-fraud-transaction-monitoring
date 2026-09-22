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
            public Flow<double> MatchSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> RejectSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> BreakSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> RequireSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }

            public Flow<double> EnsureSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleSlopeFromAbove(flow.Value, previous, elapsed, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return value.Start().MatchSlopeFromAbove(previous, elapsed, bound);
            }

            public Flow<double> RejectSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return value.Start().RejectSlopeFromAbove(previous, elapsed, bound);
            }

            public Flow<double> BreakSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return value.Start().BreakSlopeFromAbove(previous, elapsed, bound);
            }

            public Flow<double> RequireSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return value.Start().RequireSlopeFromAbove(previous, elapsed, bound);
            }

            public Flow<double> EnsureSlopeFromAbove(double previous, double elapsed, double bound)
            {
                return value.Start().EnsureSlopeFromAbove(previous, elapsed, bound);
            }
        }
    }
}
