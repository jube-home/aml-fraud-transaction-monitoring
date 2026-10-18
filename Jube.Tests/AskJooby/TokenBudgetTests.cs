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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class TokenBudgetTests
    {
        [Fact]
        public void ABudgetOfZeroIsNoBudgetAtAllRatherThanOneAlreadySpent()
        {
            var budget = new TokenBudget(0);
            budget.Record(1_000_000, 1_000_000);

            budget.IsExhausted.Should().BeFalse(
                "zero means the feature is off, and a deployment that never set it must not find "
                + "every turn refusing to use tools");
        }

        [Fact]
        public void SpendAccumulatesInputAndOutputTogetherUntilTheLimitIsReached()
        {
            var budget = new TokenBudget(1000);

            budget.Record(400, 100);
            budget.IsExhausted.Should().BeFalse();
            budget.Spent.Should().Be(500);

            budget.Record(400, 200);
            budget.Spent.Should().Be(1100);

            budget.IsExhausted.Should().BeTrue(
                "the budget is about what a turn costs, and a turn pays for what it sends as well "
                + "as what it gets back");
        }

        [Fact]
        public void NegativeOrAbsentCountsAreIgnoredRatherThanCreditedBack()
        {
            var budget = new TokenBudget(1000);

            budget.Record(500, 0);
            budget.Record(-9000, -9000);

            budget.Spent.Should().Be(500,
                "a provider reporting nothing must not hand the turn free budget back");
        }

        [Fact]
        public void ConcurrentJoobriksSpendingAtOnceAreAllCounted()
        {
            var budget = new TokenBudget(0);

            Parallel.For(0, 500, _ => budget.Record(10, 10));

            budget.Spent.Should().Be(10_000,
                "a fan out spends in parallel, and a counter that loses writes under that is "
                + "measuring the one thing it exists to measure wrongly");
        }

        [Fact]
        public void TheHardCeilingSitsAboveTheSoftOneSoAgentsGetAChanceToWriteUpFirst()
        {
            var budget = new TokenBudget(1000);

            budget.Record(1000, 0);
            budget.IsExhausted.Should().BeTrue();

            budget.IsOverrun.Should().BeFalse(
                "crossing the budget withholds tools and asks for an answer; it is the overshoot "
                + "past it that means nobody is listening and the agents should be stopped");

            budget.Record(500, 0);
            budget.IsOverrun.Should().BeTrue();
        }

        [Fact]
        public void ABudgetOfZeroIsNeverOverrunEither()
        {
            var budget = new TokenBudget(0);
            budget.Record(50_000_000, 50_000_000);

            budget.IsOverrun.Should().BeFalse("off means off, at both thresholds");
        }

        [Fact]
        public void AnOverrunMultipleOfOneCollapsesBothThresholdsOntoTheLimit()
        {
            var budget = new TokenBudget(1000, 1.0);
            budget.Record(1000, 0);

            budget.IsExhausted.Should().BeTrue();

            budget.IsOverrun.Should().BeTrue(
                "a deployment that wants no grace at all gets the hard stop at the budget itself");
        }

        [Fact]
        public async Task AToolRunsNormallyWhileThereIsBudgetLeftAsync()
        {
            var budget = new TokenBudget(1000);
            var ran = false;

            var function = new BudgetedAiFunction(
                AIFunctionFactory.Create(() =>
                {
                    ran = true;

                    return "did the work";
                }, "DoWork"), budget);

            var result = await function.InvokeAsync(new AIFunctionArguments());

            ran.Should().BeTrue();
            result?.ToString().Should().Contain("did the work");
        }

        [Fact]
        public async Task AnExhaustedBudgetStopsTheToolRunningAndTellsTheModelToAnswerAsync()
        {
            var budget = new TokenBudget(100);
            budget.Record(100, 0);

            var ran = false;

            var function = new BudgetedAiFunction(
                AIFunctionFactory.Create(() =>
                {
                    ran = true;

                    return "did the work";
                }, "DoWork"), budget);

            var result = await function.InvokeAsync(new AIFunctionArguments());

            ran.Should().BeFalse("withholding the tool is the whole mechanism; running it and then "
                                 + "complaining would spend the tokens anyway");

            result?.ToString().Should().Contain("Do not call another tool",
                "the agent has to be told what to do instead, or it retries the same call until the "
                + "turn ends some other way");
        }

        [Fact]
        public async Task UsageReportedByTheProviderIsCountedAgainstTheBudgetAsync()
        {
            var budget = new TokenBudget(0);
            var client = new TokenBudgetChatClient(new UsageReportingChatClient(), budget);

            await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hello")]);

            budget.Spent.Should().Be(150,
                "the provider's own count is the only honest measure, and it is already arriving "
                + "on every response");
        }

        [Fact]
        public async Task UsageArrivingMidStreamIsCountedTooAsync()
        {
            var budget = new TokenBudget(0);
            var client = new TokenBudgetChatClient(new UsageReportingChatClient(), budget);

            await foreach (var _ in client.GetStreamingResponseAsync(
                               [new ChatMessage(ChatRole.User, "hello")]))
            {
            }

            budget.Spent.Should().Be(150,
                "the streaming path is the one the agent loop actually uses, so a budget that only "
                + "counted the non-streaming path would never fire in practice");
        }

        private sealed class UsageReportingChatClient : IChatClient
        {
            public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
                ChatOptions? options = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi"))
                {
                    Usage = Usage()
                });
            }

            public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
                IEnumerable<ChatMessage> messages, ChatOptions? options = null,
                [EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
                yield return new ChatResponseUpdate(ChatRole.Assistant, "hi");

                yield return new ChatResponseUpdate
                {
                    Contents = [new UsageContent(Usage())]
                };
            }

            public object? GetService(Type serviceType, object? serviceKey = null)
            {
                return null;
            }

            public void Dispose()
            {
            }

            private static UsageDetails Usage()
            {
                return new UsageDetails { InputTokenCount = 100, OutputTokenCount = 50 };
            }
        }
    }
}