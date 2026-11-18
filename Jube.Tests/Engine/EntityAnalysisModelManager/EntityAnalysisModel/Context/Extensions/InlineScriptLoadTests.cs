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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Engine.Helpers;
using Jube.Engine.Models;
using Jube.Test.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class InlineScriptLoadTests
    {
        private const string Usings = """
                                      using System;
                                      using System.Threading.Tasks;
                                      using Jube.Engine.Attributes.Events;
                                      using Jube.Engine.Attributes.Properties;
                                      using Jube.Engine.EntityAnalysisModelInvoke.Context;
                                      using Jube.Engine.Interfaces;

                                      """;

        private const string IssueOtpVbTemplate = """
                                                  Imports System
                                                  Imports System.Threading.Tasks
                                                  Imports Jube.Engine.Attributes.Events
                                                  Imports Jube.Engine.Attributes.Properties
                                                  Imports Jube.Engine.EntityAnalysisModelInvoke.Context
                                                  Imports Jube.Engine.Interfaces

                                                  Public Class IssueOTP
                                                  Implements IInlineScript
                                                      {0}Public Property OTP As String

                                                      <PayloadEvent>
                                                      Public Async Function ExecuteAsync(context As Context) As Task(Of Boolean) Implements IInlineScript.ExecuteAsync
                                                          OTP = "123456"
                                                          Return True
                                                      End Function
                                                  End Class
                                                  """;

        private readonly ConcurrentDictionary<string, Assembly> hashCacheAssembly = new();
        private readonly ConcurrentDictionary<string, HashCacheAssemblyPayload> hashCacheAssemblyMetadata = new();
        private readonly List<EntityAnalysisModelInlineScript> inlineScripts = [];
        private readonly TestLog log = new();

        private static string Script(string className, string attributes, string value = "\"issued\"",
            string returns = "true")
        {
            return Usings + $$"""
                              public class {{className}} : IInlineScript
                              {
                                  {{attributes}}
                                  public string Token { get; set; }

                                  [PayloadEvent]
                                  public async Task<bool> ExecuteAsync(Context context)
                                  {
                                      Token = {{value}};
                                      await Task.CompletedTask;
                                      return {{returns}};
                                  }
                              }
                              """;
        }

        private static Data.Poco.EntityAnalysisInlineScript Record(int id, string code, byte? languageId = 2,
            string? dependency = null, DateTime? createdDate = null, string name = "Token")
        {
            return new Data.Poco.EntityAnalysisInlineScript
            {
                Id = id,
                Code = code,
                LanguageId = languageId,
                Dependency = dependency,
                Name = name,
                CreatedDate = createdDate ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        private InlineScriptLoadResult Load(Data.Poco.EntityAnalysisInlineScript record)
        {
            return InlineScriptCompilationExtensions.Load(inlineScripts, hashCacheAssembly,
                hashCacheAssemblyMetadata, record, AppContext.BaseDirectory,
                Path.GetDirectoryName(typeof(object).Assembly.Location), log);
        }

        private async Task<JObject> InvokeAndBuildResponseAsync()
        {
            var entityAnalysisModel =
                new Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel();

            entityAnalysisModel.Snapshot.EntityAnalysisModelInlineScripts.AddRange(inlineScripts);

            var context = new Jube.Engine.EntityAnalysisModelInvoke.Context.Context
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

            await context.ExecuteInlineScriptsAsync();

            var json = BuildJsonResponses.BuildPartialResponsePayloadJson(context,
                new JsonSerializationHelper().ArchiveJsonSerializer);

            return JObject.Parse(Encoding.UTF8.GetString(json));
        }

        [Fact]
        public void ANewRecordIsCompiledAddedOnceAndReportedAsCompiled()
        {
            var result = Load(Record(1, Script("First", "[ResponsePayload]")));

            result.Should().Be(new InlineScriptLoadResult(true, true, null));

            var loaded = inlineScripts.Should().ContainSingle().Subject;
            loaded.Id.Should().Be(1);
            loaded.Name.Should().Be("Token");
            loaded.ClassName.Should().Be("First");
            loaded.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload.Should().BeTrue();
            loaded.EntityAnalysisModelInlineScriptEvents.Should().ContainSingle();
            hashCacheAssembly.Should().ContainSingle();
            hashCacheAssemblyMetadata.Should().ContainSingle();
        }

        [Fact]
        public void AnUnchangedRecordKeepsTheLoadedInstanceAndIsReportedAsUnchanged()
        {
            var record = Record(2, Script("Unchanged", "[ResponsePayload]"));
            Load(record);
            var first = inlineScripts.Single();

            var result = Load(record);

            result.Changed.Should().BeFalse();
            result.Compiled.Should().BeTrue();
            inlineScripts.Should().ContainSingle().Which.Should().BeSameAs(first);
        }

        [Fact]
        public void AnUnchangedRecordStillTakesTheLatestNameAndCreatedDate()
        {
            var code = Script("Renamed", "[ResponsePayload]");
            Load(Record(3, code));
            var later = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var result = Load(Record(3, code, createdDate: later, name: "Renamed token"));

            result.Changed.Should().BeFalse();
            inlineScripts.Single().Name.Should().Be("Renamed token");
            inlineScripts.Single().CreatedDate.Should().Be(later);
        }

        [Fact]
        public void ChangedCodeReplacesTheLoadedScriptInPlaceWithTheNewAttributes()
        {
            Load(Record(4, Script("Before", "")));
            var before = inlineScripts.Single();
            before.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload.Should().BeFalse();

            var result = Load(Record(4, Script("After", "[ResponsePayload] [ReportTable]"),
                createdDate: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));

            result.Should().Be(new InlineScriptLoadResult(true, true, null));

            var after = inlineScripts.Should().ContainSingle().Subject;
            after.Should().NotBeSameAs(before);
            after.ClassName.Should().Be("After");
            after.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload.Should().BeTrue();
            after.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ReportTable.Should().BeTrue();
            before.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload.Should().BeFalse();
            before.ClassName.Should().Be("Before");
        }

        [Fact]
        public void ChangedCodeIsPickedUpWhenTheCreatedDateHasNotMoved()
        {
            Load(Record(5, Script("SameDateBefore", "")));

            var result = Load(Record(5, Script("SameDateAfter", "[ResponsePayload]")));

            result.Changed.Should().BeTrue();

            inlineScripts.Should().ContainSingle().Which.EntityAnalysisModelInlineScriptPropertyAttributes["Token"]
                .ResponsePayload.Should().BeTrue();
        }

        [Fact]
        public void ChangedCodeWithASearchKeyDoesNotAccumulateGroupingKeys()
        {
            const string searchKey = "[SearchKey(SearchKeyFetchLimit = 10, SearchKeyCacheFetchLimit = 20)]";
            Load(Record(6, Script("KeyedBefore", searchKey)));

            Load(Record(6, Script("KeyedAfter", searchKey + " [ResponsePayload]")));

            var groupingKey = inlineScripts.Should().ContainSingle().Which.GroupingKeys.Should().ContainSingle()
                .Subject;
            groupingKey.SearchKey.Should().Be("Token");
            groupingKey.SearchKeyFetchLimit.Should().Be(10);
            groupingKey.SearchKeyCacheFetchLimit.Should().Be(20);
        }

        [Fact]
        public void CodeRevertedToAnAlreadyCompiledVersionIsConfiguredInFullFromTheAssemblyCache()
        {
            var original = Script("Original", "[ResponsePayload]");
            Load(Record(7, original));
            Load(Record(7, Script("Interim", "")));
            hashCacheAssembly.Should().HaveCount(2);

            var result = Load(Record(7, original));

            result.Should().Be(new InlineScriptLoadResult(true, true, null));
            hashCacheAssembly.Should().HaveCount(2);

            var loaded = inlineScripts.Should().ContainSingle().Subject;
            loaded.ClassName.Should().Be("Original");
            loaded.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload.Should().BeTrue();
            loaded.EntityAnalysisModelInlineScriptEvents.Should().ContainSingle();
            loaded.ActivatorDelegate.Should().NotBeNull();
            loaded.ExecuteAsyncDelegate.Should().NotBeNull();
        }

        [Fact]
        public void TwoRecordsSharingTheSameCodeAreBothLoadedFromOneCompiledAssembly()
        {
            var code = Script("Shared", "[ResponsePayload]");

            Load(Record(8, code));
            Load(Record(9, code));

            inlineScripts.Select(s => s.Id).Should().Equal(8, 9);
            hashCacheAssembly.Should().ContainSingle();

            inlineScripts.Should().OnlyContain(s =>
                s.EntityAnalysisModelInlineScriptPropertyAttributes["Token"].ResponsePayload);
        }

        [Fact]
        public void ACompileErrorIsReportedAndLeavesThePreviouslyLoadedScriptInPlace()
        {
            Load(Record(10, Script("Working", "[ResponsePayload]")));
            var working = inlineScripts.Single();

            var result = Load(Record(10, Script("Broken", "[ResponsePayload]", "NotDeclared")));

            result.Changed.Should().BeTrue();
            result.Compiled.Should().BeFalse();
            result.CompileError.Should().NotBeNullOrWhiteSpace();
            inlineScripts.Should().ContainSingle().Which.Should().BeSameAs(working);
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public void ACompileErrorOnANewRecordLoadsNothing()
        {
            var result = Load(Record(11, Script("BrokenFromTheStart", "", "NotDeclared")));

            result.Compiled.Should().BeFalse();
            inlineScripts.Should().BeEmpty();
            hashCacheAssembly.Should().BeEmpty();
        }

        [Fact]
        public void CodeWithNoClassImplementingTheInterfaceIsReportedAndLoadsNothing()
        {
            var result = Load(Record(12, "public class NotAnInlineScript { public string Token { get; set; } }"));

            result.Should().Be(new InlineScriptLoadResult(true, false,
                InlineScriptCompilationExtensions.NoImplementation));

            inlineScripts.Should().BeEmpty();
        }

        [Fact]
        public void ANullLanguageIsLoadedAsVisualBasic()
        {
            var result = Load(Record(13, string.Format(IssueOtpVbTemplate, "<ResponsePayload> "), null));

            result.Compiled.Should().BeTrue();

            var loaded = inlineScripts.Should().ContainSingle().Subject;
            loaded.LanguageId.Should().Be(1);
            loaded.EntityAnalysisModelInlineScriptPropertyAttributes["OTP"].ResponsePayload.Should().BeTrue();
        }

        [Fact]
        public void TheDeclaredDependencyIsCarriedOntoTheLoadedScript()
        {
            Load(Record(14, Script("WithDependency", ""), dependency: "Jube.Cryptography.dll"));

            inlineScripts.Should().ContainSingle().Which.Dependencies.Should().Be("Jube.Cryptography.dll");
        }

        [Theory]
        [InlineData("code", null, 2, "code", null, 2, false)]
        [InlineData("code", null, 2, "code", "", 2, false)]
        [InlineData("code", "", 2, "code", null, 2, false)]
        [InlineData("code", null, 2, "other", null, 2, true)]
        [InlineData("code", null, 2, "code", "Extra.dll", 2, true)]
        [InlineData("code", "Extra.dll", 2, "code", null, 2, true)]
        [InlineData("code", null, 2, "code", null, 1, true)]
        public void HasChangedComparesCodeDependenciesAndLanguage(string loadedCode, string loadedDependencies,
            byte loadedLanguageId, string code, string dependencies, byte languageId, bool expected)
        {
            var inlineScript = new EntityAnalysisModelInlineScript
            {
                InlineScriptCode = loadedCode,
                Dependencies = loadedDependencies,
                LanguageId = loadedLanguageId
            };

            inlineScript.HasChanged(code, dependencies, languageId).Should().Be(expected);
        }

        [Fact]
        public void ConfiguringTheSameInstanceTwiceDoesNotDuplicatePropertiesOrGroupingKeys()
        {
            const string code = Usings + """
                                         public class Twice : IInlineScript
                                         {
                                             [SearchKey]
                                             public string Token { get; set; }

                                             [PayloadEvent]
                                             public async Task<bool> ExecuteAsync(Context context)
                                             {
                                                 await Task.CompletedTask;
                                                 return true;
                                             }
                                         }
                                         """;

            var inlineScript = InlineScriptTestCompiler.CompileValid(15, code);

            inlineScript.ConfigureFromAssembly(inlineScript.InlineScriptCompile);

            inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes.Should().ContainSingle();
            inlineScript.GroupingKeys.Should().ContainSingle();
            inlineScript.EntityAnalysisModelInlineScriptEvents.Should().ContainSingle();
        }

        [Fact]
        public async Task APropertyDecoratedWithResponsePayloadIsWrittenToTheResponsePayloadAsync()
        {
            Load(Record(16, Script("Responding", "[ResponsePayload]")));

            var response = await InvokeAndBuildResponseAsync();

            response["Payload"]!["Token"]!.Value<string>().Should().Be("issued");
        }

        [Fact]
        public async Task APropertyWithoutResponsePayloadIsLeftOutOfTheResponsePayloadAsync()
        {
            Load(Record(17, Script("Silent", "[ReportTable]")));

            var response = await InvokeAndBuildResponseAsync();

            ((JObject)response["Payload"]!).ContainsKey("Token").Should().BeFalse();
        }

        [Fact]
        public async Task AddingResponsePayloadToALoadedScriptPutsThePropertyInTheResponseAfterReloadAsync()
        {
            Load(Record(18, Script("ReloadBefore", "")));
            var before = await InvokeAndBuildResponseAsync();
            ((JObject)before["Payload"]!).ContainsKey("Token").Should().BeFalse();

            Load(Record(18, Script("ReloadAfter", "[ResponsePayload]", "\"reissued\"")));
            var after = await InvokeAndBuildResponseAsync();

            after["Payload"]!["Token"]!.Value<string>().Should().Be("reissued");
        }

        [Fact]
        public async Task AddingResponsePayloadToTheVisualBasicDemonstrationScriptReturnsTheOtpAsync()
        {
            Load(Record(19, string.Format(IssueOtpVbTemplate, string.Empty), 1));
            var before = await InvokeAndBuildResponseAsync();
            ((JObject)before["Payload"]!).ContainsKey("OTP").Should().BeFalse();

            Load(Record(19, string.Format(IssueOtpVbTemplate, "<ResponsePayload> "), 1));
            var after = await InvokeAndBuildResponseAsync();

            after["Payload"]!["OTP"]!.Value<string>().Should().Be("123456");
        }

        [Fact]
        public async Task AScriptReturningFalseLeavesItsPropertiesOutOfTheResponsePayloadAsync()
        {
            Load(Record(20, Script("Declining", "[ResponsePayload]", returns: "false")));

            var response = await InvokeAndBuildResponseAsync();

            ((JObject)response["Payload"]!).ContainsKey("Token").Should().BeFalse();
        }

        [Fact]
        public async Task EverySupportedPropertyTypeIsWrittenToTheResponsePayloadWithItsOwnJsonTypeAsync()
        {
            const string code = Usings + """
                                         public class Typed : IInlineScript
                                         {
                                             [ResponsePayload] public string Text { get; set; }
                                             [ResponsePayload] public int Whole { get; set; }
                                             [ResponsePayload] public double Fraction { get; set; }
                                             [ResponsePayload] public bool Flag { get; set; }
                                             [ResponsePayload] public DateTime When { get; set; }
                                             [ResponsePayload] public int? Missing { get; set; }
                                             [ResponsePayload] public decimal Unsupported { get; set; }

                                             [PayloadEvent]
                                             public async Task<bool> ExecuteAsync(Context context)
                                             {
                                                 Text = "text";
                                                 Whole = 7;
                                                 Fraction = 1.5;
                                                 Flag = true;
                                                 When = new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);
                                                 Unsupported = 9.9m;
                                                 await Task.CompletedTask;
                                                 return true;
                                             }
                                         }
                                         """;

            Load(Record(21, code));

            var response = await InvokeAndBuildResponseAsync();
            var payload = (JObject)response["Payload"]!;

            payload["Text"]!.Type.Should().Be(JTokenType.String);
            payload["Text"]!.Value<string>().Should().Be("text");
            payload["Whole"]!.Type.Should().Be(JTokenType.Integer);
            payload["Whole"]!.Value<int>().Should().Be(7);
            payload["Fraction"]!.Type.Should().Be(JTokenType.Float);
            payload["Fraction"]!.Value<double>().Should().Be(1.5);
            payload["Flag"]!.Type.Should().Be(JTokenType.Boolean);
            payload["Flag"]!.Value<bool>().Should().BeTrue();
            payload["When"]!.ToObject<DateTime>().ToUniversalTime().Should()
                .Be(new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc));
            payload.ContainsKey("Missing").Should().BeFalse();
            payload.ContainsKey("Unsupported").Should().BeFalse();
        }
    }
}
