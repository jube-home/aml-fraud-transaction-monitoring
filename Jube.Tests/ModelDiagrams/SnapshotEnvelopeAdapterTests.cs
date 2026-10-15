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
using FluentAssertions;
using Jube.Charting.Interfaces;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams;
using Jube.Service.ModelDiagrams.Adapters;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Service.ModelDiagrams.Interfaces;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class SnapshotEnvelopeAdapterTests
    {
        private static JObject Pipeline(bool standardise = true, double[]? deviations = null)
        {
            return new JObject
            {
                ["kind"] = "LogisticRegressionPipeline",
                ["schemaVersion"] = 1,
                ["inputCount"] = 3,
                ["variablesInOrder"] = new JArray("amount", "velocity", "constant"),
                ["payload"] = new JObject
                {
                    ["standardise"] = standardise,
                    ["rejectMissing"] = false,
                    ["fillValues"] = new JArray(0.0, 0.0, 1.0),
                    ["means"] = new JArray(0.0, 0.0, 1.0),
                    ["standardDeviations"] = new JArray(deviations ?? [1.0, 2.0, 0.0]),
                    ["indicatorInputs"] = new JArray(1),
                    ["weights"] = new JArray(1.5, -0.5, 0.0, 0.25),
                    ["intercept"] = -2.0,
                    ["trainingPrior"] = 0.5,
                    ["baseRate"] = 0.02
                },
                ["calibrator"] = new JObject
                {
                    ["method"] = "Platt",
                    ["parameters"] = new JArray(1.25, -0.5, 0.0, 1e-6)
                }
            };
        }

        private static CoefficientDiagramSpec Spec(JObject model, ModelDiagramOptions? options = null)
        {
            return ((CoefficientDiagram)ModelDiagramRenderer.Build(model.ToString(), options)).Spec;
        }

        [Fact]
        public void AStandardisedPipelineShowsStandardisedLogOddsPerVariable()
        {
            var spec = Spec(Pipeline());

            spec.Items.Select(i => (i.Name, i.Estimate)).Should().Equal(("amount", 1.5), ("velocity", -0.5),
                ("velocity is missing", 0.25));

            spec.EstimateName.Should().Be("Standardised log-odds");
            spec.LogScale.Should().BeFalse();
            spec.ShowOddsRatios.Should().BeTrue();
            spec.Options.Description.Should().Contain("per standard deviation");
            spec.Options.Title.Should().Be("Logistic regression: standardised log-odds");
        }

        [Fact]
        public void ConstantVariablesAreOmittedAndCounted()
        {
            var spec = Spec(Pipeline());

            spec.Items.Should().NotContain(i => i.Name == "constant");
            spec.Options.Description.Should().Contain("1 constant variable omitted");
        }

        [Fact]
        public void AnUnstandardisedPipelineIsLabelledOriginalScaleAndKeepsEveryVariable()
        {
            var spec = Spec(Pipeline(false));

            spec.Items.Select(i => i.Name).Should().Equal("amount", "velocity", "constant", "velocity is missing");
            spec.EstimateName.Should().Be("Log-odds per unit");
            spec.Options.Description.Should().Contain("original scale");
        }

        [Fact]
        public void TheCaptionCarriesTheInterceptPriorsAndCalibrator()
        {
            var subtitle = Spec(Pipeline()).Options.Subtitle;

            subtitle.Should().Contain("Intercept -2").And.Contain("prior 0.5").And.Contain("base rate 0.02")
                .And.Contain("Calibrator Platt (slope 1.25, intercept -0.5)");
        }

        [Fact]
        public void AMissingCalibratorIsSaidSo()
        {
            var model = Pipeline();
            model["calibrator"] = null;

            Spec(model).Options.Subtitle.Should().Contain("No calibrator");
        }

        [Fact]
        public void AnUnknownCalibratorMethodIsNamedCustomNotEchoed()
        {
            var model = Pipeline();
            model["calibrator"] = new JObject { ["method"] = "<script>", ["parameters"] = new JArray(1.0) };

            Spec(model).Options.Subtitle.Should().Contain("Calibrator Custom").And.NotContain("<script>");
        }

        [Fact]
        public void PayloadStandardErrorsGiveIntervals()
        {
            var model = Pipeline();
            model["payload"]!["standardErrors"] = new JArray(0.1, 0.2, 0.0, 0.3);

            var spec = Spec(model);

            spec.Items.Select(i => i.StandardError).Should().Equal(0.1, 0.2, 0.3);
        }

        [Fact]
        public void EvidenceGivesOddsRatiosWithIntervalsOnALogScale()
        {
            var spec = Spec(ModelDiagramFixtures.Parsed("snapshot-logistic-pipeline-evidence"));

            spec.LogScale.Should().BeTrue();
            spec.EstimateName.Should().Be("Odds ratio");
            spec.Items.Select(i => i.Name).Should().Equal("amount", "velocity", "noise", "velocity__missing");
            var amount = spec.Items.First();
            amount.Estimate.Should().BeApproximately(3.866046, 1e-5);
            amount.Low.Should().BeApproximately(3.027200, 1e-5);
            amount.High.Should().BeApproximately(4.937338, 1e-5);
            amount.PValue.Should().BeLessThan(1e-20);
            spec.Options.Description.Should().Contain("Odds ratios with 95% intervals");
            spec.Options.Subtitle.Should().Contain("Calibrator Platt");
        }

        [Fact]
        public void EvidenceWithoutAnOddsRatioUsesTheExponentOfTheEstimate()
        {
            var model = Pipeline();

            model["evidence"] = new JObject
            {
                ["coefficients"] = new JArray(new JObject { ["name"] = "amount", ["estimate"] = Math.Log(2) })
            };

            var item = Spec(model).Items.Single();

            item.Estimate.Should().BeApproximately(2.0, 1e-12);
            item.Low.Should().BeNull();
        }

        [Fact]
        public void EvidenceRowsNeedANameAndAnEstimate()
        {
            var model = Pipeline();
            model["evidence"] = new JObject { ["coefficients"] = new JArray(new JObject { ["name"] = "amount" }) };

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Fact]
        public void VariableNamesFromTheOptionsRelabelThePipeline()
        {
            var spec = Spec(Pipeline(), new ModelDiagramOptions { VariableNames = ["a", "b", "c"] });

            spec.Items.Select(i => i.Name).Should().Equal("a", "b", "b is missing");
        }

        [Fact]
        public void AWeightCountThatDoesNotMatchTheVariablesIsMalformed()
        {
            var model = Pipeline();
            model["payload"]!["weights"] = new JArray(1.0, 2.0);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>().WithMessage("*2 weights for 3 variables*");
        }

        [Fact]
        public void AnIndicatorForAVariableThatDoesNotExistIsMalformed()
        {
            var model = Pipeline();
            model["payload"]!["indicatorInputs"] = new JArray(7);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotRecognisedException>();
        }

        [Fact]
        public void APipelineWithOnlyConstantVariablesDoesNotVisualise()
        {
            var model = Pipeline(true, [0.0, 0.0, 0.0]);
            model["payload"]!["indicatorInputs"] = new JArray();
            model["payload"]!["weights"] = new JArray(0.0, 0.0, 0.0);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*no fitted variables*");
        }

        [Fact]
        public void AnUnknownKindInAValidEnvelopeIsNotSupportedAndListsTheKnownKinds()
        {
            var model = Pipeline();
            model["kind"] = "GradientBoostedThing";

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            act.Should().Throw<ModelDiagramNotSupportedException>()
                .WithMessage("*GradientBoostedThing*LogisticRegression, LogisticRegressionPipeline*summary*");
        }

        [Fact]
        public void AHostileKindIsCleanedInTheMessage()
        {
            var model = Pipeline();
            model["kind"] = "bad\u0000kind\nwith control" + new string('x', 200);

            Action act = () => ModelDiagramRenderer.Render(model.ToString());

            var message = act.Should().Throw<ModelDiagramNotSupportedException>().Which.Message;

            message.Should().NotContain("\u0000").And
                .NotContain("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");
        }

        [Fact]
        public void KindMatchingIgnoresCase()
        {
            var model = Pipeline();
            model["kind"] = "logisticregressionpipeline";

            Spec(model).Items.Should().NotBeEmpty();
        }

        [Fact]
        public void TheOlderRawLogisticKindShowsLogOddsWithStandardErrors()
        {
            var model = new JObject
            {
                ["kind"] = "LogisticRegression",
                ["schemaVersion"] = 1,
                ["inputCount"] = 2,
                ["variablesInOrder"] = new JArray("amount", "velocity"),
                ["payload"] = new JObject
                {
                    ["weights"] = new JArray(0.8, -0.3),
                    ["intercept"] = 0.1,
                    ["standardErrors"] = new JArray(0.05, 0.2, 0.1)
                }
            };

            var spec = Spec(model);

            spec.Items.Select(i => (i.Name, i.Estimate, i.StandardError)).Should()
                .Equal(("amount", 0.8, 0.2), ("velocity", -0.3, 0.1));

            spec.Options.Subtitle.Should().Contain("Intercept 0.1");
        }

        [Fact]
        public void AFamilyAddsItsKindInOnePlace()
        {
            var adapter = new SnapshotEnvelopeAdapter(SnapshotKindAdapters.Default.Append(new EchoKind()));
            var model = Pipeline();
            model["kind"] = "Echo";

            var chart = adapter.Build(model, new ModelDiagramOptions());

            chart.Should().BeOfType<CoefficientDiagram>();
            adapter.Kinds.Should().Contain("Echo");
        }

        [Fact]
        public void ANonEnvelopeObjectIsNotClaimed()
        {
            new SnapshotEnvelopeAdapter(SnapshotKindAdapters.Default).Detect(JToken.Parse("{\"kind\":\"x\"}")).Should()
                .BeNull();
        }

        private sealed class EchoKind : ISnapshotKindAdapter
        {
            public string Kind => "Echo";

            public ISvgChart Build(JObject envelope, JObject payload, ModelDiagramOptions options)
            {
                return Diagram.Coefficients().Item("x", 1.0).Build();
            }
        }
    }
}