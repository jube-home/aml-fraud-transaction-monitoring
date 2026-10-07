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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Xunit;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    [Trait("Category", "Unit")]
    public class ModelSnapshotCutoverTests
    {
        private static ModelSnapshot Generation(ModelSnapshot previous, int size)
        {
            var rules = new List<EntityAnalysisModelActivationRule>();
            var xPaths = new List<EntityAnalysisModelRequestXPath>();
            var lists = new Dictionary<string, List<string>>();

            for (var i = 0; i < size; i++)
            {
                rules.Add(new EntityAnalysisModelActivationRule { Id = i, Name = $"Rule{size}" });
                xPaths.Add(new EntityAnalysisModelRequestXPath { Name = $"Field{size}" });
                lists.Add($"List{size}_{i}", [$"{size}"]);
            }

            return new ModelSnapshotBuilder(previous)
            {
                ModelActivationRules = rules,
                EntityAnalysisModelRequestXPaths = xPaths,
                EntityAnalysisModelLists = lists
            }.Build();
        }

        [Fact]
        public void AModelStartsOnAnEmptyFirstSnapshot()
        {
            var model = new EngineModel();

            using var scope = new AssertionScope();
            model.Snapshot.Should().NotBeNull();
            model.Snapshot.Generation.Should().Be(0);
            model.Snapshot.ModelActivationRules.Should().BeEmpty();
        }

        [Fact]
        public void PublishingReplacesTheWholeSnapshotInOneStep()
        {
            var model = new EngineModel();
            var first = Generation(model.Snapshot, 1);

            model.PublishSnapshot(first);

            using var scope = new AssertionScope();
            model.Snapshot.Should().BeSameAs(first);
            model.Snapshot.Generation.Should().Be(1);
        }

        [Fact]
        public async Task AReaderHoldingASnapshotNeverObservesAMixtureOfTwoGenerationsAsync()
        {
            var model = new EngineModel();
            model.PublishSnapshot(Generation(model.Snapshot, 1));

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var token = cancellation.Token;
            var mixed = 0;
            var observations = 0;

            var writer = Task.Run(() =>
            {
                var size = 2;
                while (!token.IsCancellationRequested && size < 400)
                {
                    model.PublishSnapshot(Generation(model.Snapshot, size));
                    size++;
                }
            }, CancellationToken.None);

            var readers = new Task[4];
            for (var r = 0; r < readers.Length; r++)
            {
                readers[r] = Task.Run(() =>
                {
                    while (!writer.IsCompleted)
                    {
                        var snapshot = model.Snapshot;

                        var rules = snapshot.ModelActivationRules.Count;
                        Thread.SpinWait(50);
                        var xPaths = snapshot.EntityAnalysisModelRequestXPaths.Count;
                        Thread.SpinWait(50);
                        var lists = snapshot.EntityAnalysisModelLists.Count;

                        Interlocked.Increment(ref observations);

                        if (rules != xPaths || xPaths != lists)
                        {
                            Interlocked.Increment(ref mixed);
                        }
                    }
                }, CancellationToken.None);
            }

            await writer;
            await Task.WhenAll(readers);

            using var scope = new AssertionScope();
            observations.Should().BeGreaterThan(0);
            mixed.Should().Be(0);
        }

        [Fact]
        public async Task EveryGenerationAReaderObservesIsOneThatWasActuallyPublishedAsync()
        {
            var model = new EngineModel();
            model.PublishSnapshot(Generation(model.Snapshot, 1));

            var published = new HashSet<long> { model.Snapshot.Generation };
            var seen = new List<long>();

            var writer = Task.Run(() =>
            {
                for (var size = 2; size < 200; size++)
                {
                    var next = Generation(model.Snapshot, size);
                    lock (published)
                    {
                        published.Add(next.Generation);
                    }

                    model.PublishSnapshot(next);
                }
            }, CancellationToken.None);

            var reader = Task.Run(() =>
            {
                while (!writer.IsCompleted)
                {
                    seen.Add(model.Snapshot.Generation);
                }
            }, CancellationToken.None);

            await writer;
            await reader;

            using var scope = new AssertionScope();
            seen.Should().NotBeEmpty();
            lock (published)
            {
                seen.Should().OnlyContain(g => published.Contains(g));
            }
        }
    }
}