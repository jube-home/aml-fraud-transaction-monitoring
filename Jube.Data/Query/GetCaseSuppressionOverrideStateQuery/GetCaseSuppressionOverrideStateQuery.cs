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

namespace Jube.Data.Query.GetCaseSuppressionOverrideStateQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseSuppressionOverrideStateQuery(DbContext dbContext)
    {
        public async Task<List<GetCaseSuppressionOverrideStateQueryDto>> ExecuteAsync(
            IReadOnlyCollection<int> modelOverrideIds, IReadOnlyCollection<int> ruleOverrideIds,
            CancellationToken token = default)
        {
            var models = modelOverrideIds.Count == 0
                ? []
                : await dbContext.EntityAnalysisModelOverride.Where(w => modelOverrideIds.Contains(w.Id))
                    .Select(s => new GetCaseSuppressionOverrideStateQueryDto
                    {
                        Id = s.Id, IsRule = false, Deleted = s.Deleted == 1, DeleteExpiryDate = s.DeleteExpiryDate
                    }).ToListAsync(token).ConfigureAwait(false);

            var rules = ruleOverrideIds.Count == 0
                ? []
                : await dbContext.EntityAnalysisModelActivationRuleOverride
                    .Where(w => ruleOverrideIds.Contains(w.Id))
                    .Select(s => new GetCaseSuppressionOverrideStateQueryDto
                    {
                        Id = s.Id, IsRule = true, Deleted = s.Deleted == 1, DeleteExpiryDate = s.DeleteExpiryDate
                    }).ToListAsync(token).ConfigureAwait(false);

            return [.. models, .. rules];
        }
    }
}
