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
            public Flow<double> MatchOutsideRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleOutsideRange(flow.Value, minimum, maximum));
            }

            public Flow<double> RejectOutsideRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleOutsideRange(flow.Value, minimum, maximum));
            }

            public Flow<double> BreakOutsideRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleOutsideRange(flow.Value, minimum, maximum));
            }

            public Flow<double> RequireOutsideRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleOutsideRange(flow.Value, minimum, maximum));
            }

            public Flow<double> EnsureOutsideRange(double minimum, double maximum)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleOutsideRange(flow.Value, minimum, maximum));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchOutsideRange(double minimum, double maximum)
            {
                return value.Start().MatchOutsideRange(minimum, maximum);
            }

            public Flow<double> RejectOutsideRange(double minimum, double maximum)
            {
                return value.Start().RejectOutsideRange(minimum, maximum);
            }

            public Flow<double> BreakOutsideRange(double minimum, double maximum)
            {
                return value.Start().BreakOutsideRange(minimum, maximum);
            }

            public Flow<double> RequireOutsideRange(double minimum, double maximum)
            {
                return value.Start().RequireOutsideRange(minimum, maximum);
            }

            public Flow<double> EnsureOutsideRange(double minimum, double maximum)
            {
                return value.Start().EnsureOutsideRange(minimum, maximum);
            }
        }
    }
}
