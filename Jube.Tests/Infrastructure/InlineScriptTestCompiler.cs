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
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Parser.Compiler;

namespace Jube.Test.Infrastructure
{
    using EntityAnalysisModelInlineScript =
        EntityAnalysisModelInlineScript;

    public static class InlineScriptTestCompiler
    {
        public static (EntityAnalysisModelInlineScript InlineScript, Compile Compile) Compile(int id, string code,
            byte languageId = 2)
        {
            var inlineScript = new EntityAnalysisModelInlineScript
            {
                Id = id,
                InlineScriptCode = code,
                LanguageId = languageId
            };

            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

            var compile = inlineScript.CompileAndConfigure(references, TestLog.NoOp);

            return (inlineScript, compile);
        }

        public static EntityAnalysisModelInlineScript CompileValid(int id, string code, byte languageId = 2)
        {
            var (inlineScript, compile) = Compile(id, code, languageId);

            if (compile.Errors != null)
            {
                throw new InvalidOperationException(
                    $"Inline script {id} failed to compile: {compile.ErrorsSummary}");
            }

            if (inlineScript.InlineScriptType == null)
            {
                throw new InvalidOperationException(
                    $"Inline script {id} compiled but no IInlineScript implementation was found.");
            }

            return inlineScript;
        }
    }
}