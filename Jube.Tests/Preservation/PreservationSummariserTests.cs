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


using System.Linq;
using FluentAssertions;
using Jube.Preservation.Diff;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Unit")]
    public sealed class PreservationSummariserTests
    {
        private const string Payload = """
            {
              "EntityAnalysisModel": [
                { "Name": "M1",
                  "EntityAnalysisModelTag": [ { "Name": "t" } ],
                  "EntityAnalysisModelList": [ { "Name": "L", "EntityAnalysisModelListValue": [ { "ListValue": "a" }, { "ListValue": "b" } ] } ],
                  "EntityAnalysisModelDictionary": [ { "Name": "D", "EntityAnalysisModelDictionaryKvp": [ { "KvpKey": "k" } ] } ],
                  "ExhaustiveSearchInstance": [ { "Name": "E" } ] }
              ],
              "VisualisationRegistry": [ { "Name": "V", "VisualisationRegistryDatasource": [ { "Name": "d" } ] } ],
              "RoleRegistry": [ { "Name": "R", "RoleRegistryPermission": [ { "PermissionSpecificationId": 1 } ] } ],
              "EntityApproval": [ { "EntityId": 1 } ]
            }
            """;

        private static Jube.Preservation.Models.SummarySection Section(string key)
        {
            return PreservationSummariser.Summarise(JObject.Parse(Payload)).Single(s => s.Key == key);
        }

        [Fact]
        public void EverySectionIsAlwaysPresent()
        {
            PreservationSummariser.Summarise([]).Select(s => s.Key).Should().Equal("models", "visualisations",
                "roles", "lists", "dictionaries", "exhaustive", "advancedAnalytics", "approvals");
        }

        [Fact]
        public void ListsCountTheListsAndTheirValues()
        {
            Section("lists").Count.Should().Be(1);
            Section("lists").Rows.Should().Be(2);
        }

        [Fact]
        public void DictionariesCountTheDictionariesAndTheirPairs()
        {
            Section("dictionaries").Count.Should().Be(1);
            Section("dictionaries").Rows.Should().Be(1);
        }

        [Fact]
        public void ModelsDoNotCountWhatTheOtherSwitchesCarry()
        {
            Section("models").Count.Should().Be(1);
            Section("models").Rows.Should().Be(1);
        }

        [Fact]
        public void VisualisationsAndRolesCountTheirChildren()
        {
            Section("visualisations").Rows.Should().Be(1);
            Section("roles").Rows.Should().Be(1);
        }

        [Fact]
        public void ExhaustiveCountsTheInstancesNestedUnderModels()
        {
            Section("exhaustive").Count.Should().Be(1);
        }

        [Fact]
        public void EverySectionCarriesItsSizeInBytes()
        {
            Section("lists").Bytes.Should().BeGreaterThan(0);
            Section("models").Bytes.Should().BeGreaterThan(0);
            Section("advancedAnalytics").Bytes.Should().Be(0);
        }

        [Fact]
        public void ApprovalsAreCountedButNotNested()
        {
            Section("approvals").Count.Should().Be(1);
            Section("approvals").Rows.Should().Be(0);
        }
    }
}
