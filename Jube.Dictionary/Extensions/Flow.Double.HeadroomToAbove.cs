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
            public Flow<double> MatchHeadroomToAbove(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            public Flow<double> RejectHeadroomToAbove(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            public Flow<double> BreakHeadroomToAbove(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            public Flow<double> RequireHeadroomToAbove(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }

            public Flow<double> EnsureHeadroomToAbove(double limit, double bound)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHeadroomToAbove(flow.Value, limit, bound));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHeadroomToAbove(double limit, double bound)
            {
                return value.Start().MatchHeadroomToAbove(limit, bound);
            }

            public Flow<double> RejectHeadroomToAbove(double limit, double bound)
            {
                return value.Start().RejectHeadroomToAbove(limit, bound);
            }

            public Flow<double> BreakHeadroomToAbove(double limit, double bound)
            {
                return value.Start().BreakHeadroomToAbove(limit, bound);
            }

            public Flow<double> RequireHeadroomToAbove(double limit, double bound)
            {
                return value.Start().RequireHeadroomToAbove(limit, bound);
            }

            public Flow<double> EnsureHeadroomToAbove(double limit, double bound)
            {
                return value.Start().EnsureHeadroomToAbove(limit, bound);
            }
        }
    }
}
