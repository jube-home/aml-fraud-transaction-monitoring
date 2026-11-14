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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public sealed class FastPathPlan
    {
        private const int MaximumIndexDigits = 6;
        private const int MaximumTrackedChildren = 63;

        private readonly Dictionary<string, int> identifiers = new(StringComparer.Ordinal);

        private FastPathPlan()
        {
            Root = new PlanNode();
        }

        internal PlanNode Root { get; }

        internal int Count => identifiers.Count;

        public static FastPathPlan TryBuild(IEnumerable<string> xPaths)
        {
            var plan = new FastPathPlan();

            foreach (var xPath in xPaths)
            {
                if (string.IsNullOrEmpty(xPath) || plan.identifiers.ContainsKey(xPath))
                {
                    continue;
                }

                var steps = ParseSimplePath(xPath);

                if (steps == null)
                {
                    return null;
                }

                var node = plan.Root;

                foreach (var step in steps)
                {
                    node = step.Name != null ? node.ChildNamed(step.Name) : node.ChildIndexed(step.Index);

                    if (node == null)
                    {
                        return null;
                    }
                }

                var identifier = plan.identifiers.Count;

                plan.identifiers.Add(xPath, identifier);
                node.Terminals.Add(identifier);
            }

            return plan.Count == 0 ? null : plan;
        }

        public static bool IsSimplePath(string xPath)
        {
            return !string.IsNullOrEmpty(xPath) && ParseSimplePath(xPath) != null;
        }

        internal bool TryGetIdentifier(string xPath, out int identifier)
        {
            identifier = -1;

            return xPath != null && identifiers.TryGetValue(xPath, out identifier);
        }

        private static List<PathStep> ParseSimplePath(string xPath)
        {
            if (xPath[0] != '$' || xPath.Length < 3)
            {
                return null;
            }

            var steps = new List<PathStep>();
            var position = 1;

            while (position < xPath.Length)
            {
                if (xPath[position] == '.')
                {
                    var start = ++position;

                    if (position >= xPath.Length || !IsNameStart(xPath[position]))
                    {
                        return null;
                    }

                    while (position < xPath.Length && IsNamePart(xPath[position]))
                    {
                        position++;
                    }

                    steps.Add(new PathStep(xPath.Substring(start, position - start), 0));
                }
                else if (xPath[position] == '[')
                {
                    var start = ++position;

                    while (position < xPath.Length && xPath[position] >= '0' && xPath[position] <= '9')
                    {
                        position++;
                    }

                    var digits = position - start;

                    if (digits is 0 or > MaximumIndexDigits || position >= xPath.Length || xPath[position] != ']' ||
                        (digits > 1 && xPath[start] == '0'))
                    {
                        return null;
                    }

                    steps.Add(new PathStep(null,
                        int.Parse(xPath.AsSpan(start, digits), NumberStyles.None, CultureInfo.InvariantCulture)));

                    position++;
                }
                else
                {
                    return null;
                }
            }

            return steps.Count == 0 ? null : steps;
        }

        private static bool IsNameStart(char c)
        {
            return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '_';
        }

        private static bool IsNamePart(char c)
        {
            return IsNameStart(c) || c is >= '0' and <= '9';
        }

        internal sealed class PlanNode
        {
            private readonly Dictionary<string, PlanNode> children = new(StringComparer.Ordinal);
            private readonly Dictionary<int, PlanNode> items = [];

            internal List<int> Terminals { get; } = [];

            internal int ChildCount => children.Count;

            internal PlanNode ChildNamed(string name)
            {
                if (children.TryGetValue(name, out var existing))
                {
                    return existing;
                }

                if (children.Count >= MaximumTrackedChildren)
                {
                    return null;
                }

                var created = new PlanNode { Ordinal = children.Count };

                children.Add(name, created);

                return created;
            }

            internal PlanNode ChildIndexed(int index)
            {
                if (items.TryGetValue(index, out var existing))
                {
                    return existing;
                }

                var created = new PlanNode();

                items.Add(index, created);

                return created;
            }

            internal int Ordinal { get; private init; }

            internal bool TryGetNamed(string name, out PlanNode node)
            {
                return children.TryGetValue(name, out node);
            }

            internal bool TryGetIndexed(int index, out PlanNode node)
            {
                return items.TryGetValue(index, out node);
            }
        }

        private readonly record struct PathStep(string Name, int Index);
    }
}
