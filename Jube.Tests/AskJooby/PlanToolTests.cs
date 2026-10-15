using System;
using System.Collections.Generic;
using FluentAssertions;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Tools.Delegation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class PlanToolTests
    {
        [Fact]
        public void WritePlanRecordsTheRenderedPlanOnTheTurn()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn);

            var reply = tool.WritePlan(
            [
                new PlanStep("Find the counter", "advanced-analytics-counters", "The counter id comes back"),
                new PlanStep("Explain it to the caller", null, "The answer names the counter")
            ]);

            turn.HasPlan.Should().BeTrue();
            reply.Should().StartWith("Plan recorded for this turn.");
            reply.Should()
                .Contain("1. Find the counter -> advanced-analytics-counters. Check: The counter id comes back");
            reply.Should()
                .Contain("2. Explain it to the caller -> answered by you. Check: The answer names the counter");
        }

        [Fact]
        public void WritePlanRejectsAnEmptyPlan()
        {
            var tool = new PlanTool(new TurnActivityLedger());

            var act = () => tool.WritePlan([]);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WritePlanRejectsAPlanLongerThanTheCeiling()
        {
            var tool = new PlanTool(new TurnActivityLedger());
            var steps = new List<PlanStep>();

            for (var i = 0; i < 13; i++)
            {
                steps.Add(new PlanStep($"Step {i}", null, "It is observed"));
            }

            var act = () => tool.WritePlan(steps);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WritePlanRejectsAStepWithNoCheck()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn);

            var act = () => tool.WritePlan([new PlanStep("Do the thing", null, "  ")]);

            act.Should().Throw<ArgumentException>();
            turn.HasPlan.Should().BeFalse();
        }

        [Fact]
        public void WritePlanFillsTheStableFromTheSpecialistAndRecordsIt()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn);

            var reply = tool.WritePlan(
            [
                new PlanStep("Check the model", "advanced-analytics-model-inspector", "The model's inputs come back")
            ]);

            reply.Should()
                .Contain("1. Check the model -> model-risk stable, advanced-analytics-model-inspector. Check:");

            turn.PlannedStables.Should().BeEquivalentTo(new[] { "model-risk" });
        }

        [Fact]
        public void WritePlanRefusesAStableThatDoesNotOwnTheSpecialist()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn);

            var act = () => tool.WritePlan(
            [
                new PlanStep("Check the model", "advanced-analytics-model-inspector", "The inputs come back",
                    "case-analyst")
            ]);

            act.Should().Throw<ArgumentException>().WithMessage("*belongs to the model-risk stable*");
            turn.HasPlan.Should().BeFalse();
        }

        [Fact]
        public void AStablePlanIsRecordedAgainstThatStableAndNotTheRootPlan()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn, "model-risk");

            tool.WritePlan(
            [
                new PlanStep("Inspect the model", "advanced-analytics-model-inspector", "Its inputs come back")
            ]);

            turn.HasStablePlan("model-risk").Should().BeTrue();
            turn.HasPlan.Should().BeFalse();
        }

        [Fact]
        public void AStablePlanRefusesASpecialistThatBelongsToAnotherStable()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn, "model-risk");

            var act = () => tool.WritePlan(
            [
                new PlanStep("Count the cases", "case-analyst-trend", "A count comes back")
            ]);

            act.Should().Throw<ArgumentException>().WithMessage("*not one of this stable's own specialists*");
            turn.HasStablePlan("model-risk").Should().BeFalse();
        }

        [Fact]
        public void WritePlanRefusesAnUnknownStableForASpecialistWithNoStable()
        {
            var turn = new TurnActivityLedger();
            var tool = new PlanTool(turn);

            var act = () => tool.WritePlan(
            [
                new PlanStep("Look it up", "researcher", "The answer comes back", "not-a-stable")
            ]);

            act.Should().Throw<ArgumentException>().WithMessage("*is not a stable*");
            turn.HasPlan.Should().BeFalse();
        }
    }
}