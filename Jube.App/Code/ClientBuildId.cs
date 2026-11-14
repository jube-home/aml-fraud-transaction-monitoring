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

using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;

namespace Jube.App.Code
{
    public static class ClientBuildId
    {
        public const string FileName = "build-id.txt";

        private static readonly ConditionalWeakTable<IFileProvider, string> Cache = new();

        public static string Read(IFileProvider webRootFileProvider)
        {
            if (Cache.TryGetValue(webRootFileProvider, out var cached))
            {
                return cached;
            }

            var file = webRootFileProvider.GetFileInfo(FileName);

            if (!file.Exists)
            {
                return string.Empty;
            }

            using var reader = new StreamReader(file.CreateReadStream());

            var value = reader.ReadToEnd().Trim();

            Cache.AddOrUpdate(webRootFileProvider, value);

            return value;
        }
    }
}
