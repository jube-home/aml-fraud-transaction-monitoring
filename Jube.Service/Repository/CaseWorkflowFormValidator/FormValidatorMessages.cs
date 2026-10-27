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

using Jube.Resources;
using Jube.Service.Charts.Datasource.Template;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public static class FormValidatorMessages
    {
        public static string For(string code, string? authored, IStringLocalizer entryStrings)
        {
            if (!string.IsNullOrWhiteSpace(authored))
            {
                return authored;
            }

            return code switch
            {
                FormSubmissionValidator.Unknown => entryStrings[CaseWorkflowFormEntryResources.FieldUnknown],
                FormSubmissionValidator.Type => entryStrings[CaseWorkflowFormEntryResources.FieldType],
                FormSubmissionValidator.Option => entryStrings[CaseWorkflowFormEntryResources.FieldOption],
                FormValidatorEngine.Required => entryStrings[CaseWorkflowFormEntryResources.FieldRequired],
                FormValidatorEngine.Range => entryStrings[CaseWorkflowFormEntryResources.FieldRange],
                FormValidatorEngine.Length => entryStrings[CaseWorkflowFormEntryResources.FieldLength],
                FormValidatorEngine.Pattern => entryStrings[CaseWorkflowFormEntryResources.FieldPattern],
                FormValidatorEngine.Compare => entryStrings[CaseWorkflowFormEntryResources.FieldCompare],
                FormValidatorEngine.List => entryStrings[CaseWorkflowFormEntryResources.FieldList],
                FormValidatorEngine.Dictionary => entryStrings[CaseWorkflowFormEntryResources.FieldDictionary],
                FormValidatorEngine.SqlFailed => entryStrings[CaseWorkflowFormEntryResources.FieldSqlFailed],
                _ => entryStrings[CaseWorkflowFormEntryResources.FieldSql]
            };
        }
    }
}
