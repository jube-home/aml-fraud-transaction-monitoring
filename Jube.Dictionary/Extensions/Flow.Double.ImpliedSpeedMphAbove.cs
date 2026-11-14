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
            [Description("True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedMphAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, milesPerHour));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return value.Start().MatchImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return value.Start().RejectImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return value.Start().BreakImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return value.Start().RequireImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed in miles per hour is above the limit. Zero hours is infinite speed. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedMphAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in miles per hour that counts.")] double milesPerHour)
            {
                return value.Start().EnsureImpliedSpeedMphAbove(longitude1, latitude2, longitude2, hoursElapsed, milesPerHour);
            }
        }
    }
}
