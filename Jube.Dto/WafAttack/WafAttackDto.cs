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

namespace Jube.Dto.WafAttack
{
    [GridEndpoint("WafAttack", Title = "Waf attack", DefaultSort = "createdDate")]
    [GridSearch(Label = "Value, route, signature, category contains")]
    public sealed record WafAttackDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        long Id,
        [property: Description("UTC timestamp the match was captured.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Description("The transport the request arrived over: Http or WebSocket.")]
        [property: ListColumn(Order = 110)]
        string Transport,
        [property: Description("The request route the match occurred on.")]
        [property: ListColumn(Order = 50)]
        string Route,
        [property: Description("The HTTP method.")]
        [property: ListColumn(Order = 60)]
        string Method,
        [property: Description("Remote IP address of the caller, when known.")]
        [property: ListColumn(Order = 100)]
        string RemoteIp,
        [property: Description("Authenticated username of the caller, when known.")]
        [property: ListColumn(Order = 90, Title = "User")]
        string UserName,
        [property: Description("Name of the WafSignature that matched.")]
        [property: ListColumn(Order = 30, Title = "Signature")]
        string SignatureName,
        [property: Description("OWASP category of the matched signature.")]
        [property: ListColumn(Order = 40)]
        string Category,
        [property: Description("JSON path or field name that carried the matched value.")]
        [property: ListColumn(Order = 70)]
        string MatchedField,
        [property: Description("Truncated copy of the value that matched the signature's pattern.")]
        [property: ListColumn(Order = 80)]
        string MatchedValue,
        [property: Description("The action taken: Dropped (request refused) or Detected (request continued).")]
        [property: ListColumn(Order = 20)]
        string Action,
        [property: Description("Correlation id (trace identifier or connection id) of the originating request.")]
        [property: ListColumn(Hidden = true)]
        string CorrelationId,
        [property: Description("Hostname of the node that flushed this row.")]
        [property: ListColumn(Order = 120)]
        string Instance);
}