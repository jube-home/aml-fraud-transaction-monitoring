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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class InlineScriptCompilationExtensionsTests
    {
        private const string DecoratedScript = """
                                               using System;
                                               using System.Threading.Tasks;
                                               using Jube.Engine.Attributes.Events;
                                               using Jube.Engine.Attributes.Properties;
                                               using Jube.Engine.EntityAnalysisModelInvoke.Context;
                                               using Jube.Engine.Interfaces;

                                               namespace Jube.Test.Compiled
                                               {
                                                   public class DemoInlineScript : IInlineScript
                                                   {
                                                       [CacheIndex(Id = 1001)]
                                                       [ReportTable]
                                                       [ResponsePayload]
                                                       [SearchKey(SearchKeyTtlInterval = "h",
                                                           SearchKeyTtlIntervalValue = 1,
                                                           SearchKeyFetchLimit = 100,
                                                           SearchKeyCache = false,
                                                           SearchKeyCacheInterval = "d",
                                                           SearchKeyCacheValue = 1,
                                                           SearchKeyCacheSample = true,
                                                           SearchKeyCacheFetchLimit = 100,
                                                           SearchKeyCacheTtlInterval = "h",
                                                           SearchKeyCacheTtlValue = 1)]
                                                       public string UserAgent { get; set; } = string.Empty;

                                                       [Latitude]
                                                       public double Lat { get; set; }

                                                       [Longitude]
                                                       public double Lon { get; set; }

                                                       public int Count { get; set; }

                                                       public string Unset { get; set; }

                                                       [ActivationRuleOverrideEvent(Guid = "bc81ff60-3254-4f1a-9003-ecae5e114142", Priority = 1)]
                                                       [PayloadEvent]
                                                       public async Task<bool> ExecuteAsync(Context context)
                                                       {
                                                           UserAgent = "MyApp/1.0";
                                                           Lat = 5.3536;
                                                           Lon = 36.1408;
                                                           Count = 7;
                                                           await Task.CompletedTask;
                                                           return true;
                                                       }
                                                   }
                                               }
                                               """;

        private const string InvalidScript = """
                                             using System.Threading.Tasks;
                                             using Jube.Engine.EntityAnalysisModelInvoke.Context;
                                             using Jube.Engine.Interfaces;

                                             public class BrokenInlineScript : IInlineScript
                                             {
                                                 public async Task<bool> ExecuteAsync(Context context)
                                                 {
                                                     return ThisDoesNotExist();
                                                 }
                                             }
                                             """;

        private const string IssueOtpVbScript = """
                                                Imports System
                                                Imports System.Threading.Tasks
                                                Imports Jube.Engine.Attributes.Events
                                                Imports Jube.Engine.EntityAnalysisModelInvoke.Context
                                                Imports Jube.Engine.Interfaces

                                                Public Class IssueOTP
                                                Implements IInlineScript
                                                    Public Property OTP As String

                                                    <PayloadEvent>
                                                    Public Async Function ExecuteAsync(context As Context) As Task(Of Boolean) Implements IInlineScript.ExecuteAsync
                                                        OTP = RandomDigits(6)
                                                        Return True
                                                    End Function

                                                    Private Function RandomDigits(ByVal length As Integer) As String
                                                        Dim random = New Random()
                                                        Dim s As String = String.Empty

                                                        For i As Integer = 0 To length - 1
                                                        s = String.Concat(s, random.[Next](10).ToString())
                                                        Next

                                                        Return s
                                                    End Function
                                                End Class
                                                """;

        private static Jube.Engine.EntityAnalysisModelInvoke.Context.Context NewContext(
            EntityAnalysisModelInlineScript script)
        {
            var entityAnalysisModel =
                new Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel();
            entityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(script);

            return new Jube.Engine.EntityAnalysisModelInvoke.Context.Context
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

        [Fact]
        public void AValidCSharpScriptCompilesWithoutErrorsAndLocatesTheInterfaceImplementation()
        {
            var (inlineScript, compile) = InlineScriptTestCompiler.Compile(1, DecoratedScript);

            compile.Errors.Should().BeNull();
            inlineScript.InlineScriptType.Should().NotBeNull();
            inlineScript.ClassName.Should().Be("Jube.Test.Compiled.DemoInlineScript");
        }

        [Fact]
        public void ASyntaxOrSemanticErrorProducesCompileErrorsAndNoInterfaceImplementation()
        {
            var (inlineScript, compile) = InlineScriptTestCompiler.Compile(2, InvalidScript);

            compile.Errors.Should().NotBeNull();
            compile.ErrorsSummary.Should().NotBeNullOrEmpty();
            inlineScript.InlineScriptType.Should().BeNull();
        }

        [Fact]
        public void TheActivatorDelegateCreatesANewInstanceOfTheCompiledType()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(3, DecoratedScript);

            var instance = inlineScript.ActivatorDelegate();

            instance.Should().NotBeNull();
            instance.GetType().FullName.Should().Be("Jube.Test.Compiled.DemoInlineScript");
        }

        [Fact]
        public async Task TheExecuteAsyncDelegateInvokesTheCompiledMethodAndReturnsItsResultAsync()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(4, DecoratedScript);
            var instance = inlineScript.ActivatorDelegate();
            var context = NewContext(inlineScript);

            var result = await inlineScript.ExecuteAsyncDelegate(instance, context);

            result.Should().BeTrue();
        }

        [Fact]
        public void ReportTableResponsePayloadAndCacheIndexAttributesAreReflectedOntoThePropertyAttribute()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(5, DecoratedScript);

            var userAgent = inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes["UserAgent"];

            userAgent.ReportTable.Should().BeTrue();
            userAgent.ResponsePayload.Should().BeTrue();
            userAgent.CacheIndexId.Should().Be(1001);
            userAgent.PropertyType.Should().Be(typeof(string));
        }

        [Fact]
        public void SearchKeyAttributeSubFieldsAreCapturedOntoTheDistinctSearchKey()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(6, DecoratedScript);

            var searchKey = inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes["UserAgent"].SearchKey;

            searchKey.Should().NotBeNull();
            searchKey!.SearchKey.Should().Be("UserAgent");
            searchKey.SearchKeyTtlInterval.Should().Be("h");
            searchKey.SearchKeyTtlIntervalValue.Should().Be(1);
            searchKey.SearchKeyFetchLimit.Should().Be(100);
            searchKey.SearchKeyCache.Should().BeFalse();
            searchKey.SearchKeyCacheInterval.Should().Be("d");
            searchKey.SearchKeyCacheValue.Should().Be(1);
            searchKey.SearchKeyCacheSample.Should().BeTrue();
            searchKey.SearchKeyCacheFetchLimit.Should().Be(100);
            searchKey.SearchKeyCacheTtlInterval.Should().Be("h");
            searchKey.SearchKeyCacheTtlValue.Should().Be(1);
        }

        [Fact]
        public void SearchKeyDecoratedPropertyIsAlsoAddedAsAModelGroupingKey()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(7, DecoratedScript);

            inlineScript.GroupingKeys.Should().ContainSingle(k => k.SearchKey == "UserAgent");
        }

        [Fact]
        public void LatitudeAndLongitudeAttributesAreReflectedIndependentlyOfEachOther()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(8, DecoratedScript);

            var lat = inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes["Lat"];
            var lon = inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes["Lon"];

            lat.Latitude.Should().BeTrue();
            lat.Longitude.Should().BeFalse();
            lon.Longitude.Should().BeTrue();
            lon.Latitude.Should().BeFalse();
        }

        [Fact]
        public void APropertyWithNoAttributesHasEveryDecorationFlagFalseAndNoSearchKey()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(9, DecoratedScript);

            var count = inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes["Count"];

            count.ReportTable.Should().BeFalse();
            count.ResponsePayload.Should().BeFalse();
            count.Latitude.Should().BeFalse();
            count.Longitude.Should().BeFalse();
            count.CacheIndexId.Should().BeNull();
            count.SearchKey.Should().BeNull();
            count.PropertyType.Should().Be(typeof(int));
        }

        [Fact]
        public void EventAttributesOnExecuteAsyncArePopulatedAndOrderedByPriority()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(10, DecoratedScript);

            inlineScript.EntityAnalysisModelInlineScriptEvents.Should().HaveCount(2);

            var first = inlineScript.EntityAnalysisModelInlineScriptEvents[0];
            first.EntityAnalysisModelInlineScriptEventType.Should()
                .Be(EntityAnalysisModelInlineScriptEventTypeEnum.Payload);
            first.Priority.Should().Be(0);

            var second = inlineScript.EntityAnalysisModelInlineScriptEvents[1];
            second.EntityAnalysisModelInlineScriptEventType.Should()
                .Be(EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride);
            second.Priority.Should().Be(1);
            second.Guid.Should().Be(Guid.Parse("bc81ff60-3254-4f1a-9003-ecae5e114142"));
        }

        [Fact]
        public async Task
            RunningTheCompiledScriptThroughReflectInlineScriptHelperPopulatesThePayloadAndArchiveKeysAsync()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(11, DecoratedScript);
            var context = NewContext(inlineScript);

            var result = await ReflectInlineScriptHelper.ExecuteAsync(inlineScript, context);

            result.Should().BeTrue();
            context.EntityAnalysisModelInstanceEntryPayload.Payload["UserAgent"].AsString().Should().Be("MyApp/1.0");
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Count"].AsInt().Should().Be(7);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey("Unset").Should().BeFalse();

            var archiveKey = context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Should().ContainSingle()
                .Subject;
            archiveKey.Key.Should().Be("UserAgent");
            archiveKey.KeyValueString.Should().Be("MyApp/1.0");
        }

        [Fact]
        public async Task RunningTheCompiledScriptThroughExecuteInlineScriptsAsyncEndToEndPopulatesThePayloadAsync()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(12, DecoratedScript);
            var context = NewContext(inlineScript);

            await context.ExecuteInlineScriptsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Payload["Lat"].AsDouble().Should().Be(5.3536);
            context.EntityAnalysisModelInstanceEntryPayload.Payload["Lon"].AsDouble().Should().Be(36.1408);
        }

        [Fact]
        public async Task TheSystemWideDemonstrationVisualBasicScriptCompilesAndProducesASixDigitOtpAsync()
        {
            var inlineScript = InlineScriptTestCompiler.CompileValid(13, IssueOtpVbScript, 1);
            var instance = inlineScript.ActivatorDelegate();
            var context = NewContext(inlineScript);

            var result = await inlineScript.ExecuteAsyncDelegate(instance, context);

            result.Should().BeTrue();
            var otpProperty = instance.GetType().GetProperty("OTP");
            var otp = (string)otpProperty!.GetValue(instance)!;
            Regex.IsMatch(otp, "^[0-9]{6}$").Should().BeTrue($"OTP was '{otp}'");
        }

        [Fact]
        public void CompilingTheSameCodeTwiceProducesIndependentDelegatesThatStillBehaveIdentically()
        {
            var first = InlineScriptTestCompiler.CompileValid(14, DecoratedScript);
            var second = InlineScriptTestCompiler.CompileValid(15, DecoratedScript);

            first.ActivatorDelegate.Should().NotBeSameAs(second.ActivatorDelegate);
            first.ActivatorDelegate().GetType().FullName.Should()
                .Be(second.ActivatorDelegate().GetType().FullName);
        }
    }
}