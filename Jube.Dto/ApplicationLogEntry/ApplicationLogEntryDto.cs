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

namespace Jube.Dto.ApplicationLogEntry
{
    public sealed record ApplicationLogEntryDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp the event was logged.")]
        DateTime OccurredDate,
        [property: Description("The log level: WARN, ERROR or FATAL. Events below WARN are never captured.")]
        string Level,
        [property: Description("The fully-qualified class that logged the event (log4net's logger name).")]
        string LoggerName,
        [property: Description("The thread that logged the event.")]
        string ThreadContext,
        [property: Description("The rendered log message.")]
        string Message,
        [property: Description("The full exception text, if the event carried one; otherwise null.")]
        string Exception,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        string Instance);
}