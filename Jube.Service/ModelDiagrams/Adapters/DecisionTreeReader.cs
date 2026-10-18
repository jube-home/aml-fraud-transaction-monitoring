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

using Jube.Charting.Diagrams.Models;
using Newtonsoft.Json.Linq;

namespace Jube.Service.ModelDiagrams.Adapters
{
    internal sealed class DecisionTreeReader
    {
        private readonly IReadOnlyList<string> classes;
        private readonly IReadOnlyList<string> features;

        public DecisionTreeReader(IReadOnlyList<string> features, IReadOnlyList<string> classes)
        {
            this.features = features;
            this.classes = classes;
            SplitCounts = new int[features.Count];
        }

        public int[] SplitCounts { get; }
        public int Nodes { get; private set; }

        public static bool LooksLikeTree(JToken? tree)
        {
            return tree is JObject && ModelJson.Get(tree, "root") is JObject root && IsNode(root);
        }

        private static bool IsNode(JObject node)
        {
            return ModelJson.Get(node, "branches") is JArray || ModelJson.Integer(node, "output") is not null ||
                   ModelJson.Has(node, "attributeIndex");
        }

        public TreeNode Read(JToken tree)
        {
            return Node((JObject)ModelJson.Get(tree, "root")!, 0);
        }

        private TreeNode Node(JObject node, int depth)
        {
            if (depth > 200)
            {
                throw ModelJson.Inconsistent("the tree is nested too deeply");
            }

            Nodes++;
            var branches = ModelJson.Get(node, "branches") as JArray;

            if (branches is null || branches.Count == 0)
            {
                var output = ModelJson.Integer(node, "output");

                if (output is null || output < 0)
                {
                    throw ModelJson.Inconsistent("a leaf has no output class");
                }

                var name = output < classes.Count ? classes[output.Value] : "class " + output;

                return TreeNode.Leaf(prediction: name);
            }

            var attribute = ModelJson.Integer(node, "attributeIndex")
                            ?? throw ModelJson.Inconsistent("a split has no attribute index");

            if (attribute < 0 || attribute >= features.Count)
            {
                throw ModelJson.Inconsistent("a split refers to attribute " + attribute + " but there are " +
                                             features.Count);
            }

            SplitCounts[attribute]++;
            var feature = features[attribute];

            var children = branches
                .Select(b => b as JObject ?? throw ModelJson.Inconsistent("a branch is not an object"))
                .ToArray();

            var comparisons = children.Select(Comparison).ToArray();
            var values = children.Select(c => ModelJson.Number(c, "value")).ToArray();
            var subtrees = children.Select(c => Node(c, depth + 1)).ToArray();

            if (children.Length == 2 && values[0] is { } t0 && values[1] is { } t1 && t0 == t1)
            {
                var lessIndex = Array.FindIndex(comparisons, c => c is "<=" or "<");
                var greaterIndex = Array.FindIndex(comparisons, c => c is ">" or ">=");

                if (lessIndex >= 0 && greaterIndex >= 0 && lessIndex != greaterIndex)
                {
                    var less = comparisons[lessIndex];
                    var split = TreeNode.Split(feature, t0, subtrees[lessIndex], subtrees[greaterIndex]);

                    return less == "<=" ? split : split with { Condition = feature + " < " + ModelJson.Format(t0) };
                }
            }

            var labelled = children.Select((_, i) => new TreeBranch(
                comparisons[i] + " " + (values[i] is { } v ? ModelJson.Format(v) : "?"), subtrees[i])).ToArray();

            return new TreeNode { Feature = feature, Branches = labelled };
        }

        private static string Comparison(JObject node)
        {
            var token = ModelJson.Get(node, "comparison");

            if (token is { Type: JTokenType.String })
            {
                return token.Value<string>() switch
                {
                    "Equal" => "=",
                    "NotEqual" => "!=",
                    "GreaterThanOrEqual" => ">=",
                    "GreaterThan" => ">",
                    "LessThan" => "<",
                    "LessThanOrEqual" => "<=",
                    _ => throw ModelJson.Inconsistent("a branch has an unknown comparison")
                };
            }

            return ModelJson.Integer(node, "comparison") switch
            {
                1 => "=",
                2 => "!=",
                3 => ">=",
                4 => ">",
                5 => "<",
                6 => "<=",
                _ => throw ModelJson.Inconsistent("a branch has an unknown comparison")
            };
        }

        public static string[] Features(JToken model, ModelDiagramOptions options)
        {
            var stored = new List<string>();

            if (ModelJson.Get(model, "attributes") is JArray attributes)
            {
                foreach (var attribute in attributes)
                {
                    var name = ModelJson.String(attribute, "name");
                    stored.Add(string.IsNullOrWhiteSpace(name) ? "x" + (stored.Count + 1) : name);
                }
            }

            var count = stored.Count > 0 ? stored.Count : ModelJson.Integer(model, "inputCount") ?? 0;

            if (count < 1)
            {
                throw ModelJson.Inconsistent("the tree lists no attributes");
            }

            return options.Variables(stored.Count > 0 ? stored : null, count).ToArray();
        }

        public static string[] Classes(JToken model)
        {
            if (ModelJson.Strings(model, "classNames") is { Length: > 0 } named)
            {
                return named;
            }

            var count = ModelJson.Integer(model, "outputClasses") ?? ModelJson.Integer(model, "classCount") ?? 2;

            return Enumerable.Range(0, Math.Clamp(count, 1, 64)).Select(i => "class " + i).ToArray();
        }
    }
}