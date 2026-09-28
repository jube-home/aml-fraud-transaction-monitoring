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

using System.Collections.Generic;
using Jube.App.Code.Waf;
using Jube.Data.Poco;

namespace Jube.Test.Waf
{
    internal static class WafTestRuleSets
    {
        internal static readonly (string Name, string Category, string Pattern)[] SeededSignatures =
        [
            ("OWASP-SQLI-UNION", "A1-Injection", @"(?i)union[\s/*]+select"),
            ("OWASP-SQLI-TAUTOLOGY", "A1-Injection", @"(?i)(\bor\b|\band\b)[\s(]+[\w""']+\s*(=|<>|like)\s*[\w""']+"),
            ("OWASP-SQLI-STACKED", "A1-Injection",
                @"(?i);\s*(drop|truncate|delete|update|insert|alter|create|grant)\s"),
            ("OWASP-SQLI-TIMING", "A1-Injection", @"(?i)\b(sleep|pg_sleep|benchmark|waitfor\s+delay)\s*\("),
            ("OWASP-XSS-SCRIPT", "A7-XSS", @"(?i)<\s*script\b"),
            ("OWASP-XSS-JS-URI", "A7-XSS", @"(?i)javascript:"),
            ("OWASP-XSS-EVENT", "A7-XSS", @"(?i)\bon(error|load|click|mouseover|focus)\s*="),
            ("OWASP-TRAVERSAL-DOTDOT", "A5-BrokenAccessControl", @"(\.\./|\.\.\\|%2e%2e[/\\%])"),
            ("OWASP-CMDI-SHELL", "A1-Injection",
                @"(?i)(\|\||&&|;|`|\$\()\s*(cat|ls|id|whoami|curl|wget|nc|bash|sh|powershell)\b"),
            ("OWASP-SSTI-BRACES", "A3-Injection", @"(\{\{|\}\}|\$\{|<%|%>)"),
            ("OWASP-XXE-DOCTYPE", "A5-SecurityMisconfiguration", @"(?i)<!(doctype|entity)\b"),
            ("OWASP-NULLBYTE", "A5-SecurityMisconfiguration", @"(%00|\x00)")
        ];

        internal static WafRegistry SeededRegistry()
        {
            var signatures = new List<WafSignature>();
            var id = 1;
            foreach (var (name, category, pattern) in SeededSignatures)
            {
                signatures.Add(new WafSignature
                {
                    Id = id++,
                    Name = name,
                    Category = category,
                    Pattern = pattern,
                    TargetScope = 7,
                    MatchTimeoutMilliseconds = 50,
                    Drop = 1,
                    Active = 1
                });
            }

            return RegistryFor(signatures, []);
        }

        internal static WafRegistry RegistryFor(List<WafSignature> signatures, List<WafException> exceptions)
        {
            var registry = new WafRegistry();
            registry.Update(WafRuleSetCompiler.Compile(signatures, exceptions, 100));
            return registry;
        }
    }
}