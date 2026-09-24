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

namespace Jube.Engine.Integrity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
    using Parser.Dependency;

    public sealed record IntegrityCheck(
        IntegrityCode Code,
        IntegritySeverity Severity,
        string Category,
        string EntityKind,
        int? EntityId,
        string EntityName,
        string Message);

    public sealed record CompileStatus(
        string Kind,
        int Id,
        string Name,
        bool Active,
        bool? Compiled,
        string CompileError);

    public sealed record SearchWindowInput(
        int Id,
        string Name,
        string Interval,
        int Value,
        string SearchKey,
        string SearchKeyTtlInterval,
        int SearchKeyTtlValue);

    public sealed record EngineNode(string Instance, DateTime? HeartbeatDate, DateTime? SynchronisedDate);

    public sealed record EngineObservation(
        EngineStateSourceKind Source,
        IReadOnlyList<EngineModelState> States,
        IReadOnlyList<EngineNode> Nodes);

    public static class ModelIntegrityRules
    {
        private const string Dependencies = "Dependencies";
        private const string Compilation = "Compilation";
        private const string Configuration = "Configuration";
        private const string Engine = "Engine";

        private static readonly ModelEntityKind[] unreferencedKinds =
        [
            ModelEntityKind.List, ModelEntityKind.Dictionary, ModelEntityKind.TtlCounter,
            ModelEntityKind.AbstractionRule, ModelEntityKind.AbstractionCalculation, ModelEntityKind.InlineFunction,
            ModelEntityKind.HttpAdaptation, ModelEntityKind.ExhaustiveAdaptation
        ];

        private static readonly HashSet<string> loadedKinds =
        [
            nameof(ModelEntityKind.RequestXPath), nameof(ModelEntityKind.InlineFunction),
            nameof(ModelEntityKind.GatewayRule), nameof(ModelEntityKind.AbstractionRule),
            nameof(ModelEntityKind.AbstractionCalculation), nameof(ModelEntityKind.TtlCounter),
            nameof(ModelEntityKind.Sanction), nameof(ModelEntityKind.HttpAdaptation),
            nameof(ModelEntityKind.ActivationRule)
        ];

        public static IEnumerable<IntegrityCheck> DependencyChecks(ModelDependencyGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            foreach (var dangling in graph.Dangling().OrderBy(d => d.Dependent.Kind).ThenBy(d => d.Dependent.Name))
            {
                yield return Check(IntegrityCode.DependencyDangling,
                    dangling.Dependent.Active ? IntegritySeverity.Error : IntegritySeverity.Info, Dependencies,
                    dangling.Dependent,
                    $"{Describe(dangling)} names {Qualified(dangling)}, which does not exist" +
                    (dangling.Dependent.Active
                        ? ", so it will not work."
                        : "; it is inactive, so this only matters if it is activated."));
            }

            foreach (var inactive in graph.OnInactiveTargets().OrderBy(d => d.Dependent.Kind)
                         .ThenBy(d => d.Dependent.Name))
            {
                yield return Check(IntegrityCode.DependencyOnInactive, IntegritySeverity.Warning, Dependencies,
                    inactive.Dependent,
                    $"{Describe(inactive)} uses {inactive.Target.Kind} {inactive.Target.Name}, which is inactive, " +
                    "so the engine does not load it and the use will not work.");
            }

            foreach (var unused in graph.Unreferenced(unreferencedKinds).Where(e => e.Active)
                         .OrderBy(e => e.Kind).ThenBy(e => e.Name))
            {
                yield return Check(IntegrityCode.EntityUnreferenced, IntegritySeverity.Info, Dependencies, unused,
                    $"{unused.Kind} {unused.Name} is active but nothing in the model uses it.");
            }
        }

        public static IEnumerable<IntegrityCheck> CompilationChecks(IEnumerable<CompileStatus> statuses)
        {
            ArgumentNullException.ThrowIfNull(statuses);

            foreach (var status in statuses.Where(s => s.Active && s.Compiled == false)
                         .OrderBy(s => s.Kind).ThenBy(s => s.Name))
            {
                yield return new IntegrityCheck(IntegrityCode.EngineCompileFailed, IntegritySeverity.Error,
                    Compilation, status.Kind, status.Id, status.Name,
                    $"The engine could not compile {status.Kind} {status.Name} at its last synchronisation, so it " +
                    $"is not running: {status.CompileError ?? "no error was recorded"}.");
            }
        }

        public static IEnumerable<IntegrityCheck> ConfigurationChecks(bool modelActive, bool enableTtlCounter,
            ModelDependencyGraph graph, IEnumerable<SearchWindowInput> searchRules, DateTime referenceDate)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(searchRules);

            if (!modelActive)
            {
                yield return new IntegrityCheck(IntegrityCode.ModelInactive, IntegritySeverity.Info, Configuration,
                    null, null, null, "The model is inactive, so the engine does not load or invoke it.");
            }

            if (!graph.Entities.Any(e => e.Kind == ModelEntityKind.ActivationRule && e.Active))
            {
                yield return new IntegrityCheck(IntegrityCode.NoActiveActivationRules, IntegritySeverity.Info,
                    Configuration, null, null, null,
                    "The model has no active activation rules, so no transaction can activate.");
            }

            if (!enableTtlCounter)
            {
                foreach (var counter in graph.Entities.Where(e => e.Kind == ModelEntityKind.TtlCounter && e.Active)
                             .OrderBy(e => e.Name))
                {
                    yield return Check(IntegrityCode.TtlCountersDisabled, IntegritySeverity.Warning, Configuration,
                        counter,
                        $"TTL counters are disabled for the model, so the engine neither increments nor reads " +
                        $"{counter.Name}; rules see it as 0.");
                }
            }

            foreach (var rule in searchRules.OrderBy(r => r.Name))
            {
                if (!AbstractionWindow.SearchKeyShortensTheWindow(rule.Interval, rule.Value,
                        rule.SearchKeyTtlInterval, rule.SearchKeyTtlValue, referenceDate))
                {
                    continue;
                }

                yield return rule.SearchKeyTtlValue <= 0
                    ? new IntegrityCheck(IntegrityCode.SearchKeyTtlCollapsesWindow, IntegritySeverity.Warning,
                        Configuration, nameof(ModelEntityKind.AbstractionRule), rule.Id, rule.Name,
                        $"The search key {rule.SearchKey} has no TTL, so the engine limits the abstraction rule's " +
                        $"{rule.Value} {rule.Interval} window to the reference date alone and it aggregates almost " +
                        "nothing.")
                    : new IntegrityCheck(IntegrityCode.SearchKeyTtlShortensWindow, IntegritySeverity.Warning,
                        Configuration, nameof(ModelEntityKind.AbstractionRule), rule.Id, rule.Name,
                        $"The search key {rule.SearchKey} keeps history for {rule.SearchKeyTtlValue} " +
                        $"{rule.SearchKeyTtlInterval}, which is shorter than the rule's {rule.Value} " +
                        $"{rule.Interval}, so the engine silently uses the shorter window.");
            }
        }

        public static IEnumerable<IntegrityCheck> EngineChecks(EngineObservation observation, bool modelActive,
            IEnumerable<ModelEntity> entities, DateTime now, TimeSpan heartbeatTolerance)
        {
            ArgumentNullException.ThrowIfNull(observation);
            ArgumentNullException.ThrowIfNull(entities);

            var checks = new List<IntegrityCheck>();
            if (observation.Source == EngineStateSourceKind.Unavailable)
            {
                checks.Add(new IntegrityCheck(IntegrityCode.EngineStateUnavailable, IntegritySeverity.Info, Engine,
                    null, null, null,
                    "No engine runs in this process and no engine has recorded what it loaded for this model, so " +
                    "what the engine is running cannot be checked."));
            }

            if (observation.Nodes.Count == 0)
            {
                checks.Add(new IntegrityCheck(IntegrityCode.EngineNoNodes, IntegritySeverity.Warning, Engine, null,
                    null, null, "No engine instance has synchronised this tenant's models."));
            }

            foreach (var node in observation.Nodes.Where(n =>
                         n.HeartbeatDate == null || now - n.HeartbeatDate.Value > heartbeatTolerance))
            {
                checks.Add(new IntegrityCheck(IntegrityCode.EngineNodeStale, IntegritySeverity.Warning, Engine, null,
                    null, node.Instance,
                    $"Engine instance {node.Instance} last reported at {Format(node.HeartbeatDate)}, more than " +
                    $"{heartbeatTolerance.TotalMinutes:0} minutes ago; it may have stopped."));
            }

            if (!modelActive || observation.Source == EngineStateSourceKind.Unavailable)
            {
                return checks;
            }

            var active = entities.Where(e => e.Active && loadedKinds.Contains(e.Kind.ToString()))
                .Select(e => (Kind: e.Kind.ToString(), e.Id, e.Name)).ToList();
            var activeKeys = active.Select(e => (e.Kind, e.Id)).ToHashSet();

            foreach (var state in observation.States.OrderBy(s => s.Instance, StringComparer.Ordinal))
            {
                if (!state.Started)
                {
                    checks.Add(new IntegrityCheck(IntegrityCode.EngineModelNotStarted, IntegritySeverity.Warning,
                        Engine, null, null, state.Instance,
                        $"Engine instance {state.Instance} has loaded the model but not started it."));
                }

                var loaded = state.Loaded.Where(l => loadedKinds.Contains(l.Kind)).ToList();
                var loadedKeys = loaded.Select(l => (l.Kind, l.Id)).ToHashSet();

                foreach (var missing in active.Where(e => !loadedKeys.Contains((e.Kind, e.Id)))
                             .OrderBy(e => e.Kind).ThenBy(e => e.Name))
                {
                    checks.Add(new IntegrityCheck(IntegrityCode.EngineEntityNotLoaded, IntegritySeverity.Warning,
                        Engine, missing.Kind, missing.Id, missing.Name,
                        $"{missing.Kind} {missing.Name} is active but engine instance {state.Instance} has not " +
                        "loaded it: synchronise the model, and check that it compiles" +
                        (missing.Kind == nameof(ModelEntityKind.ActivationRule) ? " and is approved." : ".")));
                }

                foreach (var stale in loaded.Where(l => !activeKeys.Contains((l.Kind, l.Id)))
                             .OrderBy(l => l.Kind).ThenBy(l => l.Name))
                {
                    checks.Add(new IntegrityCheck(IntegrityCode.EngineEntityStale, IntegritySeverity.Warning, Engine,
                        stale.Kind, stale.Id, stale.Name,
                        $"Engine instance {state.Instance} is still running {stale.Kind} {stale.Name}, which is no " +
                        "longer active; synchronise the model to unload it."));
                }
            }

            var loadedInstances = observation.States.Select(s => s.Instance).ToHashSet(StringComparer.Ordinal);
            foreach (var node in observation.Nodes.Where(n => !loadedInstances.Contains(n.Instance))
                         .OrderBy(n => n.Instance, StringComparer.Ordinal))
            {
                checks.Add(new IntegrityCheck(IntegrityCode.EngineModelNotLoaded, IntegritySeverity.Warning, Engine,
                    null, null, node.Instance,
                    $"Engine instance {node.Instance} has not loaded the model; synchronise it."));
            }

            return checks;
        }

        private static IntegrityCheck Check(IntegrityCode code, IntegritySeverity severity, string category,
            ModelEntity entity, string message)
        {
            return new IntegrityCheck(code, severity, category, entity.Kind.ToString(), entity.Id, entity.Name,
                message);
        }

        private static string Describe(ModelDependency dependency)
        {
            return dependency.Kind == ModelDependencyKind.RuleText
                ? $"The rule text of {dependency.Dependent.Kind} {dependency.Dependent.Name}"
                : $"The {dependency.Kind} setting of {dependency.Dependent.Kind} {dependency.Dependent.Name}";
        }

        private static string Qualified(ModelDependency dependency)
        {
            return string.IsNullOrEmpty(dependency.Namespace)
                ? dependency.Name
                : $"{dependency.Namespace}.{dependency.Name}";
        }

        private static string Format(DateTime? date)
        {
            return date?.ToString("u") ?? "never";
        }
    }
}