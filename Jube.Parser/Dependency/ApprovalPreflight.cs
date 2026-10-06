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

    public static class ApprovalPreflight
    {
        public static ApprovalPreflightResult Evaluate(ModelDependencyGraph graph, ModelEntity candidate,
            bool deletion)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(candidate);

            var errors = new List<string>();

            if (deletion)
            {
                foreach (var dependent in graph.DependentsOf(candidate.Kind, candidate.Id)
                             .Where(d => d.Dependent.Active))
                {
                    errors.Add($"{dependent.Dependent.Kind} {dependent.Dependent.Name} uses {candidate.Kind} " +
                               $"{candidate.Name}, so approving its deletion would leave that use dangling.");
                }

                return new ApprovalPreflightResult(errors, Array.Empty<ModelEntity>());
            }

            var promoted = Promote(graph, candidate);
            var root = promoted.Entities.First(e => e.Kind == candidate.Kind && e.Id == candidate.Id);

            if (root.Active)
            {
                foreach (var dangling in promoted.DependenciesOf(root).Where(d => d.Dangling))
                {
                    errors.Add($"{root.Kind} {root.Name} names {dangling.Namespace} {dangling.Name}, which does " +
                               "not exist, so approving it would leave it broken.");
                }
            }

            return new ApprovalPreflightResult(errors, UnapprovedPrerequisites(promoted, root));
        }

        private static ModelDependencyGraph Promote(ModelDependencyGraph graph, ModelEntity candidate)
        {
            var promotedCandidate = candidate with { Approved = true };

            var entities = graph.Entities
                .Where(e => !(e.Kind == candidate.Kind && e.Id == candidate.Id))
                .Append(promotedCandidate)
                .ToList();

            var references = graph.Dependencies
                .Select(d => new ModelReference(
                    d.Dependent.Kind == candidate.Kind && d.Dependent.Id == candidate.Id
                        ? promotedCandidate
                        : d.Dependent,
                    d.Kind, d.Namespace, d.Name, d.Line))
                .ToList();

            return new ModelDependencyGraph(entities, references);
        }

        private static IReadOnlyList<ModelEntity> UnapprovedPrerequisites(ModelDependencyGraph graph,
            ModelEntity root)
        {
            var ordered = new List<ModelEntity>();
            var visited = new HashSet<ModelEntity>();

            void Visit(ModelEntity entity)
            {
                if (!visited.Add(entity))
                {
                    return;
                }

                foreach (var dependency in graph.DependenciesOf(entity)
                             .Where(d => d.Target != null && !d.Target.Approved))
                {
                    Visit(dependency.Target);
                }

                if (!entity.Equals(root) && !entity.Approved)
                {
                    ordered.Add(entity);
                }
            }

            Visit(root);
            return ordered;
        }
    }
}