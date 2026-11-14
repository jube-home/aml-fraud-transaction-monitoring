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

using System.Globalization;
using Jube.CaseRisk;
using Jube.Data.Context;
using Jube.Data.Repository;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Service.Query.CaseTriage
{
    public static class QaDisagreementSampling
    {
        public const double DefaultRate = 0.05;
        public const double DefaultFactor = 3;
        private const byte QaPicked = 2;

        public static double BoostedRate(double rate, double factor)
        {
            return Math.Clamp(rate * Math.Max(1, factor), 0, 1);
        }

        public static async Task<bool> ApplyAsync(DbContext dbContext, string userName, int caseId,
            CancellationToken token = default)
        {
            var target = await dbContext.Case.Where(c => c.Id == caseId)
                .Select(c => new { c.CaseWorkflowGuid, c.CaseKey, c.CaseKeyValue, c.QaSampleStatusId })
                .FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (target is null || target.QaSampleStatusId == QaPicked)
            {
                return false;
            }

            var workflowRate = await dbContext.CaseWorkflow.Where(w => w.Guid == target.CaseWorkflowGuid && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.RiskQaSampleRate).FirstOrDefaultAsync(token).ConfigureAwait(false);

            var rate = BoostedRate(workflowRate ?? Number("CaseRiskQaSampleRate", DefaultRate),
                Number("CaseTriageQaOverrideFactor", DefaultFactor));

            if (!QaSampler.Picked(target.CaseWorkflowGuid, caseId, rate))
            {
                return false;
            }

            var changed = await dbContext.Case.Where(c => c.Id == caseId && c.QaSampleStatusId != QaPicked)
                .Set(s => s.QaSampleStatusId, QaPicked).Set(s => s.QaSampleDate, DateTime.UtcNow)
                .UpdateAsync(token).ConfigureAwait(false);

            if (changed == 0)
            {
                return false;
            }

            await new CaseEventRepository(dbContext, userName).InsertAsync(new Data.Poco.CaseEvent
            {
                CaseId = caseId,
                CaseKey = target.CaseKey,
                CaseKeyValue = target.CaseKeyValue,
                CaseEventTypeId = 20,
                After = rate.ToString("0.####", CultureInfo.InvariantCulture)
            }, token).ConfigureAwait(false);

            return true;
        }

        private static double Number(string name, double fallback)
        {
            return double.TryParse(Environment.GetEnvironmentVariable(name), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value) && value > 0
                ? value
                : fallback;
        }
    }
}
