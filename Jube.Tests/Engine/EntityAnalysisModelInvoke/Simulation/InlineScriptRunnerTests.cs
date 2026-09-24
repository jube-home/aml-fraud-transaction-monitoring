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
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class InlineScriptRunnerTests
    {
        private const string Header = """
                                      using System;
                                      using System.Threading.Tasks;
                                      using Jube.Engine.EntityAnalysisModelInvoke.Context;
                                      using Jube.Engine.Interfaces;

                                      """;

        private static readonly InvocationContextField[] fields = [new("Payload.Amount", "Payload", "double")];

        private static RuleRunInputs Inputs(double amount)
        {
            var context = InvocationContextBuilder.Blank(1, fields, [], false, DateTime.UtcNow);
            InvocationContextBuilder.Overlay(context,
                new Dictionary<string, string> { ["Payload.Amount"] = amount.ToString("R") });
            return RuleRunner.ToInputs(context, new Dictionary<string, List<string>>());
        }

        private static Task<InlineScriptRunResult> RunAsync(string body, double amount = 10)
        {
            return InlineScriptRunner.RunAsync(Header + body, 2, null,
                Path.GetDirectoryName(typeof(InlineScriptRunner).Assembly.Location)!,
                Path.GetDirectoryName(typeof(object).Assembly.Location)!, Inputs(amount), DateTime.UtcNow,
                TimeSpan.FromSeconds(5), TestLog.NoOp);
        }

        [Fact]
        public async Task TheScriptReadsThePayloadAndItsPropertiesAreReturnedAsync()
        {
            var result = await RunAsync("""
                                        public class Doubler : IInlineScript
                                        {
                                            public double Doubled { get; set; }
                                            public string Band { get; set; }
                                            public async Task<bool> ExecuteAsync(Context context)
                                            {
                                                var amount = context.EntityAnalysisModelInstanceEntryPayload.Payload["Amount"].AsDouble();
                                                Doubled = amount * 2;
                                                Band = amount > 5 ? "High" : "Low";
                                                await Task.CompletedTask;
                                                return true;
                                            }
                                        }
                                        """);

            result.Compiled.Should().BeTrue(result.CompileErrors);
            result.Succeeded.Should().BeTrue();
            result.Properties.Should().Contain(new Dictionary<string, object> { ["Doubled"] = 20d, ["Band"] = "High" });
        }

        [Fact]
        public async Task AScriptThatDoesNotCompileReportsItsErrorsAsync()
        {
            var result = await RunAsync("""
                                        public class Broken : IInlineScript
                                        {
                                            public async Task<bool> ExecuteAsync(Context context) { return ThisDoesNotExist(); }
                                        }
                                        """);

            result.Compiled.Should().BeFalse();
            result.CompileErrors.Should().Contain("ThisDoesNotExist");
        }

        [Fact]
        public async Task AScriptThatThrowsIsReportedAsync()
        {
            var result = await RunAsync("""
                                        public class Throws : IInlineScript
                                        {
                                            public async Task<bool> ExecuteAsync(Context context)
                                            {
                                                await Task.CompletedTask;
                                                throw new InvalidOperationException("no");
                                            }
                                        }
                                        """);

            result.Compiled.Should().BeTrue();
            result.Error.Should().BeOfType<InvalidOperationException>();
            result.Properties.Should().BeEmpty();
        }

        [Fact]
        public async Task AScriptThatReturnsFalseContributesNothingAsync()
        {
            var result = await RunAsync("""
                                        public class Declines : IInlineScript
                                        {
                                            public string Value { get; set; } = "set";
                                            public async Task<bool> ExecuteAsync(Context context)
                                            {
                                                await Task.CompletedTask;
                                                return false;
                                            }
                                        }
                                        """);

            result.Succeeded.Should().BeFalse();
            result.Properties.Should().BeEmpty();
        }
    }
}