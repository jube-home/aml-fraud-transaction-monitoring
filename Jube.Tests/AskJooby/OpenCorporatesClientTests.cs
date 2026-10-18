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

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability.Interfaces;
using Jube.Service.AskJoobyOpenCorporates;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class OpenCorporatesClientTests
    {
        private const string Endpoint = "https://api.opencorporates.test/v0.4";

        private static OpenCorporatesClient Client(HttpStatusCode status, string body, out CapturingHandler handler)
        {
            handler = new CapturingHandler(status, body);

            return new OpenCorporatesClient(new HttpClient(handler), Endpoint, "test-key");
        }

        [Fact]
        public async Task SearchCompanySendsTheQueryAndApiTokenAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"total_count":0,"companies":[]}}""",
                out var handler);

            await client.SearchCompanyAsync("jube ltd", 5);

            handler.LastRequest!.RequestUri!.Query.Should().Contain("q=jube%20ltd");
            handler.LastRequest!.RequestUri!.Query.Should().Contain("per_page=5");
            handler.LastRequest!.RequestUri!.Query.Should().Contain("api_token=test-key");
        }

        [Fact]
        public async Task SearchCompanyParsesTheResultsAsync()
        {
            const string body = """
                                {
                                  "results": {
                                    "total_count": 1,
                                    "companies": [
                                      {
                                        "company": {
                                          "name": "Jube Ltd",
                                          "company_number": "12345678",
                                          "jurisdiction_code": "gb",
                                          "company_type": "Private Limited Company",
                                          "current_status": "Active",
                                          "incorporation_date": "2020-01-01",
                                          "registered_address_in_full": "1 Example Street, London",
                                          "previous_names": [{"company_name": "Jube Holdings"}],
                                          "source": {
                                            "publisher": "Companies House",
                                            "url": "https://find-and-update.company-information.service.gov.uk",
                                            "retrieved_at": "2024-01-01"
                                          }
                                        }
                                      }
                                    ]
                                  }
                                }
                                """;

            var client = Client(HttpStatusCode.OK, body, out _);

            var response = await client.SearchCompanyAsync("jube", 5);

            response.TotalCount.Should().Be(1);
            response.Companies.Should().ContainSingle();

            var company = response.Companies[0];

            company.Name.Should().Be("Jube Ltd");
            company.CompanyNumber.Should().Be("12345678");
            company.JurisdictionCode.Should().Be("gb");
            company.CurrentStatus.Should().Be("Active");
            company.RegisteredAddressInFull.Should().Be("1 Example Street, London");
            company.PreviousNames.Should().ContainSingle().Which.Should().Be("Jube Holdings");
            company.Source!.Publisher.Should().Be("Companies House");
        }

        [Fact]
        public async Task SearchCompanyCarriesTheRawResponseVerbatimAsync()
        {
            const string body = """{"results":{"total_count":0,"companies":[]}}""";
            var client = Client(HttpStatusCode.OK, body, out _);

            var response = await client.SearchCompanyAsync("jube", 5);

            response.RawJson.Should().Be(body);
        }

        [Fact]
        public async Task GetCompanySendsTheJurisdictionAndCompanyNumberInThePathAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"company":{"name":"Jube Ltd"}}}""",
                out var handler);

            await client.GetCompanyAsync("gb", "12345678");

            handler.LastRequest!.RequestUri!.AbsolutePath.Should().EndWith("/companies/gb/12345678");
        }

        [Fact]
        public async Task GetCompanyParsesTheCompanyAsync()
        {
            const string body = """
                                {"results":{"company":{"name":"Jube Ltd","company_number":"12345678",
                                "jurisdiction_code":"gb"}}}
                                """;

            var client = Client(HttpStatusCode.OK, body, out _);

            var response = await client.GetCompanyAsync("gb", "12345678");

            response.Company.Should().NotBeNull();
            response.Company!.Name.Should().Be("Jube Ltd");
        }

        [Fact]
        public async Task GetCompanyReturnsNullRatherThanThrowingOnNotFoundAsync()
        {
            var client = Client(HttpStatusCode.NotFound, """{"error":"not found"}""", out _);

            var response = await client.GetCompanyAsync("gb", "00000000");

            response.Company.Should().BeNull();
            response.RawJson.Should().Be("""{"error":"not found"}""");
        }

        [Theory]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        public async Task SearchCompanyThrowsWithTheStatusCodeOnEveryDocumentedErrorAsync(HttpStatusCode status)
        {
            var client = Client(status, """{"error":"denied"}""", out _);

            var act = () => client.SearchCompanyAsync("jube", 5);

            (await act.Should().ThrowAsync<HttpRequestException>())
                .Which.Message.Should().Contain(((int)status).ToString());
        }

        [Fact]
        public async Task SearchBeneficialOwnersSendsTheControlledEntityNameAndUboFilterAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"control_statements":[]}}""", out var handler);

            await client.SearchBeneficialOwnersAsync("Jube Ltd", "Jane Doe");

            var query = handler.LastRequest!.RequestUri!.Query;

            query.Should().Contain("controlled_entity_name=Jube%20Ltd");
            query.Should().Contain("is_ultimate_beneficial_owner=true");
            query.Should().Contain("controlling_entities_name=Jane%20Doe");
        }

        [Fact]
        public async Task SearchBeneficialOwnersOmitsTheOwnerFilterWhenNoneIsGivenAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"control_statements":[]}}""", out var handler);

            await client.SearchBeneficialOwnersAsync("Jube Ltd", null);

            handler.LastRequest!.RequestUri!.Query.Should().NotContain("controlling_entities_name");
        }

        [Fact]
        public async Task SearchBeneficialOwnersParsesEachStatementAsync()
        {
            const string body = """
                                {
                                  "results": {
                                    "control_statements": [
                                      {
                                        "properties": {
                                          "controlling_entities_name": "Jane Doe",
                                          "controlling_entities_entity_type": "person",
                                          "controlled_entity_name": "Jube Ltd",
                                          "is_ultimate_beneficial_owner": true,
                                          "started_date": "2020-01-01"
                                        },
                                        "provenances": [
                                          {"source_url": "https://example.test", "confidence": 0.9,
                                           "source_type": "official_register"}
                                        ]
                                      }
                                    ]
                                  }
                                }
                                """;

            var client = Client(HttpStatusCode.OK, body, out _);

            var response = await client.SearchBeneficialOwnersAsync("Jube Ltd", null);

            response.Statements.Should().ContainSingle();

            var statement = response.Statements[0];

            statement.ControllingEntityName.Should().Be("Jane Doe");
            statement.ControllingEntityType.Should().Be("person");
            statement.ControlledEntityName.Should().Be("Jube Ltd");
            statement.IsUltimateBeneficialOwner.Should().BeTrue();
            statement.StartDate.Should().Be("2020-01-01");
            statement.Provenances.Should().ContainSingle().Which.SourceUrl.Should().Be("https://example.test");
        }

        [Fact]
        public async Task SearchBeneficialOwnersParsesCleanlyWhenNoStatementsComeBackAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"control_statements":[]}}""", out _);

            var response = await client.SearchBeneficialOwnersAsync("Jube Ltd", null);

            response.Statements.Should().BeEmpty();
        }

        [Fact]
        public async Task RecordsEachOperationAgainstTheGivenRunWhenARecorderIsSuppliedAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"total_count":0,"companies":[]}}""", out _);
            var recorder = new RecordingRunRecorder();

            await client.SearchCompanyAsync("jube", 5, recorder, "42");

            recorder.RunId.Should().Be("42");
            recorder.Operation.Should().Be("SearchCompany");
            recorder.Query.Should().Be("jube");
        }

        [Fact]
        public Task RecordsNothingWhenNoRecorderIsSuppliedAsync()
        {
            var client = Client(HttpStatusCode.OK, """{"results":{"total_count":0,"companies":[]}}""", out _);

            var act = () => client.SearchCompanyAsync("jube", 5);

            return act.Should().NotThrowAsync();
        }

        private sealed class RecordingRunRecorder : IRunRecorder
        {
            public string? RunId { get; private set; }
            public string? Operation { get; private set; }
            public string? Query { get; private set; }

            public Task<string> StartRunAsync(string agentType, int depth, string? parentRunId, string task,
                CancellationToken token = default)
            {
                return Task.FromResult(string.Empty);
            }

            public Task CompleteRunAsync(string runId, string status, string? result, long durationMs,
                CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordToolCallAsync(string runId, string toolName, string? argumentsJson, string? result,
                string status, long durationMs, CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordModelCallAsync(string runId, string model, int? inputTokens, int? outputTokens,
                long? cachedInputTokens, string? promptPrefixHash, string? usageCountsJson,
                long? promptDetailCachedTokens, string? finishReason, long durationMs, long? firstChunkMs,
                CancellationToken token = default)
            {
                return Task.CompletedTask;
            }

            public Task RecordOpenCorporatesCallAsync(string runId, string operation, string query,
                string? responseJson, long durationMs, CancellationToken token = default)
            {
                RunId = runId;
                Operation = operation;
                Query = query;

                return Task.CompletedTask;
            }
        }

        private sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;

                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}