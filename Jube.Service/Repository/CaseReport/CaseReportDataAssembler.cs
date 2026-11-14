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

using System.Globalization;
using System.Text.Json.Nodes;
using Jube.Data.Poco;

namespace Jube.Service.Repository.CaseReport
{
    public static class CaseReportDataAssembler
    {
        public const int MaximumFieldsCharacters = 200_000;

        public static JsonObject Assemble(Data.Poco.CaseReport report, Data.Poco.Case theCase, string? workflowName,
            string? narrative, JsonObject fields, string user, DateTime now)
        {
            return new JsonObject
            {
                ["generated"] = now.ToString("O", CultureInfo.InvariantCulture),
                ["profile"] = new JsonObject { ["name"] = report.ProfileName, ["version"] = report.ProfileVersion },
                ["reportCode"] = report.ReportCode,
                ["reportGuid"] = report.Guid.ToString(),
                ["version"] = report.CurrentVersion,
                ["case"] = new JsonObject
                {
                    ["id"] = theCase.Id,
                    ["key"] = theCase.CaseKey,
                    ["keyValue"] = theCase.CaseKeyValue,
                    ["workflow"] = workflowName,
                    ["createdDate"] = theCase.CreatedDate?.ToString("O", CultureInfo.InvariantCulture),
                    ["closedDate"] = theCase.ClosedDate?.ToString("O", CultureInfo.InvariantCulture)
                },
                ["narrative"] = narrative ?? string.Empty,
                ["user"] = new JsonObject { ["name"] = user },
                ["fields"] = fields.DeepClone()
            };
        }

        public static JsonObject ParseFields(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            if (json.Length > MaximumFieldsCharacters)
            {
                throw new ArgumentException("The fields are too large.");
            }

            try
            {
                return JsonNode.Parse(json, documentOptions: new System.Text.Json.JsonDocumentOptions { MaxDepth = 32 })
                    as JsonObject ?? throw new ArgumentException("The fields must be a JSON object.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new ArgumentException("The fields are not valid JSON.");
            }
        }
    }
}
