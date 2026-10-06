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
namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Logs
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using FluentAssertions;
    using global::Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Logs;
    using global::Jube.Engine.Helpers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class InvocationLogTests
    {
        private static JsonSerializer ArchiveSerializer => new JsonSerializationHelper().ArchiveJsonSerializer;

        private static JObject Serialize(object value)
        {
            using var stringWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(stringWriter);
            ArchiveSerializer.Serialize(jsonWriter, value);
            return JObject.Parse(stringWriter.ToString());
        }

        [Fact]
        public void AddEntryAppendsToEntries()
        {
            var log = new InvocationLog();
            var entry = new InvocationLogEntry { Message = "first" };

            log.AddEntry(entry);

            log.Entries.Should().ContainSingle().Which.Should().BeSameAs(entry);
        }

        [Fact]
        public void EveryAddedEntryIsPresentInTheSerialisedOutput()
        {
            var anchorDate = new DateTime(2026, 10, 7, 6, 32, 22, DateTimeKind.Utc);
            var log = new InvocationLog { AnchorDate = anchorDate };
            log.AddEntry(new InvocationLogEntry
            {
                ElapsedMicroseconds = 10,
                SinceLastEntryMicroseconds = 5,
                CapturedByInfoSampling = true,
                CapturedByWarnThreshold = false,
                ThreadId = 7,
                Message = "Entity Invoke: did the thing"
            });
            log.AddEntry(new InvocationLogEntry { Message = "second" });

            var json = Serialize(log);

            json["anchorDate"]!.Value<DateTime>().Should().Be(anchorDate);
            var entries = json["entries"].Should().BeOfType<JArray>().Subject;
            entries.Should().HaveCount(2);
            entries[0]["message"]!.Value<string>().Should().Be("Entity Invoke: did the thing");
            entries[0]["elapsedMicroseconds"]!.Value<long>().Should().Be(10);
            entries[0]["sinceLastEntryMicroseconds"]!.Value<long>().Should().Be(5);
            entries[0]["capturedByInfoSampling"]!.Value<bool>().Should().BeTrue();
            entries[0]["capturedByWarnThreshold"]!.Value<bool>().Should().BeFalse();
            entries[0]["threadId"]!.Value<int>().Should().Be(7);
            entries[1]["message"]!.Value<string>().Should().Be("second");
        }

        [Fact]
        public void AnEmptyLogStillSerialisesTheAnchorDateWithAnEmptyEntriesArray()
        {
            var log = new InvocationLog { AnchorDate = DateTime.UtcNow };

            var json = Serialize(log);

            json["entries"].Should().BeOfType<JArray>().Which.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(ModelTypes))]
        public void NoPropertyOnTheInvocationLogModelIsNonPublic(Type modelType)
        {
            var nonPublicProperties = modelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(property => property.GetMethod is null || !property.GetMethod.IsPublic)
                .Select(property => property.Name)
                .ToList();

            nonPublicProperties.Should().BeEmpty(
                $"every property on {modelType.Name} must be publicly readable or it silently disappears " +
                "from JSON serialisation");
        }

        public static TheoryData<Type> ModelTypes => new()
        {
            typeof(InvocationLog),
            typeof(InvocationLogEntry)
        };
    }
}
