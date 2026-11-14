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
            [Description("True when: the value is latitude 1. The implied speed is below the limit. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed is below the limit. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed is below the limit. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed is below the limit. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }

            [Description("True when: the value is latitude 1. The implied speed is below the limit. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleImpliedSpeedBelow(flow.Value, longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed is below the limit. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().MatchImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed is below the limit. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().RejectImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed is below the limit. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().BreakImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed is below the limit. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().RequireImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The implied speed is below the limit. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureImpliedSpeedBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The hours between the two positions.")] double hoursElapsed,
                [Description("The speed in kilometres per hour that counts.")] double kilometersPerHour)
            {
                return value.Start().EnsureImpliedSpeedBelow(longitude1, latitude2, longitude2, hoursElapsed, kilometersPerHour);
            }
        }
    }
}
