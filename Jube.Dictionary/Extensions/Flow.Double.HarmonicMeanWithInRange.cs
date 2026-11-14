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
            [Description("True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }

            [Description("True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHarmonicMeanWithInRange(flow.Value, other, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().MatchHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RejectHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().BreakHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().RequireHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }

            [Description("Begins a pipeline from the value. True when: the harmonic mean of the value and another, for averaging rates is between the bounds, both included. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureHarmonicMeanWithInRange([Description("The other number.")] double other,
                [Description("The lower bound the calculation is compared with.")] double lowerBound,
                [Description("The upper bound the calculation is compared with.")] double upperBound)
            {
                return value.Start().EnsureHarmonicMeanWithInRange(other, lowerBound, upperBound);
            }
        }
    }
}
