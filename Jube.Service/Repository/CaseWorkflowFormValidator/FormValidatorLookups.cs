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

using Jube.Data.Repository;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public sealed class FormValidatorLookups(
        EntityAnalysisModelListRepository listRepository,
        EntityAnalysisModelListValueRepository listValueRepository,
        EntityAnalysisModelDictionaryRepository dictionaryRepository,
        EntityAnalysisModelDictionaryKvpRepository kvpRepository,
        Guid entityAnalysisModelGuid,
        int approvalsRequired) : IFormValidatorLookups
    {
        public async Task<bool> ListExistsAsync(string list, CancellationToken token)
        {
            return await listRepository.GetByNameEntityAnalysisModelGuidAsync(list, entityAnalysisModelGuid, token)
                .ConfigureAwait(false) != null;
        }

        public async Task<bool> DictionaryExistsAsync(string dictionary, CancellationToken token)
        {
            return await dictionaryRepository
                .GetByNameEntityAnalysisModelGuidAsync(dictionary, entityAnalysisModelGuid, token)
                .ConfigureAwait(false) != null;
        }

        public async Task<bool> ListContainsAsync(string list, string value, CancellationToken token)
        {
            var found = await listRepository.GetByNameEntityAnalysisModelGuidAsync(list, entityAnalysisModelGuid, token)
                .ConfigureAwait(false);

            if (found == null)
            {
                return false;
            }

            var values = await listValueRepository
                .GetApprovedByEntityAnalysisModelListIdOrderByIdAsync(found.Id, approvalsRequired, token)
                .ConfigureAwait(false);

            return values.Any(v => string.Equals(v.ListValue, value, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> DictionaryContainsAsync(string dictionary, string key, CancellationToken token)
        {
            var found = await dictionaryRepository
                .GetByNameEntityAnalysisModelGuidAsync(dictionary, entityAnalysisModelGuid, token)
                .ConfigureAwait(false);

            if (found == null)
            {
                return false;
            }

            var values = await kvpRepository
                .GetApprovedByEntityAnalysisModelDictionaryIdOrderByIdAsync(found.Id, approvalsRequired, token)
                .ConfigureAwait(false);

            return values.Any(v => string.Equals(v.KvpKey, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
