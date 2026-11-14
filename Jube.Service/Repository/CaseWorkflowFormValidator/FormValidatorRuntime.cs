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

using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Service.Repository.CaseWorkflowFormValidator.Interfaces;
using log4net;

namespace Jube.Service.Repository.CaseWorkflowFormValidator
{
    public static class FormValidatorRuntime
    {
        public static IFormValidatorLookups Lookups(DbContext dbContext, int tenantRegistryId, Guid modelGuid,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment)
        {
            return new FormValidatorLookups(
                new EntityAnalysisModelListRepository(dbContext, tenantRegistryId),
                new EntityAnalysisModelListValueRepository(dbContext, tenantRegistryId),
                new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId),
                new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId),
                modelGuid,
                ApprovalsRequiredResolver.Resolve(dynamicEnvironment.AppSettings("ApprovalsRequired")));
        }

        public static IFormSqlRunner SqlRunner(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            string userName, ILog log)
        {
            return new FormSqlRunner(
                dynamicEnvironment.AppSettings("ReportConnectionString") ??
                dynamicEnvironment.AppSettings("ConnectionString"), userName, log);
        }
    }
}
