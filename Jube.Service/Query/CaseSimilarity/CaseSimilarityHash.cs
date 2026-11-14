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

namespace Jube.Service.Query.CaseSimilarity
{
    public static class CaseSimilarityHash
    {
        private const string Purpose = "CaseSimilarity|";

        public const string Setting = "CaseSimilarityHashingKey";

        public static string? KeyOf(DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            var secret = dynamicEnvironment.AppSettings(Setting);

            return string.IsNullOrWhiteSpace(secret) || secret.Contains("[@", StringComparison.Ordinal)
                ? null
                : secret;
        }

        public static Func<string, string, string> For(string secret)
        {
            var key = SHA256.HashData(Encoding.UTF8.GetBytes(Purpose + secret));

            return (field, value) => Convert.ToHexString(
                HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(field + "\n" + value)), 0, 16);
        }
    }
}
