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
            public Flow<double> MatchHasValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHasValue(flow.Value));
            }

            public Flow<double> RejectHasValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHasValue(flow.Value));
            }

            public Flow<double> BreakHasValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHasValue(flow.Value));
            }

            public Flow<double> RequireHasValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHasValue(flow.Value));
            }

            public Flow<double> EnsureHasValue()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHasValue(flow.Value));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHasValue()
            {
                return value.Start().MatchHasValue();
            }

            public Flow<double> RejectHasValue()
            {
                return value.Start().RejectHasValue();
            }

            public Flow<double> BreakHasValue()
            {
                return value.Start().BreakHasValue();
            }

            public Flow<double> RequireHasValue()
            {
                return value.Start().RequireHasValue();
            }

            public Flow<double> EnsureHasValue()
            {
                return value.Start().EnsureHasValue();
            }
        }
    }
}
