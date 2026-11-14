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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dto.Query.CaseKeyJournal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class CaseKeyJournalBindingTests
    {
        private const string Workflow = "2bd7b1b4-6a0b-4a3d-8b0a-0d2b5f8f1c11";

        private sealed class BodyPresent : Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature
        {
            public bool CanHaveBody => true;
        }

        private static async Task<(int Status, object? Bound)> BindAsync<T>(string json)
            where T : class
        {
            object? bound = null;

            var services = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = RequestDelegateFactory.Create(([FromBody] T request) =>
            {
                bound = request;

                return Results.Ok();
            }, new RequestDelegateFactoryOptions { ServiceProvider = services });

            var context = new DefaultHttpContext { RequestServices = services };
            context.Request.Method = "POST";
            context.Request.ContentType = "application/json";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            context.Request.Body = new System.IO.MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpRequestBodyDetectionFeature>(
                new BodyPresent());
            context.Response.Body = new System.IO.MemoryStream();

            await factory.RequestDelegate(context);

            return (context.Response.StatusCode, bound);
        }

        [Theory]
        [InlineData("")]
        [InlineData(",\"from\":\"2026-07-26T00:00:00.000Z\",\"lens\":\"payload\",\"limit\":1000")]
        [InlineData(",\"activationsOnly\":true,\"responseElevation\":3,\"caseId\":9,\"pinnedOnly\":true")]
        [InlineData(",\"merge\":\"[{\\\"key\\\":\\\"Device\\\",\\\"value\\\":\\\"D-9\\\"}]\"")]
        public async Task TheRowsRequestBindsFromABodyWithOnlyTheWorkflowTheKeyAndTheValueAsync(string extra)
        {
            var json = $"{{\"caseWorkflowGuid\":\"{Workflow}\",\"key\":\"AccountId\",\"value\":\"Test5\"{extra}}}";

            (await BindAsync<CaseKeyJournalRequestDto>(json)).Status.Should().Be(200);
            (await BindAsync<CaseKeyJournalAnalyticsRequestDto>(json)).Status.Should().Be(200);
        }

        [Fact]
        public async Task OmittedOptionsOfTheRowsRequestStayNullSoTheServiceDefaultsApplyAsync()
        {
            var (status, bound) = await BindAsync<CaseKeyJournalRequestDto>(
                $"{{\"caseWorkflowGuid\":\"{Workflow}\"}}");

            status.Should().Be(200);

            var request = bound.Should().BeOfType<CaseKeyJournalRequestDto>().Subject;

            request.CaseWorkflowGuid.Should().Be(Guid.Parse(Workflow));
            request.Limit.Should().BeNull();
            request.From.Should().BeNull();
            request.ActivationsOnly.Should().BeNull();
            request.Lens.Should().BeNull();
        }

        [Fact]
        public async Task TheTransactionRequestBindsFromTheWorkflowAndTheIdAsync()
        {
            var (status, bound) = await BindAsync<CaseKeyJournalTransactionRequestDto>(
                $"{{\"caseWorkflowGuid\":\"{Workflow}\",\"id\":4}}");

            status.Should().Be(200);
            bound.Should().BeOfType<CaseKeyJournalTransactionRequestDto>().Which.Id.Should().Be(4);
        }

        [Theory]
        [InlineData("{\"caseWorkflowGuid\":\"not-a-guid\",\"id\":4}")]
        [InlineData("{\"caseWorkflowGuid\":\"2bd7b1b4-6a0b-4a3d-8b0a-0d2b5f8f1c11\",\"id\":\"abc\"}")]
        [InlineData("{\"caseWorkflowGuid\":\"2bd7b1b4-6a0b-4a3d-8b0a-0d2b5f8f1c11\",\"id\":99999999999999999999}")]
        [InlineData("{\"caseWorkflowGuid\":\"2bd7b1b4-6a0b-4a3d-8b0a-0d2b5f8f1c11\",\"id\":[1]}")]
        [InlineData("[")]
        public async Task AWronglyTypedTransactionRequestIsRefusedAtTheBoundaryAsync(string json)
        {
            (await BindAsync<CaseKeyJournalTransactionRequestDto>(json)).Status.Should().Be(400);
        }

        [Theory]
        [InlineData("{\"limit\":\"many\"}")]
        [InlineData("{\"activationsOnly\":\"maybe\"}")]
        [InlineData("{\"responseElevation\":\"high\"}")]
        [InlineData("{\"from\":\"yesterday\"}")]
        public async Task AWronglyTypedRowsOptionIsRefusedAtTheBoundaryAsync(string json)
        {
            (await BindAsync<CaseKeyJournalRequestDto>(json)).Status.Should().Be(400);
        }

        [Fact]
        public void EveryOptionOfTheRowsRequestIsOptionalOnTheWire()
        {
            foreach (var property in typeof(CaseKeyJournalAnalyticsRequestDto).GetProperties())
            {
                if (property.Name is nameof(CaseKeyJournalRequestDto.CaseWorkflowGuid))
                {
                    continue;
                }

                var type = property.PropertyType;

                (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null).Should()
                    .BeTrue($"{property.Name} would be required in the body");
            }
        }
    }
}
