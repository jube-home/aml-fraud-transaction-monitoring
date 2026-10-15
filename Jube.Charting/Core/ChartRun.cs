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

using System.Security.Cryptography;
using System.Text;
using Jube.Charting.Core.Models;

namespace Jube.Charting.Core
{
    internal static class ChartRun
    {
        public static string Execute(ChartOptions options, Func<string, string> render)
        {
            options.Validate();
            var id = options.Id;

            if (id is null)
            {
                var probe = render("jc-0");
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(probe));
                id = "jc-" + Convert.ToHexString(hash, 0, 4).ToLowerInvariant();
            }

            var result = render(id);
            ChartLimits.Output(result.Length);

            return result;
        }
    }
}