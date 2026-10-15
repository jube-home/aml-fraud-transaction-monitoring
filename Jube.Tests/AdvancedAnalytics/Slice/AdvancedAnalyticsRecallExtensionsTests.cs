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
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Extensions;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Service.Invoke;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    [Trait("Category", "Unit")]
    public sealed class AdvancedAnalyticsRecallExtensionsTests
    {
        private static IReadOnlyList<AdvancedAnalyticsModelVariable> Variables(params string[] names)
        {
            var list = new List<AdvancedAnalyticsModelVariable>();

            foreach (var name in names)
            {
                list.Add(new AdvancedAnalyticsModelVariable { Name = name, ProcessingTypeId = 1 });
            }

            return list;
        }

        private static AdvancedAnalyticsModel Model(params string[] names)
        {
            var model = new AdvancedAnalyticsModel { Name = "M", Kind = "Test" };
            model.VariablesInOrder.AddRange(Variables(names));

            return model;
        }

        [Fact]
        public void BuildScoreInputs_ReadsEachVariableByNameInOrder()
        {
            var variables = Variables("Amount", "Count");
            var body = JObject.Parse("{\"Amount\": 12.5, \"Count\": 3}");

            AdvancedAnalyticsRecallExtensions.BuildScoreInputs(variables, body).Should().Equal(12.5, 3);
        }

        [Fact]
        public void BuildScoreInputs_UsesNanForAMissingVariable()
        {
            var variables = Variables("Amount", "Count");
            var body = JObject.Parse("{\"Amount\": 12.5}");

            var inputs = AdvancedAnalyticsRecallExtensions.BuildScoreInputs(variables, body);

            inputs[0].Should().Be(12.5);
            double.IsNaN(inputs[1]).Should().BeTrue();
        }

        [Fact]
        public void BuildScoreInputs_UsesNanForAnUnparseableOrNullValue()
        {
            var variables = Variables("Amount", "Label");
            var body = JObject.Parse("{\"Amount\": null, \"Label\": \"not-a-number\"}");

            var inputs = AdvancedAnalyticsRecallExtensions.BuildScoreInputs(variables, body);

            double.IsNaN(inputs[0]).Should().BeTrue();
            double.IsNaN(inputs[1]).Should().BeTrue();
        }

        [Fact]
        public void BuildScoreInputs_ParsesBooleansAndIntegerTokensLikeExhaustiveDoes()
        {
            var variables = Variables("Flag", "Whole");
            var body = JObject.Parse("{\"Flag\": true, \"Whole\": 7}");

            AdvancedAnalyticsRecallExtensions.BuildScoreInputs(variables, body).Should().Equal(1, 7);
        }

        [Theory]
        [InlineData("{\"Amount\": 0}", true)]
        [InlineData("{\"Amount\": -1}", true)]
        [InlineData("{\"Amount\": true}", true)]
        [InlineData("{\"Amount\": null}", true)]
        [InlineData("{\"Amount\": \"12\"}", true)]
        [InlineData("{\"Amount\": \"1,5\"}", false)]
        [InlineData("{\"Amount\": {}}", false)]
        [InlineData("{\"Amount\": []}", false)]
        [InlineData("{\"Unrelated\": [1, {\"a\": 2}]}", true)]
        public void RecallBodyHasValidTypesForAdvancedAnalytics_Boundaries(string json, bool expected)
        {
            InvokeService.RecallBodyHasValidTypesForAdvancedAnalytics(Model("Amount"), JObject.Parse(json))
                .Should().Be(expected);
        }
    }
}