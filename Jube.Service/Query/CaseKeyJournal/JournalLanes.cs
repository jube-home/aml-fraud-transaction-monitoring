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

using FluentValidation.Results;
using Jube.Data.Query.ArchiveKeyLookup;
using Jube.Dto.Options;
using Jube.Resources;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Query.ArchiveKeyFields;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.CaseKeyJournal
{
    public static class JournalLanes
    {
        public static async Task<IReadOnlyList<ArchiveKeyPredicate>> ResolveAsync(ArchiveKeyFieldResolver resolver,
            int modelId, IReadOnlyList<JournalLane> lanes, IStringLocalizer strings, CancellationToken token,
            string property = "Merge", bool primaryIsCaseKey = false)
        {
            var predicates = new List<ArchiveKeyPredicate>();
            var failures = new List<ValidationFailure>();

            for (var index = 0; index < lanes.Count; index++)
            {
                var lane = lanes[index];
                var resolution = await resolver.ResolveAsync(modelId, lane.Key, lane.Value, token, lane.Operator,
                    lane.Upper).ConfigureAwait(false);

                if (resolution.Resolved)
                {
                    predicates.Add(resolution.Predicate!);
                    continue;
                }

                var name = index == 0 ? "Key" : property;

                failures.Add(resolution.State switch
                {
                    ArchiveKeyResolutionState.ValueInvalid => Failure(strings, name,
                        CaseKeyJournalResources.KeyValueInvalid),
                    ArchiveKeyResolutionState.NotLookupable => Failure(strings, name,
                        CaseKeyJournalResources.KeyNotLookupable),
                    ArchiveKeyResolutionState.OperatorNotAllowed => Failure(strings, name,
                        CaseKeyJournalResources.LaneOperatorNotAllowed),
                    _ => Failure(strings, name, index == 0 && primaryIsCaseKey
                        ? CaseKeyJournalResources.CaseKeyNotArchiveKey
                        : CaseKeyJournalResources.KeyNotArchiveKey)
                });
            }

            if (failures.Count > 0)
            {
                throw new DtoValidationException(new ValidationResult(failures));
            }

            return predicates;
        }

        public static ArchiveKeyLookupOperator OperatorOf(JournalLane lane)
        {
            return lane.Operator;
        }

        private static ValidationFailure Failure(IStringLocalizer strings, string property, string resource)
        {
            return new ValidationFailure(property, strings[resource]) { ErrorCode = resource };
        }
    }
}
