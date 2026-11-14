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
using System;
using System.Collections.Generic;

namespace Jube.Engine.GraphRecall
{
    public sealed class GraphRecallNodeIndex
    {
        private readonly Dictionary<ulong, GraphRecallNode> byHash;
        private readonly Dictionary<string, GraphRecallNode> byValue;

        public GraphRecallNodeIndex(bool hashKeys)
        {
            if (hashKeys)
            {
                byHash = new Dictionary<ulong, GraphRecallNode>();
            }
            else
            {
                byValue = new Dictionary<string, GraphRecallNode>(StringComparer.Ordinal);
            }
        }

        public bool HashKeys => byHash != null;

        public int Count => byHash?.Count ?? byValue.Count;

        public void Add(string value, in GraphRecallNode node)
        {
            if (byHash != null)
            {
                byHash[GraphRecallHash.Compute(value)] = node;
            }
            else
            {
                byValue[value] = node;
            }
        }

        public bool TryGet(string value, out GraphRecallNode node)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return byHash != null
                    ? byHash.TryGetValue(GraphRecallHash.Compute(value), out node)
                    : byValue.TryGetValue(value, out node);
            }

            node = default;

            return false;
        }
    }
}
