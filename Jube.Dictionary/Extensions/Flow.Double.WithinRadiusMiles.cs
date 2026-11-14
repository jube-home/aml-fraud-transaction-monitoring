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
            [Description("True when: the value is latitude 1. Within the radius in miles of the second point. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            [Description("True when: the value is latitude 1. Within the radius in miles of the second point. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            [Description("True when: the value is latitude 1. Within the radius in miles of the second point. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            [Description("True when: the value is latitude 1. Within the radius in miles of the second point. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }

            [Description("True when: the value is latitude 1. Within the radius in miles of the second point. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWithinRadiusMiles(flow.Value, longitude1, latitude2, longitude2, radiusMiles));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in miles of the second point. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return value.Start().MatchWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in miles of the second point. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return value.Start().RejectWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in miles of the second point. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return value.Start().BreakWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in miles of the second point. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return value.Start().RequireWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in miles of the second point. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinRadiusMiles([Description("The longitude of the first point in degrees; the value is the first latitude.")] double longitude1,
                [Description("The latitude of the second point in degrees.")] double latitude2,
                [Description("The longitude of the second point in degrees.")] double longitude2,
                [Description("The radius in miles.")] double radiusMiles)
            {
                return value.Start().EnsureWithinRadiusMiles(longitude1, latitude2, longitude2, radiusMiles);
            }
        }
    }
}
