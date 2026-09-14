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
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.
    EntityAnalysisModelInlineScriptPropertyAttribute;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class InlineScriptsExtensionsTests
    {
        private static Context NewContext(params EntityAnalysisModelInlineScript[] scripts)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.AddRange(scripts);

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    ArchiveKeys = [],
                    InvokeTaskPerformance = new InvokeTaskPerformance(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static EntityAnalysisModelInlineScript NewPayloadScript(string code, Func<object> activator,
            Func<object, Context, Task<bool>> execute,
            params (string Name, Type PropertyType, Func<object, object> GetValue, bool ReportTable)[] properties)
        {
            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = code,
                ActivatorDelegate = activator,
                ExecuteAsyncDelegate = execute
            };
            script.EntityAnalysisModelInlineScriptEvents.Add(new EntityAnalysisModelInlineScriptEvent
            {
                EntityAnalysisModelInlineScriptEventType = EntityAnalysisModelInlineScriptEventTypeEnum.Payload
            });

            foreach (var property in properties)
            {
                script.EntityAnalysisModelInlineScriptPropertyAttributes[property.Name] =
                    new EntityAnalysisModelInlineScriptPropertyAttribute
                    {
                        Name = property.Name,
                        PropertyType = property.PropertyType,
                        GetValueDelegate = property.GetValue,
                        ReportTable = property.ReportTable
                    };
            }

            return script;
        }

        [Fact]
        public async Task AScriptWithNoPayloadEventIsNeverInvokedAsync()
        {
            var wasActivated = false;
            var script = new EntityAnalysisModelInlineScript
            {
                Id = 1,
                InlineScriptCode = "Ignored",
                ActivatorDelegate = () =>
                {
                    wasActivated = true;
                    return new object();
                },
                ExecuteAsyncDelegate = (_, _) => Task.FromResult(true)
            };
            script.EntityAnalysisModelInlineScriptEvents.Add(new EntityAnalysisModelInlineScriptEvent
            {
                EntityAnalysisModelInlineScriptEventType =
                    EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride
            });
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            wasActivated.Should().BeFalse();
        }

        [Fact]
        public async Task AStringPropertyIsAddedToThePayloadAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Greeting", typeof(string), _ => "Hello", false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Greeting"].AsString().Should().Be("Hello");
        }

        [Fact]
        public async Task AnIntPropertyIsAddedToThePayloadAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Count", typeof(int), _ => 7, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            ((int)context.EntityAnalysisModelInstanceEntryPayload.Payload["Count"]).Should().Be(7);
        }

        [Fact]
        public async Task ADoublePropertyIsAddedToThePayloadAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Ratio", typeof(double), _ => 2.5, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            ((double)context.EntityAnalysisModelInstanceEntryPayload.Payload["Ratio"]).Should().Be(2.5);
        }

        [Fact]
        public async Task ANullPropertyValueIsSkippedAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Missing", typeof(string), _ => null!, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Missing").Should().BeFalse();
        }

        [Fact]
        public async Task AnUnsupportedPropertyTypeIsSkippedAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Complex", typeof(Guid), _ => Guid.NewGuid(), false));
            var context = NewContext(script);

            var act = async () => await context.ExecuteInlineScriptsAsync();

            await act.Should().NotThrowAsync();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Complex").Should().BeFalse();
        }

        [Fact]
        public async Task ReportTableAddsAnArchiveKeyWithProcessingTypeOneAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Ratio", typeof(double), _ => 4.0, true));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Subject;
            archiveKey.ProcessingTypeId.Should().Be(1);
            archiveKey.Key.Should().Be("Ratio");
            archiveKey.KeyValueFloat.Should().Be(4.0);
        }

        [Fact]
        public async Task WithoutReportTableNoArchiveKeyIsAddedAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Ratio", typeof(double), _ => 4.0, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public async Task WhenExecuteAsyncDelegateReturnsFalsePropertiesAreNotProcessedAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(false),
                ("Ratio", typeof(double), _ => 4.0, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Ratio").Should().BeFalse();
        }

        [Fact]
        public async Task AnExceptionThrownByTheExecuteDelegateIsCaughtAndSubsequentScriptsStillRunAsync()
        {
            var secondActivated = false;
            var throwing = NewPayloadScript("Throws", () => new object(),
                (_, _) => throw new InvalidOperationException("boom"));
            var second = NewPayloadScript("Second", () =>
            {
                secondActivated = true;
                return new object();
            }, (_, _) => Task.FromResult(true));
            var context = NewContext(throwing, second);

            var act = async () => await context.ExecuteInlineScriptsAsync();

            await act.Should().NotThrowAsync();
            secondActivated.Should().BeTrue();
        }

        [Fact]
        public async Task WhenTheExecuteDelegateThrowsPropertiesAreNotPopulatedFromThePossiblyBrokenInstanceAsync()
        {
            var script = NewPayloadScript("Throws", () => new object(),
                (_, _) => throw new InvalidOperationException("boom"),
                ("Ratio", typeof(double), _ => 4.0, false));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Ratio").Should().BeFalse();
        }

        [Fact]
        public async Task WhenSampledTheStageTimingRecordsOneItemPerPayloadScriptAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true));
            var context = NewContext(script);

            await context.ExecuteInlineScriptsAsync();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.InlineScripts.Should().NotBeNull();
            stages.InlineScripts!.Items.Should().ContainKey("Script1");
        }

        [Fact]
        public async Task WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRunsAsync()
        {
            var script = NewPayloadScript("Script1", () => new object(), (_, _) => Task.FromResult(true),
                ("Greeting", typeof(string), _ => "Hello", false));
            var context = NewContext(script);
            context.LogSampled = false;

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Greeting"].AsString().Should().Be("Hello");
        }

        [Fact]
        public async Task TheResponsePayloadJObjectIsClearedAfterExecutionAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.JObject = new JObject();

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.JObject.Should().BeNull();
        }

        [Fact]
        public Task WithNoScriptsConfiguredNothingThrowsAsync()
        {
            var context = NewContext();

            var act = context.ExecuteInlineScriptsAsync;

            return act.Should().NotThrowAsync();
        }
    }
}