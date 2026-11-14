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

namespace Jube.Engine.GraphRecall
{
    public readonly record struct GraphRecallDefinition(Guid RegistryGuid, string FromNodeKey, string ToNodeKey)
    {
        public bool Connects(string nodeKeyA, string nodeKeyB)
        {
            return (string.Equals(FromNodeKey, nodeKeyA, StringComparison.Ordinal) &&
                    string.Equals(ToNodeKey, nodeKeyB, StringComparison.Ordinal)) ||
                   (string.Equals(FromNodeKey, nodeKeyB, StringComparison.Ordinal) &&
                    string.Equals(ToNodeKey, nodeKeyA, StringComparison.Ordinal));
        }
    }
}
