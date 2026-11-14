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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;

    public class ModelDriftRepository(DbContext dbContext)
    {
        public const string ExhaustiveFacet = "exhaustiveAdaptation";
        public const string AdvancedAnalyticsFacet = "advancedAnalyticsAdaptation";

        public static string FacetOf(short sourceId)
        {
            return sourceId == 1 ? ExhaustiveFacet : AdvancedAnalyticsFacet;
        }

        public Task<List<ModelDriftModel>> ListModelsWithRecentArchiveAsync(DateTime since,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT a."EntityAnalysisModelId" AS "ModelId", m."TenantRegistryId", m."Name"
                FROM "Archive" a
                JOIN "EntityAnalysisModel" m ON m."Id" = a."EntityAnalysisModelId"
                    AND (m."Deleted" IS NULL OR m."Deleted" = 0) AND m."TenantRegistryId" IS NOT NULL
                WHERE a."ReferenceDate" >= @since
                """;

            return dbContext.QueryToListAsync<ModelDriftModel>(sql, token, new DataParameter("since", since));
        }

        public Task<List<string>> ListScoreNamesAsync(int modelId, short sourceId, DateTime since,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT DISTINCT jsonb_object_keys(r."Json" -> @facet)
                FROM (SELECT a."Json" FROM "Archive" a
                      WHERE a."EntityAnalysisModelId" = @model AND a."ReferenceDate" >= @since
                        AND jsonb_typeof(a."Json" -> @facet) = 'object'
                      ORDER BY a."Id" DESC LIMIT 2000) r
                """;

            return dbContext.QueryToListAsync<string>(sql, token,
                new DataParameter("facet", FacetOf(sourceId)),
                new DataParameter("model", modelId),
                new DataParameter("since", since));
        }

        public Task<List<double>> ListScoresAsync(int modelId, short sourceId, string name, DateTime from,
            DateTime to, int maximum, CancellationToken token = default)
        {
            const string sql = """
                SELECT (a."Json" -> @facet ->> @name)::double precision
                FROM "Archive" a
                WHERE a."EntityAnalysisModelId" = @model AND a."ReferenceDate" >= @from AND a."ReferenceDate" < @to
                  AND a."Json" -> @facet ->> @name ~ '^-?[0-9]+(\.[0-9]+)?([eE][-+]?[0-9]+)?$'
                ORDER BY a."Id" DESC
                LIMIT @maximum
                """;

            return dbContext.QueryToListAsync<double>(sql, token,
                new DataParameter("facet", FacetOf(sourceId)),
                new DataParameter("name", name),
                new DataParameter("model", modelId),
                new DataParameter("from", from),
                new DataParameter("to", to),
                new DataParameter("maximum", maximum));
        }

        public Task<short?> LatestStatusAsync(int modelId, short sourceId, string name,
            CancellationToken token = default)
        {
            return dbContext.ModelDriftObservation
                .Where(w => w.EntityAnalysisModelId == modelId && w.SourceId == sourceId && w.ScoreName == name)
                .OrderByDescending(o => o.ObservedDate).ThenByDescending(o => o.Id)
                .Select(s => (short?)s.StatusId)
                .FirstOrDefaultAsync(token);
        }

        public Task InsertObservationAsync(ModelDriftObservation observation, CancellationToken token = default)
        {
            return dbContext.InsertAsync(observation, token: token);
        }
    }

    public class ModelDriftModel
    {
        public int ModelId { get; set; }
        public int TenantRegistryId { get; set; }
        public string Name { get; set; }
    }
}
