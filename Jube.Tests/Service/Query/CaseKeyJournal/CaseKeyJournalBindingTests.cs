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

        private static async Task<int> StatusAsync(Delegate handler, string query)
        {
            var services = new ServiceCollection().AddLogging().BuildServiceProvider();
            var factory = RequestDelegateFactory.Create(handler, new RequestDelegateFactoryOptions
            {
                ServiceProvider = services
            });

            var context = new DefaultHttpContext { RequestServices = services };
            context.Request.Method = "GET";
            context.Request.QueryString = new QueryString(query);
            context.Response.Body = new System.IO.MemoryStream();

            await factory.RequestDelegate(context);

            return context.Response.StatusCode;
        }

        [Theory]
        [InlineData("")]
        [InlineData("&from=2026-07-26T00%3A00%3A00.000Z&lens=payload&limit=1000")]
        [InlineData("&activationsOnly=true&responseElevation=3&caseId=9&pinnedOnly=true")]
        [InlineData("&merge=%5B%7B%22key%22%3A%22Device%22%2C%22value%22%3A%22D-9%22%7D%5D")]
        public async Task TheRowsRequestBindsWithOnlyTheWorkflowTheKeyAndTheValueAsync(string extra)
        {
            var query = $"?caseWorkflowGuid={Workflow}&key=AccountId&value=Test5{extra}";

            (await StatusAsync(([AsParameters] CaseKeyJournalRequestDto request) => Results.Ok(request), query))
                .Should().Be(200);
            (await StatusAsync(([AsParameters] CaseKeyJournalAnalyticsRequestDto request) => Results.Ok(request),
                query)).Should().Be(200);
        }

        [Fact]
        public async Task TheTransactionRequestNeedsOnlyTheWorkflowAndTheIdAsync()
        {
            (await StatusAsync(([AsParameters] CaseKeyJournalTransactionRequestDto request) => Results.Ok(request),
                $"?caseWorkflowGuid={Workflow}&id=4")).Should().Be(200);
        }

        [Fact]
        public async Task ARequestWithoutTheWorkflowOrTheIdIsRefusedAtTheBoundaryAsync()
        {
            (await StatusAsync(([AsParameters] CaseKeyJournalTransactionRequestDto request) => Results.Ok(request),
                "?id=4")).Should().Be(400);
            (await StatusAsync(([AsParameters] CaseKeyJournalTransactionRequestDto request) => Results.Ok(request),
                $"?caseWorkflowGuid={Workflow}")).Should().Be(400);
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
                    .BeTrue($"{property.Name} would be required in the query string");
            }
        }
    }
}
