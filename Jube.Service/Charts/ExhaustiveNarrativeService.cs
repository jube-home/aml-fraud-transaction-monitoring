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
using Jube.Dto.Query.ExhaustiveNarrative;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Query.ExhaustiveSearchInstancePromotionHistory;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustiveNarrativeService
    {
        private readonly ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService predictedActual;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService prescriptions;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted;
        private readonly ExhaustiveSearchInstancePromotionHistoryService promotionHistory;

        private ExhaustiveNarrativeService(
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService predictedActual,
            ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted,
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService prescriptions,
            ExhaustiveSearchInstancePromotionHistoryService promotionHistory)
        {
            this.predictedActual = predictedActual;
            this.promoted = promoted;
            this.prescriptions = prescriptions;
            this.promotionHistory = promotionHistory;
        }

        public static Task<ExhaustiveNarrativeService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveNarrativeService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            return new ExhaustiveNarrativeService(
                await ExhaustiveSearchInstancePromotedTrialInstancePredictedActualService.CreateAsync(dbContext,
                    userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false),
                await ExhaustiveSearchInstancePromotedTrialInstanceQueryService.CreateAsync(dbContext, userName, log,
                    stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false),
                await ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService.CreateAsync(dbContext,
                    userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false),
                await ExhaustiveSearchInstancePromotionHistoryService.CreateAsync(dbContext, userName, log,
                    stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false));
        }

        public async Task<ExhaustiveNarrativeDto> GetAsync(int exhaustiveSearchInstanceId,
            CancellationToken token = default)
        {
            var promotedRows = await promoted.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false);
            var active = promotedRows.FirstOrDefault(p => p.Active);

            var rows = await predictedActual.GetAsync(exhaustiveSearchInstanceId, token: token).ConfigureAwait(false);

            var variablePrescriptions = active is null
                ? []
                : await prescriptions.GetAsync(active.Id, token).ConfigureAwait(false);

            var promotions = await promotionHistory.GetAsync(exhaustiveSearchInstanceId, token)
                .ConfigureAwait(false);

            var facts = ExhaustiveNarrativeFacts.Build(rows, active, variablePrescriptions, promotions);

            var groups = new Dictionary<string, List<string>>();
            AddIfPresent(groups, "Performance", ExhaustiveNarrativeRules.Performance(facts));
            AddIfPresent(groups, "Calibration", ExhaustiveNarrativeRules.Calibration(facts));
            AddIfPresent(groups, "Sensitivity", ExhaustiveNarrativeRules.Sensitivity(facts));
            AddIfPresent(groups, "Prescription", ExhaustiveNarrativeRules.Prescription(facts));
            AddIfPresent(groups, "Confusion", ExhaustiveNarrativeRules.Confusion(facts));
            AddIfPresent(groups, "Discrimination", ExhaustiveNarrativeRules.Discrimination(facts));
            AddIfPresent(groups, "Ks", ExhaustiveNarrativeRules.Ks(facts));
            AddIfPresent(groups, "ThresholdSweep", ExhaustiveNarrativeRules.ThresholdSweep(facts));
            AddIfPresent(groups, "ScoreDistribution", ExhaustiveNarrativeRules.ScoreDistribution(facts));
            AddIfPresent(groups, "Gains", ExhaustiveNarrativeRules.Gains(facts));
            AddIfPresent(groups, "PrecisionRecall", ExhaustiveNarrativeRules.PrecisionRecall(facts));
            AddIfPresent(groups, "Lift", ExhaustiveNarrativeRules.Lift(facts));
            AddIfPresent(groups, "LearningCurve", ExhaustiveNarrativeRules.LearningCurve(facts));

            return new ExhaustiveNarrativeDto
            {
                Holistic = ExhaustiveNarrativeRules.Holistic(facts).ToList(),
                Groups = groups
            };
        }

        private static void AddIfPresent(Dictionary<string, List<string>> groups, string key,
            IReadOnlyList<string> value)
        {
            if (value.Count > 0)
            {
                groups[key] = value.ToList();
            }
        }
    }
}