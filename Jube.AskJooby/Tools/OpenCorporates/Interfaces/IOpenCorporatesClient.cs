/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Tools.OpenCorporates.Models;

namespace Jube.AskJooby.Tools.OpenCorporates.Interfaces
{
    public interface IOpenCorporatesClient
    {
        Task<OpenCorporatesCompanySearchResponse> SearchCompanyAsync(string query, int maxResults,
            CancellationToken token = default);

        Task<OpenCorporatesCompanySearchResponse> SearchCompanyAsync(string query, int maxResults,
            IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            return SearchCompanyAsync(query, maxResults, token);
        }

        Task<OpenCorporatesCompanyResponse> GetCompanyAsync(string jurisdictionCode, string companyNumber,
            CancellationToken token = default);

        Task<OpenCorporatesCompanyResponse> GetCompanyAsync(string jurisdictionCode, string companyNumber,
            IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            return GetCompanyAsync(jurisdictionCode, companyNumber, token);
        }

        Task<OpenCorporatesBeneficialOwnerSearchResponse> SearchBeneficialOwnersAsync(string companyName,
            string? ownerName, CancellationToken token = default);

        Task<OpenCorporatesBeneficialOwnerSearchResponse> SearchBeneficialOwnersAsync(string companyName,
            string? ownerName, IRunRecorder? recorder, string? runId, CancellationToken token = default)
        {
            return SearchBeneficialOwnersAsync(companyName, ownerName, token);
        }
    }
}