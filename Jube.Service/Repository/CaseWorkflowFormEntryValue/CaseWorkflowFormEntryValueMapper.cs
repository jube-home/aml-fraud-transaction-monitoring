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
using Jube.Dto.Repository.CaseWorkflowFormEntryValue;
using RulePoco = Jube.Data.Poco.CaseWorkflowFormEntryValue;

namespace Jube.Service.Repository.CaseWorkflowFormEntryValue
{
    internal static class CaseWorkflowFormEntryValueMapper
    {
        [return: NotNullIfNotNull(nameof(rulePoco))]
        private static CaseWorkflowFormEntryValueDto? ToDto(RulePoco? rulePoco) => rulePoco is null
            ? null
            : new CaseWorkflowFormEntryValueDto
            {
                Id = rulePoco.Id,
                Name = rulePoco.Name,
                Value = rulePoco.Value
            };

        public static List<CaseWorkflowFormEntryValueDto> ToDto(IEnumerable<RulePoco> source) =>
            source.Select(rulePoco => ToDto(rulePoco)).ToList();
    }
}