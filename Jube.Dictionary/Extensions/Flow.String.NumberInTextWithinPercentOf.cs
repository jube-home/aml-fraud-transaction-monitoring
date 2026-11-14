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
            [Description("True when: a number found in the text is within the tolerance percentage of the target. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            [Description("True when: a number found in the text is within the tolerance percentage of the target. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            [Description("True when: a number found in the text is within the tolerance percentage of the target. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            [Description("True when: a number found in the text is within the tolerance percentage of the target. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }

            [Description("True when: a number found in the text is within the tolerance percentage of the target. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.StringNumberInTextWithinPercentOf(flow.Value, target, tolerancePercent));
            }
        }

        extension(string? value)
        {
            [Description("Begins a pipeline from the value. True when: a number found in the text is within the tolerance percentage of the target. Decides matched if so; otherwise continues.")]
            public Flow<string?> MatchNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return value.Start().MatchNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            [Description("Begins a pipeline from the value. True when: a number found in the text is within the tolerance percentage of the target. Decides rejected if so; otherwise continues.")]
            public Flow<string?> RejectNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return value.Start().RejectNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            [Description("Begins a pipeline from the value. True when: a number found in the text is within the tolerance percentage of the target. Decides broken if so; otherwise continues.")]
            public Flow<string?> BreakNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return value.Start().BreakNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            [Description("Begins a pipeline from the value. True when: a number found in the text is within the tolerance percentage of the target. Continues if so; otherwise decides rejected.")]
            public Flow<string?> RequireNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return value.Start().RequireNumberInTextWithinPercentOf(target, tolerancePercent);
            }

            [Description("Begins a pipeline from the value. True when: a number found in the text is within the tolerance percentage of the target. Continues if so; otherwise decides broken.")]
            public Flow<string?> EnsureNumberInTextWithinPercentOf([Description("The number the numbers found in the text are compared with.")] double target,
                [Description("How far from the target still counts, as a percentage of it.")] double tolerancePercent)
            {
                return value.Start().EnsureNumberInTextWithinPercentOf(target, tolerancePercent);
            }
        }
    }
}