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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities
{
    using System;
    using BackgroundTasks.TaskStarters.Archiver;
    using DynamicEnvironment;
    using log4net;

    public static class DataTableBuffersUtility
    {
        public static void CreateIfNotExists(ILog log, EntityAnalysisModel entityAnalysisModel,
            DynamicEnvironment environment)
        {
            try
            {
                int i;
                var archiverPersistThreads = Int32.Parse(environment.AppSettings("ArchiverPersistThreads"));

                for (i = 1; i <= archiverPersistThreads; i++)
                {
                    if (entityAnalysisModel.Dependencies.BulkInsertMessageBuffers.Count == archiverPersistThreads)
                    {
                        break;
                    }

                    entityAnalysisModel.Dependencies.BulkInsertMessageBuffers.TryAdd(i, new ArchiveBuffer());
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Entity Start: Create table process has created an error as {ex} for model {entityAnalysisModel.Instance.Id}.");
            }
        }
    }
}