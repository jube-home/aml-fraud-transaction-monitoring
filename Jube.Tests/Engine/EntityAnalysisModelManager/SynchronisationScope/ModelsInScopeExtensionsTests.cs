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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using SyncContext = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Context;

namespace Jube.Test.Engine.EntityAnalysisModelManager.SynchronisationScope
{
    [Trait("Category", "Unit")]
    public sealed class ModelsInScopeExtensionsTests
    {
        private static SyncContext ContextWith(params int[] modelIds)
        {
            return new SyncContext
            {
                EntityAnalysisModels =
                {
                    ActiveEntityAnalysisModels = new ConcurrentDictionary<int, EntityAnalysisModelDomain>(
                        modelIds.ToDictionary(id => id,
                            id => new EntityAnalysisModelDomain { Instance = { Id = id, Guid = Guid.NewGuid() } }))
                }
            };
        }

        [Fact]
        public void WithNoScopeEveryActiveModelIsInScope()
        {
            var context = ContextWith(1, 2, 3);

            context.ModelsInScope().Select(s => s.Key).Should().BeEquivalentTo(new[] { 1, 2, 3 });
            context.IsInScope(2).Should().BeTrue();
            context.IsInScope(99).Should().BeTrue("with no scope nothing is excluded");
        }

        [Fact]
        public void AScopeKeepsOnlyTheModelsItNames()
        {
            var context = ContextWith(1, 2, 3);
            context.EntityAnalysisModels.SynchronisationScope = new HashSet<int> { 2 };

            context.ModelsInScope().Select(s => s.Key).Should().Equal(2);
            context.IsInScope(2).Should().BeTrue();
            context.IsInScope(1).Should().BeFalse();
        }

        [Fact]
        public void AScopeNamingAModelThatIsNotLoadedYetStillAdmitsItsRecord()
        {
            var context = ContextWith(1);
            context.EntityAnalysisModels.SynchronisationScope = new HashSet<int> { 7 };

            context.ModelsInScope().Should().BeEmpty("only loaded models are iterated");
            context.IsInScope(7).Should().BeTrue("a model that is not loaded yet must still be created from its record");
        }

        [Fact]
        public void AnEmptyScopeSelectsNothing()
        {
            var context = ContextWith(1, 2);
            context.EntityAnalysisModels.SynchronisationScope = new HashSet<int>();

            context.ModelsInScope().Should().BeEmpty();
            context.IsInScope(1).Should().BeFalse();
        }
    }
}
