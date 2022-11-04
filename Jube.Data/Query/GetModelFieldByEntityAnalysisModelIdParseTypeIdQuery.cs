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

using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Repository;

namespace Jube.Data.Query
{
    public class GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery
    {
        private readonly DbContext _dbContext;
        private readonly int _tenantRegistryId;

        public GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(DbContext dbContext, string userName)
        {
            _dbContext = dbContext;
            _tenantRegistryId = _dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(DbContext dbContext, int tenantRegistryId)
        {
            _dbContext = dbContext;
            _tenantRegistryId = tenantRegistryId;
        }

        public GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IEnumerable<Dto> Execute(int entityAnalysisModelId, int parserTypeId, bool reporting)
        {
            var getModelFieldByParserTypeIdDtos = new List<Dto>();

            if (parserTypeId > 0)
            {
                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXPathRepository(_dbContext, _tenantRegistryId);

                foreach (var entityAnalysisModelRequestXpath in entityAnalysisModelRequestXPathRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId))
                {
                    var getModelFieldByParserTypeIdDto = new Dto
                    {
                        Name = $"Payload.{entityAnalysisModelRequestXpath.Name}",
                        Value = $"Payload.{entityAnalysisModelRequestXpath.Name}",
                        ValueJsonPath = $"payload.{entityAnalysisModelRequestXpath.Name}",
                        Group = "Payload",
                        ProcessingTypeId = 1
                    };

                    switch (entityAnalysisModelRequestXpath.DataTypeId)
                    {
                        case 1:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')";
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "string";
                            getModelFieldByParserTypeIdDto.DataTypeId = 1;

                            break;
                        case 2:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::int";
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "integer";
                            getModelFieldByParserTypeIdDto.DataTypeId = 2;

                            break;
                        case 3:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::double precision";
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "double";
                            getModelFieldByParserTypeIdDto.DataTypeId = 3;

                            break;
                        case 4:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::timestamp";
                            getModelFieldByParserTypeIdDto.DataTypeId = 4;
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "datetime";

                            break;
                        case 5:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::boolean";
                            getModelFieldByParserTypeIdDto.DataTypeId = 5;
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "boolean";

                            break;
                        case 6:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::double precision";
                            getModelFieldByParserTypeIdDto.DataTypeId = 6;
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "double";

                            break;
                        case 7:
                            getModelFieldByParserTypeIdDto.ValueSqlPath
                                = $"(\"Json\"-> 'payload' -> '{entityAnalysisModelRequestXpath.Name}')::double precision";
                            getModelFieldByParserTypeIdDto.DataTypeId = 7;
                            getModelFieldByParserTypeIdDto.JQueryBuilderDataType = "double";

                            break;
                    }

                    if (entityAnalysisModelRequestXpath.DataTypeId != null)
                        getModelFieldByParserTypeIdDto.DataTypeId = entityAnalysisModelRequestXpath.DataTypeId.Value;

                    getModelFieldByParserTypeIdDtos.Add(getModelFieldByParserTypeIdDto);
                }

                var entityAnalysisModelDictionaryRepository =
                    new EntityAnalysisModelDictionaryRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelDictionaryRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId)
                    .Select(s => new Dto
                    {
                        Name = $"Dictionary.{s.Name}",
                        Value = $"Dictionary.{s.Name}",
                        ValueJsonPath = $"kvp.{s.Name}",
                        ValueSqlPath = $"(\"Json\"-> 'kvp' -> '{s.Name}')::double precision",
                        DataTypeId = 3,
                        JQueryBuilderDataType = "double",
                        Group = "Reference",
                        ProcessingTypeId = 2
                    }));
            }

            if (parserTypeId >= 3)
            {
                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelTtlCounterRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"TTLCounter.{s.Name}",
                            Value = $"TTLCounter.{s.Name}",
                            ValueJsonPath = $"ttlCounter.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'ttlCounter' -> '{s.Name}')::double precision",
                            DataTypeId = 2,
                            JQueryBuilderDataType = "double",
                            Group = "TTLCounter",
                            ProcessingTypeId = 3
                        }));

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelSanctionRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Sanction.{s.Name}",
                            Value = $"Sanction.{s.Name}",
                            ValueJsonPath = $"sanction.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'sanction' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Sanction",
                            ProcessingTypeId = 4
                        }));
            }

            if (parserTypeId >= 4)
            {
                //Abstraction Calculation, needs abstraction

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelAbstractionRuleRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Abstraction.{s.Name}",
                            Value = $"Abstraction.{s.Name}",
                            ValueJsonPath = $"abstraction.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'abstraction' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Abstraction",
                            ProcessingTypeId = 5
                        }));
            }

            if (parserTypeId >= 5)
            {
                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelAbstractionCalculationRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"AbstractionCalculation.{s.Name}",
                            Value = $"AbstractionCalculation.{s.Name}",
                            ValueJsonPath = $"abstractionCalculation.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'abstractionCalculation' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Abstraction",
                            ProcessingTypeId = 7
                        }));
            }

            if (parserTypeId >= 6)
            {
                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelHttpAdaptationRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Adaptation.{s.Name}",
                            Value = $"Adaptation.{s.Name}",
                            ValueJsonPath = $"adaptation.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'adaptation' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Adaptation",
                            ProcessingTypeId = 7
                        }));

                var exhaustiveSearchInstanceRepository =
                    new ExhaustiveSearchInstanceRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(exhaustiveSearchInstanceRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Adaptation.{s.Name}",
                            Value = $"Adaptation.{s.Name}",
                            ValueJsonPath = $"adaptation.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'adaptation' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Adaptation"
                        }));
            }

            if (reporting)
            {
                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelActivationRuleRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Activation.{s.Name}",
                            Value = $"Activation.{s.Name}",
                            ValueJsonPath = $"activation.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'abstraction' -> '{s.Name}')::double precision",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "double",
                            Group = "Activation"
                        }));

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelTagRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"Tag.{s.Name}",
                            Value = $"Tag.{s.Name}",
                            ValueJsonPath = $"tag.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'tag' -> '{s.Name}')::double precision",
                            DataTypeId = 7,
                            JQueryBuilderDataType = "double",
                            Group = "Tag"
                        }));
            }
            else
            {
                var entityAnalysisModelListRepository =
                    new EntityAnalysisModelListRepository(_dbContext, _tenantRegistryId);

                getModelFieldByParserTypeIdDtos.AddRange(entityAnalysisModelListRepository
                    .GetByEntityAnalysisModelId(entityAnalysisModelId).Select(s =>
                        new Dto
                        {
                            Name = $"List.{s.Name}",
                            Value = $"List.{s.Name}",
                            DataTypeId = 3,
                            JQueryBuilderDataType = "list",
                            Group = "Reference",
                            ValueJsonPath = $"$.list.{s.Name}",
                            ValueSqlPath = $"(\"Json\"-> 'list' -> '{s.Name}')"
                        }));
            }

            return getModelFieldByParserTypeIdDtos;
        }

        public class Dto
        {
            public string Name { get; set; }
            public string Value { get; set; }
            public string ValueJsonPath { get; set; }
            public string ValueSqlPath { get; set; }
            public int DataTypeId { get; set; }
            public byte ProcessingTypeId { get; set; }
            public string Group { get; set; }
            public string JQueryBuilderDataType { get; set; }
        }
    }
}