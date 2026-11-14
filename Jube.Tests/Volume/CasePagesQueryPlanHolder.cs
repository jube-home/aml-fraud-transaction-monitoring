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

using System.Threading;
using System.Threading.Tasks;
using Jube.Test.Infrastructure.DatabaseFixture;
using Xunit;

namespace Jube.Test.Volume
{
    public sealed class CasePagesQueryPlanHolder : IAsyncLifetime
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private CasePagesQueryPlanWorld? world;

        internal async Task<CasePagesQueryPlanWorld> GetAsync(DatabaseFixture fx)
        {
            await gate.WaitAsync();

            try
            {
                if (world is null)
                {
                    var created = new CasePagesQueryPlanWorld(fx);
                    world = created;
                    await created.InitializeAsync();
                }

                return world;
            }
            finally
            {
                gate.Release();
            }
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (world is not null)
            {
                await world.DisposeAsync();
            }
        }
    }
}
