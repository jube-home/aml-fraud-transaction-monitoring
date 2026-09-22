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
                    TtlCounter = new PooledDictionary<string, double>(),
                    Sanction = new PooledDictionary<string, double>(),
                    Dictionary = new PooledDictionary<string, double>(),
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

        private static EntityAnalysisModelAbstractionCalculation NewCalculation(string name,
            EntityAnalysisModelAbstractionCalculation.Match match, bool reportTable = false, int id = 1)
        {
            return new EntityAnalysisModelAbstractionCalculation
            {
                Id = id,
                Name = name,
                ReportTable = reportTable,
                FunctionCalculationCompileDelegate = match
            };
        }

        [Fact]
        public void TheRuleResultIsStoredUnderTheCalculationName()
        {
            var context = NewContext(NewCalculation("FromRule", (_, _, _, _, _, _, _, _) => 77));

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["FromRule"].Should().Be(77);
        }

        [Fact]
        public void TheRuleDelegateReceivesEveryUpstreamSource()
        {
            var received = new Dictionary<string, object?>();
            var calculation = NewCalculation("FromRule", (data, ttl, abstraction, _, calculations, sanctions, kvp, _) =>
            {
                received["Data"] = data;
                received["TTLCounter"] = ttl;
                received["Abstraction"] = abstraction;
                received["Calculation"] = calculations;
                received["Sanctions"] = sanctions;
                received["KVP"] = kvp;
                return 1;
            });
            var context = NewContext(calculation);
            var payload = context.EntityAnalysisModelInstanceEntryPayload;

            context.ExecuteAbstractionCalculations();

            received["Data"].Should().BeSameAs(payload.Payload);
            received["TTLCounter"].Should().BeSameAs(payload.TtlCounter);
            received["Abstraction"].Should().BeSameAs(payload.Abstraction);
            received["Calculation"].Should().BeSameAs(payload.AbstractionCalculation);
            received["Sanctions"].Should().BeSameAs(payload.Sanction);
            received["KVP"].Should().BeSameAs(payload.Dictionary);
        }

        [Fact]
        public void ALaterCalculationCanReadAnEarlierOneInTheSameInvocation()
        {
            var first = NewCalculation("First", (_, _, _, _, _, _, _, _) => 21, id: 1);
            var second = NewCalculation("Second", (_, _, _, _, calculations, _, _, _) => calculations["First"] * 2,
                id: 2);
            var context = NewContext(first, second);

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Second"].Should().Be(42);
        }

        [Fact]
        public void CalculationsRunInTheOrderTheyAreConfigured()
        {
            var order = new List<string>();
            var first = NewCalculation("First", (_, _, _, _, _, _, _, _) =>
            {
                order.Add("First");
                return 1;
            }, id: 1);
            var second = NewCalculation("Second", (_, _, _, _, _, _, _, _) =>
            {
                order.Add("Second");
                return 2;
            }, id: 2);
            var context = NewContext(first, second);

            context.ExecuteAbstractionCalculations();

            order.Should().Equal("First", "Second");
        }

        [Fact]
        public void AFiniteNegativeResultIsStoredUnchanged()
        {
            var context = NewContext(NewCalculation("FromRule", (_, _, _, _, _, _, _, _) => -0.25));

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["FromRule"].Should().Be(-0.25);
        }

        [Fact]
        public void AnUndefinedResultIsStoredAsReturnedSoARuleThatWantsZeroUsesZeroIfUndefined()
        {
            var context = NewContext(NewCalculation("FromRule", (_, _, _, _, _, _, _, _) => double.NaN));

            context.ExecuteAbstractionCalculations();

            double.IsNaN(context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["FromRule"])
                .Should().BeTrue();
        }

        [Fact]
        public void ReportTableAddsAnArchiveKeyWithProcessingTypeSix()
        {
            var context = NewContext(NewCalculation("Result", (_, _, _, _, _, _, _, _) => 2, true));

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
            var context = NewContext(NewCalculation("Result", (_, _, _, _, _, _, _, _) => 2));

            context.ExecuteAbstractionCalculations();

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().BeEmpty();
        }

        [Fact]
        public void AnExceptionThrownByTheRuleDelegateIsCaughtAndTheCalculationIsSkipped()
        {
            var throwing = NewCalculation("Throws", (_, _, _, _, _, _, _, _) => throw new InvalidOperationException("boom"),
                id: 1);
            var second = NewCalculation("Second", (_, _, _, _, _, _, _, _) => 5, id: 2);
            var context = NewContext(throwing, second);

            var act = context.ExecuteAbstractionCalculations;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.ContainsKey("Throws").Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Second"].Should().Be(5);
        }

        [Fact]
        public void AnEntryWithNoCompiledDelegateIsSkippedWithoutThrowing()
        {
            var noDelegate = new EntityAnalysisModelAbstractionCalculation { Id = 1, Name = "NoScript" };
            var second = NewCalculation("Second", (_, _, _, _, _, _, _, _) => 5, id: 2);
            var context = NewContext(noDelegate, second);

            var act = context.ExecuteAbstractionCalculations;

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.ContainsKey("NoScript").Should().BeFalse();
            context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation["Second"].Should().Be(5);
        }

        [Fact]
        public void WhenSampledTheStageTimingRecordsOneItemPerCalculation()
        {
            var context = NewContext(NewCalculation("Result", (_, _, _, _, _, _, _, _) => 1));

            context.ExecuteAbstractionCalculations();

            var stages = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages;
            stages.Should().NotBeNull();
            stages!.AbstractionCalculations.Should().NotBeNull();
            stages.AbstractionCalculations!.Items.Should().ContainKey("Result");
        }

        [Fact]
        public void WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns()
        {
            var context = NewContext(NewCalculation("Result", (_, _, _, _, _, _, _, _) => 10));
            context.LogSampled = false;

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
