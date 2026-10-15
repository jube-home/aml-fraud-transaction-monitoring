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

using FluentMigrator;

namespace Jube.Migrations.Branches.AskJooby
{
    [Migration(20263107720000)]
    public class AddExhaustiveSearchInstanceDatasetTables : Migration
    {
        public override void Up()
        {
            CreateStore("ExhaustiveInstanceCsv", "FileName", "Delimiter", 1);
            CreateStore("ExhaustiveInstanceJson", "SourceLabel", "ContentHash", 64);

            Delete.Column("Delimiter").FromTable("ExhaustiveInstanceCsv");
            Alter.Table("ExhaustiveInstanceCsv").AddColumn("DelimiterId").AsByte().Nullable();

            Alter.Table("ExhaustiveSearchInstance")
                .AddColumn("DataSourceId").AsByte().Nullable().WithDefaultValue(0)
                .AddColumn("ExhaustiveInstanceCsvId").AsInt32().Nullable()
                .AddColumn("ExhaustiveInstanceJsonId").AsInt32().Nullable();

            Execute.Sql("update \"ExhaustiveSearchInstance\" set \"DataSourceId\" = 0 where \"DataSourceId\" is null");

            Create.ForeignKey("FK_ESI_InstanceCsv")
                .FromTable("ExhaustiveSearchInstance").ForeignColumn("ExhaustiveInstanceCsvId")
                .ToTable("ExhaustiveInstanceCsv").PrimaryColumn("Id");

            Create.ForeignKey("FK_ESI_InstanceJson")
                .FromTable("ExhaustiveSearchInstance").ForeignColumn("ExhaustiveInstanceJsonId")
                .ToTable("ExhaustiveInstanceJson").PrimaryColumn("Id");

            Execute.Sql("alter table \"ExhaustiveSearchInstance\" add constraint \"CK_ESI_DatasetReference\" check ("
                        + "(coalesce(\"DataSourceId\", 0) = 2 and ((\"ExhaustiveInstanceCsvId\" is not null)::int"
                        + " + (\"ExhaustiveInstanceJsonId\" is not null)::int) = 1)"
                        + " or (coalesce(\"DataSourceId\", 0) <> 2 and \"ExhaustiveInstanceCsvId\" is null"
                        + " and \"ExhaustiveInstanceJsonId\" is null))");
        }

        public override void Down()
        {
        }

        private void CreateStore(string table, string sourceColumn, string extraColumn, int extraLength)
        {
            var abbreviation = table.Replace("ExhaustiveInstance", "EI");

            Create.Table(table)
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Guid").AsGuid().NotNullable()
                .WithColumn("TenantRegistryId").AsInt32().NotNullable()
                .WithColumn("Name").AsString(256).NotNullable()
                .WithColumn(sourceColumn).AsString(256).Nullable()
                .WithColumn("ColumnsJson").AsCustom("jsonb").Nullable()
                .WithColumn("ReportJson").AsCustom("jsonb").Nullable()
                .WithColumn(extraColumn).AsString(extraLength).Nullable()
                .WithColumn("DependentColumnIndex").AsInt32().Nullable()
                .WithColumn("RowCount").AsInt64().NotNullable().WithDefaultValue(0)
                .WithColumn("SizeBytes").AsInt64().NotNullable().WithDefaultValue(0)
                .WithColumn("StatusId").AsByte().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedUser").AsString(256).Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString(256).Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey($"FK_{abbreviation}_TenantRegistry")
                .FromTable(table).ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");

            Create.Index($"IX_{abbreviation}_TenantRegistryId")
                .OnTable(table)
                .OnColumn("TenantRegistryId").Ascending();

            Create.Table(table + "Row")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn(table + "Id").AsInt32().NotNullable()
                .WithColumn("EntityAnalysisModelInstanceEntryGuid").AsGuid().NotNullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable();

            Create.ForeignKey($"FK_{abbreviation}Row_Store")
                .FromTable(table + "Row").ForeignColumn(table + "Id")
                .ToTable(table).PrimaryColumn("Id");

            Create.Index($"IX_{abbreviation}Row_StoreId_Guid")
                .OnTable(table + "Row")
                .OnColumn(table + "Id").Ascending()
                .OnColumn("EntityAnalysisModelInstanceEntryGuid").Ascending();
        }
    }
}