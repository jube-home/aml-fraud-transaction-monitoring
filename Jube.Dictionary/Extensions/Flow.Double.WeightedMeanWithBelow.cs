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
            public Flow<double> MatchWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWeightedMeanWithBelow(flow.Value, weight, other, otherWeight, bound));
            }

            public Flow<double> RejectWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWeightedMeanWithBelow(flow.Value, weight, other, otherWeight, bound));
            }

            public Flow<double> BreakWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWeightedMeanWithBelow(flow.Value, weight, other, otherWeight, bound));
            }

            public Flow<double> RequireWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWeightedMeanWithBelow(flow.Value, weight, other, otherWeight, bound));
            }

            public Flow<double> EnsureWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWeightedMeanWithBelow(flow.Value, weight, other, otherWeight, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return value.Start().MatchWeightedMeanWithBelow(weight, other, otherWeight, bound);
            }

            public Flow<double> RejectWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return value.Start().RejectWeightedMeanWithBelow(weight, other, otherWeight, bound);
            }

            public Flow<double> BreakWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return value.Start().BreakWeightedMeanWithBelow(weight, other, otherWeight, bound);
            }

            public Flow<double> RequireWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return value.Start().RequireWeightedMeanWithBelow(weight, other, otherWeight, bound);
            }

            public Flow<double> EnsureWeightedMeanWithBelow(double weight, double other, double otherWeight, double bound)
            {
                return value.Start().EnsureWeightedMeanWithBelow(weight, other, otherWeight, bound);
            }
        }
    }
}
