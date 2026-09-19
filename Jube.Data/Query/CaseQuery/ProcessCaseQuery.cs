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

namespace Jube.Data.Query.CaseQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto;
    using Newtonsoft.Json.Linq;

    public class ProcessCaseQuery(DbContext dbContext, string userName)
    {
        public async Task<CaseQueryDto> ProcessAsync(CaseQueryDto getCaseByIdDto, CancellationToken token = default)
        {
            if (getCaseByIdDto == null)
            {
                return null;
            }

            var caseWorkflowXPathByCaseWorkflowIdQuery =
                new GetCaseWorkflowXPathByCaseWorkflowIdQuery(dbContext, userName);

            var xPaths = await caseWorkflowXPathByCaseWorkflowIdQuery
                .ExecuteAsync(getCaseByIdDto.CaseWorkflowGuid, token);

            JObject json;
            try
            {
                json = JObject.Parse(getCaseByIdDto.Json ?? "{}");
            }
            catch (Newtonsoft.Json.JsonException)
            {
                json = new JObject();
            }

            getCaseByIdDto.FormattedPayload = [];

            foreach (var xPath in xPaths)
            {
                var getCaseByIdFieldEntryDto = new GetCaseByIdFieldEntryDto();
                var missing = false;
                try
                {
                    var jToken = json.SelectToken(xPath.XPath);
                    if (jToken != null)
                    {
                        getCaseByIdFieldEntryDto.Value = jToken.Value<string>();
                        getCaseByIdFieldEntryDto.Name = xPath.Name;
                        getCaseByIdFieldEntryDto.ConditionalRegularExpressionFormatting
                            = xPath.ConditionalRegularExpressionFormatting;
                        getCaseByIdFieldEntryDto.CellFormatForeColor = xPath.ConditionalFormatForeColor;
                        getCaseByIdFieldEntryDto.CellFormatBackColor = xPath.ConditionalFormatBackColor;
                        getCaseByIdFieldEntryDto.CellFormatForeRow = xPath.ForeRowColorScope;
                        getCaseByIdFieldEntryDto.CellFormatBackRow = xPath.BackRowColorScope;

                        if (getCaseByIdFieldEntryDto.ConditionalRegularExpressionFormatting)
                        {
                            if (xPath.RegularExpression != null)
                            {
                                try
                                {
                                    var regex = new Regex(xPath.RegularExpression, RegexOptions.None,
                                        TimeSpan.FromSeconds(1));

                                    var match = regex.Match(getCaseByIdFieldEntryDto.Value);
                                    getCaseByIdFieldEntryDto.ExistsMatch = match.Success;
                                }
                                catch
                                {
                                    getCaseByIdFieldEntryDto.ExistsMatch = false;
                                }
                            }
                            else
                            {
                                getCaseByIdFieldEntryDto.ExistsMatch = false;
                            }
                        }
                    }
                    else
                    {
                        missing = true;
                    }
                }
                catch
                {
                    missing = true;
                }

                if (!missing)
                {
                    getCaseByIdDto.FormattedPayload.Add(getCaseByIdFieldEntryDto);
                }
            }

            getCaseByIdDto.Activation = [];
            IEnumerable<JToken> jTokensActivation;
            try
            {
                jTokensActivation = json.SelectTokens("$.activation").ToList();
            }
            catch (Newtonsoft.Json.JsonException)
            {
                jTokensActivation = [];
            }

            foreach (var activationJToken in jTokensActivation.OfType<JObject>())
            foreach (var property in activationJToken.Properties())
            {
                if (property.Value is JObject jValue && jValue["visible"]?.Type == JTokenType.Integer
                                                     && (int)jValue["visible"] == 1)
                {
                    getCaseByIdDto.Activation.Add(new GetCaseByIdActivationDto { Name = property.Name });
                }
            }

            return getCaseByIdDto;
        }
    }
}