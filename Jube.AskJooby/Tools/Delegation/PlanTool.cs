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
using System.Text;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Observability.Interfaces;

namespace Jube.AskJooby.Tools.Delegation
{
    public sealed record PlanStep(
        [property: Description("What this step does, in one line.")]
        string Step,
        [property: Description(
            "The specialist agentType this step is handed to, or left empty if you will answer this step " +
            "yourself.")]
        string? Specialist,
        [property: Description(
            "How you will know this step worked -- a concrete result you can observe, not 'it is done'.")]
        string Check,
        [property: Description(
            "The stable (domain) that owns the specialist, such as model-risk or case-analyst. Left empty " +
            "it is filled in from the specialist; name it only when the specialist is not one of a stable's " +
            "own.")]
        string? Stable = null);

    public sealed class PlanTool(ITurnActivityLedger ledger, string? ownerStable = null)
    {
        private const int MaxSteps = 12;

        private static readonly IReadOnlySet<string> stables =
            new HashSet<string>(AgentDefinitions.HierarchyDomainOf.Values, StringComparer.OrdinalIgnoreCase);

        [Description(
            "Writes the plan for this turn before you hand anything over. Name each step in the order it " +
            "will run, the specialist it goes to (or none, if you answer it yourself), the stable that owns " +
            "that specialist, and how you will know it worked. Sending Joobriks to more than one stable at once " +
            "is refused until this has been called on the turn. Once a plan exists, a handover to a stable it " +
            "does not name is refused. " +
            "Call it once, at the start of the turn; the plan is then carried out, and a step that turns out " +
            "wrong is handled by the next step of the same plan rather than by writing a new one.")]
        public string WritePlan(
            [Description("The turn's plan, in the order its steps will run.")]
            List<PlanStep> steps)
        {
            if (steps.Count is < 1 or > MaxSteps)
            {
                throw new ArgumentException(
                    $"WritePlan needs between 1 and {MaxSteps} steps, and was given {steps.Count}.");
            }

            if (steps.Any(step => string.IsNullOrWhiteSpace(step.Step) || string.IsNullOrWhiteSpace(step.Check)))
            {
                throw new ArgumentException(
                    "Every step needs a description of what it does and a check that shows it worked.");
            }

            var resolved = steps.Select(Resolve).ToList();

            if (ownerStable is not null)
            {
                var foreign = resolved.FirstOrDefault(r =>
                    !string.IsNullOrWhiteSpace(r.Step.Specialist)
                    && !string.Equals(r.Stable, ownerStable, StringComparison.OrdinalIgnoreCase));

                if (foreign.Step is not null)
                {
                    throw new ArgumentException(
                        $"{foreign.Step.Specialist} is not one of this stable's own specialists. Hand that step " +
                        "back to Jooby rather than planning it here.");
                }
            }

            var builder = new StringBuilder();

            for (var i = 0; i < resolved.Count; i++)
            {
                var (step, stable) = resolved[i];

                var who = string.IsNullOrWhiteSpace(step.Specialist)
                    ? "answered by you"
                    : stable is null
                        ? step.Specialist
                        : $"{stable} stable, {step.Specialist}";

                builder.AppendLine($"{i + 1}. {step.Step} -> {who}. Check: {step.Check}");
            }

            var plan = builder.ToString().TrimEnd();

            var plannedStables = new HashSet<string>(
                resolved.Where(r => r.Stable is not null).Select(r => r.Stable!), StringComparer.OrdinalIgnoreCase);

            if (ownerStable is null)
            {
                ledger.RecordPlan(plan, plannedStables);
            }
            else
            {
                ledger.RecordStablePlan(ownerStable, plan);
            }

            return "Plan recorded for this turn. Carry it out in order, and check each step before the next.\n"
                   + plan;
        }

        private static (PlanStep Step, string? Stable) Resolve(PlanStep step)
        {
            if (string.IsNullOrWhiteSpace(step.Specialist))
            {
                return (step, null);
            }

            var owner = AgentDefinitions.HierarchyDomainOf.GetValueOrDefault(step.Specialist);

            if (owner is not null)
            {
                if (!string.IsNullOrWhiteSpace(step.Stable)
                    && !string.Equals(step.Stable, owner, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"{step.Specialist} belongs to the {owner} stable, not {step.Stable}. Correct the step's stable.");
                }

                return (step, owner);
            }

            if (string.IsNullOrWhiteSpace(step.Stable))
            {
                return (step, null);
            }

            if (!stables.Contains(step.Stable))
            {
                throw new ArgumentException(
                    $"{step.Stable} is not a stable. Name one of: {string.Join(", ", stables.Order())}.");
            }

            return (step, step.Stable);
        }
    }
}