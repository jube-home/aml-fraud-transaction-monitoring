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

namespace Jube.Validations.Repository.CaseResearch
{
    public static class CitationAddress
    {
        public const int MaxLength = 2000;
        public const int MaxTitleLength = 300;
        public const int MaxPerResearch = 10;

        public static bool IsWeb(string? value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Length <= MaxLength
                   && !value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c is '<' or '>' or '"' or '\'' or '`'
                       or '\\')
                   && Uri.TryCreate(value, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                   && string.IsNullOrEmpty(uri.UserInfo) && !string.IsNullOrEmpty(uri.Host);
        }
    }
}
