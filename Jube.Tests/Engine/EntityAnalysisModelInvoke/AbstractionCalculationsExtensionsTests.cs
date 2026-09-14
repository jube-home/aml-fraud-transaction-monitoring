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
    public sealed class AbstractionCalculationsExtensionsTests
    {
        private static Context NewContext(params EntityAnalysisModelAbstractionCalculation[] calculations)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.EntityAnalysisModelAbstractionCalculations.AddRange(calculations);

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Abstraction = new PooledDictionary<string, double>(),
                    AbstractionCalculation = new PooledDictionary<string, double>(),
                    ArchiveKeys = [],
                    InvokeTaskPerformance = new InvokeTaskPerformance(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };
        }

        private static EntityAnalysisModelAbstractionCalculation NewSwitchCalculation(string name, int typeId,
            string left, string right, bool reportTable = false)
        {
            return new EntityAnalysisModelAbstractionCalculation
            {
                Id = 1,
                Name = name,
                AbstractionCalculationTypeId = typeId,
                EntityAnalysisModelAbstractionNameLeft = left,
                EntityAnalysisModelAbstractionNameRight = right,
                ReportTable = reportTable
            };
        }

        [Theory]
        [InlineData(1, 6)]
        [InlineData(2, -2)]
        [InlineData(3, 0.5)]
        [InlineData(4, 8)]
        public void AdditionSubtractionDivisionAndMultiplicationProduceTheExpectedResult(int typeId,
            double expected)
        {
            var calculation = NewSwitchCalculation("Result", typeId, "Left", "Right");
            var context = NewContext(calculation);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 2;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 4;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(expected);
        }

        [Fact]
        public void AnUnrecognisedCalculationTypeProducesZero()
        {
            var calculation = NewSwitchCalculation("Result", 99, "Left", "Right");
            var context = NewContext(calculation);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 2;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 4;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(0);
        }

        [Fact]
        public void MissingLeftOrRightAbstractionValuesDefaultToZeroRatherThanThrowing()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Missing1", "Missing2");
            var context = NewContext(calculation);

            var act = () => context.ExecuteAbstractionCalculations();

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(0);
        }

        [Fact]
        public void DivisionByZeroProducesZeroRatherThanInfinity()
        {
            var calculation = NewSwitchCalculation("Result", 3, "Left", "Right");
            var context = NewContext(calculation);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 5;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 0;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(0);
        }

        [Fact]
        public void AbstractionNamesWithSpacesAreLookedUpWithUnderscoresSubstituted()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Left Value", "Right Value");
            var context = NewContext(calculation);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left_Value"] = 3;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right_Value"] = 4;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(7);
        }

        [Fact]
        public void ReportTableAddsAnArchiveKeyWithProcessingTypeSix()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Left", "Right", true);
            var context = NewContext(calculation);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 1;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 1;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle();
            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys[0];
            archiveKey.ProcessingTypeId.Should().Be(6);
            archiveKey.Key.Should().Be("Result");
            archiveKey.KeyValueFloat.Should().Be(2);
        }

        [Fact]
        public void WithoutReportTableNoArchiveKeyIsAdded()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Left", "Right");
            var context = NewContext(calculation);

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public void ATypeFiveCalculationDefersToTheCompiledRuleDelegate()
        {
            var calculation = new EntityAnalysisModelAbstractionCalculation
            {
                Id = 1,
                Name = "FromRule",
                AbstractionCalculationTypeId = 5,
                FunctionCalculationCompileDelegate = (_, _, _, _, _, _) => 77
            };
            var context = NewContext(calculation);

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["FromRule"].Should().Be(77);
        }

        [Fact]
        public void AnExceptionThrownByTheRuleDelegateIsCaughtAndTheCalculationIsSkipped()
        {
            var throwing = new EntityAnalysisModelAbstractionCalculation
            {
                Id = 1,
                Name = "Throws",
                AbstractionCalculationTypeId = 5,
                FunctionCalculationCompileDelegate = (_, _, _, _, _, _) =>
                    throw new InvalidOperationException("boom")
            };
            var second = NewSwitchCalculation("Second", 1, "Left", "Right");
            var context = NewContext(throwing, second);
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 1;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 1;

            var act = context.ExecuteAbstractionCalculations;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.ContainsKey("Throws")
                .Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Second"].Should().Be(2);
        }

        [Fact]
        public void WhenSampledTheStageTimingRecordsOneItemPerCalculation()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Left", "Right");
            var context = NewContext(calculation);

            context.ExecuteAbstractionCalculations();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.AbstractionCalculations.Should().NotBeNull();
            stages.AbstractionCalculations!.Items.Should().ContainKey("Result");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns()
        {
            var calculation = NewSwitchCalculation("Result", 1, "Left", "Right");
            var context = NewContext(calculation);
            context.LogSampled = false;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Left"] = 5;
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction["Right"] = 5;

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Result"].Should().Be(10);
        }

        [Fact]
        public void WithNoCalculationsConfiguredNothingThrowsAndTheDictionaryStaysEmpty()
        {
            var context = NewContext();

            var act = context.ExecuteAbstractionCalculations;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.Should().BeEmpty();
        }
    }
}