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
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class InlineFunctionsExtensionsTests
    {
        private static Context NewContext(params EntityAnalysisModelInlineFunction[] functions)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.EntityAnalysisModelInlineFunctions.AddRange(functions);

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Dictionary = new PooledDictionary<string, double>(),
                    ArchiveKeys = [],
                    InvokeTaskPerformance = new InvokeTaskPerformance(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static EntityAnalysisModelInlineFunction NewFunction(string name, int returnDataTypeId,
            EntityAnalysisModelInlineFunction.Match match, bool reportTable = false)
        {
            return new EntityAnalysisModelInlineFunction
            {
                Id = 1,
                Name = name,
                ReturnDataTypeId = returnDataTypeId,
                ReportTable = reportTable,
                FunctionCalculationCompileDelegate = match
            };
        }

        [Fact]
        public void AStringResultIsAddedToThePayload()
        {
            var function = NewFunction("Greeting", 1, (_, _, _, _) => "Hello");
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Greeting"].AsString().Should().Be("Hello");
        }

        [Fact]
        public void AnIntegerResultIsAddedToThePayload()
        {
            var function = NewFunction("Count", 2, (_, _, _, _) => 5);
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            ((int)context.EntityAnalysisModelInstanceEntryPayload.Payload["Count"]).Should().Be(5);
        }

        [Fact]
        public void ADoubleResultIsAddedToThePayload()
        {
            var function = NewFunction("Ratio", 3, (_, _, _, _) => 3.5);
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            ((double)context.EntityAnalysisModelInstanceEntryPayload.Payload["Ratio"]).Should().Be(3.5);
        }

        [Fact]
        public void ABooleanResultIsAddedToThePayload()
        {
            var function = NewFunction("Flag", 5, (_, _, _, _) => true);
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            ((bool)context.EntityAnalysisModelInstanceEntryPayload.Payload["Flag"]).Should().BeTrue();
        }

        [Fact]
        public void ANullStringResultIsAddedToThePayloadAsNullRatherThanThrowing()
        {
            var function = NewFunction("Missing", 1, (_, _, _, _) => null);
            var context = NewContext(function);

            var act = () => context.ExecuteInlineFunctions();

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Missing").Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Missing"].AsString().Should().BeEmpty();
        }

        [Fact]
        public void DoesNotOverwriteAnExistingPayloadValueWithTheSameName()
        {
            var function = NewFunction("Existing", 1, (_, _, _, _) => "NewValue");
            var context = NewContext(function);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Existing", "OriginalValue");

            context.ExecuteInlineFunctions();

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Existing"].AsString().Should()
                .Be("OriginalValue");
        }

        [Fact]
        public void ReportTableAddsAStringArchiveKeyWithProcessingTypeThree()
        {
            var function = NewFunction("Greeting", 1, (_, _, _, _) => "Hello",
                true);
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle();
            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys[0];
            archiveKey.ProcessingTypeId.Should().Be(3);
            archiveKey.Key.Should().Be("Greeting");
            archiveKey.KeyValueString.Should().Be("Hello");
        }

        [Fact]
        public void ReportTableAddsADoubleArchiveKeyWithTheNumericValue()
        {
            var function = NewFunction("Ratio", 3, (_, _, _, _) => 3.5, true);
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Subject;
            archiveKey.KeyValueFloat.Should().Be(3.5);
        }

        [Fact]
        public void WithoutReportTableNoArchiveKeyIsAdded()
        {
            var function = NewFunction("Greeting", 1, (_, _, _, _) => "Hello");
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public void AnExceptionThrownByTheRuleDelegateIsCaughtAndSubsequentFunctionsStillRun()
        {
            var secondEvaluated = false;
            var throwing = NewFunction("Throws", 1,
                (_, _, _, _) => throw new InvalidOperationException("boom"));
            var second = NewFunction("Second", 1, (_, _, _, _) =>
            {
                secondEvaluated = true;
                return "Value";
            });
            var context = NewContext(throwing, second);

            var act = context.ExecuteInlineFunctions;

            act.Should().NotThrow();
            secondEvaluated.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Throws").Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Second"].AsString().Should().Be("Value");
        }

        [Fact]
        public void WhenSampledTheStageTimingRecordsOneItemPerFunction()
        {
            var function = NewFunction("Greeting", 1, (_, _, _, _) => "Hello");
            var context = NewContext(function);

            context.ExecuteInlineFunctions();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.InlineFunctions.Should().NotBeNull();
            stages.InlineFunctions!.Items.Should().ContainKey("Greeting");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns()
        {
            var function = NewFunction("Greeting", 1, (_, _, _, _) => "Hello");
            var context = NewContext(function);
            context.LogSampled = false;

            context.ExecuteInlineFunctions();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Greeting"].AsString().Should().Be("Hello");
        }

        [Fact]
        public void WithNoFunctionsConfiguredNothingThrowsAndThePayloadStaysUntouched()
        {
            var context = NewContext();

            var act = context.ExecuteInlineFunctions;

            act.Should().NotThrow();
        }
    }
}