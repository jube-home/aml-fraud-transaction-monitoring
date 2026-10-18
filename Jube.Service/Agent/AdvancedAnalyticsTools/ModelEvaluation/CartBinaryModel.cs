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

using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class CartBinaryModel : IBinaryModel
    {
        private readonly CartTree tree;

        public CartBinaryModel(CartTree tree)
        {
            this.tree = tree;
        }

        public bool OutputsProbability => true;

        public double[] Score(double[][] rows)
        {
            return tree.Predict(rows);
        }

        public ModelDescription Describe(string[] names)
        {
            var importance = tree.Importance();

            return new ModelDescription(
                "A CART tree that splits on the variable and threshold reducing weighted impurity most, with leaf values equal to the training share of the positive class.",
                [
                    ModelText.Fact("Depth", tree.Height),
                    ModelText.Fact("Leaves", tree.LeafCount),
                    ModelText.Fact("Nodes", tree.Nodes().Count())
                ],
                tree.Rules(names, v => ModelText.Probability(v[0]), 25),
                tree.ToText(names, v => ModelText.Probability(v[0])),
                ModelText.Rank(names, importance));
        }
    }
}