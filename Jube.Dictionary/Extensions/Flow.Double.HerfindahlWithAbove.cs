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
            public Flow<double> MatchHerfindahlWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHerfindahlWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RejectHerfindahlWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHerfindahlWithAbove(flow.Value, bound, others));
            }

            public Flow<double> BreakHerfindahlWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHerfindahlWithAbove(flow.Value, bound, others));
            }

            public Flow<double> RequireHerfindahlWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHerfindahlWithAbove(flow.Value, bound, others));
            }

            public Flow<double> EnsureHerfindahlWithAbove(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHerfindahlWithAbove(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHerfindahlWithAbove(double bound, params double[] others)
            {
                return value.Start().MatchHerfindahlWithAbove(bound, others);
            }

            public Flow<double> RejectHerfindahlWithAbove(double bound, params double[] others)
            {
                return value.Start().RejectHerfindahlWithAbove(bound, others);
            }

            public Flow<double> BreakHerfindahlWithAbove(double bound, params double[] others)
            {
                return value.Start().BreakHerfindahlWithAbove(bound, others);
            }

            public Flow<double> RequireHerfindahlWithAbove(double bound, params double[] others)
            {
                return value.Start().RequireHerfindahlWithAbove(bound, others);
            }

            public Flow<double> EnsureHerfindahlWithAbove(double bound, params double[] others)
            {
                return value.Start().EnsureHerfindahlWithAbove(bound, others);
            }
        }
    }
}
