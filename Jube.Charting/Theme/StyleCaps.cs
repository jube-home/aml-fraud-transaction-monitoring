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

namespace Jube.Charting.Theme
{
    internal static class StyleCaps
    {
        public const int Nodes = 64;
        public const int Rows = 48;
        public const int MatrixRows = 64;
        public const int ComponentRows = 12;
        public const int ComponentColumns = 8;
        public const int FuzzyVariables = 12;
        public const int FuzzyRules = 30;
        public const int SankeyNodes = 40;
        public const int SankeyLinks = 100;
        public const int PieDirections = 36;

        public static bool Keep(bool external, int index, int cap)
        {
            return !external || index < cap;
        }

        public static string Tokens(bool external, string prefix, IEnumerable<int> indexes, int cap = Nodes)
        {
            return string.Join(' ', indexes.Where(i => Keep(external, i, cap)).Select(i => prefix + i));
        }

        public static bool KeyClass(string key)
        {
            return TopLayerKey(key) || (key.Length > 1 && key[0] == 'q' && int.TryParse(key.AsSpan(1), out var index) &&
                                        index < FuzzyRules);
        }

        public static bool TopLayerKey(string key)
        {
            return key.Length > 1 && key[0] is 'n' or 'x' && int.TryParse(key.AsSpan(1), out var index) &&
                   index < Nodes;
        }
    }
}