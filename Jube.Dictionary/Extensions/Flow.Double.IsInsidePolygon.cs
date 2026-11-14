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
            [Description("True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            [Description("True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }
        }

        extension(double value)
        {
            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides matched if so; otherwise continues.")]
            public Flow<double> MatchIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return value.Start().MatchIsInsidePolygon(longitude, vertices);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides rejected if so; otherwise continues.")]
            public Flow<double> RejectIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return value.Start().RejectIsInsidePolygon(longitude, vertices);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Decides broken if so; otherwise continues.")]
            public Flow<double> BreakIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return value.Start().BreakIsInsidePolygon(longitude, vertices);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Continues if so; otherwise decides rejected.")]
            public Flow<double> RequireIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return value.Start().RequireIsInsidePolygon(longitude, vertices);
            }

            [Description("Begins a pipeline from the value. True when: the point at the value as latitude and the argument as longitude is inside the polygon given as a list of latitude,longitude vertices. Continues if so; otherwise decides broken.")]
            public Flow<double> EnsureIsInsidePolygon([Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
                [Description("The polygon's vertices as latitude,longitude text in order, usually a List token.")] IEnumerable<string>? vertices)
            {
                return value.Start().EnsureIsInsidePolygon(longitude, vertices);
            }
        }
    }
}