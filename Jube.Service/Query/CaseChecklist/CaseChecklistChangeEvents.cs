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

using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using log4net;

namespace Jube.Service.Query.CaseChecklist
{
    public static class CaseChecklistChangeEvents
    {
        public const string Area = "CaseChecklist";

        public static void Publish(IServiceChangeBus serviceChangeBus, int tenantRegistryId, int caseId,
            string actor, ILog log)
        {
            try
            {
                _ = serviceChangeBus.PublishAsync(new ServiceChangeEvent(Area, tenantRegistryId,
                    ServiceChangeKind.Updated, caseId, null, actor, DateTimeOffset.UtcNow, "-"));
            }
            catch (Exception e)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{Area}: change-event publish failed caseId={caseId}", e);
                }
            }
        }
    }
}
