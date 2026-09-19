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

namespace Jube.App.Code.signalr.Models
{
    internal sealed class Entry(string userName, System.Action abort, string issuedMilliseconds)
    {
        private readonly HashSet<string> groups = [];
        public string UserName { get; } = userName;

        public string IssuedMilliseconds { get; } = issuedMilliseconds;

        public void Abort()
        {
            try
            {
                abort?.Invoke();
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch (System.Exception)
            {
                //Ignored
            }
        }

        public void Add(string group)
        {
            lock (groups)
            {
                groups.Add(group);
            }
        }

        public void Remove(string group)
        {
            lock (groups)
            {
                groups.Remove(group);
            }
        }

        public IReadOnlyCollection<string> Snapshot()
        {
            lock (groups)
            {
                return groups.ToList();
            }
        }
    }
}