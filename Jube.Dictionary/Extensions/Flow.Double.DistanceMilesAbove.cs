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
            [Description("True when: the value is latitude 1. The distance to the second point is above the miles. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceMilesAbove(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the miles. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceMilesAbove(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the miles. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceMilesAbove(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the miles. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceMilesAbove(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is above the miles. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceMilesAbove(flow.Value, longitude1, latitude2, longitude2, miles));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the miles. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().MatchDistanceMilesAbove(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the miles. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().RejectDistanceMilesAbove(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the miles. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().BreakDistanceMilesAbove(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the miles. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().RequireDistanceMilesAbove(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is above the miles. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceMilesAbove([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().EnsureDistanceMilesAbove(longitude1, latitude2, longitude2, miles);
            }
        }
    }
}
