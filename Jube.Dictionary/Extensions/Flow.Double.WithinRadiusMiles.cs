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
            public Flow<double> MatchWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            public Flow<double> RejectWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            public Flow<double> BreakWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            public Flow<double> RequireWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            public Flow<double> EnsureWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return value.Start().MatchWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            public Flow<double> RejectWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return value.Start().RejectWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            public Flow<double> BreakWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return value.Start().BreakWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            public Flow<double> RequireWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return value.Start().RequireWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            public Flow<double> EnsureWithinRadiusMiles(double longitude1, double latitude2, double longitude2, double radiusMiles)
            {
                return value.Start().EnsureWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }
        }
    }
}
