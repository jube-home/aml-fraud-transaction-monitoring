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

namespace Jube.Migrations.Branches.DatasourceBuilder
{
    using FluentMigrator;

    [Migration(20263107790000)]
    public class ReplaceKendoDisplayWithDisplayKinds : Migration
    {
        public override void Up()
        {
            Alter.Table("VisualisationRegistryDatasource")
                .AddColumn("DisplayKindId").AsInt32().Nullable()
                .AddColumn("TemplateText").AsString().Nullable();

            Alter.Table("VisualisationRegistryDatasourceVersion")
                .AddColumn("DisplayKindId").AsInt32().Nullable()
                .AddColumn("TemplateText").AsString().Nullable();

            Alter.Table("VisualisationRegistryDatasourceSeries")
                .AddColumn("Ordinal").AsInt32().NotNullable().WithDefaultValue(0)
                .AddColumn("RoleId").AsInt32().Nullable()
                .AddColumn("RoleOrder").AsInt32().Nullable();

            Execute.Sql("""
                        UPDATE "VisualisationRegistryDatasourceSeries" s
                        SET "Ordinal" = ordered.position
                        FROM (
                            SELECT "Id",
                                   ROW_NUMBER() OVER (PARTITION BY "VisualisationRegistryDatasourceId" ORDER BY "Id") AS position
                            FROM "VisualisationRegistryDatasourceSeries"
                        ) ordered
                        WHERE s."Id" = ordered."Id";
                        """);

            Execute.Sql("""
                        UPDATE "VisualisationRegistryDatasource"
                        SET "DisplayKindId" = 11
                        WHERE "DisplayKindId" IS NULL
                          AND "Name" = 'ExamplePie'
                          AND "CreatedUser" = 'Administrator'
                          AND "VisualisationText" LIKE '%type: "pie"%';

                        UPDATE "VisualisationRegistryDatasource"
                        SET "DisplayKindId" = 2
                        WHERE "DisplayKindId" IS NULL
                          AND "Name" = 'ExampleBar'
                          AND "CreatedUser" = 'Administrator'
                          AND "VisualisationText" LIKE '%type: "bar"%';

                        UPDATE "VisualisationRegistryDatasource"
                        SET "IncludeGrid" = 1
                        WHERE ("DisplayKindId" IS NULL OR "DisplayKindId" = 0)
                          AND ("IncludeGrid" IS NULL OR "IncludeGrid" = 0);

                        UPDATE "VisualisationRegistryDatasourceVersion"
                        SET "IncludeGrid" = 1
                        WHERE ("DisplayKindId" IS NULL OR "DisplayKindId" = 0)
                          AND ("IncludeGrid" IS NULL OR "IncludeGrid" = 0);
                        """);

            foreach (var table in new[] { "VisualisationRegistryDatasource", "VisualisationRegistryDatasourceVersion" })
            {
                Delete.Column("VisualisationTypeId").FromTable(table);
                Delete.Column("VisualisationText").FromTable(table);
                Delete.Column("IncludeDisplay").FromTable(table);
            }

            Execute.Sql("""
                        COMMENT ON COLUMN public."VisualisationRegistryDatasource"."DisplayKindId" IS
                        'Which chart the Datasource is drawn as, an enumeration id from DatasourceDisplayKind: 1=Column, 2=Bar, 3=StackedColumn, 4=StackedBar, 5=PercentStackedColumn, 6=PercentStackedBar, 7=Line, 8=Area, 9=StackedArea, 10=PercentStackedArea, 11=Pie, 12=Donut, 13=Scatter, 14=Histogram, 15=Heatmap, 16=Combo, 17=BoxPlot, 18=Candlestick, 19=Waterfall, 20=Funnel, 21=Radar, 22=Polar, 23=Gauge, 24=Bullet, 25=Sparkline, 26=Treemap, 27=Gantt, 28=Map, 29=Template. NULL or 0 means no chart is drawn and the Datasource is shown as a grid only. The chart is drawn on the server as an SVG from the columns of the Command, each column filling a chart role by position unless the RoleId of a series row says otherwise. Replaces the legacy VisualisationTypeId and VisualisationText, which held a Kendo JavaScript literal.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasource"."TemplateText" IS
                        'The template the Datasource is drawn from when DisplayKindId is 29 (Template): markup in a small Vue-style dialect, rendered on the server with every value escaped. NULL when unset or when the Datasource is drawn as another kind.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasourceVersion"."DisplayKindId" IS
                        'The DisplayKindId the Datasource held at this version. See the same column on VisualisationRegistryDatasource.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasourceVersion"."TemplateText" IS
                        'The TemplateText the Datasource held at this version. See the same column on VisualisationRegistryDatasource.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasourceSeries"."Ordinal" IS
                        'Position of the column in the SELECT list of the Datasource Command, counting from 1. Charts take their series from the columns in this order. Rebuilt every time the Datasource is saved. Rows written before this column existed were numbered by Id.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasourceSeries"."RoleId" IS
                        'The chart role this column has been explicitly assigned to, an enumeration id from DatasourceChartRole: 1=Category, 2=Value, 3=Series, 4=X, 5=Y, 6=Size, 7=Open, 8=High, 9=Low, 10=Close, 11=Minimum, 12=FirstQuartile, 13=Median, 14=ThirdQuartile, 15=Maximum, 16=Latitude, 17=Longitude, 18=Country, 19=Label, 20=Parent, 21=Start, 22=End, 23=Target, 24=Row, 25=Column, 26=Line, 27=SecondaryLine. NULL means the column has no explicit role: when the chart is drawn it is assigned a role by position, so most columns of most Datasources are NULL. A column holds at most one role.';
                        COMMENT ON COLUMN public."VisualisationRegistryDatasourceSeries"."RoleOrder" IS
                        'Order among the columns assigned to the same RoleId, counting from 1, for roles that take several columns such as Value, where each column is drawn as its own series. NULL when RoleId is NULL.';
                        """);
        }

        public override void Down()
        {
        }
    }
}
