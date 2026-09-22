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
            public Flow<double> MatchImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> RejectImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> BreakImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> RequireImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            public Flow<double> EnsureImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().MatchImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> RejectImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().RejectImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> BreakImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().BreakImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> RequireImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().RequireImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            public Flow<double> EnsureImpliedSpeedMphAbove(double longitude1, double latitude2, double longitude2, double hoursElapsed, double milesPerHour)
            {
                return value.Start().EnsureImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }
        }
    }
}
