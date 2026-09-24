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
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Engine.EntityAnalysisModelManager.Helpers;
using Jube.Engine.Models;

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

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug(
                    "Entity Start: Executing EntityAnalysisInlineScriptRepository.Get.");
            }

            var records = await repository.GetAsync();

            foreach (var record in records)
            {
                try
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script with the id of {record.Id} and will proceed to check if already have this inline script available.");
                    }

                    var inlineScript =
                        context.EntityAnalysisModels.EntityAnalysisModelInlineScripts.Find(x => x.Id == record.Id);
                    if (inlineScript == null)
                    {
                        inlineScript = new EntityAnalysisModelInlineScript();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Have not found an inline script in the available inline scripts, with the id of {record.Id} hence a new one will be created.");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Found an inline script in the available inline scripts, with the id of {record.Id} and this will be used.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script {record.Id} has Created Date: {inlineScript.CreatedDate.HasValue} of {inlineScript.CreatedDate}. A check will be made to see if it has changed recently");
                    }

                    if ((!inlineScript.CreatedDate.HasValue ||
                         !(DateTime.SpecifyKind(Convert.ToDateTime(record.CreatedDate), DateTimeKind.Utc) >
                           inlineScript.CreatedDate)) &&
                        inlineScript.CreatedDate.HasValue)
                    {
                        continue;
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has changed recently or is new,  setting created date.");
                    }

                    inlineScript.CreatedDate = record.CreatedDate;
                    inlineScript.Id = record.Id;
                    inlineScript.InlineScriptCode = record.Code;
                    inlineScript.LanguageId = record.LanguageId ?? 1;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.ClassName}.");
                    }

                    inlineScript.Name = record.Name;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has name of {inlineScript.Name}.");
                    }

                    var dependencyArray = BuildDependencyArray(context, inlineScript.Dependencies);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} is being checked for dll dependencies.  Has created {dependencyArray.Length} dependencies.");
                    }

                    var inlineScriptHash = HashHelper.GetHash(inlineScript.InlineScriptCode);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and the hash cache will now be checked.");
                    }

                    if (context.Caching.HashCacheAssembly.TryGetValue(inlineScriptHash, out var value))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache,  this will be used.  Creating a delegate.");
                        }

                        inlineScript.InlineScriptCompile = value;
                        InlineScriptCompilationExtensions.SetupInlineScriptDelegates(inlineScript);
                        InlineScriptCompilationExtensions.SetupEvents(inlineScript);

                        await repository.UpdateCompileStatusAsync(record.Id, true, null,
                            context.Services.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache and allocated to a delegate.");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} but has not been located in the hash cache.  The inline script will now be compiled.");
                        }

                        var compile = inlineScript.CompileAndConfigure(dependencyArray, context.Services.Log);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors != null)
                        {
                            foreach (var error in compile.Errors)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} with error: {error.ToString()}.");
                            }

                            await repository.UpdateCompileStatusAsync(record.Id, false, compile.ErrorsSummary,
                                context.Services.CancellationToken).ConfigureAwait(false);

                            continue;
                        }

                        if (inlineScript.InlineScriptType == null)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Could not compile inline script: {inlineScript.Id} did not fine class entry point for {inlineScript.ClassName}.");

                            continue;
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled and allocated to a delegate.");
                        }

                        context.EntityAnalysisModels.EntityAnalysisModelInlineScripts.Add(inlineScript);
                        context.Caching.HashCacheAssembly.TryAdd(inlineScriptHash, compile.CompiledAssembly);
                        context.Caching.HashCacheAssemblyMetadata.TryAdd(inlineScriptHash,
                            new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary,
                                inlineScript.InlineScriptCode));

                        await repository.UpdateCompileStatusAsync(record.Id, true, null,
                            context.Services.CancellationToken).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");

                    await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
            }

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start:  Completed creating Inline Scripts and closed the reader.");
            }

            return context;
        }

        private static string[] BuildDependencyArray(Context context, string dependencies)
        {
            return InlineScriptCompilationExtensions.BuildDependencyArray(context.Paths.BinaryPath,
                context.Paths.FrameworkPath, dependencies);
        }
    }
}