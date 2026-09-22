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
            public Flow<double> MatchNotEqual(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleNotEqual(flow.Value, other));
            }

            public Flow<double> RejectNotEqual(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleNotEqual(flow.Value, other));
            }

            public Flow<double> BreakNotEqual(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleNotEqual(flow.Value, other));
            }

            public Flow<double> RequireNotEqual(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleNotEqual(flow.Value, other));
            }

            public Flow<double> EnsureNotEqual(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleNotEqual(flow.Value, other));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchNotEqual(double other)
            {
                return value.Start().MatchNotEqual(other);
            }

            public Flow<double> RejectNotEqual(double other)
            {
                return value.Start().RejectNotEqual(other);
            }

            public Flow<double> BreakNotEqual(double other)
            {
                return value.Start().BreakNotEqual(other);
            }

            public Flow<double> RequireNotEqual(double other)
            {
                return value.Start().RequireNotEqual(other);
            }

            public Flow<double> EnsureNotEqual(double other)
            {
                return value.Start().EnsureNotEqual(other);
            }
        }
    }
}
