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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jube.Test.Engine.Sanctions;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using Jube.Test.Service.Invoke.Models;
using LinqToDB;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Service.Invoke
{
    [Collection("Database")]
    public sealed class InvokeEndpointTests(
        InvokeModelFixture fixture,
        DatabaseFixture database,
        ITestOutputHelper output)
        : IClassFixture<InvokeModelFixture>
    {
        private const string Json = "application/json";
        private const string JsonUtf8 = "application/json; charset=utf-8";
        private const string ModelRoute = "/api/Invoke/EntityAnalysisModel/";

        private ModelInvokeHost Api => fixture.Api;
        private ModelScaffold Model => fixture.Shared;
        private string User => fixture.Shared.UserName;

        private static byte[] Bytes(string value) => Encoding.UTF8.GetBytes(value);

        private static Task<T> WithHostAsync<T>(ModelInvokeHost host, Func<ModelInvokeHost, Task<T>> action)
        {
            return Task.Run(async () =>
            {
                await using (host)
                {
                    return await action(host);
                }
            });
        }

        [Fact]
        public async Task Invoke_WithTheDocumentationPayload_Returns200JsonFromTheRealEngineAsync()
        {
            var errorsBefore = fixture.Engine.Log.Entries.Count(e => e.Level == "ERROR"
                                                                     && e.Message.Contains("Entity Invoke"));
            var response = await Api.InvokeAsync(Model, Payloads.Example());

            Assert.Equal(200, response.Status);
            Assert.Equal(Json, response.ContentType);
            Assert.True(response.BodyBytes.Length > 1000, "the archive style response is a substantial document");
            Assert.Equal("0987654321", response.String("EntityInstanceEntryId"));
            Assert.StartsWith("2018-08-19", response.String("ReferenceDate").Required());
            Assert.True(Guid.TryParse(response.String("EntityAnalysisModelInstanceEntryGuid"), out _));
            Assert.Equal(errorsBefore, fixture.Engine.Log.Entries.Count(e => e.Level == "ERROR"
                                                                             && e.Message.Contains("Entity Invoke")));
        }

        [Fact]
        public async Task Invoke_OverridesInThePayload_AreReadByTheModelAsync()
        {
            var unique = "txn-" + Guid.NewGuid().ToString("N")[..12];

            var response = await Api.InvokeAsync(Model, Payloads.Example().With("TxnId", unique));

            Assert.Equal(200, response.Status);
            Assert.Equal(unique, response.String("EntityInstanceEntryId"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("text/plain")]
        public async Task Invoke_DoesNotRequireAJsonContentTypeAsync(string? contentType)
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid, User,
                Payloads.Example().ToBytes(), contentType);

            Assert.Equal(200, response.Status);
            Assert.Equal(Json, response.ContentType);
            Assert.Equal("0987654321", response.String("EntityInstanceEntryId"));
        }

        [Theory]
        [InlineData("Async")]
        [InlineData("async")]
        [InlineData("ASYNC")]
        public async Task Invoke_AsyncSegment_ReturnsACallbackTokenInsteadOfTheResponseAsync(string segment)
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + "/" + segment, User,
                Payloads.Example().ToBytes(), Json);

            Assert.Equal(200, response.Status);
            Assert.Equal(Json, response.ContentType);
            Assert.Equal(["entityAnalysisModelInstanceEntryGuid"], response.PropertyNames);
            Assert.True(Guid.TryParse(response.String("entityAnalysisModelInstanceEntryGuid"), out _));
        }

        [Fact]
        public async Task Invoke_AnyOtherSegment_IsSynchronousAsync()
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + "/Sync", User,
                Payloads.Example().ToBytes(), Json);

            Assert.Equal(200, response.Status);
            Assert.Equal("0987654321", response.String("EntityInstanceEntryId"));
        }

        [Fact]
        public async Task Invoke_AsyncThenCallback_DeliversTheResponseOfTheAsynchronousInvocationAsync()
        {
            var unique = "cb-" + Guid.NewGuid().ToString("N")[..12];
            var token = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + "/Async", User,
                Payloads.Example().With("TxnId", unique).ToBytes(), Json);
            Assert.Equal(200, token.Status);
            var entryGuid = token.String("entityAnalysisModelInstanceEntryGuid");

            var callback = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{entryGuid}?timeout=20000", User);

            Assert.Equal(200, callback.Status);
            Assert.Equal(Json, callback.ContentType);
            Assert.Equal(unique, callback.String("EntityInstanceEntryId"));
            Assert.Equal(entryGuid, callback.String("EntityAnalysisModelInstanceEntryGuid"));
        }

        [Fact]
        public async Task Invoke_UnknownGuid_Returns404Async()
        {
            var response = await Api.SendAsync("POST", ModelRoute + Guid.NewGuid(), User,
                Payloads.Example().ToBytes(), Json);

            Assert.Equal(404, response.Status);
            Assert.Empty(response.BodyBytes);
        }

        [Fact]
        public async Task Invoke_GuidThatIsNotAGuid_Returns404Async()
        {
            var response = await Api.SendAsync("POST", ModelRoute + "not-a-guid", User,
                Payloads.Example().ToBytes(), Json);

            Assert.Equal(404, response.Status);
        }

        [Theory]
        [InlineData("{not json")]
        [InlineData("{\"AccountId\":\"a\"")]
        [InlineData("[1,2]")]
        [InlineData("\"just a string\"")]
        [InlineData("123")]
        [InlineData("null")]
        [InlineData("{\"AccountId\":}")]
        public async Task Invoke_MalformedOrNonObjectJson_Returns400WithNoParserDetailAsync(string body)
        {
            var response = await Api.InvokeAsync(Model, Bytes(body));

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Malformed JSON in POST body.\"", response.Body);
            Assert.DoesNotContain("Newtonsoft", response.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("JsonReader", response.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("line ", response.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Invoke_MalformedJsonOnTheAsyncRoute_Returns400AndQueuesNothingAsync()
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + "/Async", User,
                Bytes("{not json"), Json);

            Assert.Equal(400, response.Status);
            Assert.Equal("\"Malformed JSON in POST body.\"", response.Body);
        }

        [Fact]
        public async Task Invoke_WhenTheEngineHitsAnError_Returns500WithAnEmptyBodyAndLogsItAsync()
        {
            const string segment = "";
            var cache = (fixture.Engine.FindActiveModel(Model.ModelGuid) ??
                         throw new InvalidOperationException("The model is not synchronised.")).Services.CacheService;
            var original = cache.CacheReferenceDateRepository;

            InvokeResponse response;
            cache.CacheReferenceDateRepository = null;
            try
            {
                response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + segment, User,
                    Payloads.Example().ToBytes(), Json);
            }
            finally
            {
                cache.CacheReferenceDateRepository = original;
            }

            Assert.Equal(500, response.Status);
            Assert.Empty(response.BodyBytes);
            Assert.Contains(fixture.Engine.Log.Entries,
                e => e.Level == "ERROR" && e.Message.Contains("Returning 500"));

            var healthy = await Api.InvokeAsync(Model, Payloads.Example());
            Assert.Equal(200, healthy.Status);
        }

        [Fact]
        public async Task Invoke_EmptyBody_Returns400WithTheMessageAsAJsonStringAsync()
        {
            var response = await Api.InvokeAsync(Model, []);

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Empty POST body.\"", response.Body);
        }

        [Fact]
        public async Task Invoke_BodyOverTheConfiguredMaximum_Returns400Async()
        {
            var body = Payloads.Example().With("AccountId", new string('x', 25_000)).ToBytes();
            Assert.True(body.Length > 20_000);

            var response = await Api.InvokeAsync(Model, body);

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Exceeded the maximum allowed bytes in POST body.\"", response.Body);
        }

        [Fact]
        public async Task Invoke_WithoutAContentLength_Returns400Async()
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid, User,
                Payloads.Example().ToBytes(), Json, sendContentLength: false);

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Content body is zero length.\"", response.Body);
        }

        [Fact]
        public async Task Invoke_ByAUserNotGrantedTheModel_Returns403Async()
        {
            var response = await Api.InvokeAsync(Model, Payloads.Example(), user: Model.OtherUserName);

            Assert.Equal(403, response.Status);
        }

        [Fact]
        public async Task Invoke_Unauthenticated_Returns401Async()
        {
            var response = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid, null,
                Payloads.Example().ToBytes(), Json);

            Assert.Equal(401, response.Status);
        }

        [Fact]
        public async Task Invoke_WhenThePublicInvokeControllerIsDisabled_Returns404Async()
        {
            var environment = ModelEngineHost.CreateEnvironment(
                new Dictionary<string, string> { ["EnablePublicInvokeController"] = "False" });
            var host = await ModelInvokeHost.StartAsync(environment, fixture.Engine.Log, fixture.Engine.Engine);

            var response = await WithHostAsync(host, h => h.InvokeAsync(Model, Payloads.Example()));

            Assert.Equal(404, response.Status);
        }

        [Fact]
        public async Task Invoke_WhenTheEngineIsNotAvailable_Returns503Async()
        {
            var host = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);

            var response = await WithHostAsync(host, h => h.InvokeAsync(Model, Payloads.Example()));

            Assert.Equal(503, response.Status);
        }

        [Fact]
        public async Task Invoke_WithTheWrongVerb_Returns405Async()
        {
            var response = await Api.SendAsync("GET", ModelRoute + Model.ModelGuid, User);

            Assert.Equal(405, response.Status);
        }

        [Theory]
        [InlineData("nope")]
        [InlineData("00000000-0000-0000-0000-000000000001")]
        public async Task Exhaustive_UnknownInstance_Returns404Async(string guid)
        {
            var response = await Api.SendAsync("POST", "/api/Invoke/ExhaustiveSearchInstance/" + guid, User,
                Bytes("{}"), Json);

            Assert.Equal(404, response.Status);
        }

        [Fact]
        public async Task Exhaustive_TheGuidOfAModelThatIsNotAnExhaustiveInstance_Returns404Async()
        {
            var response = await Api.SendAsync("POST", "/api/Invoke/ExhaustiveSearchInstance/" + Model.ModelGuid,
                User, Bytes("{no"), Json);

            Assert.Equal(404, response.Status);
        }

        [Fact]
        public async Task Exhaustive_Unauthenticated_Returns401Async()
        {
            var response = await Api.SendAsync("POST", "/api/Invoke/ExhaustiveSearchInstance/" + Model.ModelGuid,
                null, Bytes("{}"), Json);

            Assert.Equal(401, response.Status);
        }

        [Fact]
        public async Task Exhaustive_GateOffAndEngineMissing_Return404And503Async()
        {
            var gateOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
                    { ["EnablePublicInvokeController"] = "False" }), fixture.Engine.Log, fixture.Engine.Engine);
            var noEngine = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);
            var path = "/api/Invoke/ExhaustiveSearchInstance/" + Model.ModelGuid;

            Assert.Equal(404,
                (await WithHostAsync(gateOff, h => h.SendAsync("POST", path, User, Bytes("{}"), Json))).Status);
            Assert.Equal(503,
                (await WithHostAsync(noEngine, h => h.SendAsync("POST", path, User, Bytes("{}"), Json))).Status);
        }

        private async Task<T> WithExhaustiveInstanceAsync<T>(Func<Guid, Task<T>> action)
        {
            var engineModel = fixture.Engine.FindActiveModel(Model.ModelGuid) ??
                              throw new InvalidOperationException("The model is not synchronised.");
            var instance = new global::Jube.Engine.Exhaustive.Models.ExhaustiveSearchInstance
            {
                Name = "InvokeTestExhaustive",
                Id = -1,
                Guid = Guid.NewGuid(),
                TopologyNetwork = new Accord.Neuro.ActivationNetwork(new Accord.Neuro.SigmoidFunction(), 2,
                    [], 1)
            };
            instance.NetworkVariablesInOrder.Add(new global::Jube.Engine.Exhaustive.Models
                .ExhaustiveSearchInstancePromotedTrialInstanceVariable { Name = "Amount", NormalisationTypeId = 1 });
            instance.NetworkVariablesInOrder.Add(new global::Jube.Engine.Exhaustive.Models
                .ExhaustiveSearchInstancePromotedTrialInstanceVariable { Name = "Count", NormalisationTypeId = 1 });

            engineModel.Collections.ExhaustiveModels.Add(instance);
            try
            {
                return await action(instance.Guid);
            }
            finally
            {
                engineModel.Collections.ExhaustiveModels.Remove(instance);
            }
        }

        [Theory]
        [InlineData("{\"Amount\":10,\"Count\":2}")]
        [InlineData("{\"Amount\":10.5,\"Count\":\"2\"}")]
        [InlineData("{\"Amount\":true,\"Count\":null}")]
        [InlineData("{\"Amount\":10}")]
        [InlineData("{}")]
        [InlineData("{\"Unrelated\":[1,2,{\"a\":1}],\"Amount\":1}")]
        public async Task Exhaustive_ValidTypesAndMissingVariables_Return200WithANumberAsync(string body)
        {
            var response = await WithExhaustiveInstanceAsync(guid => Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + guid, User, Bytes(body), Json));

            Assert.Equal(200, response.Status);
            Assert.Equal(JsonValueKind.Number, response.Json.ValueKind);
            Assert.InRange(response.Json.GetDouble(), 0, 1);
        }

        [Theory]
        [InlineData("{\"Amount\":[1,2],\"Count\":1}")]
        [InlineData("{\"Amount\":{\"a\":1},\"Count\":1}")]
        [InlineData("{\"Amount\":\"abc\",\"Count\":1}")]
        [InlineData("{\"Amount\":\"\",\"Count\":1}")]
        [InlineData("{\"Amount\":1,\"Count\":\"NaN\"}")]
        [InlineData("{\"Amount\":1,\"Count\":\"Infinity\"}")]
        [InlineData("{\"Amount\":1,\"Count\":\"1e999\"}")]
        [InlineData("{\"Amount\":[],\"Count\":[[]]}")]
        public async Task Exhaustive_WrongTypedValues_Return400WithAPlainMessageAsync(string body)
        {
            var response = await WithExhaustiveInstanceAsync(guid => Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + guid, User, Bytes(body), Json));

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Exhaustive recall input has an invalid shape or types.\"", response.Body);
        }

        [Theory]
        [InlineData("[1,2]")]
        [InlineData("\"text\"")]
        [InlineData("123")]
        [InlineData("null")]
        [InlineData("{\"Amount\":")]
        [InlineData("")]
        public async Task Exhaustive_NotAJsonObject_Returns400WithAPlainMessageAsync(string body)
        {
            var response = await WithExhaustiveInstanceAsync(guid => Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + guid, User, Bytes(body), Json));

            Assert.Equal(400, response.Status);
            Assert.Equal("\"Malformed JSON in POST body.\"", response.Body);
            Assert.DoesNotContain("Newtonsoft", response.Body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Exhaustive_AnOutOfRangeNumber_Returns400AsAnInvalidShapeAsync()
        {
            var response = await WithExhaustiveInstanceAsync(guid => Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + guid, User, Bytes("{\"Amount\":1e999}"), Json));

            Assert.Equal(400, response.Status);
            Assert.Equal("\"Exhaustive recall input has an invalid shape or types.\"", response.Body);
        }

        [Fact]
        public async Task Exhaustive_DeeplyNestedBody_Returns400NotA500Async()
        {
            var body = new string('[', 5_000) + new string(']', 5_000);

            var response = await WithExhaustiveInstanceAsync(guid => Api.SendAsync("POST",
                "/api/Invoke/ExhaustiveSearchInstance/" + guid, User, Bytes(body), Json));

            Assert.Equal(400, response.Status);
        }

        [Theory]
        [InlineData("?timeout=0")]
        [InlineData("?timeout=1")]
        [InlineData("?timeout=-5")]
        public async Task Callback_ThatNeverArrives_Returns408Async(string query)
        {
            var response = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}{query}", User);

            Assert.Equal(408, response.Status);
            Assert.Empty(response.BodyBytes);
        }

        [Fact]
        public async Task Callback_TimeoutIsInSeconds_AndTheWaitIsCutShortByTheCacheAtCallbackTimeoutAsync()
        {
            var started = DateTime.UtcNow;

            var response = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=2", User);

            Assert.Equal(408, response.Status);
            Assert.InRange(DateTime.UtcNow - started, TimeSpan.FromMilliseconds(1500), TimeSpan.FromSeconds(6));

            started = DateTime.UtcNow;
            response = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=30000", User);

            Assert.Equal(408, response.Status);
            Assert.InRange(DateTime.UtcNow - started, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        }

        [Fact]
        public async Task Callback_ClientAbandoningTheRequest_CancelsTheServerSideWaitAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(400));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=30000", User,
                cancellationToken: cts.Token));

            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline && !fixture.Engine.Log.Entries.Any(e =>
                       e.Level == "ERROR" && e.Message.Contains("Callback Fetch") &&
                       e.Message.Contains("TaskCanceledException")))
            {
                await Task.Delay(100, CancellationToken.None);
            }

            Assert.Contains(fixture.Engine.Log.Entries, e => e.Level == "ERROR" &&
                                                             e.Message.Contains("Callback Fetch") &&
                                                             e.Message.Contains("TaskCanceledException"));

            Assert.Equal(200,
                (await Api.InvokeAsync(Model, Payloads.Example(), cancellationToken: CancellationToken.None)).Status);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Callback_OfAnotherTenantOrNoTenant_IsIndistinguishableFromAGuidThatNeverArrivedAsync(
            bool userWithoutTenant)
        {
            var unique = "cbt-" + Guid.NewGuid().ToString("N")[..12];
            var token = await Api.SendAsync("POST", ModelRoute + Model.ModelGuid + "/Async", User,
                Payloads.Example().With("TxnId", unique).ToBytes(), Json);
            Assert.Equal(200, token.Status);
            var entryGuid = token.String("entityAnalysisModelInstanceEntryGuid");
            var stranger = userWithoutTenant ? database.Seed.UserNoTenant : database.Seed.UserTenantB;

            var foreign = System.Diagnostics.Stopwatch.StartNew();
            var refused = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{entryGuid}?timeout=2", stranger);
            foreign.Stop();

            var unknown = System.Diagnostics.Stopwatch.StartNew();
            var missing = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=2", stranger);
            unknown.Stop();

            Assert.Equal(408, refused.Status);
            Assert.Equal(missing.Status, refused.Status);
            Assert.Empty(refused.BodyBytes);
            Assert.Equal(missing.ContentType, refused.ContentType);
            Assert.DoesNotContain(unique, refused.Body, StringComparison.Ordinal);
            Assert.True(foreign.Elapsed >= TimeSpan.FromMilliseconds(900),
                $"the refusal took {foreign.ElapsedMilliseconds} ms: an immediate answer reveals that the guid exists");
            output.WriteLine($"foreign {foreign.ElapsedMilliseconds} ms, unknown {unknown.ElapsedMilliseconds} ms");

            var owner = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{entryGuid}?timeout=20000", User);

            Assert.Equal(200, owner.Status);
            Assert.Equal(unique, owner.String("EntityInstanceEntryId"));
        }

        [Fact]
        public async Task Callback_GuidThatIsNotAGuid_Returns404Async()
        {
            var response = await Api.SendAsync("GET", "/api/Invoke/EntityAnalysisModel/Callback/nope", User);

            Assert.Equal(404, response.Status);
        }

        [Fact]
        public async Task Callback_Unauthenticated_Returns401Async()
        {
            var response = await Api.SendAsync("GET",
                $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=0");

            Assert.Equal(401, response.Status);
        }

        [Fact]
        public async Task Callback_GateOffAndEngineMissing_Return404And503Async()
        {
            var gateOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
                    { ["EnablePublicInvokeController"] = "False" }), fixture.Engine.Log, fixture.Engine.Engine);
            var noEngine = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);
            var path = $"/api/Invoke/EntityAnalysisModel/Callback/{Guid.NewGuid()}?timeout=0";

            Assert.Equal(404, (await WithHostAsync(gateOff, h => h.SendAsync("GET", path, User))).Status);
            Assert.Equal(503, (await WithHostAsync(noEngine, h => h.SendAsync("GET", path, User))).Status);
        }

        [Theory]
        [InlineData("?multiPartString=john%20smith")]
        [InlineData("?multiPartString=john%20smith&distance=2&maxDistanceRatio=0.5&maxCoverageRatio=0.7")]
        [InlineData("?multiPartString=a&multiPartString=b&distance=1&distance=2")]
        [InlineData("?multiPartString=a&distance=0&maxDistanceRatio=0&maxCoverageRatio=100")]
        [InlineData("?multiPartString=a&distance=50&maxDistanceRatio=1")]
        public async Task Sanction_AnswersCamelCaseJson_WhateverTheQueryHoldsAsync(string query)
        {
            var response = await Api.SendAsync("GET", "/api/Invoke/Sanction" + query, User);

            Assert.Equal(200, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal(["aggregations", "entries"], response.PropertyNames);
            Assert.NotNull(response.Find("aggregations", "total", "confidence").GetValueOrDefault().ToString());
            Assert.Equal(JsonValueKind.Array, response.Find("aggregations", "bySource")?.ValueKind);
            Assert.Equal(JsonValueKind.Array, response.Find("entries")?.ValueKind);
        }

        [Fact]
        public async Task Sanction_FindsAnEntryImportedAfterTheEngineStartedWithinTheChangePollAsync()
        {
            int sourceId;
            await using (var dbContext = database.GetDbContext())
            {
                sourceId = await dbContext.InsertWithInt32IdentityAsync(
                    new Data.Poco.SanctionEntrySource
                    {
                        Name = $"{DatabaseFixture.Prefix}InvokePoll{Guid.NewGuid():N}"[..40], Severity = 2,
                        Delimiter = ',', MultiPartStringIndex = "0", ReferenceIndex = 0,
                        EnableDirectoryLocation = 0, EnableHttpLocation = 0,
                        DirectoryLocation = "/tmp/does-not-matter", Skip = 0
                    });
            }

            try
            {
                const string phrase = "zzinvokepollquux zzinvokepollcorge";
                var before = await Api.SendAsync("GET",
                    "/api/Invoke/Sanction?multiPartString=" + Uri.EscapeDataString(phrase), User);
                Assert.DoesNotContain("REF-" + phrase, before.Body);

                await SanctionsChangePollTests.ImportAsync(database, sourceId,
                    phrase);

                var found = false;
                var deadline = DateTime.UtcNow.AddSeconds(30);
                while (!found && DateTime.UtcNow < deadline)
                {
                    var response = await Api.SendAsync("GET",
                        "/api/Invoke/Sanction?multiPartString=" + Uri.EscapeDataString(phrase), User);
                    Assert.Equal(200, response.Status);
                    found = response.Body.Contains("REF-" + phrase, StringComparison.Ordinal);
                    if (!found)
                    {
                        await Task.Delay(250);
                    }
                }

                Assert.True(found, "the imported entry must be searchable within the change poll");
            }
            finally
            {
                await using var dbContext = database.GetDbContext();
                await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntryImport.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
            }
        }

        [Theory]
        [InlineData("", "multipart string is required")]
        [InlineData("?multiPartString=", "multipart string is required")]
        [InlineData("?multiPartString=%20%20%09", "multipart string is required")]
        [InlineData("?distance=2", "multipart string is required")]
        [InlineData("?multiPartString=a&distance=x", "Distance must be")]
        [InlineData("?multiPartString=a&distance=-1", "Distance must be")]
        [InlineData("?multiPartString=a&distance=51", "Distance must be")]
        [InlineData("?multiPartString=a&distance=2147483648", "Distance must be")]
        [InlineData("?multiPartString=a&distance=1.5", "Distance must be")]
        [InlineData("?multiPartString=a&distance=1e2", "Distance must be")]
        [InlineData("?multiPartString=a&maxDistanceRatio=NaN", "Max distance ratio")]
        [InlineData("?multiPartString=a&maxDistanceRatio=Infinity", "Max distance ratio")]
        [InlineData("?multiPartString=a&maxDistanceRatio=-0.1", "Max distance ratio")]
        [InlineData("?multiPartString=a&maxDistanceRatio=1.01", "Max distance ratio")]
        [InlineData("?multiPartString=a&maxDistanceRatio=abc", "Max distance ratio")]
        [InlineData("?multiPartString=a&maxCoverageRatio=NaN", "Max coverage ratio")]
        [InlineData("?multiPartString=a&maxCoverageRatio=-Infinity", "Max coverage ratio")]
        [InlineData("?multiPartString=a&maxCoverageRatio=-1", "Max coverage ratio")]
        [InlineData("?multiPartString=a&maxCoverageRatio=100.5", "Max coverage ratio")]
        [InlineData("?multiPartString=a&maxCoverageRatio=1e999", "Max coverage ratio")]
        [InlineData("?multiPartString=a%00b", "null characters")]
        public async Task Sanction_InvalidInput_Returns400WithTheFluentValidationStructureAsync(string query,
            string expected)
        {
            var response = await Api.SendAsync("GET", "/api/Invoke/Sanction" + query, User);

            Assert.Equal(400, response.Status);
            var messages = response.Json.GetProperty("errors").EnumerateArray()
                .Select(e => e.GetProperty("errorMessage").GetString()).ToList();
            Assert.Contains(messages, m => m is not null && m.Contains(expected, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("Exception", response.Body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("   at ", response.Body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Sanction_MultiPartStringOverTheLimit_Returns400ButTheLimitItselfIsAcceptedAsync()
        {
            var atLimit = await Api.SendAsync("GET",
                "/api/Invoke/Sanction?multiPartString=" + new string('a', 500), User);
            var overLimit = await Api.SendAsync("GET",
                "/api/Invoke/Sanction?multiPartString=" + new string('a', 501), User);
            var huge = await Api.SendAsync("GET",
                "/api/Invoke/Sanction?multiPartString=" + new string('a', 20_000), User);

            Assert.Equal(200, atLimit.Status);
            Assert.Equal(400, overLimit.Status);
            Assert.Contains("500 characters", overLimit.Body);
            Assert.Contains(huge.Status, [400, 414]);
        }

        [Fact]
        public async Task Sanction_SeveralInvalidValues_ReportEveryErrorInOneResponseAsync()
        {
            var response = await Api.SendAsync("GET",
                "/api/Invoke/Sanction?multiPartString=&distance=99&maxDistanceRatio=5&maxCoverageRatio=-1", User);

            Assert.Equal(400, response.Status);
            Assert.Equal(4, response.Json.GetProperty("errors").GetArrayLength());
        }

        [Theory]
        [InlineData("' OR 1=1 --")]
        [InlineData("1; DROP TABLE \"SanctionEntry\"; --")]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("${7*7}{{7*7}}")]
        [InlineData("%s%n%x")]
        [InlineData("../../etc/passwd")]
        public async Task Sanction_HostileStrings_AreSearchedAsDataAnd200Async(string hostile)
        {
            var response = await Api.SendAsync("GET",
                "/api/Invoke/Sanction?distance=1&multiPartString=" + Uri.EscapeDataString(hostile), User);

            Assert.Equal(200, response.Status);
            Assert.Equal(["aggregations", "entries"], response.PropertyNames);
        }

        [Fact]
        public async Task Sanction_HostileNumbersAndStrings_AreNeverA500AndNeverLeakAsync()
        {
            var numbers = new[]
            {
                "-1", "0", "1", "50", "51", "2147483647", "2147483648", "-2147483649", "abc", "", "1e9", "NaN", "-NaN",
                "Infinity", "+Infinity", "0x10", "1.5", "1,5", "[]", "{}", "null", "true", "9999999999999999999999",
                "1' OR '1'='1", "1;SELECT pg_sleep(5)", "%00", "\u0660", "١٢", "1e-400", "-0", "+1", " 1 ", "0.0000001"
            };
            var queries = new List<string>();
            foreach (var value in numbers.Select(Uri.EscapeDataString))
            {
                queries.Add("?multiPartString=a&distance=" + value);
                queries.Add("?multiPartString=a&maxDistanceRatio=" + value);
                queries.Add("?multiPartString=a&maxCoverageRatio=" + value);
                queries.Add("?multiPartString=a&distance=" + value + "&distance=1&maxDistanceRatio=" + value);
            }

            foreach (var text in new[]
                     {
                         "\t", "\r\n", "\u0000", "\u202e", "\ufeff", "𝔍𝔬𝔥𝔫", new string('é', 500),
                         new string('é', 501), "%", "%zz", "a&b=c"
                     })
            {
                queries.Add("?multiPartString=" + Uri.EscapeDataString(text));
            }

            var started = DateTime.UtcNow;
            foreach (var query in queries)
            {
                var response = await Api.SendAsync("GET", "/api/Invoke/Sanction" + query, User);

                Assert.True(response.Status is 200 or 400, $"{query} answered {response.Status}");
                Assert.DoesNotContain("Npgsql", response.Body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("StackTrace", response.Body, StringComparison.OrdinalIgnoreCase);
            }

            Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(60), "no request may wait on injected SQL");
        }

        [Fact]
        public async Task Sanction_InvalidInputDoesNotTakeTheGatesOrTheEngineChecksAsync()
        {
            var gateOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
                    { ["EnablePublicInvokeController"] = "False" }), fixture.Engine.Log, fixture.Engine.Engine);
            var noEngine = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);

            Assert.Equal(404,
                (await WithHostAsync(gateOff, h => h.SendAsync("GET", "/api/Invoke/Sanction", User))).Status);
            Assert.Equal(503,
                (await WithHostAsync(noEngine, h => h.SendAsync("GET", "/api/Invoke/Sanction", User))).Status);
        }

        [Fact]
        public async Task Sanction_Unauthenticated_Returns401Async()
        {
            Assert.Equal(401, (await Api.SendAsync("GET", "/api/Invoke/Sanction")).Status);
        }

        [Fact]
        public async Task Sanction_GateOff_EngineOff_EngineMissing_Return404_404_503Async()
        {
            var gateOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
                    { ["EnablePublicInvokeController"] = "False" }), fixture.Engine.Log, fixture.Engine.Engine);
            var engineOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string> { ["EnableEngine"] = "False" }),
                fixture.Engine.Log, fixture.Engine.Engine);
            var noEngine = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);

            Assert.Equal(404,
                (await WithHostAsync(gateOff, h => h.SendAsync("GET", "/api/Invoke/Sanction", User))).Status);
            Assert.Equal(404,
                (await WithHostAsync(engineOff, h => h.SendAsync("GET", "/api/Invoke/Sanction", User))).Status);
            Assert.Equal(503,
                (await WithHostAsync(noEngine, h => h.SendAsync("GET", "/api/Invoke/Sanction", User))).Status);
        }

        [Fact]
        public async Task Sanction_WithThePostVerb_Returns405Async()
        {
            Assert.Equal(405, (await Api.SendAsync("POST", "/api/Invoke/Sanction", User, Bytes("{}"), Json)).Status);
        }

        private static byte[] TagBody(string casing = "camel")
        {
            var key = casing == "camel"
                ? "entityAnalysisModelInstanceEntryGuid"
                : "EntityAnalysisModelInstanceEntryGuid";
            return Bytes($"{{\"{key}\":\"{Guid.NewGuid()}\"}}");
        }

        [Theory]
        [InlineData("application/json")]
        [InlineData("application/json; charset=utf-8")]
        [InlineData("text/json")]
        [InlineData("application/hal+json")]
        public async Task Tag_WithAJsonMediaType_Returns200Async(string contentType)
        {
            var response = await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, TagBody(), contentType);

            Assert.Equal(200, response.Status);
            Assert.Empty(response.BodyBytes);
        }

        [Theory]
        [InlineData("camel")]
        [InlineData("pascal")]
        public async Task Tag_PropertyNamesAreMatchedIgnoringCaseAsync(string casing)
        {
            Assert.Equal(200,
                (await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, TagBody(casing), Json)).Status);
        }

        [Fact]
        public async Task Tag_EmptyObject_Returns200_ButJsonNullAndMalformedJson_Return400Async()
        {
            Assert.Equal(200, (await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, Bytes("{}"), Json)).Status);
            Assert.Equal(400,
                (await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, Bytes("null"), Json)).Status);
            Assert.Equal(400, (await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, Bytes("{no"), Json)).Status);
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("application/xml")]
        [InlineData(null)]
        public async Task Tag_WithoutAJsonMediaType_Returns415Async(string? contentType)
        {
            var response = await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", User,
                contentType == null ? null : Bytes("{}"), contentType);

            Assert.Equal(415, response.Status);
        }

        [Fact]
        public async Task Tag_Unauthenticated_Returns401Async()
        {
            Assert.Equal(401, (await Api.SendAsync("PUT", "/api/Invoke/Archive/Tag", null, Bytes("{}"), Json)).Status);
        }

        [Fact]
        public async Task Tag_GateOffAndEngineMissing_Return404And503_EvenForMalformedJsonAsync()
        {
            var gateOff = await ModelInvokeHost.StartAsync(
                ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
                    { ["EnablePublicInvokeController"] = "False" }), fixture.Engine.Log, fixture.Engine.Engine);
            var noEngine = await ModelInvokeHost.StartAsync(fixture.Engine.Environment, fixture.Engine.Log, null);

            Assert.Equal(404,
                (await WithHostAsync(gateOff,
                    h => h.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, Bytes("{no"), Json))).Status);
            Assert.Equal(503,
                (await WithHostAsync(noEngine,
                    h => h.SendAsync("PUT", "/api/Invoke/Archive/Tag", User, Bytes("{no"), Json))).Status);
        }

        [Fact]
        public async Task Invoke_ResponseCarriesTheActivationsAndElevationOfTheModelAsync()
        {
            var accountId = "ZzTestAccount" + Guid.NewGuid().ToString("N");

            var response = await Api.InvokeAsync(Model, Payloads.Example().With("AccountId", accountId));

            Assert.Equal(200, response.Status);
            Assert.NotEmpty(response.ActivatedRules);
            Assert.True(response.ResponseElevationValue > 0,
                "an activated rule with response elevation raises the elevation; activated: " +
                string.Join(",", response.ActivatedRules) + "; elevation: " + response.ResponseElevationValue);
            Assert.All(response.ActivatedRules,
                rule => Assert.True(response.Find("Activation", rule, "Visible") != null));
            Assert.Equal(accountId, response.PayloadField("AccountId"));
        }

        [Theory]
        [InlineData("AccountId")]
        [InlineData("TxnId")]
        [InlineData("CurrencyAmount")]
        public async Task Invoke_MissingFields_AreDefaultedByTheModel_NotRejectedAsync(string missing)
        {
            var response = await Api.InvokeAsync(Model, Payloads.Example().Without(missing));

            Assert.Equal(200, response.Status);
            Assert.Equal(Json, response.ContentType);
        }

        [Fact]
        public async Task Invoke_MissingTxnId_GivesAnEmptyEntryIdAsync()
        {
            var response = await Api.InvokeAsync(Model, Payloads.Example().Without("TxnId"));

            Assert.Equal(200, response.Status);
            Assert.Equal(string.Empty, response.String("EntityInstanceEntryId"));
        }

        [Theory]
        [InlineData("TxnDateTime", null)]
        [InlineData("TxnDateTime", "not-a-date")]
        public async Task Invoke_MissingOrUnreadableReferenceDate_DefaultsToNowAsync(string field, string? value)
        {
            var payload = value == null ? Payloads.Example().Without(field) : Payloads.Example().With(field, value);

            var response = await Api.InvokeAsync(Model, payload);

            Assert.Equal(200, response.Status);
            Assert.InRange(DateTime.UtcNow - DateTime.Parse(response.String("ReferenceDate") ?? string.Empty,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal), TimeSpan.Zero - TimeSpan.FromSeconds(5),
                TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task Invoke_ReferenceDateInTheFuture_Returns400WithTheMessageAsAJsonStringAsync()
        {
            var response = await Api.InvokeAsync(Model, Payloads.Example().With("TxnDateTime", "2999-01-01T00:00:00"));

            Assert.Equal(400, response.Status);
            Assert.Equal(JsonUtf8, response.ContentType);
            Assert.Equal("\"Reference Date can't be in the future.\"", response.Body);
        }

        [Theory]
        [InlineData("CurrencyAmount", "abc")]
        [InlineData("Unmapped", "extra")]
        public async Task Invoke_UnreadableOrUnmappedValues_DoNotFailTheInvocationAsync(string field, string value)
        {
            var response = await Api.InvokeAsync(Model, Payloads.Example().With(field, value));

            Assert.Equal(200, response.Status);
            Assert.Equal("0987654321", response.String("EntityInstanceEntryId"));
        }
    }
}