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

namespace Jube.Migrations.Branches.WafInclusion
{
    using System;
    using FluentMigrator;

    [Migration(20260924100100)]
    public class AddTableWafSignature : Migration
    {
        public override void Up()
        {
            Create.Table("WafSignature")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Category").AsString().Nullable()
                .WithColumn("Description").AsCustom("text").Nullable()
                .WithColumn("Pattern").AsCustom("text").Nullable()
                .WithColumn("TargetScope").AsInt32().Nullable()
                .WithColumn("MatchTimeoutMilliseconds").AsInt32().Nullable()
                .WithColumn("Drop").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Seed("OWASP-SQLI-UNION", "A1-Injection", "SQL injection: UNION SELECT",
                @"(?i)union[\s/*]+select");
            Seed("OWASP-SQLI-TAUTOLOGY", "A1-Injection", "SQL injection: boolean tautology",
                @"(?i)(\bor\b|\band\b)[\s(]+[\w""']+\s*(=|<>|like)\s*[\w""']+");
            Seed("OWASP-SQLI-STACKED", "A1-Injection", "SQL injection: stacked destructive statement",
                @"(?i);\s*(drop|truncate|delete|update|insert|alter|create|grant)\s");
            Seed("OWASP-SQLI-TIMING", "A1-Injection", "SQL injection: timing function",
                @"(?i)\b(sleep|pg_sleep|benchmark|waitfor\s+delay)\s*\(");
            Seed("OWASP-XSS-SCRIPT", "A7-XSS", "Cross-site scripting: script tag",
                @"(?i)<\s*script\b");
            Seed("OWASP-XSS-JS-URI", "A7-XSS", "Cross-site scripting: javascript URI",
                @"(?i)javascript:");
            Seed("OWASP-XSS-EVENT", "A7-XSS", "Cross-site scripting: inline event handler",
                @"(?i)\bon(error|load|click|mouseover|focus)\s*=");
            Seed("OWASP-TRAVERSAL-DOTDOT", "A5-BrokenAccessControl", "Path traversal: dot-dot sequence",
                @"(\.\./|\.\.\\|%2e%2e[/\\%])");
            Seed("OWASP-CMDI-SHELL", "A1-Injection", "OS command injection: shell metacharacter and command",
                @"(?i)(\|\||&&|;|`|\$\()\s*(cat|ls|id|whoami|curl|wget|nc|bash|sh|powershell)\b");
            Seed("OWASP-SSTI-BRACES", "A3-Injection", "Server-side template injection: template delimiters",
                @"(\{\{|\}\}|\$\{|<%|%>)");
            Seed("OWASP-XXE-DOCTYPE", "A5-SecurityMisconfiguration", "XML external entity: DOCTYPE or ENTITY",
                @"(?i)<!(doctype|entity)\b");
            Seed("OWASP-NULLBYTE", "A5-SecurityMisconfiguration", "Null byte injection",
                @"(%00|\x00)");
        }

        private void Seed(string name, string category, string description, string pattern)
        {
            Insert.IntoTable("WafSignature").Row(new
            {
                Name = name,
                Category = category,
                Description = description,
                Pattern = pattern,
                TargetScope = 7,
                MatchTimeoutMilliseconds = 50,
                Drop = 1,
                Active = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = "Administrator"
            });
        }

        public override void Down()
        {
        }
    }
}