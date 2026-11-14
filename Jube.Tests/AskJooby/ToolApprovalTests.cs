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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.Security;
using Jube.AskJooby.Security.Models;
using Jube.Service.Agent.Models;
using Jube.Service.AskJoobyConversation;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class ToolApprovalTests
    {
        private static readonly IReadOnlyDictionary<string, object?> arguments =
            new Dictionary<string, object?> { ["name"] = "x", ["id"] = 5 };

        private static ToolApprovalGate GateFor(ToolApprovalLedger ledger, string user = "alice", int tenant = 1,
            string conversation = "7")
        {
            return new ToolApprovalGate(ledger, conversation, user, tenant);
        }

        [Fact]
        public void AWriteIsHeldBackUntilTheUserApprovesAndThenRunsExactlyOnce()
        {
            var ledger = new ToolApprovalLedger();
            var gate = GateFor(ledger);

            gate.Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Pending);

            var pending = ledger.Pending("7", "alice", 1).Single();
            ledger.Decide(pending.Id, "7", "alice", 1, true).Should().Be(ToolApprovalDecisionResult.Recorded);

            gate.Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Proceed);
            gate.Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Pending);
        }

        [Fact]
        public void ARepeatedRequestForTheSameChangeIsOneApprovalNotMany()
        {
            var ledger = new ToolApprovalLedger();
            var gate = GateFor(ledger);

            gate.Review("ListUpdate", arguments);
            gate.Review("ListUpdate", arguments);

            ledger.Pending("7", "alice", 1).Should().HaveCount(1);
        }

        [Fact]
        public void ApprovalOfOneSetOfArgumentsNeverCoversDifferentArgumentsOrAnotherTool()
        {
            var ledger = new ToolApprovalLedger();
            var gate = GateFor(ledger);

            gate.Review("ListUpdate", arguments);
            ledger.Decide(ledger.Pending("7", "alice", 1).Single().Id, "7", "alice", 1, true);

            var changed = new Dictionary<string, object?> { ["name"] = "y", ["id"] = 5 };

            gate.Review("ListUpdate", changed).Should().Be(ToolApprovalVerdict.Pending);
            gate.Review("ListDelete", arguments).Should().Be(ToolApprovalVerdict.Pending);
        }

        [Fact]
        public void ArgumentOrderDoesNotChangeWhichChangeWasApproved()
        {
            var ledger = new ToolApprovalLedger();
            var gate = GateFor(ledger);

            gate.Review("ListUpdate", new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 });
            ledger.Decide(ledger.Pending("7", "alice", 1).Single().Id, "7", "alice", 1, true);

            gate.Review("ListUpdate", new Dictionary<string, object?> { ["b"] = 2, ["a"] = 1 })
                .Should().Be(ToolApprovalVerdict.Proceed);
        }

        [Fact]
        public void ADeclinedChangeIsRefusedOnceAndThenAsksAgain()
        {
            var ledger = new ToolApprovalLedger();
            var gate = GateFor(ledger);

            gate.Review("ListUpdate", arguments);
            ledger.Decide(ledger.Pending("7", "alice", 1).Single().Id, "7", "alice", 1, false);

            gate.Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Rejected);
            gate.Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Pending);
        }

        [Fact]
        public void OnlyTheSameUserTenantAndConversationCanDecideOrConsumeAnApproval()
        {
            var ledger = new ToolApprovalLedger();

            GateFor(ledger).Review("ListUpdate", arguments);
            var id = ledger.Pending("7", "alice", 1).Single().Id;

            ledger.Decide(id, "7", "bob", 1, true).Should().Be(ToolApprovalDecisionResult.NotFound);
            ledger.Decide(id, "7", "alice", 2, true).Should().Be(ToolApprovalDecisionResult.NotFound);
            ledger.Decide(id, "8", "alice", 1, true).Should().Be(ToolApprovalDecisionResult.NotFound);
            ledger.Decide(id, "7", "ALICE", 1, true).Should().Be(ToolApprovalDecisionResult.Recorded);
            ledger.Decide(id, "7", "alice", 1, false).Should().Be(ToolApprovalDecisionResult.AlreadyDecided);

            GateFor(ledger, "bob").Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Pending);
            GateFor(ledger, tenant: 2).Review("ListUpdate", arguments).Should().Be(ToolApprovalVerdict.Pending);
            GateFor(ledger, conversation: "8").Review("ListUpdate", arguments)
                .Should().Be(ToolApprovalVerdict.Pending);
        }

        [Fact]
        public void AnExpiredApprovalIsForgotten()
        {
            var ledger = new ToolApprovalLedger(TimeSpan.FromMilliseconds(-1));

            GateFor(ledger).Review("ListUpdate", arguments);

            ledger.Pending("7", "alice", 1).Should().BeEmpty();
        }

        [Fact]
        public void TheLedgerIsBoundedInTotalAndPerConversation()
        {
            var ledger = new ToolApprovalLedger(capacity: 5, pendingPerConversation: 3);
            var gate = GateFor(ledger);

            for (var i = 0; i < 10; i++)
            {
                gate.Review("ListUpdate", new Dictionary<string, object?> { ["i"] = i });
            }

            ledger.Pending("7", "alice", 1).Should().HaveCount(3);
        }

        [Fact]
        public async Task TheGatedFunctionNeverRunsTheToolUntilApprovedAsync()
        {
            var ledger = new ToolApprovalLedger();
            var calls = 0;

            var function = AIFunctionFactory.Create((string name) =>
            {
                calls++;

                return $"changed {name}";
            }, "ListUpdate");

            var gated = new ApprovalGatedAiFunction(function, GateFor(ledger));
            var args = new AIFunctionArguments { ["name"] = "x" };

            var first = await gated.InvokeAsync(args);

            calls.Should().Be(0);
            first!.ToString().Should().Contain("needs the user's approval");

            ledger.Decide(ledger.Pending("7", "alice", 1).Single().Id, "7", "alice", 1, true);

            var second = await gated.InvokeAsync(new AIFunctionArguments { ["name"] = "x" });

            calls.Should().Be(1);
            second!.ToString().Should().Be("changed x");
        }

        [Fact]
        public async Task ADeclinedGatedFunctionTellsTheModelNotToLookForAnotherWayAsync()
        {
            var ledger = new ToolApprovalLedger();
            var function = AIFunctionFactory.Create((string name) => name, "ListUpdate");
            var gated = new ApprovalGatedAiFunction(function, GateFor(ledger));

            await gated.InvokeAsync(new AIFunctionArguments { ["name"] = "x" });
            ledger.Decide(ledger.Pending("7", "alice", 1).Single().Id, "7", "alice", 1, false);

            var result = await gated.InvokeAsync(new AIFunctionArguments { ["name"] = "x" });

            result!.ToString().Should().Contain("declined");
        }

        [Fact]
        public void TheApprovalDecisionIsNeverOfferedToAnAgentAsATool()
        {
            var method = typeof(AskJoobyConversationService).GetMethod(nameof(AskJoobyConversationService
                .DecideApprovalAsync));

            method.Should().NotBeNull();
            method!.GetCustomAttributes(typeof(ServiceOperationAttribute), true).Should().BeEmpty();

            Jube.Service.Agent.ServiceToolCatalogue.ServiceToolCatalogue.All
                .Select(descriptor => descriptor.Name)
                .Should().NotContain(name => name.Contains("DecideApproval") || name.Contains("Approvals"));
        }
    }
}
