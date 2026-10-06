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

using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models;
using Xunit;
using EngineModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using InvokeContext = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public class SnapshotPinningTests
    {
        [Fact]
        public void AnInvocationContextKeepsThePinnedGenerationWhenTheModelPublishesANewOne()
        {
            var model = new EngineModel();
            var pinned = model.Snapshot;
            var context = new InvokeContext { EntityAnalysisModel = model, Snapshot = pinned };

            model.PublishSnapshot(new ModelSnapshotBuilder(pinned).Build());

            using var scope = new AssertionScope();
            context.Snapshot.Should().BeSameAs(pinned);
            model.Snapshot.Should().NotBeSameAs(pinned);
            model.Snapshot.Generation.Should().BeGreaterThan(pinned.Generation);
        }

        [Fact]
        public void AContextWithoutAPinnedSnapshotFollowsTheModelsCurrentOne()
        {
            var model = new EngineModel();
            var context = new InvokeContext { EntityAnalysisModel = model };

            model.PublishSnapshot(new ModelSnapshotBuilder(model.Snapshot).Build());

            context.Snapshot.Should().BeSameAs(model.Snapshot);
        }
    }
}
