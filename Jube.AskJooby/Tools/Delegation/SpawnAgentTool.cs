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

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Delegation;
using Jube.AskJooby.Delegation.Models;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Microsoft.Extensions.AI;

namespace Jube.AskJooby.Tools.Delegation
{
    public sealed record SpawnAgentSubTask(
        [property: Description("The specialist agent type to spawn for this sub-task.")]
        string AgentType,
        [property: Description(
            "A self-contained description of this sub-task -- the Joobrik has no access to this " +
            "conversation or to any other sub-task here, so include everything it needs to know.")]
        string Task);

    public sealed class SpawnAgentTool(
        IChatClient chatClient,
        IRagClient rag,
        IMemoryClient memory,
        IConversationHistoryClient history,
        int depth,
        RunContext childContext,
        ISqlQueryClient? sqlQuery = null,
        IToolResultCache? cache = null,
        IFileExportClient? export = null,
        IScriptExecutionClient? scriptExecution = null,
        string? spawnerAgentType = null)
    {
        [Description(
            "Delegates a self-contained sub-task to a specialist Joobrik and returns only its " +
            "final report -- the Joobrik's own tool calls and reasoning never reach you, only " +
            "its finished answer. Prefer a matching specialist over doing squarely-theirs work " +
            "yourself. Do not send a second Joobrik to re-read what one has already reported this " +
            "turn; send another only when the caller asked for a check. The agentType must be one of the specialist names listed in your own " +
            "instructions above -- any other name, or one the current caller is not permitted to " +
            "use, is refused. For two or more sub-tasks that are genuinely independent of each " +
            "other, use SpawnAgents instead to run them at the same time rather than one after " +
            "another. If you end up combining several of these specialists' separately-returned " +
            "numbers or rows into one finding, cache them with CacheAssembledTable before reporting " +
            "it onward rather than repeating them as prose -- that is the only way the result " +
            "becomes chartable or exportable afterwards.")]
        public Task<string> SpawnAgent(
            [Description("The specialist agent type to spawn.")]
            string agentType,
            [Description(
                "A self-contained description of the sub-task -- the Joobrik has no access to " +
                "this conversation, so include everything it needs to know.")]
            string task,
            CancellationToken token = default)
        {
            if (childContext.TokenBudget?.IsExhausted == true)
            {
                return Task.FromResult(
                    "You have sent out a lot of Joobriks on this turn, so stop here and take stock "
                    + "rather than send another. Tell the caller what you have established so far, name "
                    + "the piece you were about to delegate next, and ask whether you are on the right "
                    + "lines -- so that a yes is all it takes to carry on. Say plainly what is still "
                    + "outstanding. Nothing you sent out earlier is still working in the background: "
                    + "every Joobrik has either reported above or been called in, so a later turn starts "
                    + "the outstanding work afresh rather than picking it up where it stopped.");
            }

            if (RefuseDelegateNotAllowed(agentType) is { } disallowedSingle)
            {
                return Task.FromResult(disallowedSingle);
            }

            if (RefuseWithoutPlan(requiresPlan: false) is { } unplannedSingle)
            {
                return Task.FromResult(unplannedSingle);
            }

            if (RefuseUnplannedStable(agentType) is { } unplannedStableSingle)
            {
                return Task.FromResult(unplannedStableSingle);
            }

            return RunIsolatedAndForwardAsync(
                agentType, task, AgentStreamNarrator.DescribeSpawnLabel(agentType, spawnerAgentType), token);
        }

        [Description(
            "Runs two or more independent sub-tasks at the same time, each delegated to its own " +
            "specialist Joobrik, and returns all their final reports together. Only worth it when " +
            "the sub-tasks are genuinely independent -- none needs another's answer first, and " +
            "none depends on something only you know from the wider conversation beyond what you " +
            "put in its own task description. For a single sub-task, or a sequence where one " +
            "depends on the last, use SpawnAgent instead. When you combine several of these " +
            "specialists' separately-returned numbers or rows into one finding, cache them with " +
            "CacheAssembledTable before reporting it onward rather than repeating them as prose -- " +
            "that is the only way the result becomes chartable or exportable afterwards.")]
        public async Task<string> SpawnAgents(
            [Description(
                "Two or more independent sub-tasks, each naming its own specialist agentType and " +
                "carrying a self-contained task description.")]
            List<SpawnAgentSubTask> subTasks,
            CancellationToken token = default)
        {
            if (subTasks.Count < 2)
            {
                throw new ArgumentException(
                    "SpawnAgents needs at least two independent sub-tasks -- use SpawnAgent for just one.");
            }

            if (childContext.TokenBudget?.IsExhausted == true)
            {
                return "You have sent out a lot of Joobriks on this turn, so stop here and take stock "
                       + "rather than send more. Tell the caller what you have established so far, name "
                       + "the pieces you were about to delegate next, and ask whether you are on the "
                       + "right lines -- so that a yes is all it takes to carry on. Say plainly what is "
                       + "still outstanding. Nothing you sent out earlier is still working in the "
                       + "background: every Joobrik has either reported above or been called in, so a "
                       + "later turn starts the outstanding work afresh rather than picking it up where "
                       + "it stopped.";
            }

            if (subTasks.Count > childContext.MaxFanOut)
            {
                throw new ArgumentException(
                    $"SpawnAgents was asked for {subTasks.Count} sub-tasks at once, more than the " +
                    $"{childContext.MaxFanOut} allowed in a single call -- split this into smaller batches.");
            }

            if (RefuseWithoutPlan(requiresPlan: SpansStables(subTasks)) is { } unplannedBatch)
            {
                return unplannedBatch;
            }

            if (RefuseFanOutWithoutStablePlan() is { } unplannedFanOut)
            {
                return unplannedFanOut;
            }

            foreach (var subTask in subTasks)
            {
                if (RefuseDelegateNotAllowed(subTask.AgentType) is { } disallowedBatch)
                {
                    return disallowedBatch;
                }

                if (RefuseUnplannedStable(subTask.AgentType) is { } unplannedStableBatch)
                {
                    return unplannedStableBatch;
                }
            }

            var results = await Task.WhenAll(subTasks.Select((subTask, index) =>
                RunIsolatedAndForwardAsync(
                    subTask.AgentType, subTask.Task,
                    AgentStreamNarrator.DescribeSpawnLabel(subTask.AgentType, spawnerAgentType, index), token)));

            var builder = new StringBuilder();

            for (var i = 0; i < subTasks.Count; i++)
            {
                builder.AppendLine($"[{subTasks[i].AgentType} #{i + 1}] {subTasks[i].Task}");
                builder.AppendLine(results[i]);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private async Task<string> RunIsolatedAndForwardAsync(
            string agentType, string task, string label, CancellationToken token)
        {
            if (childContext.IsolatedRunScopeFactory is null)
            {
                return await RunAndForwardAsync(rag, memory, history, childContext, agentType, task, label,
                        sqlQuery, cache, export, scriptExecution, token)
                    .ConfigureAwait(false);
            }

            await using var scope = await childContext.IsolatedRunScopeFactory.CreateAsync(token)
                .ConfigureAwait(false);

            var isolatedContext = childContext with
            {
                Recorder = scope.Recorder, WorkflowProgress = scope.WorkflowProgress
            };

            return await RunAndForwardAsync(scope.Rag, scope.Memory, scope.History, isolatedContext,
                    agentType, task, label, scope.SqlQuery, scope.Cache, scope.Export, scope.ScriptExecution,
                    token)
                .ConfigureAwait(false);
        }

        private async Task<string> RunAndForwardAsync(
            IRagClient ragClient, IMemoryClient memoryClient, IConversationHistoryClient historyClient,
            RunContext context,
            string agentType, string task, string label, ISqlQueryClient? sqlQueryClient,
            IToolResultCache? cacheClient, IFileExportClient? exportClient,
            IScriptExecutionClient? scriptExecutionClient, CancellationToken token)
        {
            if (context.DelegationChannel is not null)
            {
                await context.DelegationChannel.WriteAsync(new AgentStreamItem(
                        AgentStreamItemKind.Delegation,
                        AgentStreamNarrator.DescribeSpawnAnnouncement(agentType, spawnerAgentType), label), token)
                    .ConfigureAwait(false);
            }

            var handBack = new HandBackSignal();
            context = context with { HandBack = handBack };

            var runnable = await RunnableFactory
                .CreateAsync(agentType, chatClient, ragClient, memoryClient, historyClient, task, depth + 1, context,
                    sqlQueryClient, cacheClient, exportClient, scriptExecutionClient, token)
                .ConfigureAwait(false);

            context.TurnActivity?.RecordAgentCall();

            var replyBuilder = new ReplyDraft();
            var returnedToTheWoodlands = false;
            var killedOnOverrun = false;
            var runClock = Stopwatch.StartNew();

            using var overrun = CancellationTokenSource.CreateLinkedTokenSource(token);

            var stream = AgentIdleWatchdog.WithIdleTimeoutAsync(
                cancellation => runnable.RunStreamingAsync(task, cancellation),
                TimeSpan.FromSeconds(context.AgentIdleTimeoutSeconds),
                () => returnedToTheWoodlands = true,
                overrun.Token);

            await foreach (var item in Guarded(stream, context, overrun,
                               () => killedOnOverrun = true).WithCancellation(overrun.Token).ConfigureAwait(false))
            {
                if (item.Kind == AgentStreamItemKind.Text)
                {
                    replyBuilder.Append(item.Text);
                    continue;
                }

                replyBuilder.Observe(item);

                if (context.DelegationChannel is null)
                {
                    continue;
                }

                var forwarded = item.Kind == AgentStreamItemKind.Error
                    ? new AgentStreamItem(AgentStreamItemKind.Step, item.Text, label, Detail: item.Detail)
                    : item.AgentLabel is null
                        ? item with { AgentLabel = label }
                        : item;

                await context.DelegationChannel.WriteAsync(forwarded, token).ConfigureAwait(false);
            }

            if (killedOnOverrun || returnedToTheWoodlands)
            {
                if (context.Recorder is not null)
                {
                    await context.Recorder.CompleteRunAsync(
                            runnable.RunId, "Cancelled", null, runClock.ElapsedMilliseconds, token)
                        .ConfigureAwait(false);
                }
            }

            if (killedOnOverrun)
            {
                var killed =
                    $"{AgentStreamNarrator.DescribeAgentLabel(agentType)} was called in mid-task, so "
                    + "the turn can take stock of a long run and check it is on the right lines before "
                    + "going further.";

                if (context.DelegationChannel is not null)
                {
                    await context.DelegationChannel
                        .WriteAsync(new AgentStreamItem(AgentStreamItemKind.Step, killed, label), token)
                        .ConfigureAwait(false);
                }

                var cut = replyBuilder.ToString();

                return Account(context, string.IsNullOrWhiteSpace(cut)
                    ? killed + " It had reported nothing by then. Answer now from what you already "
                             + "have and say plainly that this part went unanswered."
                    : killed + " What it had written by then follows, and is probably incomplete: "
                             + cut);
            }

            if (returnedToTheWoodlands)
            {
                var retired =
                    $"{AgentStreamNarrator.DescribeAgentLabel(agentType)} went quiet for "
                    + $"{context.AgentIdleTimeoutSeconds}s and has been sent back to the woodlands "
                    + "for a lie down.";

                if (context.DelegationChannel is not null)
                {
                    await context.DelegationChannel
                        .WriteAsync(new AgentStreamItem(AgentStreamItemKind.Step, retired, label), token)
                        .ConfigureAwait(false);
                }

                var partial = replyBuilder.ToString();

                return Account(context, string.IsNullOrWhiteSpace(partial)
                    ? retired + " It reported nothing, so treat this sub-task as unanswered: either "
                              + "do it yourself, or say plainly that it could not be completed."
                    : retired + " What it had got to before then follows, and may be incomplete: "
                              + partial);
            }

            var report = replyBuilder.ToString();

            if (handBack.Raised is { } handedBack)
            {
                context.TurnActivity?.NoteHandBack();

                var description = Describe(agentType, handedBack);

                var combined = string.IsNullOrWhiteSpace(report)
                    ? description
                    : report + "\n\n" + description;

                if (context.TurnActivity is { HandBacks: > 1 })
                {
                    return Account(context,
                        combined
                        + "\n\nA hand-back has already been sent out earlier this turn, so this is the "
                        + "turn's second: the report above stands, but it is not being chased further by "
                        + "sending this specialist, or anyone else, out again on the same task this turn. "
                        + "Answer now from what is reported above.");
                }

                return Account(context, combined);
            }

            if (string.IsNullOrWhiteSpace(report))
            {
                return Account(context,
                    $"{AgentStreamNarrator.DescribeAgentLabel(agentType)} finished without reporting "
                    + "anything at all. Do NOT send it out again on the same task -- it has already had "
                    + "its turn and would do the same thing twice. Either answer this part yourself "
                    + "from what you already have, or say plainly that it could not be established.");
            }

            return Account(context, report);
        }

        private string? RefuseFanOutWithoutStablePlan()
        {
            if (depth != 1
                || spawnerAgentType is null
                || childContext.TurnActivity is not { } turn
                || !AgentDefinitions.HierarchyDomainOf.Values.Contains(spawnerAgentType)
                || turn.HasStablePlan(spawnerAgentType))
            {
                return null;
            }

            return
                $"You are sending several Joobriks at once, so write a plan for your stable first: call WritePlan, " +
                "naming each step, the specialist it goes to, and how you will know it worked. Then send them.";
        }

        private string? RefuseDelegateNotAllowed(string agentType)
        {
            if (spawnerAgentType is null
                || !AgentDefinitions.All.TryGetValue(spawnerAgentType, out var spawner)
                || spawner.AllowedDelegates is not { } allowed
                || allowed.Contains(agentType))
            {
                return null;
            }

            return $"{agentType} is not one of the specialists {spawnerAgentType} may hand over to -- only " +
                   "the names listed in your own instructions above are legal here. Pick one of those, or " +
                   "answer this part yourself.";
        }

        private string? RefuseUnplannedStable(string agentType)
        {
            if (depth != 0
                || childContext.TurnActivity is not { } turn
                || !turn.HasPlan
                || !AgentDefinitions.HierarchyDomainOf.TryGetValue(agentType, out var stable)
                || turn.PlannedStables.Contains(stable))
            {
                return null;
            }

            return $"The plan has no step for the {stable} stable, so {agentType} is not handed over. Add a step " +
                   $"owned by {stable}, or answer that part yourself.";
        }

        private string? RefuseWithoutPlan(bool requiresPlan)
        {
            if (depth != 0 || childContext.TurnActivity?.HasPlan == true)
            {
                return null;
            }

            childContext.TurnActivity?.NoteUnplannedHandover();

            if (childContext.TurnActivity is not null && !requiresPlan)
            {
                return null;
            }

            return "No plan has been written for this turn, so nothing is handed over yet. Call WritePlan "
                   + "first: name each step in order, the specialist it goes to (or none, if you answer it "
                   + "yourself), and how you will know it worked. Then hand the first step over.";
        }

        private static bool SpansStables(IEnumerable<SpawnAgentSubTask> subTasks)
        {
            return subTasks
                .Select(subTask => AgentDefinitions.HierarchyDomainOf.GetValueOrDefault(subTask.AgentType))
                .Where(stable => stable is not null)
                .Distinct()
                .Count() >= 2;
        }

        private static string Describe(string agentType, HandBack handedBack)
        {
            var who = AgentStreamNarrator.DescribeAgentLabel(agentType);

            var owner = handedBack.OwningAgent is { } named
                ? $"It believes this belongs to {named}. "
                : "It could not name which slice owns it. ";

            return $"NOT ANSWERED -- {who} handed this back rather than guessing, because the work is "
                   + $"outside the operations it holds. {owner}What is needed: {handedBack.WhatIsNeeded} "
                   + $"What it did establish, which you do not need to work out again: "
                   + $"{handedBack.WhatIEstablished}\n\n"
                   + "Act on this now. If the slice that owns it is one of your own specialists, send it "
                   + "there with the question above. If it is not, hand this back to whoever sent you, "
                   + "naming that slice, rather than answering it yourself from free-form SQL -- a value "
                   + "read off a table nobody owns is the failure this exists to stop. If neither is "
                   + "possible -- no specialist of yours fits and there is no one above you to hand this "
                   + "to -- say so to the person now, plainly, naming what is needed and which slice you "
                   + "believe owns it. Ending this turn with no reply at all is not a valid way to act on "
                   + "a hand-back.";
        }

        private static async IAsyncEnumerable<AgentStreamItem> Guarded(
            IAsyncEnumerable<AgentStreamItem> stream, RunContext context, CancellationTokenSource overrun,
            Action onKilled)
        {
            if (context.TokenBudget is null)
            {
                await foreach (var item in stream.WithCancellation(overrun.Token).ConfigureAwait(false))
                {
                    yield return item;
                }

                yield break;
            }

            await using var enumerator = stream.GetAsyncEnumerator(overrun.Token);

            while (true)
            {
                bool moved;

                try
                {
                    moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (overrun.IsCancellationRequested)
                {
                    yield break;
                }

                if (!moved)
                {
                    yield break;
                }

                yield return enumerator.Current;

                if (!context.TokenBudget.IsOverrun)
                {
                    continue;
                }

                onKilled();
                await overrun.CancelAsync().ConfigureAwait(false);
                yield break;
            }
        }

        private static string Account(RunContext context, string report)
        {
            return context.TurnActivity is { } activity
                ? report + "\n\n" + activity.Describe()
                : report;
        }
    }
}