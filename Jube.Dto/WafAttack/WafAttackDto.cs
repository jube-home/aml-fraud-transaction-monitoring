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

namespace Jube.Dto.WafAttack
{
    public sealed record WafAttackDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        long Id,
        [property: Description("UTC timestamp the match was captured.")]
        DateTime CreatedDate,
        [property: Description("The transport the request arrived over: Http or WebSocket.")]
        string Transport,
        [property: Description("The request route the match occurred on.")]
        string Route,
        [property: Description("The HTTP method.")]
        string Method,
        [property: Description("Remote IP address of the caller, when known.")]
        string RemoteIp,
        [property: Description("Authenticated username of the caller, when known.")]
        string UserName,
        [property: Description("Name of the WafSignature that matched.")]
        string SignatureName,
        [property: Description("OWASP category of the matched signature.")]
        string Category,
        [property: Description("JSON path or field name that carried the matched value.")]
        string MatchedField,
        [property: Description("Truncated copy of the value that matched the signature's pattern.")]
        string MatchedValue,
        [property: Description("The action taken: Dropped (request refused) or Detected (request continued).")]
        string Action,
        [property: Description("Correlation id (trace identifier or connection id) of the originating request.")]
        string CorrelationId,
        [property: Description("Hostname of the node that flushed this row.")]
        string Instance);
}