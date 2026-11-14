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

namespace Jube.Data.Query.GetArchiveKeyFieldsByEntityAnalysisModelIdQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ArchiveKeyLookup;
    using Context;
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;

    public class GetArchiveKeyFieldsByEntityAnalysisModelIdQuery(DbContext dbContext)
    {
        public async Task<List<ArchiveKeyField>> ExecuteAsync(int entityAnalysisModelId,
            CancellationToken token = default)
        {
            var fields = new List<ArchiveKeyField>();

            var requestXPaths = await dbContext.EntityAnalysisModelRequestXpath
                .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                            && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                .OrderBy(x => x.Id)
                .Select(x => new { x.Guid, x.Name, x.DataTypeId, x.ReportTable, x.EncryptionId })
                .ToListAsync(token).ConfigureAwait(false);

            fields.AddRange(requestXPaths.Select(x => new ArchiveKeyField(
                ArchiveKeySourceKind.EntityAnalysisModelRequestXPath, x.Guid, x.Name, x.DataTypeId ?? 1,
                ArchiveKeyPredicate.ColumnOf(x.DataTypeId), ArchiveKeyPredicate.RequestXPathProcessingTypeId,
                "payload", x.ReportTable == 1, x.EncryptionId)));

            var model = await dbContext.EntityAnalysisModel.Where(m => m.Id == entityAnalysisModelId)
                .Select(m => new { m.Guid, m.EntryName }).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (model is not null && !string.IsNullOrEmpty(model.EntryName)
                && fields.All(f => !string.Equals(f.Name, model.EntryName, StringComparison.Ordinal)))
            {
                fields.Add(new ArchiveKeyField(ArchiveKeySourceKind.EntryKey, model.Guid, model.EntryName, 1,
                    ArchiveKeyValueColumn.String, 0, "payload", true, null));
            }

            fields.AddRange((await dbContext.EntityAnalysisModelInlineFunction
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReturnDataTypeId, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelInlineFunction, x.Guid,
                    x.Name, x.ReturnDataTypeId ?? 1, ArchiveKeyPredicate.ColumnOf(x.ReturnDataTypeId), 3,
                    "payload", x.ReportTable == 1, null)));

            fields.AddRange((await dbContext.EntityAnalysisModelAbstractionRule
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelAbstractionRule, x.Guid,
                    x.Name, 3, ArchiveKeyValueColumn.Float, 5, "abstraction", x.ReportTable == 1, null)));

            fields.AddRange((await dbContext.EntityAnalysisModelTtlCounter
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelTtlCounter, x.Guid,
                    x.Name, 2, ArchiveKeyValueColumn.Long, 5, "ttlCounter", x.ReportTable == 1, null)));

            fields.AddRange((await dbContext.EntityAnalysisModelAbstractionCalculation
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelAbstractionCalculation,
                    x.Guid, x.Name, 3, ArchiveKeyValueColumn.Float, 6, "abstractionCalculation",
                    x.ReportTable == 1, null)));

            fields.AddRange((await dbContext.EntityAnalysisModelHttpAdaptation
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelHttpAdaptation, x.Guid,
                    x.Name, 3, ArchiveKeyValueColumn.Float, 9, "HttpAdaptation", x.ReportTable == 1, null)));

            fields.AddRange((await dbContext.EntityAnalysisModelActivationRule
                    .Where(x => x.EntityAnalysisModelId == entityAnalysisModelId && x.Active == 1
                                && (x.Deleted == null || x.Deleted == 0) && x.Name != null)
                    .OrderBy(x => x.Id).Select(x => new { x.Guid, x.Name, x.ReportTable })
                    .ToListAsync(token).ConfigureAwait(false))
                .Select(x => new ArchiveKeyField(ArchiveKeySourceKind.EntityAnalysisModelActivationRule, x.Guid,
                    x.Name, 5, ArchiveKeyValueColumn.Boolean, 11, "activation", x.ReportTable == 1, null)));

            return fields;
        }
    }
}
