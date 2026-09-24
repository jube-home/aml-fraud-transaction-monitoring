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

namespace Jube.Parser.Dependency
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed record ModelEntity(ModelEntityKind Kind, int Id, string Name, bool Active);

    public sealed record ModelReference(
        ModelEntity Dependent,
        ModelDependencyKind Kind,
        string Namespace,
        string Name,
        int? Line = null);

    public sealed record ModelDependency(
        ModelEntity Dependent,
        ModelEntity Target,
        ModelDependencyKind Kind,
        string Namespace,
        string Name,
        int? Line)
    {
        public bool Dangling => Target == null;
        public bool TargetInactive => Target is { Active: false };
    }

    public sealed class ModelDependencyGraph
    {
        private static readonly Dictionary<string, ModelEntityKind[]> namespaceKinds = new()
        {
            [RuleReference.Payload] =
                [ModelEntityKind.RequestXPath, ModelEntityKind.InlineScriptProperty, ModelEntityKind.InlineFunction],
            [RuleReference.TtlCounter] = [ModelEntityKind.TtlCounter],
            [RuleReference.Abstraction] = [ModelEntityKind.AbstractionRule],
            [RuleReference.Dictionary] = [ModelEntityKind.Dictionary],
            [RuleReference.Sanction] = [ModelEntityKind.Sanction],
            [RuleReference.AbstractionCalculation] = [ModelEntityKind.AbstractionCalculation],
            [RuleReference.ExhaustiveAdaptation] = [ModelEntityKind.ExhaustiveAdaptation],
            [RuleReference.HttpAdaptation] = [ModelEntityKind.HttpAdaptation],
            [RuleReference.List] = [ModelEntityKind.List],
            [RuleReference.Activation] = [ModelEntityKind.ActivationRule]
        };

        private readonly List<ModelDependency> dependencies;
        private readonly List<ModelEntity> entities;

        public ModelDependencyGraph(IEnumerable<ModelEntity> entities, IEnumerable<ModelReference> references)
        {
            ArgumentNullException.ThrowIfNull(entities);
            ArgumentNullException.ThrowIfNull(references);

            this.entities = entities.ToList();
            var byKindAndName = this.entities
                .GroupBy(e => (e.Kind, e.Name), e => e)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.Active).First());

            dependencies = references.Select(r => new ModelDependency(r.Dependent, Resolve(byKindAndName, r), r.Kind,
                r.Namespace, r.Name, r.Line)).ToList();
        }

        public IReadOnlyList<ModelEntity> Entities => entities;
        public IReadOnlyList<ModelDependency> Dependencies => dependencies;

        public static string NamespaceOf(ModelEntityKind kind)
        {
            return kind switch
            {
                ModelEntityKind.RequestXPath or ModelEntityKind.InlineScriptProperty
                    or ModelEntityKind.InlineFunction => RuleReference.Payload,
                ModelEntityKind.TtlCounter => RuleReference.TtlCounter,
                ModelEntityKind.AbstractionRule => RuleReference.Abstraction,
                ModelEntityKind.Dictionary => RuleReference.Dictionary,
                ModelEntityKind.Sanction => RuleReference.Sanction,
                ModelEntityKind.AbstractionCalculation => RuleReference.AbstractionCalculation,
                ModelEntityKind.ExhaustiveAdaptation => RuleReference.ExhaustiveAdaptation,
                ModelEntityKind.HttpAdaptation => RuleReference.HttpAdaptation,
                ModelEntityKind.List => RuleReference.List,
                ModelEntityKind.ActivationRule => RuleReference.Activation,
                _ => null
            };
        }

        public IEnumerable<ModelDependency> DependentsOf(ModelEntityKind kind, string name)
        {
            return dependencies.Where(d => d.Target != null && d.Target.Kind == kind &&
                                           string.Equals(d.Target.Name, name, StringComparison.Ordinal));
        }

        public IEnumerable<ModelDependency> DependentsOf(ModelEntityKind kind, int id)
        {
            return dependencies.Where(d => d.Target != null && d.Target.Kind == kind && d.Target.Id == id);
        }

        public IEnumerable<ModelDependency> DependenciesOf(ModelEntity entity)
        {
            return dependencies.Where(d => d.Dependent == entity);
        }

        public IEnumerable<ModelDependency> Dangling()
        {
            return dependencies.Where(d => d.Dangling);
        }

        public IEnumerable<ModelDependency> OnInactiveTargets()
        {
            return dependencies.Where(d => d.TargetInactive && d.Dependent.Active);
        }

        public IEnumerable<ModelEntity> Unreferenced(params ModelEntityKind[] kinds)
        {
            var referenced = dependencies.Where(d => d.Target != null).Select(d => d.Target).ToHashSet();
            return entities.Where(e => kinds.Contains(e.Kind) && !referenced.Contains(e));
        }

        private static ModelEntity Resolve(Dictionary<(ModelEntityKind, string), ModelEntity> byKindAndName,
            ModelReference reference)
        {
            if (!namespaceKinds.TryGetValue(reference.Namespace, out var kinds))
            {
                kinds = Enum.TryParse<ModelEntityKind>(reference.Namespace, out var direct) ? [direct] : [];
            }

            foreach (var kind in kinds)
            {
                if (byKindAndName.TryGetValue((kind, reference.Name), out var entity))
                {
                    return entity;
                }
            }

            return null;
        }
    }
}