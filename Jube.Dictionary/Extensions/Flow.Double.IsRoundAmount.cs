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
            [Description("True when: an exact multiple of the rounding unit, for example 1000. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            [Description("True when: an exact multiple of the rounding unit, for example 1000. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            [Description("True when: an exact multiple of the rounding unit, for example 1000. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            [Description("True when: an exact multiple of the rounding unit, for example 1000. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }

            [Description("True when: an exact multiple of the rounding unit, for example 1000. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleIsRoundAmount(flow.Value, nearest));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: an exact multiple of the rounding unit, for example 1000. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return value.Start().MatchIsRoundAmount(nearest);
            }

            [Description("Begins a pipeline from the value. True when: an exact multiple of the rounding unit, for example 1000. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return value.Start().RejectIsRoundAmount(nearest);
            }

            [Description("Begins a pipeline from the value. True when: an exact multiple of the rounding unit, for example 1000. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return value.Start().BreakIsRoundAmount(nearest);
            }

            [Description("Begins a pipeline from the value. True when: an exact multiple of the rounding unit, for example 1000. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return value.Start().RequireIsRoundAmount(nearest);
            }

            [Description("Begins a pipeline from the value. True when: an exact multiple of the rounding unit, for example 1000. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsRoundAmount([Description("The multiple to test against (e.g. 100 or 1000).")] double nearest)
            {
                return value.Start().EnsureIsRoundAmount(nearest);
            }
        }
    }
}
