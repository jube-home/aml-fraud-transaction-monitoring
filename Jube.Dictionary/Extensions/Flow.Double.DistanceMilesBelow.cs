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
            [Description("True when: the value is latitude 1. The distance to the second point is below the miles. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is below the miles. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is below the miles. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is below the miles. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }

            [Description("True when: the value is latitude 1. The distance to the second point is below the miles. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleDistanceMilesBelow(flow.Value, longitude1, latitude2, longitude2, miles));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is below the miles. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().MatchDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is below the miles. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().RejectDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is below the miles. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().BreakDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is below the miles. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().RequireDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. The distance to the second point is below the miles. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureDistanceMilesBelow([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The distance in miles that counts.")] double miles)
            {
                return value.Start().EnsureDistanceMilesBelow(longitude1, latitude2, longitude2, miles);
            }
        }
    }
}
