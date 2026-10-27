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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Models
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Cache.Redis.Callback;
    using Data.Poco;
    using EntityAnalysisModelInvoke.Context;
    using EntityAnalysisModelInvoke.Models;
    using EntityAnalysisModelInvoke.Models.CaseManagement;

    public class ConcurrentQueues
    {
        public ConcurrentQueue<CreateCase> PendingCases = new ConcurrentQueue<CreateCase>();
        public ConcurrentQueue<Context> PendingEntityInvoke = new ConcurrentQueue<Context>();

        public ConcurrentQueue<ActivationWatcher> PersistToActivationWatcher { get; } =
            new ConcurrentQueue<ActivationWatcher>();

        public ConcurrentQueue<Notification> PendingNotifications { get; set; }
        public ConcurrentQueue<Webhook> PendingWebhooks { get; set; }

        public ConcurrentDictionary<Guid, TaskCompletionSource<Callback>> Callbacks { get; } =
            new ConcurrentDictionary<Guid, TaskCompletionSource<Callback>>();
    }
}