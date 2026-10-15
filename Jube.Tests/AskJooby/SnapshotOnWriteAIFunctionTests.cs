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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Observability.Interfaces;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class SnapshotOnWriteAiFunctionTests
    {
        [Fact]
        public async Task ASnapshotIsTakenBeforeTheChangeRunsAsync()
        {
            var order = new List<string>();
            var snapshot = new RecordingAutoSnapshot(order);

            var wrapped = new SnapshotOnWriteAiFunction(
                StubFunction("EntityAnalysisModelCreate", () => order.Add("write")), snapshot);

            await wrapped.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            order.Should().Equal(["snapshot", "write"],
                "a snapshot taken after the change has already run is not a restore point for it");
        }

        [Fact]
        public async Task EveryChangeGetsItsOwnRestorePointRatherThanOnePerRunAsync()
        {
            var snapshot = new RecordingAutoSnapshot([]);
            var first = new SnapshotOnWriteAiFunction(StubFunction("EntityAnalysisModelCreate"), snapshot);
            var second = new SnapshotOnWriteAiFunction(StubFunction("ActivationRuleCreate"), snapshot);

            await first.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);
            await second.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            snapshot.Taken.Select(record => record.ToolName)
                .Should().Equal(["EntityAnalysisModelCreate", "ActivationRuleCreate"],
                    "one snapshot per turn would only let the whole turn be undone, where the point is "
                    + "that any single change can be rolled back to the state immediately before it");
        }

        [Fact]
        public async Task TheChangeDoesNotRunIfItsRestorePointCouldNotBeTakenAsync()
        {
            var ran = false;

            var wrapped = new SnapshotOnWriteAiFunction(
                StubFunction("EntityAnalysisModelCreate", () => ran = true), new FailingAutoSnapshot());

            var act = async () => await wrapped.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();

            ran.Should().BeFalse(
                "an unsnapshotted change is the exact state this exists to prevent, and a failed call is "
                + "recoverable where an irreversible one is not");
        }

        [Fact]
        public async Task TheRestorePointIsAnnouncedWithTheIdNeededToUseItAsync()
        {
            var channel = Channel.CreateUnbounded<AgentStreamItem>();

            var wrapped = new SnapshotOnWriteAiFunction(
                StubFunction("EntityAnalysisModelCreate"), new RecordingAutoSnapshot([]), channel.Writer,
                "Model Admin (Greshgosh)");

            await wrapped.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);
            channel.Writer.Complete();

            var announced = new List<AgentStreamItem>();

            await foreach (var item in channel.Reader.ReadAllAsync(CancellationToken.None))
            {
                announced.Add(item);
            }

            announced.Should().ContainSingle();
            announced[0].Kind.Should().Be(AgentStreamItemKind.Step);

            announced[0].Text.Should().Contain("1").And.Contain("EntityAnalysisModelCreate",
                "the reader is being told a restore point exists and what to quote to use it, which is "
                + "the whole value of having taken it");

            announced[0].AgentLabel.Should().Be("Model Admin (Greshgosh)");
        }

        [Fact]
        public async Task AToolWithNoChannelStillRunsAndStillSnapshotsAsync()
        {
            var snapshot = new RecordingAutoSnapshot([]);
            var wrapped = new SnapshotOnWriteAiFunction(StubFunction("EntityAnalysisModelCreate"), snapshot);

            await wrapped.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            snapshot.Taken.Should().ContainSingle("the announcement is optional, the restore point is not");
        }

        private static AIFunction StubFunction(string name, Action? onInvoke = null)
        {
            return AIFunctionFactory.Create(() =>
            {
                onInvoke?.Invoke();

                return "done";
            }, name);
        }

        private sealed class RecordingAutoSnapshot(List<string> order) : IAutoSnapshot
        {
            private readonly List<AutoSnapshotRecord> taken = [];
            public IReadOnlyList<AutoSnapshotRecord> Taken => taken;

            public Task<AutoSnapshotRecord?> TakeBeforeWriteAsync(string toolName, CancellationToken token)
            {
                order.Add("snapshot");

                var record = new AutoSnapshotRecord(taken.Count + 1, Guid.NewGuid(), toolName,
                    DateTimeOffset.UtcNow);
                taken.Add(record);

                return Task.FromResult<AutoSnapshotRecord?>(record);
            }
        }

        private sealed class FailingAutoSnapshot : IAutoSnapshot
        {
            public IReadOnlyList<AutoSnapshotRecord> Taken => [];

            public Task<AutoSnapshotRecord?> TakeBeforeWriteAsync(string toolName, CancellationToken token)
            {
                throw new InvalidOperationException("the export failed");
            }
        }
    }
}