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
using Jube.Data.Repository;
using Jube.Dictionary;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    public static class ConfigureTokenParserForSecurityExtensions
    {
        public static async Task<Context> ConfigureTokenParserForSecurityAsync(this Context context)
        {
            try
            {
                if (EvalExpressionRegistry.Enabled)
                {
                    await context.RegisterDictionaryEvalExpressionsAsync().ConfigureAwait(false);
                }

                var repository = new RuleScriptTokenRepository(context.Services.DbContext);
                var tokens = (await repository.GetAsync(context.Services.CancellationToken).ConfigureAwait(false))
                    .Select(s => s.Token).ToList();

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info(
                        $"Entity Start: Has fetched {tokens.Count} tokens.  Will construct and return the parser.");
                }

                context.Services.Parser = new Parser.Parser(context.Services.Log, tokens);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info("Entity Start: Starting soft code parser.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ConfigureTokenParserForSecurityAsync: has produced an error {ex}");
            }

            return context;
        }

        private static async Task RegisterDictionaryEvalExpressionsAsync(this Context context)
        {
            var repository = new DictionaryEvalExpressionRepository(context.Services.DbContext);
            var records = await repository.GetAsync(context.Services.CancellationToken).ConfigureAwait(false);

            foreach (var record in records)
            {
                try
                {
                    EvalExpressionRegistry.Register(record.Name, record.Expression,
                        record.ResultTypeId.GetValueOrDefault());

                    await repository.UpdateCompileStatusAsync(record.Id, true, null,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Services.Log.Error(
                        $"ConfigureTokenParserForSecurityAsync: DictionaryEvalExpression '{record.Name}' failed to compile: {ex}");

                    await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}