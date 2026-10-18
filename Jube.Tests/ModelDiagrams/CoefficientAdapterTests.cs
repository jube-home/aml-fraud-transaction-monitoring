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
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Service.ModelDiagrams;
using Jube.Service.ModelDiagrams.Adapters;
using Xunit;
using Jube.Service.ModelDiagrams.Exceptions;

namespace Jube.Test.ModelDiagrams
{
    public class CoefficientAdapterTests
    {
        private static CoefficientDiagramSpec Spec(string json, ModelDiagramOptions? options = null)
        {
            return ((CoefficientDiagram)ModelDiagramRenderer.Build(json, options)).Spec;
        }

        [Fact]
        public void LinearRegressionWeightsBecomePlainCoefficients()
        {
            var spec = Spec("{\"weights\":[2.0,-1.0,0.5],\"intercept\":0.25}",
                new ModelDiagramOptions { VariableNames = ["a", "b", "c"] });

            spec.Items.Select(i => (i.Name, i.Estimate)).Should().Equal(("a", 2.0), ("b", -1.0), ("c", 0.5));
            spec.ShowOddsRatios.Should().BeFalse();
            spec.EstimateName.Should().Be("Coefficient");
            spec.Options.Subtitle.Should().Contain("Intercept 0.25");
        }

        [Fact]
        public void TheRealLinearFixtureMapsItsWeights()
        {
            var model = ModelDiagramFixtures.Parsed("regression-linear");

            var spec = Spec(model.ToString());

            spec.Items.Select(i => i.Estimate).Should().Equal(model["weights"]!.Select(w => (double)w));
            spec.Options.Title.Should().Be("Linear regression coefficients");
        }

        [Fact]
        public void ALogisticModelIsRecognisedByItsLinkAndShowsOddsRatios()
        {
            var model = ModelDiagramFixtures.Parsed("regression-logistic");

            var spec = Spec(model.ToString());

            spec.Options.Title.Should().Be("Logistic regression coefficients");
            spec.ShowOddsRatios.Should().BeTrue();
            spec.Items.Select(i => i.Estimate).Should().Equal(model["weights"]!.Select(w => (double)w));
        }

        [Fact]
        public void ALogisticTypeMarkerAloneIsEnoughToLabelTheModelLogistic()
        {
            var spec = Spec(
                "{\"$type\":\"Accord.Statistics.Models.Regression.LogisticRegression, Accord.Statistics\",\"weights\":[1.0],\"intercept\":0}");

            spec.Options.Title.Should().Be("Logistic regression coefficients");
        }

        [Fact]
        public void StandardErrorsWithTheInterceptFirstAreAlignedToTheWeights()
        {
            var spec = Spec("{\"weights\":[1.0,2.0],\"intercept\":0,\"standardErrors\":[9.0,0.1,0.2],\"link\":{}}");

            spec.Items.Select(i => i.StandardError).Should().Equal(0.1, 0.2);
        }

        [Fact]
        public void StaleStandardErrorsOfTheWrongLengthAreIgnored()
        {
            var spec = Spec("{\"weights\":[1.0,2.0,3.0],\"intercept\":0,\"standardErrors\":[0.0,0.0],\"link\":{}}");

            spec.Items.Should().OnlyContain(i => i.StandardError == null);
        }

        [Fact]
        public void AllZeroStandardErrorsAreIgnored()
        {
            var spec = Spec("{\"weights\":[1.0,2.0],\"intercept\":0,\"standardErrors\":[0.0,0.0,0.0],\"link\":{}}");

            spec.Items.Should().OnlyContain(i => i.StandardError == null);
        }

        [Fact]
        public void ARealModelThatAnEnvelopeWrapsIsNotAlsoClaimedHere()
        {
            new LinearCoefficientsAdapter()
                .Detect(ModelDiagramFixtures.Parsed("snapshot-logistic-pipeline")).Should().BeNull();
        }

        [Fact]
        public void ANonFiniteWeightDoesNotVisualise()
        {
            Action act = () => ModelDiagramRenderer.Render("{\"weights\":[1.0,\"NaN\"],\"intercept\":0}");

            act.Should().Throw<ModelDiagramNotSupportedException>();
        }

        [Fact]
        public void MoreCoefficientsThanTheChartCanHoldIsTooLarge()
        {
            var weights = string.Join(",", Enumerable.Range(0, 2500).Select(i => (i % 7 - 3).ToString()));

            Action act = () => ModelDiagramRenderer.Render("{\"weights\":[" + weights + "],\"intercept\":0}",
                new ModelDiagramOptions { MaximumElements = 100_000 });

            act.Should().Throw<ModelDiagramNotSupportedException>()
                .WithMessage("*coefficients*maximum is 2000*summary*");
        }

        [Fact]
        public void TheElementCapAppliesToCoefficients()
        {
            var weights = string.Join(",", Enumerable.Range(0, 60).Select(_ => "1.5"));

            Action act = () => ModelDiagramRenderer.Render("{\"weights\":[" + weights + "],\"intercept\":0}",
                new ModelDiagramOptions { MaximumElements = 50 });

            act.Should().Throw<ModelDiagramNotSupportedException>().WithMessage("*60 coefficients*maximum is 50*");
        }

        [Fact]
        public void ManyCoefficientsWithinTheLimitsAreElidedToTheStrongest()
        {
            var weights = string.Join(",", Enumerable.Range(0, 300).Select(i => (i % 9 - 4).ToString()));

            var svg = ModelDiagramRenderer.Render("{\"weights\":[" + weights + "],\"intercept\":0}");

            svg.Should().StartWith("<svg ");
        }
    }
}