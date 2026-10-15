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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20263107630000)]
    public class AddSchemaColumnCommentsForEnumerations : Migration
    {
        public override void Up()
        {
            Set("EntityAnalysisModelSanction", "AggregationTypeId",
                "How the Levenshtein distances of every matched sanction entry are combined into the one " +
                "value rules see. 1=Sum, 2=Average, 3=Count, 4=Max, 5=Min, 6=First, 7=Last, 8=Confidence. " +
                "Anything unrecognised, including NULL, behaves as 2 (Average).");
            Set("EntityAnalysisModelSanctionVersion", "AggregationTypeId",
                "How the Levenshtein distances of every matched sanction entry are combined into the one " +
                "value rules see. 1=Sum, 2=Average, 3=Count, 4=Max, 5=Min, 6=First, 7=Last, 8=Confidence. " +
                "Anything unrecognised, including NULL, behaves as 2 (Average).");

            foreach (var table in new[] { "EntityAnalysisModelRequestXpath", "EntityAnalysisModelRequestXpathVersion" })
            {
                Set(table, "DataTypeId",
                    "Datatype the value extracted from the request payload is treated as, which governs " +
                    "what rules may predicate on it. 1=String, 2=Integer, 3=Float, 4=Date, 5=Boolean, " +
                    "6=Latitude, 7=Longitude. Latitude and Longitude are Float in effect.");

                Set(table, "PayloadLocationTypeId",
                    "Where in the request the value is taken from. 1=Body, the JSON post body. Shares its " +
                    "numbering with ReferenceDatePayloadLocationTypeId on EntityAnalysisModel.");
            }

            foreach (var table in new[]
                     {
                         "EntityAnalysisModelRequestXpath", "EntityAnalysisModelRequestXpathVersion",
                         "EntityAnalysisModelInlineFunction", "EntityAnalysisModelInlineFunctionVersion"
                     })
            {
                Set(table, "EncryptionId",
                    "Field level encryption applied before the value is stored and archived. 0=None, " +
                    "1=Deterministic (equal plaintexts encrypt alike, so the value stays searchable and " +
                    "groupable), 2=Random (not searchable). NULL behaves as 0.");
            }

            foreach (var table in new[] { "EntityAnalysisModel", "EntityAnalysisModelVersion" })
            {
                Set(table, "ReferenceDatePayloadLocationTypeId",
                    "Where the transaction's reference date comes from. 1=Body, read from the payload at " +
                    "ReferenceDateXPath; 3=Now, the time of invocation, in which case ReferenceDateXPath " +
                    "is not used.");
            }

            foreach (var table in new[]
                         { "EntityAnalysisModelAbstractionRule", "EntityAnalysisModelAbstractionRuleVersion" })
            {
                Set(table, "OffsetTypeId",
                    "Which part of the matched set of historic transactions the aggregation is taken " +
                    "over. 1=First, 2=Last, 3=Skip First, 4=Take Last.");

                Set(table, "SearchFunctionTypeId",
                    "The aggregation computed over the matched historic transactions. 1=Count, " +
                    "2=Distinct Count, 3=Sum, 4=Average, 5=Median, 6=Kurtosis, 7=Skew, 8=Standard " +
                    "Deviation, 11=Mode, 12=Same Count, 13=Actual Value, 14=Max, 15=Min, 16=Since. The " +
                    "numbering is not contiguous: 9 and 10 are not in use.");
            }

            foreach (var table in new[]
                         { "EntityAnalysisModelActivationRule", "EntityAnalysisModelActivationRuleVersion" })
            {
                Set(table, "ReviewStatusId",
                    "Maker-checker state of the rule. 0=New, 1=New Pending Review, 2=Updated Pending " +
                    "Review, 3=Rejected by Review, 4=Approved by Review.");
            }

            foreach (var table in new[]
                     {
                         "EntityAnalysisModelOverride", "EntityAnalysisModelOverrideVersion",
                         "EntityAnalysisModelActivationRuleOverride",
                         "EntityAnalysisModelActivationRuleOverrideVersion"
                     })
            {
                Set(table, "OverrideKind",
                    "Direction of the override. 0=Suppress, preventing the rule from activating; 1=Force, " +
                    "activating it regardless. Force is only available on a rule that opted into it.");
            }

            foreach (var table in new[]
                     {
                         "CaseWorkflowAction", "CaseWorkflowActionVersion", "CaseWorkflowForm",
                         "CaseWorkflowFormVersion", "CaseWorkflowMacro", "CaseWorkflowMacroVersion",
                         "CaseWorkflowXPath", "CaseWorkflowXPathVersion"
                     })
            {
                Set(table, "HttpEndpointTypeId",
                    "HTTP verb used when calling the configured endpoint. 1=POST, 2=GET.");
            }

            Set("PreservationSnapshot", "SnapshotSourceId",
                "What caused the snapshot. 1=Preservation page, a person acting directly, which is the " +
                "only source that requires the snapshot permission; 2=Model synchronisation; 3=AskJooby, " +
                "taken automatically before the assistant changes configuration.");

            foreach (var table in new[] { "ArchiverStagePerformanceCounter", "ArchiverWarning" })
            {
                Set(table, "StageId",
                    "Stage of the archiver pipeline the counter or warning belongs to. " +
                    "1=Build archive JSON, 2=Case creation dispatch, 3=RDBMS archive write, " +
                    "4=Bulk copy archive buffer.");
            }

            foreach (var table in new[] { "CaseCreationStagePerformanceCounter", "CaseCreationWarning" })
            {
                Set(table, "StageId",
                    "Stage of case creation the counter or warning belongs to. " +
                    "1=Existing case priority lookup, 2=Workflow status lookup and persist, " +
                    "3=Notification, 4=HTTP endpoint.");
            }

            Set("CaptureQueueHealth", "QueueId",
                "Which capture queue the health sample is for. 1=Model invoke warning, 2=Case creation " +
                "warning, 3=Archiver warning, 4=Redis sentinel event, 5=Redis connection event, " +
                "6=OpenTelemetry metric.");
        }

        public override void Down()
        {
        }

        private void Set(string table, string column, string text)
        {
            var quoted = text.Replace("'", "''");

            Execute.Sql($"""
                         DO $$
                         BEGIN
                           IF EXISTS (SELECT 1 FROM information_schema.columns
                                      WHERE table_schema = 'public' AND table_name = '{table}'
                                        AND column_name = '{column}') THEN
                             EXECUTE format('COMMENT ON COLUMN public."{table}"."{column}" IS %L', '{quoted}');
                           END IF;
                         END $$;
                         """);
        }
    }
}