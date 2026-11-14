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

using Jube.Resources;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Microsoft.Extensions.Localization;
using Npgsql;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class ArchiveFilterFailure
    {
        public static bool Is(PostgresException exception, string? filter)
        {
            return !string.IsNullOrWhiteSpace(filter)
                   && exception.SqlState is { } state && state.StartsWith("22", StringComparison.Ordinal);
        }

        public static DtoValidationException Unreadable(IStringLocalizer strings)
        {
            return new DtoValidationException(new FluentValidation.Results.ValidationResult(
            [
                new FluentValidation.Results.ValidationFailure("Filter", strings[CaseKeyJournalResources.FilterUnreadable])
                {
                    ErrorCode = "FilterUnreadable"
                }
            ]));
        }
    }
}
