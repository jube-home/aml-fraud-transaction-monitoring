/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.Observability.Interfaces;
using Jube.AskJooby.Security.Interfaces;
using Jube.AskJooby.Tools.Export.Interfaces;
using Jube.AskJooby.Tools.History.Interfaces;
using Jube.AskJooby.Tools.Memory.Interfaces;
using Jube.AskJooby.Tools.Rag.Interfaces;
using Jube.AskJooby.Tools.ScriptExecution.Interfaces;
using Jube.AskJooby.Tools.SqlQuery.Interfaces;
using Jube.AskJooby.Tools.Workflows.Interfaces;

namespace Jube.AskJooby.Tools.Delegation.Interfaces
{
    public interface IIsolatedRunScope : IAsyncDisposable
    {
        IRagClient Rag { get; }
        IMemoryClient Memory { get; }
        IConversationHistoryClient History { get; }
        IRunRecorder Recorder { get; }
        ISqlQueryClient? SqlQuery { get; }
        IToolResultCache? Cache { get; }
        IFileExportClient? Export { get; }
        IWorkflowProgressClient? WorkflowProgress { get; }
        IScriptExecutionClient? ScriptExecution { get; }
        IAgentGuardRejectionLedger GuardRejectionLedger { get; }
    }

    public interface IIsolatedRunScopeFactory
    {
        Task<IIsolatedRunScope> CreateAsync(CancellationToken token = default);
    }
}