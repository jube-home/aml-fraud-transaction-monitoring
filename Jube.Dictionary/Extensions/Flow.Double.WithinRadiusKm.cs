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
            [Description("True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleWithinRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            [Description("True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleWithinRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            [Description("True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleWithinRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            [Description("True when: the value is latitude 1. Within the radius in kilometres of the second point. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleWithinRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }

            [Description("True when: the value is latitude 1. Within the radius in kilometres of the second point. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleWithinRadiusKm(flow.Value, longitude1, latitude2, longitude2, radiusKm));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return value.Start().MatchWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return value.Start().RejectWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in kilometres of the second point. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return value.Start().BreakWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in kilometres of the second point. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return value.Start().RequireWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the value is latitude 1. Within the radius in kilometres of the second point. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureWithinRadiusKm([Description("Longitude of the first point.")] double longitude1,
                [Description("Latitude of the second point.")] double latitude2,
                [Description("Longitude of the second point.")] double longitude2,
                [Description("The radius to test against.")] double radiusKm)
            {
                return value.Start().EnsureWithinRadiusKm(longitude1, latitude2, longitude2, radiusKm);
            }
        }
    }
}
