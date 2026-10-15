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
        extension(Flow<string?> flow)
        {
            public Flow<string?> MatchNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            public Flow<string?> RejectNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            public Flow<string?> BreakNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            public Flow<string?> RequireNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            public Flow<string?> EnsureNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }
        }

        extension(string? value)
        {
            public Flow<string?> MatchNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return value.Start().MatchNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            public Flow<string?> RejectNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return value.Start().RejectNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            public Flow<string?> BreakNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return value.Start().BreakNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            public Flow<string?> RequireNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return value.Start().RequireNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            public Flow<string?> EnsureNumberInTextWithinPercentOf(double target, double tolerancePercent)
            {
                return value.Start().EnsureNumberInTextWithinPercentOf(target, tolerancePercent);
            }
        }
    }
}