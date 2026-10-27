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
using Jube.Dto.Forms;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.ApplicationLogEntry
{
    [GridEndpoint("ApplicationLogEntry", Title = "Application log", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Message contains")]
    public sealed record ApplicationLogEntryDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("UTC timestamp the event was logged.")]
        [property: ListColumn(Order = 10, Title = "Occurred")]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Description("The log level: WARN, ERROR or FATAL. Events below WARN are never captured.")]
        [property: ListColumn(Order = 20)]
        string Level,
        [property: Description("The fully-qualified class that logged the event (log4net's logger name).")]
        [property: ListColumn(Order = 30, Title = "Logger")]
        string LoggerName,
        [property: Description("The thread that logged the event.")]
        [property: ListColumn(Order = 40, Title = "Thread")]
        string ThreadContext,
        [property: Description("The rendered log message.")]
        [property: ListColumn(Order = 50)]
        string Message,
        [property: Description("The full exception text, if the event carried one; otherwise null.")]
        [property: ListColumn(Order = 60)]
        string Exception,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 70, Title = "Flushed")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node that flushed this row.")]
        [property: ListColumn(Order = 80)]
        string Instance);
}