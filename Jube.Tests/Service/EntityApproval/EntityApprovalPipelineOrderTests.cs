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

namespace Jube.Test.Service.EntityApproval
{
    using System;
    using System.Linq;
    using FluentAssertions;
    using Jube.Data.Query.Models;
    using Jube.Data.Repository;
    using Jube.Service.EntityApproval;
    using Xunit;

    [Trait("Category", "Unit")]
    public sealed class EntityApprovalPipelineOrderTests
    {
        [Fact]
        public void EveryApprovalKindHasAPipelineRank()
        {
            foreach (var kind in Enum.GetValues<EntityApprovalKind>())
            {
                EntityApprovalPipelineOrder.Rank(kind).Should().BeLessThan(int.MaxValue, kind.ToString());
            }
        }

        [Fact]
        public void ReferenceDataAndPayloadParsingComeBeforeTheInlineStagesThatUseThem()
        {
            EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelRequestXPath)
                .Should().BeLessThan(
                    EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelInlineFunction));
            EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelDictionary)
                .Should().BeLessThan(
                    EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelInlineScript));
        }

        [Fact]
        public void InlineStagesComeBeforeRulesAndCalculationsThatReadThem()
        {
            EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelInlineScript)
                .Should().BeLessThan(
                    EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelAbstractionRule));
            EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelAbstractionRule)
                .Should().BeLessThan(
                    EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelAbstractionCalculation));
            EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelAbstractionCalculation)
                .Should().BeLessThan(
                    EntityApprovalPipelineOrder.Rank(EntityApprovalKind.EntityAnalysisModelActivationRule));
        }

        [Fact]
        public void OrderFollowsThePipelineThenNameWithinAKind()
        {
            var ordered = EntityApprovalPipelineOrder.Order(new[]
            {
                Subject(EntityApprovalKind.EntityAnalysisModelActivationRule, 1, "Zeta"),
                Subject(EntityApprovalKind.EntityAnalysisModelInlineFunction, 2, "beta"),
                Subject(EntityApprovalKind.EntityAnalysisModelAbstractionCalculation, 3, "Alpha"),
                Subject(EntityApprovalKind.EntityAnalysisModelInlineFunction, 4, "Alpha")
            });

            ordered.Select(s => (s.Kind, s.Name)).Should().Equal(
                (EntityApprovalKind.EntityAnalysisModelInlineFunction, "Alpha"),
                (EntityApprovalKind.EntityAnalysisModelInlineFunction, "beta"),
                (EntityApprovalKind.EntityAnalysisModelAbstractionCalculation, "Alpha"),
                (EntityApprovalKind.EntityAnalysisModelActivationRule, "Zeta"));
        }

        [Fact]
        public void TagsComeLastBecauseTheyAreNotOnTheInvocationPath()
        {
            var last = Enum.GetValues<EntityApprovalKind>()
                .OrderByDescending(EntityApprovalPipelineOrder.Rank).First();

            last.Should().Be(EntityApprovalKind.EntityAnalysisModelTag);
        }

        [Fact]
        public void EveryApprovalKindHasAChangeSource()
        {
            EntityApprovalChangeSources.Kinds.Should().BeEquivalentTo(Enum.GetValues<EntityApprovalKind>());
        }

        private static EntityApprovalSubject Subject(EntityApprovalKind kind, int entityId, string name)
        {
            return new EntityApprovalSubject(kind, entityId, name, true, false, 1, "maker", string.Empty, 1);
        }
    }
}