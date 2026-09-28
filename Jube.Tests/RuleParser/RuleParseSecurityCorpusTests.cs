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
using FluentAssertions;
using Jube.Parser;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class RuleParseSecurityCorpusTests
    {
        private static readonly string[] hostileSeeds =
        [
            "Return System.Diagnostics.Process.Start(\"id\") Is Nothing",
            "Return Microsoft.VisualBasic.Interaction.Shell(\"id\") = 0",
            "Return Shell(\"id\") = 0",
            "Return CallByName(Payload, \"ToString\", CallType.Method) = \"\"",
            "Return Activator.CreateInstance(GetType(String)) Is Nothing",
            "Return GetType(String).Assembly Is Nothing",
            "Return Type.GetType(\"System.IO.File\") Is Nothing",
            "Return System.IO.File.ReadAllText(\"/etc/passwd\") = \"\"",
            "Return System.IO.Directory.Exists(\"/\")",
            "Return System.Environment.MachineName = \"x\"",
            "Return Environment.GetEnvironmentVariable(\"PATH\") = \"\"",
            "System.Environment.Exit(0)\nReturn True",
            "System.Threading.Thread.Sleep(600000)\nReturn True",
            "Return New System.Net.WebClient().DownloadString(\"http://127.0.0.1:1/\") = \"\"",
            "Return System.Net.Http.HttpClient Is Nothing",
            "Return System.Reflection.Assembly.Load(\"x\") Is Nothing",
            "Return AppDomain.CurrentDomain Is Nothing",
            "Return System.Runtime.InteropServices.Marshal.GetLastWin32Error() = 0",
            "Return System.Diagnostics.Debugger.IsAttached",
            "Return New System.IO.StreamReader(\"/etc/passwd\").ReadToEnd() = \"\"",
            "Return System.IO.File.Exists(\"/etc/shadow\")",
            "Return System.Data.SqlClient.SqlConnection Is Nothing",
            "Imports System.IO\nReturn True",
            "<Assembly: System.Reflection.AssemblyVersion(\"1.0\")>\nReturn True",
            "Declare Function GetTickCount Lib \"kernel32\" () As Integer\nReturn True",
            "#Const X = 1\nReturn True",
            "#If True Then\nReturn True\n#End If",
            "Return CBool(1)",
            "Return CType(Payload, Object) Is Nothing",
            "Return DirectCast(Payload, Object) Is Nothing",
            "Return TypeOf Payload Is String",
            "Return NameOf(Payload) = \"\"",
            "Return Global.System.String.Empty = \"\"",
            "Return [System].[String].Empty = \"\"",
            "Return Chr(65) = \"A\"",
            "Return ChrW(65) = \"A\"",
            "Return Len(\"x\") = 1",
            "Return Eval(\"1\") = 1",
            "Return CObj(1) Is Nothing",
            "Return Environment.CommandLine = \"\"",
            "Return Me Is Nothing",
            "Return MyBase Is Nothing",
            "Return My.Computer.FileSystem.CurrentDirectory = \"\"",
            "Return My.Application.Info.DirectoryPath = \"\"",
            "Throw New System.Exception(\"x\")",
            "Try\nReturn True\nCatch\nReturn False\nEnd Try",
            "Stop\nReturn True",
            "End\nReturn True",
            "GoTo Done\nReturn True",
            "Sub X()\nEnd Sub\nReturn True",
            "Function X() As Boolean\nReturn True\nEnd Function\nReturn True",
            "Class X\nEnd Class\nReturn True",
            "Module X\nEnd Module\nReturn True",
            "Namespace X\nEnd Namespace\nReturn True",
            "Return New Object() Is Nothing",
            "Return AddressOf Payload Is Nothing",
            "Return Payload.GetType() Is Nothing",
            "Return Payload.ToString() = \"\"",
            "Return \"x\".GetType().Assembly Is Nothing",
            "Return DateTime.Now.Year = 1",
            "Return Math.Abs(1) = 1",
            "Return Convert.ToInt32(\"1\") = 1",
            "Return System.Text.RegularExpressions.Regex.IsMatch(\"a\", \"(a+)+$\")",
            "Return Microsoft.VisualBasic.FileIO.FileSystem.CurrentDirectory = \"\"",
            "Return Process.Start(\"id\") Is Nothing",
            "Return System.Type.GetType(\"System.Diagnostics.Process\") Is Nothing",
            "Return GetType(System.AppDomain).FullName = \"\"",
            "Dim p As New System.Diagnostics.ProcessStartInfo()\nReturn True"
        ];

        private static IEnumerable<(string Name, string Text)> Spellings(string rule)
        {
            yield return ("identity", rule);
            yield return ("upper", rule.ToUpperInvariant());
            yield return ("lower", rule.ToLowerInvariant());
            yield return ("tab-for-space", rule.Replace(" ", "\t"));
            yield return ("colon-separated", rule.Replace("\n", ":"));
            yield return ("crlf", rule.Replace("\n", "\r\n"));
            yield return ("comment-tail", rule.Replace("\n", " ' note\n") + " ' end");
            yield return ("rem-line", "REM harmless\n" + rule);
            yield return ("bom", "﻿" + rule);
            yield return ("nul", rule.Replace(" ", "\0"));
            yield return ("cyrillic-a", rule.Replace('a', 'а'));
            yield return ("zero-width", rule.Replace(".", ".​"));
        }

        public static IEnumerable<object[]> HostileCorpus()
        {
            foreach (var seed in hostileSeeds)
            {
                foreach (var (name, text) in Spellings(seed))
                {
                    yield return [name, text];
                }
            }
        }

        [Theory]
        [MemberData(nameof(HostileCorpus))]
        public void AHostileRuleNeverCompilesRegardlessOfSpelling(string spelling, string ruleText)
        {
            var result = RuleParse.Execute(ruleText, RuleParse.GatewayRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse($"'{spelling}' spelling of a hostile rule must not compile");
            result.ClassName.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(HostileCorpus))]
        public void AHostileRuleIsAlsoRefusedAsAnActivationRule(string spelling, string ruleText)
        {
            var result = RuleParse.Execute(ruleText, RuleParse.ActivationRule, RuleParseTests.Environment(),
                TestLog.NoOp, RuleParse.DefaultReferences());

            result.Compiled.Should().BeFalse($"'{spelling}' spelling of a hostile activation rule must not compile");
        }
    }
}