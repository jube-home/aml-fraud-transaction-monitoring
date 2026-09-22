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
            public Flow<double> MatchImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedMphBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> RejectImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedMphBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> BreakImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedMphBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> RequireImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedMphBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> EnsureImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedMphBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().MatchImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> RejectImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().RejectImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> BreakImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().BreakImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> RequireImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().RequireImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> EnsureImpliedSpeedMphBelow(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().EnsureImpliedSpeedMphBelow(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }
        }
    }
}
