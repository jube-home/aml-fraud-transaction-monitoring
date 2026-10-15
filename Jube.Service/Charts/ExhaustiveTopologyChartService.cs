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
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery;
using Jube.Service.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription;
using Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceListQuery;
using Jube.Service.Query.ExhaustiveSearchInstanceTrialInstanceVariable;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class ExhaustiveTopologyChartService
    {
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService prescriptions;
        private readonly ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted;
        private readonly ExhaustiveSearchInstanceTrialInstanceListQueryService trials;
        private readonly ExhaustiveSearchInstanceTrialInstanceVariableService variables;

        private ExhaustiveTopologyChartService(ExhaustiveSearchInstancePromotedTrialInstanceQueryService promoted,
            ExhaustiveSearchInstanceTrialInstanceVariableService variables,
            ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService prescriptions,
            ExhaustiveSearchInstanceTrialInstanceListQueryService trials)
        {
            this.promoted = promoted;
            this.variables = variables;
            this.prescriptions = prescriptions;
            this.trials = trials;
        }

        public static Task<ExhaustiveTopologyChartService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveTopologyChartService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var promoted = await ExhaustiveSearchInstancePromotedTrialInstanceQueryService
                .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false);
            var variables = await ExhaustiveSearchInstanceTrialInstanceVariableService
                .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false);
            var prescriptions = await ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionService
                .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false);
            var trials = await ExhaustiveSearchInstanceTrialInstanceListQueryService
                .CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token)
                .ConfigureAwait(false);

            return new ExhaustiveTopologyChartService(promoted, variables, prescriptions, trials);
        }

        public async Task<string> GetTrialSvgAsync(int trialInstanceId, CancellationToken token = default)
        {
            var id = "ex-tt-" + trialInstanceId;
            var json = await trials.GetDiagramJsonAsync(trialInstanceId, token).ConfigureAwait(false);

            return ExhaustiveTopologyRenderer.Topology(json, id: id);
        }

        public Task<string> GetSvgAsync(int exhaustiveSearchInstanceId, CancellationToken token = default)
        {
            return GetSvgAsync(exhaustiveSearchInstanceId, null, token);
        }

        public async Task<string> GetSvgAsync(int exhaustiveSearchInstanceId,
            IReadOnlyList<IReadOnlyList<double>>? liveRelevance, CancellationToken token = default)
        {
            var id = "ex-tp-" + exhaustiveSearchInstanceId;

            var active = (await promoted.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false))
                .FirstOrDefault(p => p.Active);

            if (active is null || string.IsNullOrWhiteSpace(active.Json))
            {
                return ExhaustiveTopologyRenderer.Empty(id: id);
            }

            var names = (await variables.GetAsync(exhaustiveSearchInstanceId, token).ConfigureAwait(false))
                .Select(v => v.Name).ToList();

            var sensitivities = (await prescriptions.GetAsync(active.Id, token).ConfigureAwait(false))
                .Select(p => new TopologyInputSensitivity(p.Name, p.Sensitivity)).ToList();

            return ExhaustiveTopologyRenderer.Topology(active.Json, names, sensitivities, active.TopologyComplexity,
                active.Score, id: id, liveRelevance: liveRelevance);
        }
    }
}