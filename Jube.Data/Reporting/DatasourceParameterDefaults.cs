/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Repository;

namespace Jube.Data.Reporting
{
    public static class DatasourceParameterDefaults
    {
        public static async Task<Dictionary<string, object>> LoadAsync(DbContext dbContext, int tenantRegistryId,
            int visualisationRegistryId, bool inCase, CancellationToken token)
        {
            var parameters = await new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId)
                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryId, token).ConfigureAwait(false);

            var defaults = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                object defaultValue = parameter.DataTypeId switch
                {
                    1 => "",
                    2 => 0,
                    3 => 0d,
                    4 => new DateTime(),
                    5 => (byte)0,
                    _ => ""
                };

                defaults.TryAdd(parameter.Name.Replace(" ", "_"), defaultValue);
            }

            if (inCase)
            {
                ContextParameters.Defaults(defaults);
            }

            return defaults;
        }
    }
}
