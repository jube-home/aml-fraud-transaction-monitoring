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
            [Description("True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            [Description("True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            [Description("True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            [Description("True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }

            [Description("True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIncreasedByMoreThanPercent(flow.Value, previous, percent));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return value.Start().MatchIncreasedByMoreThanPercent(previous, percent);
            }

            [Description("Begins a pipeline from the value. True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return value.Start().RejectIncreasedByMoreThanPercent(previous, percent);
            }

            [Description("Begins a pipeline from the value. True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return value.Start().BreakIncreasedByMoreThanPercent(previous, percent);
            }

            [Description("Begins a pipeline from the value. True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return value.Start().RequireIncreasedByMoreThanPercent(previous, percent);
            }

            [Description("Begins a pipeline from the value. True when: the percentage increase over the previous value exceeds the percentage. An increase from zero is infinite, so true. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIncreasedByMoreThanPercent([Description("The earlier value the change is measured from.")] double previous,
                [Description("The percentage increase that counts.")] double percent)
            {
                return value.Start().EnsureIncreasedByMoreThanPercent(previous, percent);
            }
        }
    }
}
