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

using FluentAssertions;
using Jube.Data.Poco;
using Jube.Engine.CaseEscalation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Jube.Test.CaseEscalation
{
    [Trait("Category", "Unit")]
    public sealed class CaseStatusNotifierTests
    {
        private static readonly JsonSerializer serializer = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private static readonly Jube.Data.Poco.Case caseModel = new()
        {
            Id = 7,
            CaseKey = "AccountId",
            CaseKeyValue = "ACC-1",
            Json = "{\"Amount\":5,\"Name\":\"Smith\"}"
        };

        private static readonly CaseWorkflowStatus status = new() { Name = "Review" };

        [Fact]
        public void ThePostBodyCarriesTheCaseItsPayloadAsContextAndTheStatusNameButNeverTheRawJson()
        {
            var body = JObject.Parse(CaseStatusNotifier.PreparePostBodyString(caseModel, serializer, status));

            body["id"]!.Value<int>().Should().Be(7);
            body["caseKey"]!.Value<string>().Should().Be("AccountId");
            body["caseKeyValue"]!.Value<string>().Should().Be("ACC-1");
            body["caseWorkflowStatus"]!.Value<string>().Should().Be("Review");
            body["context"]!["Amount"]!.Value<int>().Should().Be(5);
            body["context"]!["Name"]!.Value<string>().Should().Be("Smith");
            body.ContainsKey("json").Should().BeFalse();
        }
    }
}
