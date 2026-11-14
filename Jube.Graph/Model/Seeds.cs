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

namespace Jube.Graph.Model
{
    public sealed class Seeds
    {
        public Seeds(int[] indices, int[] signs)
        {
            ArgumentNullException.ThrowIfNull(indices);
            ArgumentNullException.ThrowIfNull(signs);

            if (indices.Length != signs.Length)
            {
                throw new ArgumentException("Indices and signs must have the same length.");
            }

            if (signs.Any(s => s != 1 && s != -1))
            {
                throw new ArgumentException("Signs must be +1 for bad or -1 for good.");
            }

            Indices = indices;
            Signs = signs;
        }

        public int[] Indices { get; }

        public int[] Signs { get; }

        public int Count => Indices.Length;

        public static Seeds Bad(params int[] indices)
        {
            return new Seeds(indices, Enumerable.Repeat(1, indices.Length).ToArray());
        }

        public IEnumerable<int> Positive()
        {
            for (var i = 0; i < Indices.Length; i++)
            {
                if (Signs[i] > 0)
                {
                    yield return Indices[i];
                }
            }
        }

        public IEnumerable<int> Negative()
        {
            for (var i = 0; i < Indices.Length; i++)
            {
                if (Signs[i] < 0)
                {
                    yield return Indices[i];
                }
            }
        }
    }
}
