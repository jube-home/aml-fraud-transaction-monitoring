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
using Jube.Cache;
using Jube.Data.Context;
using Jube.Engine.EntityAnalysisModelInvoke.ImplicitAsync.Interfaces;
using log4net;
using RabbitMQ.Client;

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models
{
    public class Services
    {
        public ILog Log { get; set; }
        public DynamicEnvironment.DynamicEnvironment DynamicEnvironment { get; set; }
        public CacheService CacheService { get; set; }
        public IImplicitAsyncInvocationTracker ImplicitAsyncInvocationTracker { get; set; }
        public IModel RabbitMqChannel { get; set; }
        public DbContext DbContext { get; set; }
        public Parser.Parser Parser { get; set; }
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    }
}