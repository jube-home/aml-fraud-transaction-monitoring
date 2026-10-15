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
            public Flow<double> MatchIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            public Flow<double> RejectIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            public Flow<double> BreakIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            public Flow<double> RequireIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }

            public Flow<double> EnsureIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure,
                        FlowPredicates.DoubleIsInsidePolygon(flow.Value, longitude, vertices));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return value.Start().MatchIsInsidePolygon(longitude, vertices);
            }

            public Flow<double> RejectIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return value.Start().RejectIsInsidePolygon(longitude, vertices);
            }

            public Flow<double> BreakIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return value.Start().BreakIsInsidePolygon(longitude, vertices);
            }

            public Flow<double> RequireIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return value.Start().RequireIsInsidePolygon(longitude, vertices);
            }

            public Flow<double> EnsureIsInsidePolygon(double longitude, IEnumerable<string>? vertices)
            {
                return value.Start().EnsureIsInsidePolygon(longitude, vertices);
            }
        }
    }
}