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

using System.Collections.Generic;
using System.Linq;
using Jube.Graph.Model;

namespace Jube.Test.Graph
{
    internal static class KarateClub
    {
        public static readonly (int From, int To)[] Edges =
        [
            (1, 2), (1, 3), (1, 4), (1, 5), (1, 6), (1, 7), (1, 8), (1, 9), (1, 11), (1, 12), (1, 13), (1, 14),
            (1, 18), (1, 20), (1, 22), (1, 32),
            (2, 3), (2, 4), (2, 8), (2, 14), (2, 18), (2, 20), (2, 22), (2, 31),
            (3, 4), (3, 8), (3, 9), (3, 10), (3, 14), (3, 28), (3, 29), (3, 33),
            (4, 8), (4, 13), (4, 14),
            (5, 7), (5, 11),
            (6, 7), (6, 11), (6, 17),
            (7, 17),
            (9, 31), (9, 33), (9, 34),
            (10, 34),
            (14, 34),
            (15, 33), (15, 34),
            (16, 33), (16, 34),
            (19, 33), (19, 34),
            (20, 34),
            (21, 33), (21, 34),
            (23, 33), (23, 34),
            (24, 26), (24, 28), (24, 30), (24, 33), (24, 34),
            (25, 26), (25, 28), (25, 32),
            (26, 32),
            (27, 30), (27, 34),
            (28, 34),
            (29, 32), (29, 34),
            (30, 33), (30, 34),
            (31, 33), (31, 34),
            (32, 33), (32, 34),
            (33, 34)
        ];

        public static readonly HashSet<int> MrHi = [1, 2, 3, 4, 5, 6, 7, 8, 11, 12, 13, 14, 17, 18, 20, 22];

        public static CompressedGraph Graph()
        {
            return CompressedGraphBuilder.FromEdges(Edges.Select(e => ((long)e.From, (long)e.To, 1f, (byte)0)), true);
        }

        public static int Index(CompressedGraph graph, int oneBasedNode)
        {
            return graph.IndexOf(oneBasedNode);
        }
    }
}
