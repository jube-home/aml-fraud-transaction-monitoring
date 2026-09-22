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
            public Flow<double> MatchLess(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleLess(flow.Value, other));
            }

            public Flow<double> RejectLess(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleLess(flow.Value, other));
            }

            public Flow<double> BreakLess(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleLess(flow.Value, other));
            }

            public Flow<double> RequireLess(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleLess(flow.Value, other));
            }

            public Flow<double> EnsureLess(double other)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleLess(flow.Value, other));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchLess(double other)
            {
                return value.Start().MatchLess(other);
            }

            public Flow<double> RejectLess(double other)
            {
                return value.Start().RejectLess(other);
            }

            public Flow<double> BreakLess(double other)
            {
                return value.Start().BreakLess(other);
            }

            public Flow<double> RequireLess(double other)
            {
                return value.Start().RequireLess(other);
            }

            public Flow<double> EnsureLess(double other)
            {
                return value.Start().EnsureLess(other);
            }
        }
    }
}
