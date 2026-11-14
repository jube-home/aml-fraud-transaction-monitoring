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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.GraphRecallProtocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Unit")]
    public sealed class GraphEntityTests
    {
        [Fact]
        public void AFreshEntityCarriesTypedDefaultsWithDistancesBeyondTheCap()
        {
            var entity = new GraphEntity();

            entity.Known.Should().BeFalse();
            entity.Value.Should().BeEmpty();
            entity.Hub.Should().BeFalse();
            entity.Degree.Should().Be(0);
            entity.DistanceToConfirmed.Should().Be(5);
            entity.DistanceToNotConfirmed.Should().Be(5);
            entity.RelaxedDistanceToConfirmed.Should().Be(5);
            entity.NearestTag.Should().BeEmpty();
            entity.NearestTagDistance.Should().Be(5);
            entity.CommunityId.Should().Be(0);
            entity.Edge.Exists.Should().BeFalse();
            entity.Edge.Kind.Should().BeEmpty();
            entity.Tags.Should().BeEmpty();
            entity.Patterns.Should().BeEmpty();
            entity.Tag("Mule").Known.Should().BeFalse();
            entity.Tag("Mule").Distance.Should().Be(5);
            entity.Tag(null!).Distance.Should().Be(5);
            entity.Pattern("FanOut").Should().BeFalse();
            entity.Pattern(null!).Should().BeFalse();
            entity.ReportTable.Should().BeTrue();
            new GraphEntity().Beyond.Should().Be(GraphEntity.DefaultBeyond);
        }

        [Fact]
        public void TagsTrackTheNearestAndPatternsAreMembershipByName()
        {
            var entity = new GraphEntity();
            entity.AddTag("Mule", new GraphTagProximity(5) { Distance = 2, Within2 = 1, Known = true });
            entity.AddTag("Fraud", new GraphTagProximity(5) { Distance = 1, Within1 = 1, Known = true });
            entity.AddTag("Far", new GraphTagProximity(5) { Distance = 4, Known = true });
            entity.AddTag(string.Empty, new GraphTagProximity(5));
            entity.AddPattern("FanOut");
            entity.AddPattern(null!);

            entity.NearestTag.Should().Be("Fraud");
            entity.NearestTagDistance.Should().Be(1);
            entity.Tag("Mule").Distance.Should().Be(2);
            entity.Tag("Mule").Within2.Should().Be(1);
            entity.Tag("mule").Known.Should().BeFalse();
            entity.Tags.Keys.Should().Equal("Far", "Fraud", "Mule");
            entity.Pattern("FanOut").Should().BeTrue();
            entity.Patterns.Should().Equal("FanOut");
        }

        [Fact]
        public void TheMemberCatalogueReadsWritesAndNamesEveryScalar()
        {
            var entity = new GraphEntity
            {
                Degree = 7, PropagationScore = 0.25, Hub = true, Value = "A1", CommunityId = 9000000000,
                Edge = { Count = 3 }
            };

            GraphEntityMembers.TryGet("degree", out var degree).Should().BeTrue();
            degree!.Read(entity).Should().Be(7);
            degree.DataTypeId.Should().Be(GraphEntityMembers.IntegerType);
            degree.JQueryBuilderDataType.Should().Be("integer");
            degree.JsonPath.Should().Be("degree");
            degree.SqlPath("AccountId").Should().Be("(\"Json\"-> 'graph' -> 'AccountId' ->> 'degree')::int");

            GraphEntityMembers.TryGet("Edge.Count", out var edgeCount).Should().BeTrue();
            edgeCount!.JsonPath.Should().Be("edge.count");
            edgeCount.SqlPath("AccountId").Should()
                .Be("(\"Json\"-> 'graph' -> 'AccountId' -> 'edge' ->> 'count')::int");
            edgeCount.Read(entity).Should().Be(3);

            GraphEntityMembers.TryGet("CommunityId", out var community).Should().BeTrue();
            community!.SqlPath("A").Should().EndWith("::bigint");
            GraphEntityMembers.TryGet("Value", out var value).Should().BeTrue();
            value!.JQueryBuilderDataType.Should().Be("string");
            value.SqlPath("A").Should().Be("(\"Json\"-> 'graph' -> 'A' ->> 'value')");
            GraphEntityMembers.TryGet("Nope", out _).Should().BeFalse();
            GraphEntityMembers.IsMember("Tag").Should().BeTrue();
            GraphEntityMembers.IsMember("pattern").Should().BeTrue();
            GraphEntityMembers.IsMember("Edge.Nope").Should().BeFalse();
            GraphEntityMembers.Tokens.Should().Contain(["Tag", "Pattern", "Edge", "Distance", "Within3", "DistanceToConfirmed", "Kind"]);
            GraphEntityMembers.Scalars.Select(s => s.Name).Should().OnlyHaveUniqueItems();

            var graph = new PooledDictionary<string, GraphEntity> { ["AccountId"] = entity };

            GraphEntityMembers.TryReadNumber(graph, "AccountId.Degree", out var number).Should().BeTrue();
            number.Should().Be(7);
            GraphEntityMembers.TryReadNumber(graph, "AccountId.Hub", out number).Should().BeTrue();
            number.Should().Be(1);
            GraphEntityMembers.TryReadNumber(graph, "AccountId.Value", out _).Should().BeFalse();
            GraphEntityMembers.TryReadNumber(graph, "Other.Degree", out _).Should().BeFalse();
            GraphEntityMembers.TryReadNumber(graph, "AccountId", out _).Should().BeFalse();
            GraphEntityMembers.TryReadText(graph, "AccountId.PropagationScore", out var text).Should().BeTrue();
            text.Should().Be("0.25");
            GraphEntityMembers.TryReadText(graph, "AccountId.Hub", out text).Should().BeTrue();
            text.Should().Be("true");
            GraphEntityMembers.TryReadText(null!, "AccountId.Hub", out _).Should().BeFalse();

            GraphEntityMembers.TryWrite(graph, "ToAccountId.DistanceToConfirmed", 2).Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.Hub", "1").Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.Known", true).Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.PageRank", "0.5").Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.Edge.Kind", "MoneyFlow").Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.CommunityId", 12.0).Should().BeTrue();
            GraphEntityMembers.TryWrite(graph, "ToAccountId.Nope", 1).Should().BeFalse();
            GraphEntityMembers.TryWrite(graph, "Nope", 1).Should().BeFalse();

            var written = graph["ToAccountId"];
            written.DistanceToConfirmed.Should().Be(2);
            written.Hub.Should().BeTrue();
            written.Known.Should().BeTrue();
            written.PageRank.Should().Be(0.5);
            written.Edge.Kind.Should().Be("MoneyFlow");
            written.CommunityId.Should().Be(12);
            written.DistanceToNotConfirmed.Should().Be(GraphEntity.DefaultBeyond);
            GraphEntityMembers.CamelCase("DistanceToConfirmed").Should().Be("distanceToConfirmed");
            GraphEntityMembers.CamelCase("").Should().BeEmpty();
        }

        [Fact]
        public void TheArchiveConverterWritesCamelCaseMembersAndSkipsEntitiesNotFlaggedForTheReportTable()
        {
            var shown = new GraphEntity { Value = "A1", Known = true, DistanceToConfirmed = 2 };
            shown.AddTag("Mule", new GraphTagProximity(5) { Distance = 1, Known = true });
            shown.AddPattern("FanOut");
            shown.Edge.Exists = true;
            shown.Edge.Count = 4;

            var graph = new PooledDictionary<string, GraphEntity>
            {
                ["AccountId"] = shown,
                ["ToAccountId"] = new() { ReportTable = false },
                ["Empty"] = null!
            };

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
                Converters = [new GraphArchiveJsonConverter()]
            };

            var json = JObject.Parse(JsonConvert.SerializeObject(new Dictionary<string, object> { ["graph"] = graph }, settings));

            var entities = (JObject)json["graph"]!;
            entities.Properties().Select(p => p.Name).Should().Equal("AccountId");
            entities["AccountId"]!["distanceToConfirmed"]!.Value<int>().Should().Be(2);
            entities["AccountId"]!["value"]!.Value<string>().Should().Be("A1");
            entities["AccountId"]!["edge"]!["count"]!.Value<int>().Should().Be(4);
            entities["AccountId"]!["tags"]!["Mule"]!["distance"]!.Value<int>().Should().Be(1);
            entities["AccountId"]!["patterns"]!.Values<string>().Should().Equal("FanOut");
            entities["AccountId"]!["reportTable"].Should().BeNull();
            entities["AccountId"]!["beyond"].Should().BeNull();
            new GraphArchiveJsonConverter().CanRead.Should().BeFalse();

            JsonConvert.SerializeObject(new Dictionary<string, object> { ["graph"] = null! }, settings)
                .Should().Be("{\"graph\":null}");
        }
    }
}
