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
using System.Linq;
using System.Text;
using FluentAssertions;
using Jube.Charting.Interfaces;
using Jube.Service.ModelDiagrams;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Interfaces;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class ModelDiagramDetectionTests
    {
        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void EveryFixtureIsClaimedByExactlyOneAdapter(string name)
        {
            var model = JToken.Parse(ModelDiagramFixtures.Read(name));

            var claimants = ModelDiagramAdapters.Default.Where(a => a.Detect(model) is not null).Select(a => a.Name)
                .ToArray();

            claimants.Should().Equal(ModelDiagramFixtures.ExpectedAdapter(name));
        }

        [Theory]
        [MemberData(nameof(ModelDiagramFixtures.EveryFixtureData), MemberType = typeof(ModelDiagramFixtures))]
        public void TheRendererSelectsTheExpectedAdapter(string name)
        {
            ModelDiagramRenderer.DetectedAdapter(ModelDiagramFixtures.Read(name)).Should()
                .Be(ModelDiagramFixtures.ExpectedAdapter(name));
        }

        [Fact]
        public void EveryAdapterClaimsAtLeastOneFixture()
        {
            var claimed = ModelDiagramFixtures.Stored.Select(c => c.Adapter).Append("NetworkTopology").Distinct()
                .ToArray();

            ModelDiagramAdapters.Default.Select(a => a.Name).Should().BeEquivalentTo(claimed);
        }

        [Fact]
        public void AdapterNamesAreUnique()
        {
            ModelDiagramAdapters.Default.Select(a => a.Name).Should().OnlyHaveUniqueItems();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\t")]
        public void EmptyInputIsNotRecognised(string json)
        {
            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*empty*");
        }

        [Theory]
        [InlineData("not json at all")]
        [InlineData("{")]
        [InlineData("{\"layers\":[{\"neurons\":[")]
        [InlineData("[1,2,")]
        [InlineData("{\"a\":1}}")]
        [InlineData("{\"a\":1} trailing")]
        [InlineData("{\"a\":1,\"a\":2}")]
        [InlineData("<svg/>")]
        public void GarbageAndTruncatedJsonAreNotRecognised(string json)
        {
            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("42")]
        [InlineData("\"kind\"")]
        [InlineData("true")]
        [InlineData("{\"layers\":[]}")]
        [InlineData("{\"weights\":[],\"intercept\":1}")]
        [InlineData("{\"kind\":\"\",\"schemaVersion\":1,\"payload\":{}}")]
        [InlineData("{\"kind\":\"X\",\"payload\":{}}")]
        [InlineData("{\"kind\":\"X\",\"schemaVersion\":1,\"payload\":[]}")]
        public void ValidJsonThatIsNotAModelIsNotRecognised(string json)
        {
            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*not a serialised model*");
        }

        [Fact]
        public void TheTruncatedFixtureIsNotRecognised()
        {
            var json = ModelDiagramFixtures.Read("bayes-tan");

            Action act = () => ModelDiagramRenderer.Render(json[..(json.Length / 2)]);

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Fact]
        public void AHugelyNestedDocumentHitsTheDepthCap()
        {
            var json = new StringBuilder().Insert(0, "[", 100_000).Append(']', 100_000).ToString();

            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*nested deeper*");
        }

        [Fact]
        public void TheDepthCapIsConfigurable()
        {
            var json = "{\"a\":{\"b\":{\"c\":{}}}}";

            Action act = () => ModelDiagramRenderer.Render(json, new ModelDiagramOptions { MaximumJsonDepth = 2 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*nested deeper than 2*");
        }

        [Fact]
        public void ADocumentOverTheSizeCapIsNotSupported()
        {
            var json = "{\"padding\":\"" + new string('x', 5000) + "\"}";

            Action act = () =>
                ModelDiagramRenderer.Render(json, new ModelDiagramOptions { MaximumJsonCharacters = 1000 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*maximum is 1000*").And.Message
                .Should()
                .Contain("summary");
        }

        [Fact]
        public void TheDefaultSizeCapAppliesBeforeParsing()
        {
            var json = new string(' ', ModelDiagramOptions.DefaultMaximumJsonCharacters + 1) + "{}";

            Action act = () => ModelDiagramRenderer.Render(json);

            act.Should().Throw<ModelDiagramNotSupportedException>();
        }

        [Fact]
        public void ARealModelJustUnderACustomCapRenders()
        {
            var json = ModelDiagramFixtures.Read("bayes-tan");

            var svg = ModelDiagramRenderer.Render(json,
                new ModelDiagramOptions { MaximumJsonCharacters = json.Length });

            svg.Should().StartWith("<svg ");
        }

        [Fact]
        public void NullJsonIsAnArgumentError()
        {
            Action act = () => ModelDiagramRenderer.Render(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TwoAdaptersClaimingTheSameModelEquallyIsAnError()
        {
            var adapters = new IModelDiagramAdapter[]
                { new FixedAdapter("First", 0.8), new FixedAdapter("Second", 0.8) };

            Action act = () => ModelDiagramRenderer.Render("{}", null, adapters);

            act.Should().Throw<ModelDiagramAmbiguousException>().Which.Adapters.Should().Equal("First", "Second");
        }

        [Fact]
        public void ThePreferredAdapterWinsWhenConfidencesDiffer()
        {
            var adapters = new IModelDiagramAdapter[]
                { new FixedAdapter("Weak", 0.5), new FixedAdapter("Strong", 0.9) };

            ModelDiagramRenderer.Select(JToken.Parse("{}"), adapters).Name.Should().Be("Strong");
        }

        [Fact]
        public void AnAdapterThatReturnsNothingOrZeroDoesNotClaim()
        {
            var adapters = new IModelDiagramAdapter[] { new FixedAdapter("None", null), new FixedAdapter("Zero", 0) };

            Action act = () => ModelDiagramRenderer.Select(JToken.Parse("{}"), adapters);

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Fact]
        public void AMalformedModelThatAnAdapterClaimedIsReportedAsMalformed()
        {
            var json = ModelDiagramFixtures.Read("bayes-tan");
            var model = JObject.Parse(json);
            model["parents"] = new JArray(-1, 0, 1);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*inconsistent*");
        }

        [Fact]
        public void PropertyNamesAreMatchedWithoutRegardToCase()
        {
            var pascal = ModelDiagramFixtures.Parsed("bayes-tan");

            var renamed = new JObject(pascal.Properties().Select(p =>
                new JProperty(char.ToUpperInvariant(p.Name[0]) + p.Name[1..], p.Value)));

            ModelDiagramRenderer.DetectedAdapter(renamed.ToString()).Should().Be("TreeAugmentedNaiveBayes");
        }

        [Fact]
        public void NonFiniteNumbersWrittenAsStringsAreUnderstood()
        {
            var model = ModelDiagramFixtures.Parsed("topology:Sigmoid2");
            var neuron = (JObject)model["layers"]![1]!["neurons"]![0]!;
            ((JArray)neuron["weights"]!)[0] = "NaN";

            var svg = ModelDiagramRenderer.Render(model.ToString());

            svg.Should().StartWith("<svg ");
        }

        private sealed class FixedAdapter : IModelDiagramAdapter
        {
            private readonly double? confidence;

            public FixedAdapter(string name, double? confidence)
            {
                Name = name;
                this.confidence = confidence;
            }

            public string Name { get; }

            public double? Detect(JToken model)
            {
                return confidence;
            }

            public ISvgChart Build(JToken model, ModelDiagramOptions options)
            {
                throw new NotSupportedException();
            }
        }
    }
}