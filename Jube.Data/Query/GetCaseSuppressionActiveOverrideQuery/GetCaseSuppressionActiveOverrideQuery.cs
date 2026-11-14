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

namespace Jube.Data.Query.GetCaseSuppressionActiveOverrideQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetCaseSuppressionActiveOverrideQuery(DbContext dbContext)
    {
        public Task<GetCaseSuppressionActiveOverrideQueryDto> ExecuteAsync(Guid entityAnalysisModelGuid,
            string overrideKey, string overrideKeyValue, string ruleName, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            if (string.IsNullOrEmpty(ruleName))
            {
                return dbContext.EntityAnalysisModelOverride
                    .Where(w => w.EntityAnalysisModelGuid == entityAnalysisModelGuid && w.OverrideKey == overrideKey
                                                                                    && w.OverrideKeyValue ==
                                                                                    overrideKeyValue
                                                                                    && (w.Deleted == 0 ||
                                                                                        w.Deleted == null)
                                                                                    && (w.DeleteExpiryDate == null ||
                                                                                        w.DeleteExpiryDate > now))
                    .Select(s => new GetCaseSuppressionActiveOverrideQueryDto
                    {
                        Id = s.Id, OverrideKindId = s.OverrideKind ?? 0
                    }).FirstOrDefaultAsync(token);
            }

            return dbContext.EntityAnalysisModelActivationRuleOverride
                .Where(w => w.EntityAnalysisModelGuid == entityAnalysisModelGuid && w.OverrideKey == overrideKey
                                                                                && w.OverrideKeyValue ==
                                                                                overrideKeyValue
                                                                                && w
                                                                                    .EntityAnalysisModelActivationRuleName ==
                                                                                ruleName
                                                                                && (w.Deleted == 0 ||
                                                                                    w.Deleted == null)
                                                                                && (w.DeleteExpiryDate == null ||
                                                                                    w.DeleteExpiryDate > now))
                .Select(s => new GetCaseSuppressionActiveOverrideQueryDto
                {
                    Id = s.Id, OverrideKindId = s.OverrideKind ?? 0
                }).FirstOrDefaultAsync(token);
        }
    }
}
