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


using Jube.Data.Context;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Resources;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using LinqToDB;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalViewFilter
    {
        public static async Task<JournalMatchFilter> BuildAsync(DbContext dbContext, string userName,
            CaseKeyJournalTagJobPreviewRequestDto request, IStringLocalizer strings, CancellationToken token)
        {
            IReadOnlyDictionary<string, ArchiveFilterField> fields = new Dictionary<string, ArchiveFilterField>();

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                var modelId = await dbContext.CaseWorkflow
                    .Where(w => w.Guid == request.CaseWorkflowGuid && (w.Deleted == null || w.Deleted == 0))
                    .Select(w => w.EntityAnalysisModelId).FirstOrDefaultAsync(token).ConfigureAwait(false);

                if (modelId is not null)
                {
                    fields = await ArchiveFilterFields.LoadAsync(dbContext, userName, modelId.Value, token)
                        .ConfigureAwait(false);
                }
            }

            return Build(request, strings, fields);
        }

        public static JournalMatchFilter Build(CaseKeyJournalTagJobPreviewRequestDto request, IStringLocalizer strings,
            IReadOnlyDictionary<string, ArchiveFilterField> fields)
        {
            var failures = new List<FluentValidation.Results.ValidationFailure>();
            var to = request.To?.UtcDateTime ?? DateTime.UtcNow;
            var from = request.From?.UtcDateTime ?? to.AddDays(-CaseKeyJournalService.DefaultDays);
            var merge = JournalMerge.Parse(request.Merge);
            ArchiveFilter? columnFilter = null;

            try
            {
                columnFilter = ArchiveFilter.Parse(request.Filter, fields);
            }
            catch (ArgumentException)
            {
                failures.Add(Failure(strings, nameof(request.Filter), CaseKeyJournalResources.FilterInvalid, "FilterInvalid"));
            }

            if (string.IsNullOrWhiteSpace(request.Key))
            {
                failures.Add(Failure(strings, nameof(request.Key), CaseKeyJournalResources.KeyRequired, "KeyRequired"));
            }
            else if (request.Key.Length > JournalMerge.MaximumKeyLength || request.Key.Any(char.IsControl))
            {
                failures.Add(Failure(strings, nameof(request.Key), CaseKeyJournalResources.KeyInvalid, "KeyInvalid"));
            }

            if (string.IsNullOrEmpty(request.Value) || request.Value.Length > JournalMerge.MaximumValueLength
                                                    || request.Value.Contains('\0'))
            {
                failures.Add(Failure(strings, nameof(request.Value), CaseKeyJournalResources.ValueRequired, "ValueRequired"));
            }

            if (from >= to || (to - from).TotalDays > CaseKeyJournalService.MaximumDays)
            {
                failures.Add(Failure(strings, nameof(request.From), CaseKeyJournalResources.PeriodInvalid, "PeriodInvalid"));
            }

            if (!JournalMerge.IsValid(merge))
            {
                failures.Add(Failure(strings, nameof(request.Merge), CaseKeyJournalResources.MergeInvalid, "MergeInvalid"));
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new FluentValidation.Results.ValidationResult(failures));
            }

            return new JournalMatchFilter(request.Key!, request.Value!, from, to, request.ActivationsOnly == true,
                request.ResponseElevation ?? 0, merge!, columnFilter);
        }

        private static FluentValidation.Results.ValidationFailure Failure(IStringLocalizer strings, string property,
            string resource, string code)
        {
            return new FluentValidation.Results.ValidationFailure(property, strings[resource]) { ErrorCode = code };
        }
    }
}
