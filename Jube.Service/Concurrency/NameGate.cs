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

using Jube.Service.Concurrency.Models;

namespace Jube.Service.Concurrency
{
    internal static class NameGate
    {
        private const int Stripes = 256;

        private static readonly SemaphoreSlim[] gates = Enumerable.Range(0, Stripes)
            .Select(_ => new SemaphoreSlim(1, 1)).ToArray();

        public static async Task<IDisposable> EnterAsync(string key, CancellationToken token = default)
        {
            var gate = gates[(uint)StringComparer.OrdinalIgnoreCase.GetHashCode(key) % Stripes];
            await gate.WaitAsync(token).ConfigureAwait(false);
            return new Releaser(gate);
        }
    }
}