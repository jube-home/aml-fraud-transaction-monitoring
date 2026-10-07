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

using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Engine.Integrity;
using Jube.Parser;
using Jube.Parser.Dependency;
using Xunit;

namespace Jube.Test.RuleParser
{
    [Trait("Category", "Unit")]
    public sealed class ApprovalPreflightTests
    {
        private static readonly ModelEntity amount = new(ModelEntityKind.RequestXPath, 1, "Amount", true);
        private static readonly ModelEntity newList = new(ModelEntityKind.List, 2, "NewList", true, false);
        private static readonly ModelEntity newRule = new(ModelEntityKind.ActivationRule, 3, "Alert", true, false);
        private static readonly ModelEntity oldRule = new(ModelEntityKind.ActivationRule, 4, "Old", true);
        private static readonly ModelEntity unusedRule = new(ModelEntityKind.ActivationRule, 5, "Unused", true);

        private static ModelDependencyGraph Graph(params ModelReference[] references)
        {
            return new ModelDependencyGraph([amount, newList, newRule, oldRule, unusedRule], references);
        }

        [Fact]
        public void ApprovingAnEntityWhoseTargetsAreAlreadyApprovedIsAllowed()
        {
            var graph = Graph(new ModelReference(oldRule, ModelDependencyKind.RuleText, RuleReference.Payload,
                "Amount", 0));

            var result = ApprovalPreflight.Evaluate(graph, oldRule, deletion: false);

            using var scope = new AssertionScope();
            result.Errors.Should().BeEmpty();
            result.Prerequisites.Should().BeEmpty();
            result.Allowed.Should().BeTrue();
        }

        [Fact]
        public void ApprovingARuleThatUsesAnUnapprovedListRefusesAndNamesTheListAsAPrerequisite()
        {
            var graph = Graph(new ModelReference(newRule, ModelDependencyKind.RuleText, RuleReference.List,
                "NewList", 0));

            var result = ApprovalPreflight.Evaluate(graph, newRule, deletion: false);

            using var scope = new AssertionScope();
            result.Allowed.Should().BeFalse("the list is not yet approved, so promoting the rule alone would break it");
            result.Prerequisites.Select(p => p.Name).Should().Equal("NewList");
        }

        [Fact]
        public void PrerequisitesAreListedDependenciesFirstSoTheyCanBeApprovedInThatOrder()
        {
            var middle = new ModelEntity(ModelEntityKind.AbstractionRule, 6, "Middle", true, false);
            var graph = new ModelDependencyGraph([amount, newList, newRule, middle],
            [
                new ModelReference(newRule, ModelDependencyKind.RuleText, RuleReference.Abstraction, "Middle", 0),
                new ModelReference(middle, ModelDependencyKind.RuleText, RuleReference.List, "NewList", 0)
            ]);

            var result = ApprovalPreflight.Evaluate(graph, newRule, deletion: false);

            result.Prerequisites.Select(p => p.Name).Should().Equal("NewList", "Middle");
        }

        [Fact]
        public void ApprovingAnEntityThatNamesSomethingThatDoesNotExistIsRefused()
        {
            var graph = Graph(new ModelReference(oldRule, ModelDependencyKind.RuleText, RuleReference.Payload,
                "NoSuchField", 0));

            var result = ApprovalPreflight.Evaluate(graph, oldRule, deletion: false);

            result.Errors.Should().ContainSingle().Which.Should().Contain("NoSuchField");
            result.Allowed.Should().BeFalse();
        }

        [Fact]
        public void ApprovingTheDeletionOfAnEntityAnActiveRuleUsesIsRefused()
        {
            var list = new ModelEntity(ModelEntityKind.List, 2, "NewList", true);
            var graph = new ModelDependencyGraph([amount, list, oldRule],
            [
                new ModelReference(oldRule, ModelDependencyKind.RuleText, RuleReference.List, "NewList", 0)
            ]);

            var result = ApprovalPreflight.Evaluate(graph, list, deletion: true);

            using var scope = new AssertionScope();
            result.Allowed.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Contain("Old");
        }

        [Fact]
        public void ApprovingTheDeletionOfAnEntityNothingUsesIsAllowed()
        {
            var graph = Graph();

            var result = ApprovalPreflight.Evaluate(graph, unusedRule, deletion: true);

            result.Allowed.Should().BeTrue();
        }

        [Fact]
        public void ADeletionUsedOnlyByAnInactiveRuleIsNotRefused()
        {
            var inactive = new ModelEntity(ModelEntityKind.ActivationRule, 7, "Dormant", false);
            var list = new ModelEntity(ModelEntityKind.List, 2, "NewList", true);
            var graph = new ModelDependencyGraph([list, inactive],
            [
                new ModelReference(inactive, ModelDependencyKind.RuleText, RuleReference.List, "NewList", 0)
            ]);

            var result = ApprovalPreflight.Evaluate(graph, list, deletion: true);

            result.Allowed.Should().BeTrue("an inactive dependent is informational, not a reason to refuse");
        }

        [Fact]
        public void AnActiveDependentOfAnUnapprovedTargetIsReportedAsDependencyOnUnapproved()
        {
            var graph = Graph(new ModelReference(oldRule, ModelDependencyKind.RuleText, RuleReference.List,
                "NewList", 0));

            var unapproved = graph.OnUnapprovedTargets().ToList();
            var findings = ModelIntegrityRules.DependencyChecks(graph).ToList();

            using var scope = new AssertionScope();
            unapproved.Should().ContainSingle();
            findings.Should().Contain(f => f.Code == IntegrityCode.DependencyOnUnapproved);
        }

        [Fact]
        public void AnInactiveDependentOfAnUnapprovedTargetIsNotReported()
        {
            var inactive = new ModelEntity(ModelEntityKind.ActivationRule, 7, "Dormant", false);
            var graph = new ModelDependencyGraph([newList, inactive],
            [
                new ModelReference(inactive, ModelDependencyKind.RuleText, RuleReference.List, "NewList", 0)
            ]);

            graph.OnUnapprovedTargets().Should().BeEmpty();
        }
    }
}