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
            public Flow<double> MatchHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHeadroomToInRange(flow.Value, limit, lowerBound, upperBound));
            }

            public Flow<double> RejectHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHeadroomToInRange(flow.Value, limit, lowerBound, upperBound));
            }

            public Flow<double> BreakHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHeadroomToInRange(flow.Value, limit, lowerBound, upperBound));
            }

            public Flow<double> RequireHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHeadroomToInRange(flow.Value, limit, lowerBound, upperBound));
            }

            public Flow<double> EnsureHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHeadroomToInRange(flow.Value, limit, lowerBound, upperBound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return value.Start().MatchHeadroomToInRange(limit, lowerBound, upperBound);
            }

            public Flow<double> RejectHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return value.Start().RejectHeadroomToInRange(limit, lowerBound, upperBound);
            }

            public Flow<double> BreakHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return value.Start().BreakHeadroomToInRange(limit, lowerBound, upperBound);
            }

            public Flow<double> RequireHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return value.Start().RequireHeadroomToInRange(limit, lowerBound, upperBound);
            }

            public Flow<double> EnsureHeadroomToInRange(double limit, double lowerBound, double upperBound)
            {
                return value.Start().EnsureHeadroomToInRange(limit, lowerBound, upperBound);
            }
        }
    }
}
