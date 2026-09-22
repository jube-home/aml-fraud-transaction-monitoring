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
            public Flow<double> MatchGeometricMeanWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            public Flow<double> RejectGeometricMeanWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            public Flow<double> BreakGeometricMeanWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            public Flow<double> RequireGeometricMeanWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }

            public Flow<double> EnsureGeometricMeanWithAbove(double other, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleGeometricMeanWithAbove(flow.Value, other, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchGeometricMeanWithAbove(double other, double bound)
            {
                return value.Start().MatchGeometricMeanWithAbove(other, bound);
            }

            public Flow<double> RejectGeometricMeanWithAbove(double other, double bound)
            {
                return value.Start().RejectGeometricMeanWithAbove(other, bound);
            }

            public Flow<double> BreakGeometricMeanWithAbove(double other, double bound)
            {
                return value.Start().BreakGeometricMeanWithAbove(other, bound);
            }

            public Flow<double> RequireGeometricMeanWithAbove(double other, double bound)
            {
                return value.Start().RequireGeometricMeanWithAbove(other, bound);
            }

            public Flow<double> EnsureGeometricMeanWithAbove(double other, double bound)
            {
                return value.Start().EnsureGeometricMeanWithAbove(other, bound);
            }
        }
    }
}
