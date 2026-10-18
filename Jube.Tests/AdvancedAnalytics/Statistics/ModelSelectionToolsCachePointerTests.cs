/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Interfaces;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public sealed class ModelSelectionToolsCachePointerTests
    {
        private static readonly string[] names = ModelEvaluationData.Names;

        private sealed class FakePointerReader(double[][] inputs, int[] labels)
            : IAdvancedAnalyticsTrainingDataPointerReader
        {
            public Guid? RequestedPointer { get; private set; }
            public int RequestedLabelColumn { get; private set; }
            public int RequestedVariableCount { get; private set; }

            public Task<(double[][] Inputs, int[] Labels)> ResolveAsync(Guid pointer, int labelColumn,
                int variableCount, CancellationToken token)
            {
                RequestedPointer = pointer;
                RequestedLabelColumn = labelColumn;
                RequestedVariableCount = variableCount;

                return Task.FromResult((inputs, labels));
            }
        }

        private sealed class FakePointerWriter(AdvancedAnalyticsTrainingDataDto result)
            : IAdvancedAnalyticsTrainingDataPointerWriter
        {
            public string? RequestedDataPointer { get; private set; }
            public string[]? RequestedVariableNames { get; private set; }
            public string? RequestedLabelColumnName { get; private set; }

            public Task<AdvancedAnalyticsTrainingDataDto> PutAsync(string dataPointer, string[] variableNames,
                string labelColumnName, CancellationToken token)
            {
                RequestedDataPointer = dataPointer;
                RequestedVariableNames = variableNames;
                RequestedLabelColumnName = labelColumnName;

                return Task.FromResult(result);
            }
        }

        [Fact]
        public Task PutModelComparisonDataWithNoBoundWriterIsRefusedAsync()
        {
            var tools = new ModelSelectionTools();

            return tools.Awaiting(t =>
                    t.PutModelComparisonDataAsync("tbl_demo", ["a"], "Dependent"))
                .Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        [Trait("Covers", "PutModelComparisonDataAsync")]
        public async Task PutModelComparisonDataDelegatesToTheBoundWriterAndReturnsItsResultAsync()
        {
            var expected = new AdvancedAnalyticsTrainingDataDto
            {
                CachePointer = Guid.NewGuid(),
                Rows = 10,
                Columns = 3,
                LabelColumn = 2
            };

            var writer = new FakePointerWriter(expected);
            var tools = new ModelSelectionTools(pointerWriter: writer);

            var result = await tools.PutModelComparisonDataAsync("tbl_demo", ["a", "b"], "Dependent");

            result.Should().BeSameAs(expected);
            writer.RequestedDataPointer.Should().Be("tbl_demo");
            writer.RequestedVariableNames.Should().Equal("a", "b");
            writer.RequestedLabelColumnName.Should().Be("Dependent");
        }

        [Fact]
        public Task GivingBothInlineRowsAndACachePointerIsRefusedAsync()
        {
            var (x, y) = ModelEvaluationData.Separable();
            var tools = new ModelSelectionTools(new FakePointerReader(x, y));

            return tools.Awaiting(t => t.CompareModelsAsync(names, inputs: x, labels: y, cachePointer: Guid.NewGuid()))
                .Should().ThrowAsync<ArgumentException>().Where(e => e.ParamName == "cachePointer");
        }

        [Fact]
        public Task GivingNeitherInlineRowsNorACachePointerIsRefusedAsync()
        {
            var tools = new ModelSelectionTools(new FakePointerReader([], []));

            return tools.Awaiting(t => t.CompareModelsAsync(names)).Should()
                .ThrowAsync<ArgumentException>().Where(e => e.ParamName == "inputs");
        }

        [Fact]
        public Task ACachePointerWithNoBoundReaderIsRefusedAsync()
        {
            var tools = new ModelSelectionTools();

            return tools.Awaiting(t => t.CompareModelsAsync(names, cachePointer: Guid.NewGuid())).Should()
                .ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task ACachePointerResolvesToTheSameResultAsTheIdenticalInlineRowsAsync()
        {
            var (x, y) = ModelEvaluationData.Separable(300);
            var families = new[] { EvaluationFamily.LogisticRegression, EvaluationFamily.DecisionTree };
            var reader = new FakePointerReader(x, y);
            var pointerTools = new ModelSelectionTools(reader);
            var inlineTools = new ModelSelectionTools();
            var pointer = Guid.NewGuid();

            var viaPointer = await pointerTools.CompareModelsAsync(names, families: families, folds: 3,
                cachePointer: pointer, labelColumn: 2);

            var viaInline = await inlineTools.CompareModelsAsync(names, inputs: x, labels: y, families: families,
                folds: 3);

            ModelEvaluationData.Json(viaPointer).Should().Be(ModelEvaluationData.Json(viaInline));
            reader.RequestedPointer.Should().Be(pointer);
            reader.RequestedLabelColumn.Should().Be(2);
            reader.RequestedVariableCount.Should().Be(names.Length);
        }
    }
}