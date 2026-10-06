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

using System;
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Repository;

namespace Jube.Data.Query.Models
{
    public sealed class EntityApprovalSubjectProjection(DbContext dbContext, int tenantRegistryId)
    {
        public IQueryable<EntityApprovalSubject> SubjectsOf(EntityApprovalKind kind)
        {
            return kind switch
            {
                EntityApprovalKind.EntityAnalysisModel => ModelSubjects(),
                EntityApprovalKind.EntityAnalysisModelList => ListSubjects(),
                EntityApprovalKind.EntityAnalysisModelListValue => ListValueSubjects(),
                EntityApprovalKind.EntityAnalysisModelDictionary => DictionarySubjects(),
                EntityApprovalKind.EntityAnalysisModelDictionaryKvp => KvpSubjects(),
                EntityApprovalKind.EntityAnalysisModelRequestXPath => RequestXPathSubjects(),
                EntityApprovalKind.EntityAnalysisModelInlineScript => InlineScriptSubjects(),
                EntityApprovalKind.EntityAnalysisModelInlineFunction => InlineFunctionSubjects(),
                EntityApprovalKind.EntityAnalysisModelGatewayRule => GatewayRuleSubjects(),
                EntityApprovalKind.EntityAnalysisModelSanction => SanctionSubjects(),
                EntityApprovalKind.EntityAnalysisModelAbstractionRule => AbstractionRuleSubjects(),
                EntityApprovalKind.EntityAnalysisModelAbstractionCalculation => AbstractionCalculationSubjects(),
                EntityApprovalKind.EntityAnalysisModelTtlCounter => TtlCounterSubjects(),
                EntityApprovalKind.EntityAnalysisModelHttpAdaptation => HttpAdaptationSubjects(),
                EntityApprovalKind.ExhaustiveSearchInstance => ExhaustiveSearchInstanceSubjects(),
                EntityApprovalKind.EntityAnalysisModelActivationRule => ActivationRuleSubjects(),
                EntityApprovalKind.EntityAnalysisModelTag => TagSubjects(),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        private IQueryable<EntityApprovalSubject> ModelSubjects()
        {
            return dbContext.EntityAnalysisModel
                .Where(m => m.TenantRegistryId == tenantRegistryId)
                .Select(m => new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModel, m.Id,
                    m.Name ?? "", m.Active == 1, m.Deleted == 1, m.Version ?? 1, m.CreatedUser ?? "",
                    m.DeletedUser ?? "", m.Id));
        }

        private IQueryable<EntityApprovalSubject> ListSubjects()
        {
            return from l in dbContext.EntityAnalysisModelList
                join m in dbContext.EntityAnalysisModel on l.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelList, l.Id,
                    l.Name ?? "", l.Active == 1, l.Deleted == 1, l.Version ?? 1, l.CreatedUser ?? "",
                    l.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> ListValueSubjects()
        {
            return from v in dbContext.EntityAnalysisModelListValue
                join l in dbContext.EntityAnalysisModelList on v.EntityAnalysisModelListId equals l.Id
                join m in dbContext.EntityAnalysisModel on l.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelListValue, v.Id,
                    v.ListValue ?? "", true, v.Deleted == 1, v.Version ?? 1, v.CreatedUser ?? "",
                    v.DeletedUser ?? "", m.Id, v.EntityAnalysisModelListId);
        }

        private IQueryable<EntityApprovalSubject> DictionarySubjects()
        {
            return from d in dbContext.EntityAnalysisModelDictionary
                join m in dbContext.EntityAnalysisModel on d.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelDictionary, d.Id,
                    d.Name ?? "", d.Active == 1, d.Deleted == 1, d.Version ?? 1, d.CreatedUser ?? "",
                    d.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> KvpSubjects()
        {
            return from k in dbContext.EntityAnalysisModelDictionaryKvp
                join d in dbContext.EntityAnalysisModelDictionary on k.EntityAnalysisModelDictionaryId equals d.Id
                join m in dbContext.EntityAnalysisModel on d.EntityAnalysisModelGuid equals m.Guid
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelDictionaryKvp, k.Id,
                    k.KvpKey ?? "", true, k.Deleted == 1, k.Version ?? 1, k.CreatedUser ?? "",
                    k.DeletedUser ?? "", m.Id, k.EntityAnalysisModelDictionaryId);
        }

        private IQueryable<EntityApprovalSubject> RequestXPathSubjects()
        {
            return from x in dbContext.EntityAnalysisModelRequestXpath
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelRequestXPath, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> InlineScriptSubjects()
        {
            return from x in dbContext.EntityAnalysisModelInlineScript
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelInlineScript, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> InlineFunctionSubjects()
        {
            return from x in dbContext.EntityAnalysisModelInlineFunction
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelInlineFunction, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> GatewayRuleSubjects()
        {
            return from x in dbContext.EntityAnalysisModelGatewayRule
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelGatewayRule, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> SanctionSubjects()
        {
            return from x in dbContext.EntityAnalysisModelSanction
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelSanction, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> AbstractionRuleSubjects()
        {
            return from x in dbContext.EntityAnalysisModelAbstractionRule
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelAbstractionRule, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> AbstractionCalculationSubjects()
        {
            return from x in dbContext.EntityAnalysisModelAbstractionCalculation
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelAbstractionCalculation, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> TtlCounterSubjects()
        {
            return from x in dbContext.EntityAnalysisModelTtlCounter
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelTtlCounter, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> HttpAdaptationSubjects()
        {
            return from x in dbContext.EntityAnalysisModelHttpAdaptation
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelHttpAdaptation, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> ExhaustiveSearchInstanceSubjects()
        {
            return from x in dbContext.ExhaustiveSearchInstance
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.ExhaustiveSearchInstance, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> ActivationRuleSubjects()
        {
            return from x in dbContext.EntityAnalysisModelActivationRule
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelActivationRule, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }

        private IQueryable<EntityApprovalSubject> TagSubjects()
        {
            return from x in dbContext.EntityAnalysisModelTag
                join m in dbContext.EntityAnalysisModel on x.EntityAnalysisModelId equals m.Id
                where m.TenantRegistryId == tenantRegistryId
                select new EntityApprovalSubject(EntityApprovalKind.EntityAnalysisModelTag, x.Id,
                    x.Name ?? "", x.Active == 1, x.Deleted == 1, x.Version ?? 1, x.CreatedUser ?? "",
                    x.DeletedUser ?? "", m.Id);
        }
    }
}