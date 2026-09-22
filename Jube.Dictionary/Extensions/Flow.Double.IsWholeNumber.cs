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
            public Flow<double> MatchIsWholeNumber()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsWholeNumber(flow.Value));
            }

            public Flow<double> RejectIsWholeNumber()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsWholeNumber(flow.Value));
            }

            public Flow<double> BreakIsWholeNumber()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsWholeNumber(flow.Value));
            }

            public Flow<double> RequireIsWholeNumber()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsWholeNumber(flow.Value));
            }

            public Flow<double> EnsureIsWholeNumber()
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsWholeNumber(flow.Value));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsWholeNumber()
            {
                return value.Start().MatchIsWholeNumber();
            }

            public Flow<double> RejectIsWholeNumber()
            {
                return value.Start().RejectIsWholeNumber();
            }

            public Flow<double> BreakIsWholeNumber()
            {
                return value.Start().BreakIsWholeNumber();
            }

            public Flow<double> RequireIsWholeNumber()
            {
                return value.Start().RequireIsWholeNumber();
            }

            public Flow<double> EnsureIsWholeNumber()
            {
                return value.Start().EnsureIsWholeNumber();
            }
        }
    }
}
