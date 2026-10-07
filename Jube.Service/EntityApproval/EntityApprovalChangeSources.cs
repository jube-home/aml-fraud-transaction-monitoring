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

namespace Jube.Service.EntityApproval
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using LinqToDB;

    public static class EntityApprovalChangeSources
    {
        public static readonly IReadOnlySet<string> ReviewIgnoredFields = new HashSet<string>(
            [
                "Id", "Version", "CreatedUser", "CreatedDate", "UpdatedUser", "UpdatedDate", "DeletedUser",
                "DeletedDate", "Deleted", "Guid", "DeleteExpiryDate", "ImportId", "InheritedId"
            ],
            StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyDictionary<EntityApprovalKind, Func<DbContext, int, int, CancellationToken,
            Task<(object? Current, object? Earlier)>>> sources =
            new Dictionary<EntityApprovalKind, Func<DbContext, int, int, CancellationToken,
                Task<(object? Current, object? Earlier)>>>
            {
                [EntityApprovalKind.EntityAnalysisModel] = Source<EntityAnalysisModel, EntityAnalysisModelVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows.Where(r => r.EntityAnalysisModelId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelList] = Source<EntityAnalysisModelList,
                    EntityAnalysisModelListVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows.Where(r => r.EntityAnalysisModelListId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelListValue] = Source<EntityAnalysisModelListValue,
                    EntityAnalysisModelListValueVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelListValueId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelDictionary] = Source<EntityAnalysisModelDictionary,
                    EntityAnalysisModelDictionaryVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelDictionaryId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelDictionaryKvp] = Source<EntityAnalysisModelDictionaryKvp,
                    EntityAnalysisModelDictionaryKvpVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelDictionaryKvpId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelRequestXPath] = Source<EntityAnalysisModelRequestXpath,
                    EntityAnalysisModelRequestXpathVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelRequestXpathId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelInlineScript] = Source<EntityAnalysisModelInlineScript,
                    EntityAnalysisModelInlineScriptVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelInlineScriptId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelInlineFunction] = Source<EntityAnalysisModelInlineFunction,
                    EntityAnalysisModelInlineFunctionVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelInlineFunctionId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelGatewayRule] = Source<EntityAnalysisModelGatewayRule,
                    EntityAnalysisModelGatewayRuleVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelGatewayRuleId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelSanction] = Source<EntityAnalysisModelSanction,
                    EntityAnalysisModelSanctionVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows.Where(r => r.EntityAnalysisModelSanctionId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelAbstractionRule] = Source<EntityAnalysisModelAbstractionRule,
                    EntityAnalysisModelAbstractionRuleVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelAbstractionRuleId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelAbstractionCalculation] =
                    Source<EntityAnalysisModelAbstractionCalculation,
                        EntityAnalysisModelAbstractionCalculationVersion>(
                        (rows, id) => rows.Where(r => r.Id == id),
                        (rows, id, version) => rows.Where(r =>
                                r.EntityAnalysisModelAbstractionCalculationId == id && r.Version < version)
                            .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelTtlCounter] = Source<EntityAnalysisModelTtlCounter,
                    EntityAnalysisModelTtlCounterVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelTtlCounterId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelHttpAdaptation] = Source<EntityAnalysisModelHttpAdaptation,
                    EntityAnalysisModelHttpAdaptationVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelHttpAdaptationId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.ExhaustiveSearchInstance] = Source<ExhaustiveSearchInstance,
                    ExhaustiveSearchInstanceVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows.Where(r => r.ExhaustiveSearchInstanceId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelActivationRule] = Source<EntityAnalysisModelActivationRule,
                    EntityAnalysisModelActivationRuleVersion>(
                    (rows, id) => rows.Where(r => r.Id == id),
                    (rows, id, version) => rows
                        .Where(r => r.EntityAnalysisModelActivationRuleId == id && r.Version < version)
                        .OrderByDescending(r => r.Version)),
                [EntityApprovalKind.EntityAnalysisModelTag] =
                    Source<EntityAnalysisModelTag, EntityAnalysisModelTagVersion>(
                        (rows, id) => rows.Where(r => r.Id == id),
                        (rows, id, version) => rows.Where(r => r.EntityAnalysisModelTagId == id && r.Version < version)
                            .OrderByDescending(r => r.Version))
            };

        public static IReadOnlySet<EntityApprovalKind> Kinds => sources.Keys.ToHashSet();

        public static Task<(object? Current, object? Earlier)> LoadAsync(DbContext dbContext, EntityApprovalKind kind,
            int entityId, int version, CancellationToken token)
        {
            return sources[kind](dbContext, entityId, version, token);
        }

        private static Func<DbContext, int, int, CancellationToken, Task<(object? Current, object? Earlier)>> Source<
            TEntity,
            TVersion>(Func<IQueryable<TEntity>, int, IQueryable<TEntity>> current,
            Func<IQueryable<TVersion>, int, int, IQueryable<TVersion>> earlier)
            where TEntity : class
            where TVersion : class
        {
            return async (dbContext, entityId, version, token) =>
            {
                var currentRow = await current(dbContext.GetTable<TEntity>(), entityId)
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);
                var earlierRow = await earlier(dbContext.GetTable<TVersion>(), entityId, version)
                    .FirstOrDefaultAsync(token).ConfigureAwait(false);

                return (currentRow, earlierRow);
            };
        }
    }
}