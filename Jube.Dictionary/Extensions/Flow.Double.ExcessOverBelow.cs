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
            public Flow<double> MatchExcessOverBelow(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleExcessOverBelow(flow.Value, limit, bound));
            }

            public Flow<double> RejectExcessOverBelow(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleExcessOverBelow(flow.Value, limit, bound));
            }

            public Flow<double> BreakExcessOverBelow(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleExcessOverBelow(flow.Value, limit, bound));
            }

            public Flow<double> RequireExcessOverBelow(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleExcessOverBelow(flow.Value, limit, bound));
            }

            public Flow<double> EnsureExcessOverBelow(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleExcessOverBelow(flow.Value, limit, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchExcessOverBelow(double limit, double bound)
            {
                return value.Start().MatchExcessOverBelow(limit, bound);
            }

            public Flow<double> RejectExcessOverBelow(double limit, double bound)
            {
                return value.Start().RejectExcessOverBelow(limit, bound);
            }

            public Flow<double> BreakExcessOverBelow(double limit, double bound)
            {
                return value.Start().BreakExcessOverBelow(limit, bound);
            }

            public Flow<double> RequireExcessOverBelow(double limit, double bound)
            {
                return value.Start().RequireExcessOverBelow(limit, bound);
            }

            public Flow<double> EnsureExcessOverBelow(double limit, double bound)
            {
                return value.Start().EnsureExcessOverBelow(limit, bound);
            }
        }
    }
}
