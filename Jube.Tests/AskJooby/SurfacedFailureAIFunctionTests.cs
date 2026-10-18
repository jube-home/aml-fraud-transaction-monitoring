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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class SurfacedFailureAiFunctionTests
    {
        [Fact]
        public async Task TheServicesOwnValidationMessageReachesTheModelRatherThanAGenericFailureAsync()
        {
            var function = new SurfacedFailureAiFunction(AIFunctionFactory.Create(
                (Func<string>)(() => throw new InvalidOperationException(
                    "CoderRuleScript: Line 1: Abstraction does not exist for AmountGreaterThan100Volume24Hours.")),
                "EntityAnalysisModelActivationRuleCreate"));

            var result = await function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            var failure = result as ToolInvocationFailure;

            (failure is null).Should().BeFalse(
                "a thrown tool must come back as a failure the model can read, not as an exception the "
                + "harness flattens to its own constant");

            failure!.Message.Should().Contain("Abstraction does not exist for");
            failure.ToolName.Should().Be("EntityAnalysisModelActivationRuleCreate");
            failure.ToString().Should().Contain("Abstraction does not exist for");

            JsonSerializer.Serialize<object>(failure).Should()
                .Contain("Abstraction does not exist for")
                .And.Contain("calling the same tool again with the same arguments will fail the same way");
        }

        [Fact]
        public async Task TheInnermostMessageIsTakenBecauseAWrappedOneSaysNothingUsefulAsync()
        {
            var function = new SurfacedFailureAiFunction(AIFunctionFactory.Create(
                (Func<string>)(() => throw new InvalidOperationException("One or more errors occurred.",
                    new ArgumentException("Name is required and must be unique within the Model."))),
                "Create"));

            var result = (ToolInvocationFailure)(await function.InvokeAsync(
                new AIFunctionArguments(), CancellationToken.None))!;

            result.Message.Should().Be("Name is required and must be unique within the Model.");
        }

        [Fact]
        public Task CancellationStillPropagatesBecauseSomethingDeliberatelyAskedTheRunToStopAsync()
        {
            var function = new SurfacedFailureAiFunction(AIFunctionFactory.Create(
                (Func<string>)(() => throw new OperationCanceledException()), "Slow"));

            var act = async () => await function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            return act.Should().ThrowAsync<OperationCanceledException>(
                "turning a cancellation into a result the model reasons about would keep a run alive "
                + "that the budget or the watchdog had ended");
        }

        [Fact]
        public async Task AToolThatSucceedsIsPassedStraightThroughUntouchedAsync()
        {
            var function = new SurfacedFailureAiFunction(
                AIFunctionFactory.Create(() => "3 tables you may query", "ListQueryableTables"));

            var result = await function.InvokeAsync(new AIFunctionArguments(), CancellationToken.None);

            (result as ToolInvocationFailure is null).Should().BeTrue();
            result!.ToString().Should().Contain("3 tables you may query");
        }
    }
}