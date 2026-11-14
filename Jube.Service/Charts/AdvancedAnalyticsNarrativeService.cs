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
using Jube.Dto.Query.AdvancedAnalyticsNarrative;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AskJoobyAnalyticsModel;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Charts
{
    public sealed class AdvancedAnalyticsNarrativeService
    {
        private readonly AskJoobyAnalyticsModelService inner;

        private AdvancedAnalyticsNarrativeService(AskJoobyAnalyticsModelService inner)
        {
            this.inner = inner;
        }

        public static Task<AdvancedAnalyticsNarrativeService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsNarrativeService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            return new AdvancedAnalyticsNarrativeService(await AskJoobyAnalyticsModelService.CreateAsync(dbContext,
                userName, log, stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false));
        }

        public async Task<AdvancedAnalyticsNarrativeDto> GetAsync(int versionId, CancellationToken token = default)
        {
            var source = await inner.GetChartSourceAsync(versionId, token).ConfigureAwait(false);

            if (source == null)
            {
                return new AdvancedAnalyticsNarrativeDto();
            }

            var facts = AdvancedAnalyticsNarrativeFacts.Build(source);

            var groups = new Dictionary<string, List<string>>();
            AddIfPresent(groups, "ModelDiagram", AdvancedAnalyticsNarrativeRules.ModelDiagram(facts));
            AddIfPresent(groups, "Calibration", AdvancedAnalyticsNarrativeRules.Calibration(facts));
            AddIfPresent(groups, "Roc", AdvancedAnalyticsNarrativeRules.Roc(facts));

            return new AdvancedAnalyticsNarrativeDto
            {
                Holistic = AdvancedAnalyticsNarrativeRules.Holistic(facts).ToList(),
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