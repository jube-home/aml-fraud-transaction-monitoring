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
            public Flow<double> MatchOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleOutsideRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            public Flow<double> RejectOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleOutsideRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            public Flow<double> BreakOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleOutsideRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            public Flow<double> RequireOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleOutsideRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            public Flow<double> EnsureOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleOutsideRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return value.Start().MatchOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            public Flow<double> RejectOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return value.Start().RejectOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            public Flow<double> BreakOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return value.Start().BreakOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            public Flow<double> RequireOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return value.Start().RequireOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            public Flow<double> EnsureOutsideRadiusKm(double longitude1, double latitude2, double longitude2, double radiusKm)
            {
                return value.Start().EnsureOutsideRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }
        }
    }
}
