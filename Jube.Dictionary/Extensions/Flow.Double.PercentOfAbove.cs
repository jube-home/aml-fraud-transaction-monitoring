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
            public Flow<double> MatchPercentOfAbove(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentOfAbove(flow.Value, total, bound));
            }

            public Flow<double> RejectPercentOfAbove(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentOfAbove(flow.Value, total, bound));
            }

            public Flow<double> BreakPercentOfAbove(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentOfAbove(flow.Value, total, bound));
            }

            public Flow<double> RequirePercentOfAbove(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentOfAbove(flow.Value, total, bound));
            }

            public Flow<double> EnsurePercentOfAbove(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentOfAbove(flow.Value, total, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentOfAbove(double total, double bound)
            {
                return value.Start().MatchPercentOfAbove(total, bound);
            }

            public Flow<double> RejectPercentOfAbove(double total, double bound)
            {
                return value.Start().RejectPercentOfAbove(total, bound);
            }

            public Flow<double> BreakPercentOfAbove(double total, double bound)
            {
                return value.Start().BreakPercentOfAbove(total, bound);
            }

            public Flow<double> RequirePercentOfAbove(double total, double bound)
            {
                return value.Start().RequirePercentOfAbove(total, bound);
            }

            public Flow<double> EnsurePercentOfAbove(double total, double bound)
            {
                return value.Start().EnsurePercentOfAbove(total, bound);
            }
        }
    }
}
