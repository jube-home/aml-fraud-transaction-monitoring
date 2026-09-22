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
            public Flow<double> MatchInRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleInRange(flow.Value, minimum, maximum));
            }

            public Flow<double> RejectInRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleInRange(flow.Value, minimum, maximum));
            }

            public Flow<double> BreakInRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleInRange(flow.Value, minimum, maximum));
            }

            public Flow<double> RequireInRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleInRange(flow.Value, minimum, maximum));
            }

            public Flow<double> EnsureInRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleInRange(flow.Value, minimum, maximum));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchInRange(double minimum, double maximum)
            {
                return value.Start().MatchInRange(minimum, maximum);
            }

            public Flow<double> RejectInRange(double minimum, double maximum)
            {
                return value.Start().RejectInRange(minimum, maximum);
            }

            public Flow<double> BreakInRange(double minimum, double maximum)
            {
                return value.Start().BreakInRange(minimum, maximum);
            }

            public Flow<double> RequireInRange(double minimum, double maximum)
            {
                return value.Start().RequireInRange(minimum, maximum);
            }

            public Flow<double> EnsureInRange(double minimum, double maximum)
            {
                return value.Start().EnsureInRange(minimum, maximum);
            }
        }
    }
}
