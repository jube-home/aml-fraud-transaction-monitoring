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

    [Migration(20260209125900)]
    public class GitHubIssueBranch108 : Migration
    {
        public override void Up()
        {
            AddPriorityToEntityAnalysisModelActivationRule();
            ChangeDataTypeForEntityAnalysisModelGatewayRulePriority();
            UpdateVbInlineScript();
        }

        public override void Down()
        {
        }

        private void UpdateVbInlineScript()
        {
            const string code = """
                                Imports System
                                Imports System.Net.Http
                                Imports System.Threading
                                Imports System.Threading.Tasks
                                Imports Newtonsoft.Json.Linq
                                Imports Jube.Engine.Attributes
                                Imports Jube.Engine.Attributes.Events
                                Imports Jube.Engine.Attributes.Properties
                                Imports Jube.Engine.EntityAnalysisModelInvoke.Context
                                Imports Jube.Engine.Interfaces

                                Public Class IssueOTP
                                Implements IInlineScript
                                	Public Property OTP As String

                                    <PayloadEvent>
                                	Public Async Function ExecuteAsync(context As Context) As Task(Of Boolean) Implements IInlineScript.ExecuteAsync
                                        OTP = RandomDigits(6)
                                        Return True
                                    End Function
                                	
                                	Private Function RandomDigits(ByVal length As Integer) As String
                                	    Dim random = New Random()
                                	    Dim s As String = String.Empty

                                	    For i As Integer = 0 To length - 1
                                		s = String.Concat(s, random.[Next](10).ToString())
                                	    Next

                                	    Return s
                                	End Function	
                                End Class
                                """;

            Update.Table("EntityAnalysisInlineScript").Set(new
            {
                Code = code
            }).Where(new
            {
                Id = 1
            });
        }

        private void AddPriorityToEntityAnalysisModelActivationRule()
        {
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("Priority").AsDouble().Nullable();
            Alter.Table("EntityAnalysisModelActivationRuleVersion").AddColumn("Priority").AsDouble().Nullable();
            Update.Table("EntityAnalysisModelActivationRule").Set(new { Priority = 0 }).AllRows();
            Update.Table("EntityAnalysisModelActivationRuleVersion").Set(new { Priority = 0 }).AllRows();
        }

        private void ChangeDataTypeForEntityAnalysisModelGatewayRulePriority()
        {
            Alter.Table("EntityAnalysisModelGatewayRule").AlterColumn("Priority").AsDouble();
            Alter.Table("EntityAnalysisModelGatewayRuleVersion").AlterColumn("Priority").AsDouble();
        }
    }
}