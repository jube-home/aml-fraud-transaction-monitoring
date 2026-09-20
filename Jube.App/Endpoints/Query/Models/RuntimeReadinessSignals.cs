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

using Jube.App.Code.WatcherDispatch;
using Jube.Service.Query.Ready;
using Microsoft.Extensions.Hosting;

namespace Jube.App.Endpoints.Query.Models
{
    internal sealed class RuntimeReadinessSignals(
        IHostApplicationLifetime lifetime,
        Jube.Engine.Engine engine,
        Relay relay) : IReadinessSignals
    {
        public bool StoppingRequested => lifetime.ApplicationStopping.IsCancellationRequested;

        public bool EngineReady => engine.Context is { Ready: true };

        public bool RelayReady => relay is { Ready: true };
    }
}