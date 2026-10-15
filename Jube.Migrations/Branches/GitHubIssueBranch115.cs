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

namespace Jube.Migrations.Branches
{
    using FluentMigrator;

    [Migration(20260222075400)]
    public class GitHubIssueBranch115 : Migration
    {
        public override void Up()
        {
            UpdateCaseWorkflowDisplayForNewTokenisationFormat();
            UpdateCaseWorkflowFilterForIncorrectCaseWorkflowStatusGuidToName();
        }

        public override void Down()
        {
        }

        private void UpdateCaseWorkflowFilterForIncorrectCaseWorkflowStatusGuidToName()
        {
            Execute.Sql("""
                        update "CaseWorkflowFilter"
                        set "SelectJson" =
                                jsonb_set(
                                        "SelectJson",
                                        array ['rules', elem_index::text, 'field'],
                                        '"\"CaseWorkflowStatus\".\"Name\""'::jsonb,
                                        true)
                        from (select pos - 1 as elem_index
                              from "CaseWorkflowFilter" s,
                                   jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                              where elem ->> 'id' = 'CaseWorkflowStatus') sub; 
                        """);
        }

        private void UpdateCaseWorkflowDisplayForNewTokenisationFormat()
        {
            Execute.Sql("""
                        update "CaseWorkflowDisplay" set "Html" = 
                        replace("Html",'<div style=''font-size:30px''>[@CurrencyAmount@]</div>','<div style=''font-size:30px''>[@Payload.AccountId@]</div>')
                        where "Html" like '%<div style=''font-size:30px''>[@CurrencyAmount@]</div>%'
                        """);
        }
    }
}