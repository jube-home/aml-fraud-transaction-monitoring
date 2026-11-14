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
            [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match,
                        FlowPredicates.DoubleIsWithinAnyRadiusKm(flow.Value, longitude, centres, radiusKm));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.DoubleIsWithinAnyRadiusKm(flow.Value, longitude, centres, radiusKm));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break,
                        FlowPredicates.DoubleIsWithinAnyRadiusKm(flow.Value, longitude, centres, radiusKm));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.DoubleIsWithinAnyRadiusKm(flow.Value, longitude, centres, radiusKm));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.DoubleIsWithinAnyRadiusKm(flow.Value, longitude, centres, radiusKm));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return value.Start().MatchIsWithinAnyRadiusKm(longitude, centres, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return value.Start().RejectIsWithinAnyRadiusKm(longitude, centres, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return value.Start().BreakIsWithinAnyRadiusKm(longitude, centres, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return value.Start().RequireIsWithinAnyRadiusKm(longitude, centres, radiusKm);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsWithinAnyRadiusKm([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
                [Description("The radius in kilometres.")] double radiusKm)
            {
                return value.Start().EnsureIsWithinAnyRadiusKm(longitude, centres, radiusKm);
            }
        }
    }
}