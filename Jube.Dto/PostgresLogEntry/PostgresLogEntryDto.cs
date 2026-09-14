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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.PostgresLogEntry
{
    public sealed record PostgresLogEntryDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property:
            Description(
                "UTC timestamp Postgres itself logged this line at, parsed from the line's own timestamp prefix.")]
        DateTime OccurredDate,
        [property:
            Description(
                "The Postgres log level: LOG, WARNING, ERROR, FATAL, PANIC, DEBUG1-5, STATEMENT, DETAIL, HINT, CONTEXT, or null if the line did not match the expected log_line_prefix format.")]
        string? Level,
        [property:
            Description(
                "The Postgres backend process id that logged this line, useful for correlating a run of continuation lines (STATEMENT/DETAIL/CONTEXT) back to the same backend.")]
        int? Pid,
        [property:
            Description(
                "The log message text. Any STATEMENT/DETAIL/CONTEXT/HINT continuation lines Postgres printed immediately after this one are folded in here, newline-separated, since Postgres's own log format has no separate exception-vs-message distinction.")]
        string Message,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description("Hostname of the Jube node that captured this row (not the Postgres server's own hostname).")]
        string Instance);
}