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
        extension(Flow<bool> flow)
        {
            public Flow<bool> MatchIsFalse()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.BooleanIsFalse(flow.Value));
            }

            public Flow<bool> RejectIsFalse()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.BooleanIsFalse(flow.Value));
            }

            public Flow<bool> BreakIsFalse()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.BooleanIsFalse(flow.Value));
            }

            public Flow<bool> RequireIsFalse()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.BooleanIsFalse(flow.Value));
            }

            public Flow<bool> EnsureIsFalse()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.BooleanIsFalse(flow.Value));
            }
        }

        extension(bool value)
        {
            public Flow<bool> MatchIsFalse()
            {
                return value.Start().MatchIsFalse();
            }

            public Flow<bool> RejectIsFalse()
            {
                return value.Start().RejectIsFalse();
            }

            public Flow<bool> BreakIsFalse()
            {
                return value.Start().BreakIsFalse();
            }

            public Flow<bool> RequireIsFalse()
            {
                return value.Start().RequireIsFalse();
            }

            public Flow<bool> EnsureIsFalse()
            {
                return value.Start().EnsureIsFalse();
            }
        }
    }
}
