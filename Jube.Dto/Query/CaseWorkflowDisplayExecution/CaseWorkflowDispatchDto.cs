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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseWorkflowDisplayExecution
{
    [Description("One email, SMS or HTTP call fired by a case workflow Form, Macro, Action or Status, as it was sent.")]
    public class CaseWorkflowDispatchDto
    {
        [Description("Server-assigned identifier of the dispatch.")]
        public int Id { get; set; }

        [Description("Integer Id of the case it was fired for.")]
        public int CaseId { get; set; }

        [Description("What fired it: 1 Form, 2 Macro, 3 Action, 4 Status.")]
        public int SourceKindId { get; set; }

        [Description("Name of the Form, Macro, Action or Status that fired it.")]
        public string SourceName { get; set; } = string.Empty;

        [Description("How it was sent: 1 Email, 2 SMS, 3 HTTP POST, 4 HTTP GET.")]
        public int ChannelId { get; set; }

        [Description("How it was drawn: 1 stored payload, 2 SQL.")]
        public int ModeId { get; set; }

        [Description("The rendered recipients or HTTP URL.")]
        public string? Destination { get; set; }

        [Description("The rendered subject, for an email.")]
        public string? Subject { get; set; }

        [Description("The rendered message, or the JSON body of an HTTP POST.")]
        public string? Body { get; set; }

        [Description("What happened: 1 Sent, 2 Failed, 3 Refused before it left.")]
        public int OutcomeId { get; set; }

        [Description("Why it failed or was refused.")]
        public string? Detail { get; set; }

        [Description("The HTTP status the provider or endpoint answered with, when it answered.")]
        public int? ResponseStatus { get; set; }

        [Description("The correlation id sent with an HTTP call, to find it at the other end.")]
        public Guid CorrelationId { get; set; }

        [Description("The SQL rows the call was drawn from, as a JSON array; null when drawn from the stored payload.")]
        public string? Rows { get; set; }

        [Description("User whose action fired it.")]
        public string CreatedUser { get; set; } = string.Empty;

        [Description("When it was fired.")]
        public DateTimeOffset? CreatedDate { get; set; }
    }
}
