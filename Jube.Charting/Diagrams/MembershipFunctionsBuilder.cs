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

using Jube.Charting.Core;
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public sealed class MembershipFunctionsBuilder : ChartBuilder<MembershipFunctionsBuilder>
    {
        private readonly List<FuzzyRule> rules = [];
        private readonly List<FuzzyVariable> variables = [];
        private int columns = 3;

        public MembershipFunctionsBuilder Variable(string name, double min, double max, params FuzzySet[] sets)
        {
            variables.Add(new FuzzyVariable(name, min, max, sets));

            return this;
        }

        public MembershipFunctionsBuilder Rule(FuzzyTerm consequent, double weight, bool conjunction,
            params FuzzyTerm[] conditions)
        {
            rules.Add(new FuzzyRule(conditions, consequent) { Weight = weight, Conjunction = conjunction });

            return this;
        }

        public MembershipFunctionsBuilder Rule(FuzzyRule rule)
        {
            rules.Add(rule);

            return this;
        }

        public MembershipFunctionsBuilder Columns(int columnCount)
        {
            columns = columnCount;

            return this;
        }

        public MembershipFunctions Build()
        {
            return new MembershipFunctions(new MembershipFunctionsSpec
            {
                Options = Options, Variables = [.. variables], Rules = [.. rules], MaxColumns = columns
            });
        }

        public override string Render()
        {
            return Build().Render();
        }
    }
}