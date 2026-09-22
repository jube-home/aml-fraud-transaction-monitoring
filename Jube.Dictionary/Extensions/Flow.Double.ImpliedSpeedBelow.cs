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
            public Flow<double> MatchImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            public Flow<double> RejectImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            public Flow<double> BreakImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            public Flow<double> RequireImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            public Flow<double> EnsureImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return value.Start().MatchImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            public Flow<double> RejectImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return value.Start().RejectImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            public Flow<double> BreakImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return value.Start().BreakImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            public Flow<double> RequireImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return value.Start().RequireImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            public Flow<double> EnsureImpliedSpeedBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double kilometersPerHour)
            {
                return value.Start().EnsureImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }
        }
    }
}
