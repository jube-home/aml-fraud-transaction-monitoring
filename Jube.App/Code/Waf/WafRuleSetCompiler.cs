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
using System.Text.RegularExpressions;
using Jube.App.Code.Waf.Models;
using Jube.Data.Poco;
using log4net;

namespace Jube.App.Code.Waf
{
    public static class WafRuleSetCompiler
    {
        private const int DefaultMatchTimeoutMilliseconds = 50;

        public static WafRuleSet Compile(IEnumerable<WafSignature> signatures, IEnumerable<WafException> exceptions,
            int maxMatchTimeoutMilliseconds, ILog log = null)
        {
            var compiledSignatures = new List<WafCompiledSignature>();
            foreach (var signature in signatures ?? [])
            {
                var compiled = CompileSignature(signature, maxMatchTimeoutMilliseconds, log);
                if (compiled != null)
                {
                    compiledSignatures.Add(compiled);
                }
            }

            var compiledExceptions = new List<WafCompiledException>();
            foreach (var exception in exceptions ?? [])
            {
                var compiled = CompileException(exception, maxMatchTimeoutMilliseconds, log);
                if (compiled != null)
                {
                    compiledExceptions.Add(compiled);
                }
            }

            return new WafRuleSet(compiledSignatures, compiledExceptions);
        }

        private static WafCompiledSignature CompileSignature(WafSignature signature, int maxMatchTimeoutMilliseconds,
            ILog log)
        {
            if (signature == null || string.IsNullOrEmpty(signature.Pattern))
            {
                return null;
            }

            var regex = TryCompile(signature.Pattern, signature.MatchTimeoutMilliseconds, maxMatchTimeoutMilliseconds,
                $"signature '{signature.Name}'", log);
            if (regex == null)
            {
                return null;
            }

            return new WafCompiledSignature
            {
                Id = signature.Id,
                Name = signature.Name,
                Category = signature.Category,
                Scope = ToScope(signature.TargetScope),
                Drop = signature.Drop == 1,
                Pattern = regex
            };
        }

        private static WafCompiledException CompileException(WafException exception, int maxMatchTimeoutMilliseconds,
            ILog log)
        {
            if (exception == null || string.IsNullOrEmpty(exception.RouteRegex))
            {
                return null;
            }

            var routeRegex = TryCompile(exception.RouteRegex, null, maxMatchTimeoutMilliseconds,
                $"exception '{exception.Name}' route", log);
            if (routeRegex == null)
            {
                return null;
            }

            Regex fieldRegex = null;
            if (!string.IsNullOrEmpty(exception.FieldRegex))
            {
                fieldRegex = TryCompile(exception.FieldRegex, null, maxMatchTimeoutMilliseconds,
                    $"exception '{exception.Name}' field", log);
                if (fieldRegex == null)
                {
                    return null;
                }
            }

            return new WafCompiledException
            {
                Id = exception.Id,
                Name = exception.Name,
                RouteRegex = routeRegex,
                FieldRegex = fieldRegex,
                WafSignatureId = exception.WafSignatureId
            };
        }

        private static Regex TryCompile(string pattern, int? requestedTimeoutMilliseconds,
            int maxMatchTimeoutMilliseconds, string description, ILog log)
        {
            var timeoutMilliseconds = requestedTimeoutMilliseconds ?? DefaultMatchTimeoutMilliseconds;
            if (timeoutMilliseconds <= 0 || timeoutMilliseconds > maxMatchTimeoutMilliseconds)
            {
                timeoutMilliseconds = maxMatchTimeoutMilliseconds;
            }

            try
            {
                return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    TimeSpan.FromMilliseconds(timeoutMilliseconds));
            }
            catch (ArgumentException ex)
            {
                log?.Error($"Waf: Ignoring {description} because its pattern did not compile: {ex.Message}");
                return null;
            }
        }

        private static WafTargetScope ToScope(int? targetScope)
        {
            if (!targetScope.HasValue || targetScope.Value <= 0)
            {
                return WafTargetScope.All;
            }

            return (WafTargetScope)(targetScope.Value & (int)WafTargetScope.All);
        }
    }
}