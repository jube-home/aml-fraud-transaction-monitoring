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
            public Flow<double> MatchDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            public Flow<double> RejectDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            public Flow<double> BreakDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            public Flow<double> RequireDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            public Flow<double> EnsureDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return value.Start().MatchDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            public Flow<double> RejectDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return value.Start().RejectDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            public Flow<double> BreakDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return value.Start().BreakDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            public Flow<double> RequireDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return value.Start().RequireDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            public Flow<double> EnsureDistanceMilesBelow(double longitude1, double latitude2, double longitude2, double miles)
            {
                return value.Start().EnsureDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }
        }
    }
}
