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

using System;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Stages.Models;

namespace Jube.Engine.Exhaustive.Stages
{
    internal static class CategoricalFieldStore
    {
        internal static async Task PersistAsync(InstanceRun run)
        {
            var encoders = run.Variables.Values
                .Where(w => w.CategoricalEncoder != null)
                .Select(s => s.CategoricalEncoder)
                .Distinct()
                .ToList();

            if (encoders.Count == 0)
            {
                return;
            }

            var repository = new ExhaustiveSearchInstanceCategoricalFieldRepository(run.DbContext);

            foreach (var encoder in encoders)
            {
                var id = await repository.InsertAsync(new ExhaustiveSearchInstanceCategoricalField
                {
                    ExhaustiveSearchInstanceId = run.Instance.Id,
                    SourceName = encoder.SourceName,
                    ProcessingTypeId = 1,
                    VocabularyJson = encoder.ToJson(),
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0
                }, run.Token).ConfigureAwait(false);

                foreach (var variable in
                         run.Variables.Values.Where(w => ReferenceEquals(w.CategoricalEncoder, encoder)))
                {
                    variable.CategoricalFieldId = id;
                }
            }
        }
    }
}