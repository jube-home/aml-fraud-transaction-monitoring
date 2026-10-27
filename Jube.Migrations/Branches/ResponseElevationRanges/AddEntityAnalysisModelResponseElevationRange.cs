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

namespace Jube.Migrations.Branches.ResponseElevationRanges
{
    using FluentMigrator;

    [Migration(20263108150000)]
    public class AddEntityAnalysisModelResponseElevationRange : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisModelResponseElevationRange")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Minimum").AsInt32().Nullable()
                .WithColumn("Maximum").AsInt32().Nullable()
                .WithColumn("Decline").AsByte().Nullable()
                .WithColumn("Hold").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Content").AsString().Nullable()
                .WithColumn("Redirect").AsString().Nullable()
                .WithColumn("ForeColor").AsString().Nullable()
                .WithColumn("BackColor").AsString().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelResponseElevationRange")
                .OnColumn("EntityAnalysisModelId").Ascending()
                .OnColumn("Deleted").Ascending();

            Create.ForeignKey().FromTable("EntityAnalysisModelResponseElevationRange")
                .ForeignColumn("EntityAnalysisModelId")
                .ToTable("EntityAnalysisModel").PrimaryColumn("Id");

            Create.Table("EntityAnalysisModelResponseElevationRangeVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelResponseElevationRangeId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Minimum").AsInt32().Nullable()
                .WithColumn("Maximum").AsInt32().Nullable()
                .WithColumn("Decline").AsByte().Nullable()
                .WithColumn("Hold").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Content").AsString().Nullable()
                .WithColumn("Redirect").AsString().Nullable()
                .WithColumn("ForeColor").AsString().Nullable()
                .WithColumn("BackColor").AsString().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.Index().OnTable("EntityAnalysisModelResponseElevationRangeVersion")
                .OnColumn("EntityAnalysisModelResponseElevationRangeId").Ascending();

            Execute.Sql("""
                        DO $$
                        DECLARE collision record;
                        BEGIN
                            FOR collision IN
                                SELECT "EntityAnalysisModelId" AS model, round("ResponseElevation")::integer AS level,
                                       count(DISTINCT "ResponseElevation") AS originals
                                FROM public."EntityAnalysisModelActivationRule"
                                WHERE coalesce("Deleted", 0) = 0 AND "EnableResponseElevation" = 1
                                  AND "ResponseElevation" IS NOT NULL
                                GROUP BY "EntityAnalysisModelId", round("ResponseElevation")::integer
                                HAVING count(DISTINCT "ResponseElevation") > 1
                            LOOP
                                RAISE NOTICE 'Model % has % distinct response elevations that round to %; they share one range and must be reviewed by hand.',
                                    collision.model, collision.originals, collision.level;
                            END LOOP;
                        END $$;

                        INSERT INTO public."EntityAnalysisModelResponseElevationRange"
                            ("Guid", "EntityAnalysisModelId", "Name", "Minimum", "Maximum", "Decline", "Hold", "Locked",
                             "Active", "Content", "Redirect", "ForeColor", "BackColor", "EnableHttpEndpoint",
                             "EnableNotification", "CreatedDate", "CreatedUser", "Deleted", "Version")
                        SELECT gen_random_uuid(), r."EntityAnalysisModelId", 'Level ' || r."Level", r."Level", r."Level",
                               0, 0, 0, 1, r."ResponseElevationContent", r."ResponseElevationRedirect",
                               r."ResponseElevationForeColor", r."ResponseElevationBackColor", 0, 0,
                               now() at time zone 'utc', 'Administrator', 0, 1
                        FROM (
                            SELECT DISTINCT ON ("EntityAnalysisModelId", round("ResponseElevation")::integer)
                                   "EntityAnalysisModelId", round("ResponseElevation")::integer AS "Level",
                                   "ResponseElevationContent", "ResponseElevationRedirect",
                                   "ResponseElevationForeColor", "ResponseElevationBackColor"
                            FROM public."EntityAnalysisModelActivationRule"
                            WHERE coalesce("Deleted", 0) = 0 AND "EnableResponseElevation" = 1
                              AND "ResponseElevation" IS NOT NULL AND "EntityAnalysisModelId" IS NOT NULL
                            ORDER BY "EntityAnalysisModelId", round("ResponseElevation")::integer, "Id"
                        ) r;

                        INSERT INTO public."EntityApproval" ("TenantRegistryId", "EntityApprovalKindId", "EntityId",
                            "EntityVersion", "StateId", "CreatedDate", "CreatedUser", "Guid")
                        SELECT m."TenantRegistryId", 19, r."Id", coalesce(r."Version", 1), 1,
                               now() at time zone 'utc', 'Administrator', gen_random_uuid()
                        FROM public."EntityAnalysisModelResponseElevationRange" r
                        JOIN public."EntityAnalysisModel" m ON m."Id" = r."EntityAnalysisModelId"
                        WHERE coalesce(r."Deleted", 0) = 0 AND m."TenantRegistryId" IS NOT NULL;

                        ALTER TABLE public."EntityAnalysisModelActivationRule"
                            ALTER COLUMN "ResponseElevation" TYPE integer USING round("ResponseElevation")::integer;
                        ALTER TABLE public."EntityAnalysisModelActivationRuleVersion"
                            ALTER COLUMN "ResponseElevation" TYPE integer USING round("ResponseElevation")::integer;

                        COMMENT ON TABLE public."EntityAnalysisModelResponseElevationRange" IS 'A band of response elevation levels defined under a model. An activation rule may only elevate the response to a level that falls inside an active range of its model, and the highest level reached by a transaction resolves to exactly one range, which says whether the outcome is a decline or a hold and carries the asynchronous web hook and notification fired for it. Ranges of one model must not overlap. A single level is a range whose Minimum and Maximum are equal.';
                        COMMENT ON TABLE public."EntityAnalysisModelResponseElevationRangeVersion" IS 'The earlier state of an EntityAnalysisModelResponseElevationRange recorded each time it is updated or deleted; the live row is in EntityAnalysisModelResponseElevationRange.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Minimum" IS 'The lowest response elevation level, inclusive, that falls in this range. The receiving system expects an integer.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Maximum" IS 'The highest response elevation level, inclusive, that falls in this range; equal to Minimum for a single level.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Decline" IS 'When 1 the outcome of reaching this range is a decline. A decline never holds, so Decline and Hold are never both 1.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Hold" IS 'When 1 the outcome of reaching this range is that the payment is held for review. Never 1 together with Decline.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Content" IS 'Free text served in the response when this range is reached, for example the message to show the customer.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."Redirect" IS 'A URL to redirect the user or advertising exchange to when this range is reached.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."ForeColor" IS 'The fore colour, as hex, sent to the Activation Watcher and in the response when this range is reached.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."BackColor" IS 'The back colour, as hex, sent to the Activation Watcher and in the response when this range is reached.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."EnableHttpEndpoint" IS 'When 1 a web hook is called asynchronously when this range is reached.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."HttpEndpointTypeId" IS 'The HTTP method of the web hook: 1 POST, 2 GET.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."EnableNotification" IS 'When 1 a notification is sent asynchronously when this range is reached.';
                        COMMENT ON COLUMN public."EntityAnalysisModelResponseElevationRange"."NotificationTypeId" IS 'The notification channel: 1 Email, 2 SMS.';
                        COMMENT ON COLUMN public."EntityAnalysisModelActivationRule"."ResponseElevation" IS 'The integer response elevation level this rule raises when it matches. It must fall in an active EntityAnalysisModelResponseElevationRange of the model for the elevation to be sent out. Was a floating point number before ranges were introduced; existing values were rounded and given a range each.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
