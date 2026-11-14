/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Jube.AskJooby.Prompts;
using Jube.AskJooby.Tools.ScriptExecution;
using Jube.AskJooby.Tools.SqlQuery;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class ValidateFirstPromptTests
    {
        private static string DescriptionOf(Type type, string method)
        {
            return type.GetMethod(method)!.GetCustomAttribute<DescriptionAttribute>()!.Description;
        }

        [Fact]
        public void EveryAgentsInstructionsCarryTheValidateFirstDirection()
        {
            var instructions = InstructionsBuilder.Build("Agent specific text.", 0);

            instructions.Should().Contain(ValidateFirst.Instructions);
        }

        [Theory]
        [InlineData("VALIDATE FIRST, ALWAYS")]
        [InlineData("ValidateSqlQuery")]
        [InlineData("ValidateRScript")]
        [InlineData("FINDINGS ARE AUTHORITATIVE")]
        [InlineData("did you mean")]
        [InlineData("do not retry the same statement")]
        [InlineData("do not ask the caller about a fault")]
        [InlineData("NEVER EXPECT DATABASE TEXT")]
        [InlineData("only a SQLSTATE")]
        public void TheDirectionStatesEachRuleItExistsToTeach(string phrase)
        {
            ValidateFirst.Instructions.Should().ContainEquivalentOf(phrase);
        }

        [Fact]
        public void TheSqlValidateToolDescriptionSaysToCallItBeforeRunning()
        {
            var description = DescriptionOf(typeof(RunSqlQueryTool), nameof(RunSqlQueryTool.ValidateSqlQuery));

            description.Should().Contain("CALL THIS BEFORE RunSqlQuery").And.Contain("Findings are authoritative")
                .And.Contain("SQLSTATE");
        }

        [Fact]
        public void TheSqlRunToolDescriptionSaysToValidateFirst()
        {
            DescriptionOf(typeof(RunSqlQueryTool), nameof(RunSqlQueryTool.RunSqlQuery))
                .Should().Contain("VALIDATE FIRST").And.Contain("ValidateSqlQuery");
        }

        [Fact]
        public void TheRValidateAndRunToolDescriptionsCarryTheSameDirection()
        {
            DescriptionOf(typeof(RunScriptTool), nameof(RunScriptTool.ValidateRScript))
                .Should().Contain("CALL THIS BEFORE RunScript").And.Contain("authoritative");

            DescriptionOf(typeof(RunScriptTool), nameof(RunScriptTool.RunScript))
                .Should().Contain("ValidateRScript").And.Contain("refuses a script that fails");
        }

        [Fact]
        public void NoPromptOrToolDescriptionPromisesDatabaseErrorText()
        {
            var texts = new[]
            {
                ValidateFirst.Instructions,
                DescriptionOf(typeof(RunSqlQueryTool), nameof(RunSqlQueryTool.ValidateSqlQuery)),
                DescriptionOf(typeof(RunSqlQueryTool), nameof(RunSqlQueryTool.ExplainSqlQuery))
            };

            texts.Should().OnlyContain(t => !t.Contains("what the database objects to") &&
                                            !t.Contains("the database's error") &&
                                            !t.Contains("error message from the database"));
        }
    }
}
