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

// ReSharper disable once RedundantUsingDirective

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Jube.Test.Infrastructure.ModelScaffolding
{
    public class ModelScaffoldFixture : IAsyncLifetime
    {
        private readonly List<ModelScaffold> isolated = [];

        private ModelEngineHost? engine;
        private ModelInvokeHost? api;
        private ModelScaffold? shared;

        public ModelEngineHost Engine
        {
            get => engine.Required();
            private set => engine = value;
        }

        public ModelInvokeHost Api
        {
            get => api.Required();
            private set => api = value;
        }

        public ModelScaffold Shared
        {
            get => shared.Required();
            private set => shared = value;
        }

        public ModelSyncResult SharedSync
        {
            get => field.Required();
            private set;
        }

        protected virtual IReadOnlyDictionary<string, string>? EngineSettings => null;

        protected virtual Task BeforeEngineStartAsync() => Task.CompletedTask;

        protected virtual Task AfterDisposeAsync() => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            await BeforeEngineStartAsync().ConfigureAwait(false);

            Shared = await ModelScaffold.CreateAsync().ConfigureAwait(false);
            try
            {
                Engine = await ModelEngineHost.StartAsync(EngineSettings).ConfigureAwait(false);
                SharedSync = await ModelSync.SyncAsync(Engine, Shared).ConfigureAwait(false);
                Api = await ModelInvokeHost.StartAsync(Engine).ConfigureAwait(false);
            }
            catch
            {
                await DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<ModelScaffold> CreateIsolatedAsync(ModelScaffoldOptions? options = null)
        {
            var scaffold = await ModelScaffold.CreateAsync(options ?? ModelScaffoldOptions.Isolated)
                .ConfigureAwait(false);
            lock (isolated)
            {
                isolated.Add(scaffold);
            }

            await ModelSync.SyncAsync(Engine, scaffold).ConfigureAwait(false);
            return scaffold;
        }

        public async Task DisposeAsync()
        {
            List<ModelScaffold> copies;
            lock (isolated)
            {
                copies = [.. isolated];
                isolated.Clear();
            }

            foreach (var scaffold in copies)
            {
                await scaffold.DisposeAsync().ConfigureAwait(false);
            }

            if (api != null)
            {
                await api.DisposeAsync().ConfigureAwait(false);
            }

            if (engine != null)
            {
                await engine.DisposeAsync().ConfigureAwait(false);
            }

            if (shared != null)
            {
                await shared.DisposeAsync().ConfigureAwait(false);
            }

            await AfterDisposeAsync().ConfigureAwait(false);
        }
    }
}