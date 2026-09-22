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
            public Flow<double> MatchHerfindahlWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Match, FlowPredicates.DoubleHerfindahlWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RejectHerfindahlWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Reject, FlowPredicates.DoubleHerfindahlWithBelow(flow.Value, bound, others));
            }

            public Flow<double> BreakHerfindahlWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Break, FlowPredicates.DoubleHerfindahlWithBelow(flow.Value, bound, others));
            }

            public Flow<double> RequireHerfindahlWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Require, FlowPredicates.DoubleHerfindahlWithBelow(flow.Value, bound, others));
            }

            public Flow<double> EnsureHerfindahlWithBelow(double bound, params double[] others)
            {
                return flow.IsDecided
                    ? flow
                    : flow.Apply(FlowKind.Ensure, FlowPredicates.DoubleHerfindahlWithBelow(flow.Value, bound, others));
            }
        }

        extension(double value)
        {
            public Flow<double> MatchHerfindahlWithBelow(double bound, params double[] others)
            {
                return value.Start().MatchHerfindahlWithBelow(bound, others);
            }

            public Flow<double> RejectHerfindahlWithBelow(double bound, params double[] others)
            {
                return value.Start().RejectHerfindahlWithBelow(bound, others);
            }

            public Flow<double> BreakHerfindahlWithBelow(double bound, params double[] others)
            {
                return value.Start().BreakHerfindahlWithBelow(bound, others);
            }

            public Flow<double> RequireHerfindahlWithBelow(double bound, params double[] others)
            {
                return value.Start().RequireHerfindahlWithBelow(bound, others);
            }

            public Flow<double> EnsureHerfindahlWithBelow(double bound, params double[] others)
            {
                return value.Start().EnsureHerfindahlWithBelow(bound, others);
            }
        }
    }
}
