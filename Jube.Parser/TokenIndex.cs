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
using System.Linq;

namespace Jube.Parser
{
    internal sealed class TokenIndex
    {
        private TokenIndex(HashSet<string> singleWords, List<string> multiWords)
        {
            SingleWords = singleWords;
            MultiWords = multiWords;
        }

        public HashSet<string> SingleWords { get; }
        public List<string> MultiWords { get; }

        public static TokenIndex Build(IEnumerable<string> tokens)
        {
            var snapshot = tokens.Where(w => w != null).ToList();

            return new TokenIndex(
                snapshot.Where(w => !w.Contains(' ')).ToHashSet(StringComparer.OrdinalIgnoreCase),
                snapshot.Where(w => w.Contains(' ')).ToList());
        }
    }
}
