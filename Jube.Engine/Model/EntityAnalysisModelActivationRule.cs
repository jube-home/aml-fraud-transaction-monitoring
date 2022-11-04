﻿/* Copyright (C) 2022-present Jube Holdings Limited.
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
using System.Reflection;
using log4net;

namespace Jube.Engine.Model
{
    public class EntityAnalysisModelActivationRule
    {
        public delegate bool Match(Dictionary<string,object> data, Dictionary<string, int> ttlCounter, Dictionary<string,double> abstraction,
            Dictionary<string, double> httpAdaptation,Dictionary<string, double> exhaustiveAdaptation, Dictionary<string, List<string>> list,
            Dictionary<string, double> calculation,
            Dictionary<string, double> sanctions, Dictionary<string, double> kvp, ILog log);
        public int Id { get; init; }
        public bool Visible { get; set; }
        public int RuleScriptTypeId { get; set; }
        public string ActivationRuleScript { get; set; }
        public string Name { get; set; }
        public bool EnableCaseWorkflow { get; set; }
        public int CaseWorkflowId { get; set; }
        public int CaseWorkflowStatusId { get; set; }
        public Assembly ActivationRuleCompile { get; set; }
        public Match ActivationRuleCompileDelegate { get; set; }
        public bool ResponsePayload { get; set; }
        public bool EnableTtlCounter { get; set; }
        public int EntityAnalysisModelTtlCounterId { get; set; }
        public int EntityAnalysisModelIdTtlCounter { get; set; }
        public double ResponseElevation { get; set; }
        public string ResponseElevationContent { get; set; }
        public string ResponseElevationRedirect { get; set; }
        public bool EnableReprocessing { get; set; }
        public bool SendToActivationWatcher { get; set; }
        public string ResponseElevationForeColor { get; set; }
        public string ResponseElevationBackColor { get; set; }
        public char BypassSuspendInterval { get; set; }
        public int BypassSuspendValue { get; set; }
        public double BypassSuspendSample { get; set; }
        public int Counter { get; set; }
        public double ActivationSample { get; set; }
        public bool ReportTable { get; set; }
        public bool EnableNotification { get; set; }
        public int NotificationTypeId { get; set; }
        public string NotificationDestination { get; set; }
        public string NotificationSubject { get; set; }
        public string NotificationBody { get; set; }
        public string CaseKey { get; set; }
        public bool EnableResponseElevation { get; set; }
        public string ResponseElevationKey { get; set; }
    }
}