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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;

namespace Jube.Test.Service.Query.CaseChecklist
{
    public sealed class CountingSqlRunner(IFormSqlRunner inner, Func<Task>? whileEvaluating = null) : IFormSqlRunner
    {
        private int calls;

        public int Calls => calls;

        public async Task<bool> PassesAsync(string sql, IReadOnlyDictionary<string, object> parameters,
            CancellationToken token)
        {
            Interlocked.Increment(ref calls);

            var result = await inner.PassesAsync(sql, parameters, token).ConfigureAwait(false);

            if (whileEvaluating != null)
            {
                await whileEvaluating().ConfigureAwait(false);
            }

            return result;
        }
    }
}
