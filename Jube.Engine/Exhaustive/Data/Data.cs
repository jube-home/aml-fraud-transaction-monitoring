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
using System.Collections.Generic;
using System.Linq;
using Jube.Data.Context;
using Jube.Data.Query;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Variables;
using Newtonsoft.Json.Linq;

namespace Jube.Engine.Exhaustive.Data
{
    public static class Extraction
    {
        public static void GetClassData(DbContext dbContext,
            int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            Dictionary<int, Variable> variables,
            double[][] existingData,
            double[] existingOutputs,
            bool mockData,
            out double[][] newData, 
            out double[] outputs)
        {
            var dataList = existingData.ToList();
            List<double> outputsList; 
            if(existingOutputs != null)
            {
                outputsList = existingOutputs.ToList();
            }
            else
            {
                outputsList = new List<double>();
            
                for (var i = 0; i < dataList.Count; i++)
                {
                    outputsList.Add(0);
                }
            }
            
            var postgres = new Postgres(dbContext.ConnectionString);
            
            foreach (var json in
                postgres.ExecuteReturnOnlyJsonFromArchiveSample(entityAnalysisModelId, filterSql, filterTokens, 10000,mockData))
            {
                var jObject = JObject.Parse(json);

                var row = new double[variables.Count];
                for (var i = 0; i < row.Length; i++)
                {
                    double value = 0;
                    try
                    {
                        var jToken = jObject.SelectToken(variables[i].ValueJsonPath);
                        if (jToken != null)
                        {
                            value = jToken.Value<double>();
                        }
                    }
                    catch
                    {
                        //Ignored
                    }

                    row[i] = value;
                }

                dataList.Add(row);
                outputsList.Add(1);
            }

            var shuffleArray = new int[outputsList.Count];
            for (var i = 0; i < shuffleArray.Length; i++)
            {
                shuffleArray[i] = i;
            }
            var r = new Random();
            shuffleArray = shuffleArray.OrderBy(_ => r.Next()).ToArray();
            
            var newDataBeforeShuffle = dataList.ToArray();
            var outputsBeforeShuffle = outputsList.ToArray();

            var newDataAfterShuffle = new double[shuffleArray.Length][];
            var outputsAfterShuffle = new double[shuffleArray.Length];

            for (var i = 0; i < shuffleArray.Length; i++)
            {
                newDataAfterShuffle[i] = newDataBeforeShuffle[shuffleArray[i]];
                outputsAfterShuffle[i] = outputsBeforeShuffle[shuffleArray[i]];
            }

            newData = newDataAfterShuffle;
            outputs = outputsAfterShuffle;
        }

        public static void GetSampleData(DbContext dbContext,
            int tenantRegistryId,
            int entityAnalysisModelId,
            bool mockData,
            out Dictionary<int, Variable> variables,
            out double[][] data)
        {
            if (mockData)
            {
                var mockArchiveRepository = new MockArchiveRepository(dbContext);
                var jsonList = 
                    mockArchiveRepository.GetJsonByEntityAnalysisModelIdRandomLimit(entityAnalysisModelId, 10000);
                
                ProcessJson(dbContext, tenantRegistryId, entityAnalysisModelId,true, out variables, out data, jsonList);
            }
            else
            {
                var archiveRepository = new ArchiveRepository(dbContext);
                var jsonList = 
                    archiveRepository.GetJsonByEntityAnalysisModelIdRandomLimit(entityAnalysisModelId, 10000);
                
                ProcessJson(dbContext, tenantRegistryId, entityAnalysisModelId,false, out variables, out data, jsonList);
            }
        }
    
        public static void GetSampleData(DbContext dbContext, 
            int tenantRegistryId,
            int entityAnalysisModelId,
            string filterSql,
            string filterTokens,
            bool mockData,
            out Dictionary<int, Variable> variables, 
            out double[][] data)
        {
            var postgres = new Postgres(dbContext.ConnectionString);
            var jsonList = postgres.ExecuteReturnOnlyJsonFromArchiveSample(entityAnalysisModelId,
                "NOT (" + filterSql + ")",
                filterTokens, 10000,mockData);
            
            ProcessJson(dbContext, tenantRegistryId, entityAnalysisModelId,mockData, out variables, out data, jsonList);
        }

        private static void ProcessJson(DbContext dbContext, int tenantRegistryId, int entityAnalysisModelId,
            bool mockData, out Dictionary<int, Variable> variables, out double[][] data, IEnumerable<string> jsonList)
        {
            variables = new Dictionary<int, Variable>();
            var headerSequence = 0;

            if (!mockData)
            {
                var getModelFieldByEntityAnalysisModelIdParseTypeIdQuery =
                    new GetModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext, tenantRegistryId);
            
                var fields = getModelFieldByEntityAnalysisModelIdParseTypeIdQuery
                    .Execute(entityAnalysisModelId, 5, true).ToList();
            
                foreach (var variable in from field in fields where field.ProcessingTypeId is 3 or 5 or 7 select new Variable
                {
                    Name = field.Name,
                    ProcessingTypeId = field.ProcessingTypeId,
                    ValueJsonPath = field.ValueJsonPath
                })
                {
                    variables.Add(headerSequence, variable);
                    headerSequence += 1;
                }    
            }
            else
            {
                variables.Add(headerSequence,new Variable
                {
                    Name = "Abstraction.IsChip",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsChip"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.IsSwipe",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsSwipe"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.IsManual",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.IsManual"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactions1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.Authenticated",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.Authenticated"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountTransactionsPINDecline1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactionsPINDecline1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountTransactionsDeclined1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountTransactionsDeclined1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountUnsafeTerminals1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountUnsafeTerminals1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountInPerson1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountInPerson1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountInternet1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountInternet1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.ATM",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.ATM"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountATM1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountATM1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountOver30SEK1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountOver30SEK1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.InPerson",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.InPerson"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.TransactionAmt",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.TransactionAmt"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.SumTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.SumTransactions1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.SumATMTransactions1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.SumATMTransactions1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.Foreign",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.Foreign"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.DifferentCountryTransactions1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCountryTransactions1Week"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.DifferentMerchantTypes1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentMerchantTypes1Week"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.DifferentDeclineReasons1Day",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentDeclineReasons1Day"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.DifferentCities1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCities1Week"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.DifferentCities1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.DifferentCities1Week"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CountSameMerchantUsedBefore1Week",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CountSameMerchantUsedBefore1Week"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.HasBeenAbroad",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.HasBeenAbroad"
                });
                
                variables.Add(headerSequence+=1,new Variable
                {
                    Name = "Abstraction.CashTransaction",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.CashTransaction"
                });
                
                variables.Add(headerSequence+1,new Variable
                {
                    Name = "Abstraction.HighRiskCountry",
                    ProcessingTypeId = 5,
                    ValueJsonPath = "$.abstraction.HighRiskCountry"
                });
            }
            
            var rows = new List<double[]>();
            foreach (var json in jsonList)
            {
                var jObject = JObject.Parse(json);

                var row = new double[variables.Count];
                for (var i = 0; i < row.Length; i++)
                {
                    double value = 0;
                    try
                    {
                        var jToken = jObject.SelectToken(variables[i].ValueJsonPath);
                        if (jToken != null)
                        {
                            value = jToken.Value<double>();
                        }
                    }
                    catch
                    {
                        //Ignored
                    }

                    row[i] = value;
                }

                rows.Add(row);
            }

            data = rows.ToArray();
        }
    }
}