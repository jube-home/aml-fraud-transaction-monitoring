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
            [Description("True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceKmAbove(flow.Value, longitude1, latitude2, longitude2, kilometers));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceKmAbove(flow.Value, longitude1, latitude2, longitude2, kilometers));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceKmAbove(flow.Value, longitude1, latitude2, longitude2, kilometers));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the kilometres. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceKmAbove(flow.Value, longitude1, latitude2, longitude2, kilometers));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the kilometres. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceKmAbove(flow.Value, longitude1, latitude2, longitude2, kilometers));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return value.Start().MatchDistanceKmAbove(longitude1, latitude2, longitude2, kilometers);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return value.Start().RejectDistanceKmAbove(longitude1, latitude2, longitude2, kilometers);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the kilometres. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return value.Start().BreakDistanceKmAbove(longitude1, latitude2, longitude2, kilometers);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the kilometres. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return value.Start().RequireDistanceKmAbove(longitude1, latitude2, longitude2, kilometers);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the kilometres. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceKmAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in kilometres that counts.")] double kilometers)
            {
                return value.Start().EnsureDistanceKmAbove(longitude1, latitude2, longitude2, kilometers);
            }
        }
    }
}
