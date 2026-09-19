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

using System.Diagnostics.CodeAnalysis;
using Jube.Dto.Repository.CaseWorkflowXPath;
using RulePoco = Jube.Data.Poco.CaseWorkflowXPath;

namespace Jube.Service.Repository.CaseWorkflowXPath
{
    internal static class CaseWorkflowXPathMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        public static CaseWorkflowXPathDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowXPathDto
            {
                Id = rulePoco.Id,
                Name = rulePoco.Name,
                Active = rulePoco.Active == 1,
                Locked = rulePoco.Locked == 1,
                XPath = rulePoco.XPath,
                Drill = rulePoco.Drill == 1,
                BoldLineMatched = rulePoco.BoldLineMatched == 1,
                BoldLineFormatForeColor = rulePoco.BoldLineFormatForeColor,
                BoldLineFormatBackColor = rulePoco.BoldLineFormatBackColor,
                ConditionalRegularExpressionFormatting = rulePoco.ConditionalRegularExpressionFormatting == 1,
                RegularExpression = rulePoco.RegularExpression,
                ConditionalFormatForeColor = rulePoco.ConditionalFormatForeColor,
                ConditionalFormatBackColor = rulePoco.ConditionalFormatBackColor,
                ForeRowColorScope = rulePoco.ForeRowColorScope == 1,
                BackRowColorScope = rulePoco.BackRowColorScope == 1,
                CaseWorkflowId = rulePoco.CaseWorkflowId.GetValueOrDefault(),
                Guid = rulePoco.Guid,
                CreatedDate = ToOffset(rulePoco.CreatedDate),
                CreatedUser = rulePoco.CreatedUser,
                UpdatedUser = rulePoco.UpdatedUser,
                UpdatedDate = ToOffset(rulePoco.UpdatedDate),
                Version = rulePoco.Version.GetValueOrDefault(),
                DeletedUser = rulePoco.DeletedUser,
                DeletedDate = ToOffset(rulePoco.DeletedDate)
            };

        public static List<CaseWorkflowXPathDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();

        public static RulePoco ToPoco(CaseWorkflowXPathDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            XPath = dto.XPath,
            Drill = (byte)(dto.Drill ? 1 : 0),
            BoldLineMatched = (byte)(dto.BoldLineMatched ? 1 : 0),
            BoldLineFormatForeColor = dto.BoldLineFormatForeColor,
            BoldLineFormatBackColor = dto.BoldLineFormatBackColor,
            ConditionalRegularExpressionFormatting = (byte)(dto.ConditionalRegularExpressionFormatting ? 1 : 0),
            RegularExpression = dto.RegularExpression,
            ConditionalFormatForeColor = dto.ConditionalFormatForeColor,
            ConditionalFormatBackColor = dto.ConditionalFormatBackColor,
            ForeRowColorScope = (byte)(dto.ForeRowColorScope ? 1 : 0),
            BackRowColorScope = (byte)(dto.BackRowColorScope ? 1 : 0),
            CaseWorkflowId = dto.CaseWorkflowId
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}