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


using System.Text.RegularExpressions;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;

namespace Jube.Service.Query.CaseKeyJournal
{
    public sealed record JournalLocation(string Latitude, string Longitude)
    {
        private static readonly Regex payloadPath = new(
            @"^(?:\$\s*\.?\s*)?(?:payload|\[\s*['""]payload['""]\s*\])\s*(?:\.\s*(?<name>[^.\[\]\s]+)|\[\s*['""](?<name>[^'""]+)['""]\s*\])\s*\z",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        public static async Task<JournalLocation?> LoadAsync(DbContext dbContext, int modelId,
            IEnumerable<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths, CancellationToken token)
        {
            var fields = await LinqToDB.AsyncExtensions.ToListAsync(
                dbContext.EntityAnalysisModelRequestXpath
                    .Where(f => f.EntityAnalysisModelId == modelId && (f.DataTypeId == 6 || f.DataTypeId == 7)
                                                                  && f.Active == 1 && (f.Deleted == null || f.Deleted == 0))
                    .OrderBy(f => f.Id).Select(f => new { f.DataTypeId, f.Name }), token).ConfigureAwait(false);

            return Find(xPaths, fields.FirstOrDefault(f => f.DataTypeId == 6)?.Name,
                fields.FirstOrDefault(f => f.DataTypeId == 7)?.Name);
        }

        public static string? PayloadName(string? xPath)
        {
            var match = xPath is null ? Match.Empty : payloadPath.Match(xPath.Trim());

            return match.Success ? match.Groups["name"].Value : null;
        }

        public static JournalLocation? Find(IEnumerable<GetCaseWorkflowXPathByCaseWorkflowIdQueryDto> xPaths,
            string? latitudeField, string? longitudeField)
        {
            if (string.IsNullOrEmpty(latitudeField) || string.IsNullOrEmpty(longitudeField))
            {
                return null;
            }

            string? latitude = null;
            string? longitude = null;

            foreach (var xPath in xPaths)
            {
                var name = PayloadName(xPath.XPath);

                if (name == latitudeField)
                {
                    latitude ??= xPath.Name;
                }
                else if (name == longitudeField)
                {
                    longitude ??= xPath.Name;
                }
            }

            return latitude is null || longitude is null ? null : new JournalLocation(latitude, longitude);
        }
    }
}
