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
            [Description("True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }

            [Description("True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHerfindahlWithOutsideRange(flow.Value, lowerBound, upperBound, others));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().MatchHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RejectHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().BreakHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().RequireHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }

            [Description("Begins a pipeline from the value. True when: how concentrated the total is in a few of the value and the others, from 1 over the count to 1 is below the lower or above the upper bound. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHerfindahlWithOutsideRange([Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound,
                [Description("The other numbers.")] params double[] others)
            {
                return value.Start().EnsureHerfindahlWithOutsideRange(lowerBound, upperBound, others);
            }
        }
    }
}
