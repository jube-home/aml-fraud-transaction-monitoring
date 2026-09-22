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
            public Flow<double> MatchPercentOfBelow(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoublePercentOfBelow(flow.Value, total, bound));
            }

            public Flow<double> RejectPercentOfBelow(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoublePercentOfBelow(flow.Value, total, bound));
            }

            public Flow<double> BreakPercentOfBelow(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoublePercentOfBelow(flow.Value, total, bound));
            }

            public Flow<double> RequirePercentOfBelow(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoublePercentOfBelow(flow.Value, total, bound));
            }

            public Flow<double> EnsurePercentOfBelow(double total, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoublePercentOfBelow(flow.Value, total, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchPercentOfBelow(double total, double bound)
            {
                return value.Start().MatchPercentOfBelow(total, bound);
            }

            public Flow<double> RejectPercentOfBelow(double total, double bound)
            {
                return value.Start().RejectPercentOfBelow(total, bound);
            }

            public Flow<double> BreakPercentOfBelow(double total, double bound)
            {
                return value.Start().BreakPercentOfBelow(total, bound);
            }

            public Flow<double> RequirePercentOfBelow(double total, double bound)
            {
                return value.Start().RequirePercentOfBelow(total, bound);
            }

            public Flow<double> EnsurePercentOfBelow(double total, double bound)
            {
                return value.Start().EnsurePercentOfBelow(total, bound);
            }
        }
    }
}
