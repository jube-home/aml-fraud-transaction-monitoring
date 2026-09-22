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
            public Flow<double> MatchComplementAbove(double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            public Flow<double> RejectComplementAbove(double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            public Flow<double> BreakComplementAbove(double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            public Flow<double> RequireComplementAbove(double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }

            public Flow<double> EnsureComplementAbove(double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleComplementAbove(flow.Value, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchComplementAbove(double bound)
            {
                return value.Start().MatchComplementAbove(bound);
            }

            public Flow<double> RejectComplementAbove(double bound)
            {
                return value.Start().RejectComplementAbove(bound);
            }

            public Flow<double> BreakComplementAbove(double bound)
            {
                return value.Start().BreakComplementAbove(bound);
            }

            public Flow<double> RequireComplementAbove(double bound)
            {
                return value.Start().RequireComplementAbove(bound);
            }

            public Flow<double> EnsureComplementAbove(double bound)
            {
                return value.Start().EnsureComplementAbove(bound);
            }
        }
    }
}
