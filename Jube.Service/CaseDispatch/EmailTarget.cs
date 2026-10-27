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


using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Jube.Service.CaseDispatch
{
    public static class EmailTarget
    {
        public const int MaximumRecipients = 10;

        private static readonly Regex Telephone = new(@"^\+?[0-9 ()\-]{5,32}$", RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        public static IReadOnlyList<string>? Recipients(string? destination)
        {
            if (string.IsNullOrWhiteSpace(destination) || destination.Any(char.IsControl))
            {
                return null;
            }

            var parts = destination.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length is 0 or > MaximumRecipients)
            {
                return null;
            }

            return parts.All(p => p.Length <= 320 && MailAddress.TryCreate(p, out var address) && address.Address == p)
                ? parts
                : null;
        }

        public static string? Number(string? destination)
        {
            return !string.IsNullOrWhiteSpace(destination) && Telephone.IsMatch(destination.Trim())
                ? destination.Trim()
                : null;
        }

        public static string Subject(string? subject)
        {
            var clean = new string((subject ?? string.Empty).Where(c => !char.IsControl(c)).ToArray()).Trim();

            return clean.Length > 998 ? clean[..998] : clean;
        }
    }
}
