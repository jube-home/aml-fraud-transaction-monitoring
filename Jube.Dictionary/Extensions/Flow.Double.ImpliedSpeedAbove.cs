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
            [Description("True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedAbove(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().MatchImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().RejectImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().BreakImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().RequireImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The speed implied by travelling to the second point in the hours is above the limit (impossible travel). Zero hours is infinite speed. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().EnsureImpliedSpeedAbove(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }
        }
    }
}
