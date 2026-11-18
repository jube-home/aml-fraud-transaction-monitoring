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
using System.Threading.Tasks;
using Jube.Data.Repository;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class SyncEntityAnalysisInlineScriptsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisInlineScriptAsync(this Context context)
        {
            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start: Getting all Inline Scripts from Database.");
            }

            var repository = new EntityAnalysisInlineScriptRepository(context.Services.DbContext);
            var records = await repository.GetAsync();

            foreach (var record in records)
            {
                try
                {
                    var result = InlineScriptCompilationExtensions.Load(
                        context.EntityAnalysisModels.EntityAnalysisModelInlineScripts,
                        context.Caching.HashCacheAssembly, context.Caching.HashCacheAssemblyMetadata, record,
                        context.Paths.BinaryPath, context.Paths.FrameworkPath, context.Services.Log);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} changed {result.Changed} compiled {result.Compiled}.");
                    }

                    if (!result.Changed)
                    {
                        continue;
                    }

                    await repository.UpdateCompileStatusAsync(record.Id, result.Compiled, result.CompileError,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");

                    await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
            }

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start: Completed creating Inline Scripts.");
            }

            return context;
        }
    }
}
