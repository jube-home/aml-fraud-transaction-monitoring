﻿/* Copyright (C) 2022-present Jube Holdings Limited.
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
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Jube.Engine.Model.Compiler
{
    public class Compile
    {
        public Assembly CompiledAssembly { get; private set; }
        public int Errors { get; private set; }

        public void CompileCode(string code, ILog log, string[] refs)
        {
            try
            {
                var assemblyGuid = Guid.NewGuid().ToString();
                
                log.Info("Roslyn Compilation in VB.net: Is about to compile the code " + code +
                         " with the assembly GUID of " + assemblyGuid + ".");

                var compilation = VisualBasicCompilationConfig(code, log, refs, assemblyGuid);

                log.Info(
                    "Roslyn Compilation in VB.net: Has configured the compiler and will now proceed to compile the code.");

                var peStream = new MemoryStream();
                var result = compilation.Emit(peStream);

                log.Info(
                    "Roslyn Compilation in VB.net: Code compilation process has concluded.  Will now inspect any errors.");

                if (!result.Success)
                {
                    log.Error(code);
                    Errors = HandleCompileErrors(log, result);
                }
                else
                {
                    HandleCompile(log, peStream);
                }
            }
            catch (Exception ex)
            {
                Errors = 1;
                
                log.Error("Roslyn Compilation in VB.net: has experienced fatal errors as " + ex + ".");
            }
        }

        private void HandleCompile(ILog log, Stream peStream)
        {
            peStream.Position = 0;
            log.Info("Roslyn Compilation in VB.net: Is about to load the assembly from a stream of " +
                     peStream.Length + ".");

            var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
            CompiledAssembly = assemblyLoadContext.LoadFromStream(peStream);

            log.Info(
                "Roslyn Compilation in VB.net: Loaded compiled assembly.  Will now proceed to unload the assembly context.");

            assemblyLoadContext.Unload();
            
            log.Info("Roslyn Compilation in VB.net: Unloaded assembly context.");
        }

        private static int HandleCompileErrors(ILog log, EmitResult result)
        {
            log.Info("Roslyn Compilation in VB.net: It was not successful.");
            var failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
            IEnumerable<Diagnostic> diagnostics = failures as Diagnostic[] ?? failures.ToArray();
            foreach (var diagnostic in diagnostics)
                log.Error("Roslyn Compilation in VB.net: Error " + diagnostic.Id + " " +
                          diagnostic.GetMessage() + ".");
            
            log.Info("Roslyn Compilation in VB.net: There were " + diagnostics.Count() + " in total.");
            
            return diagnostics.Count();
        }

        private static VisualBasicCompilation VisualBasicCompilationConfig(string code, ILog log, IReadOnlyList<string> refs,
            string assemblyGuid)
        {
            var parseOptions = VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
            var compileOptions = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release, embedVbCoreRuntime: true);

            var references = MetadataReferences(log, refs);

            var compilation = VisualBasicCompilation.Create(assemblyGuid).WithOptions(compileOptions)
                .AddSyntaxTrees(VisualBasicSyntaxTree.ParseText(code, parseOptions)).WithReferences(references);
            return compilation;
        }

        private static MetadataReference[] MetadataReferences(ILog log, IReadOnlyList<string> refs)
        {
            var references = new MetadataReference[refs.Count + 2];
            int i;
            for (i = 0; i < refs.Count; i++)
            {
                references[i] = MetadataReference.CreateFromFile(refs[i]);
                
                log.Info("Roslyn Compilation in VB.net: Included custom reference " + refs[i] +
                         ".  Will now add the mandated reference.");
            }

            var directoryForDll = Path.GetDirectoryName(typeof(object).Assembly.Location);
            references[i] = MetadataReference.CreateFromFile(typeof(object).Assembly.Location); //Dummy
            references[i + 1] = MetadataReference.CreateFromFile(Path.Join(directoryForDll, "System.Runtime.dll")); //Dummy
            
            log.Info("Roslyn Compilation in VB.net: Included mandated reference " + references[i] +
                     ".  Will now configure the compiler.");
            
            return references;
        }
    }
}