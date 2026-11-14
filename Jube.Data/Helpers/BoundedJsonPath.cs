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
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Jube.Data.Helpers
{
    public static class BoundedJsonPath
    {
        public static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromMilliseconds(250);

        private static readonly JsonSelectSettings settings = new() { RegexMatchTimeout = RegexMatchTimeout };

        public static JToken Select(JToken json, string path)
        {
            try
            {
                return json?.SelectToken(path, settings);
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
        }
    }
}
