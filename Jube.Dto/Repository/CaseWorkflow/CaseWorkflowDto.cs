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

using Jube.Dto.Repository.CaseWorkflowAction;
using Jube.Dto.Repository.CaseWorkflowDisplay;
using Jube.Dto.Repository.CaseWorkflowFilter;
using Jube.Dto.Repository.CaseWorkflowFormValidator;
using Jube.Dto.Repository.CaseWorkflowForm;
using Jube.Dto.Repository.CaseWorkflowMacro;
using Jube.Dto.Repository.CaseWorkflowReleaseHook;
using Jube.Dto.Repository.CaseWorkflowStatus;
using Jube.Dto.Repository.CaseWorkflowXPath;
using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Interfaces;
using Jube.Dto.Options;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Repository.CaseWorkflow
{
    [FormChild(typeof(CaseWorkflowStatusDto), Label = "Status", Hub = "", Order = 10)]
    [FormChild(typeof(CaseWorkflowXPathDto), Label = "XPath", Hub = "", Order = 20)]
    [FormChild(typeof(CaseWorkflowFormDto), Label = "Form", Hub = "", Order = 30)]
    [FormChild(typeof(CaseWorkflowActionDto), Label = "Action", Hub = "", Order = 40)]
    [FormChild(typeof(CaseWorkflowDisplayDto), Label = "Display", Hub = "", Order = 50)]
    [FormChild(typeof(CaseWorkflowMacroDto), Label = "Macro", Hub = "", Order = 60)]
    [FormChild(typeof(CaseWorkflowFilterDto), Label = "Filter", Hub = "", Order = 70)]
    [FormChild(typeof(CaseWorkflowFormValidatorDto), Label = "Form validators", Hub = "", Order = 80)]
    [FormChild(typeof(CaseWorkflowReleaseHookDto), Label = "Release hooks", Hub = "", Order = 90)]
    [FormEndpoint("CaseWorkflow")]
    [FormRoles]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Visualisation", Order = 20, Collapsed = true)]
    [FormGroup("Risk", Order = 30, Collapsed = true)]
    [FormGroup("Ranking", Order = 35, Collapsed = true)]
    [FormGroup("ServiceLevel", Order = 40, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class CaseWorkflowDto : IUpdated, IActivatable, ILockable
    {
        [Description("Identifier of the Model this Case Workflow belongs to. Set from the parent Model context; " +
                     "not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of the Case Workflow, shown to investigators for navigation. Unique within " +
                     "the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10, Placeholder = "e.g. Card fraud investigation")]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Present on the API contract for backward compatibility, but not backed by any column -- " +
                     "the server never persists a submitted value and always returns null. The legacy edit form " +
                     "never exposed an input for it either.")]
        public string? Description { get; set; }

        [Description("When true, the Case Workflow participates in case creation -- new cases can be raised " +
                     "against it.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("When true, the case is locked to read-only and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        public bool Locked { get; set; }

        [Description("When true, cases raised against this workflow present a visualisation built from the " +
                     "case's transaction payload.")]
        [FormField(Group = "Visualisation", Order = 10, Widget = "switch")]
        public bool EnableVisualisation { get; set; }

        [Description("Identifier of the Visualisation Registry entry to present. Required when " +
                     "EnableVisualisation is true.")]
        [FormField(Group = "Visualisation", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(EnableVisualisation), true)]
        [RequiredWhen(nameof(EnableVisualisation), true)]
        [Lookup("/api/VisualisationRegistry", TextField = "name", ValueField = "guid",
            ParentField = nameof(EntityAnalysisModelId))]
        public Guid VisualisationRegistryGuid { get; set; }

        [Description("Name of the field in the case payload that holds the money involved, such as Amount. The " +
                     "case risk impact reads it to place a Case in a money band. Leave empty when the workflow " +
                     "has no money measure, so that impact rests on typology alone.")]
        [FormField(Group = "Risk", Order = 10, Placeholder = "e.g. Amount")]
        public string? ValueField { get; set; }

        [Description("How severe Cases of this workflow are by nature, the typology component of case risk " +
                     "impact: 1 = Minimal, 2 = Low, 3 = Moderate, 4 = High, 5 = Severe. A terrorist financing " +
                     "workflow is 5, structuring 3, low-value first-party fraud 2.")]
        [FormField(Group = "Risk", Order = 20, Widget = "select")]
        [NewDefault(3)]
        [Options(typeof(CaseImpactBand))]
        public int TypologySeverityId { get; set; }

        [Description("Service level: the most minutes a new Case may wait before anyone first looks at it, " +
                     "measured from creation to the first peek, skim or fetch. Leave empty for no target.")]
        [FormField(Group = "ServiceLevel", Order = 10, Placeholder = "e.g. 30")]
        public int? FirstViewTargetMinutes { get; set; }

        [Description("Service level: the most minutes a new Case may wait before someone takes ownership by " +
                     "locking it, measured from creation to the first lock. Leave empty for no target.")]
        [FormField(Group = "ServiceLevel", Order = 20, Placeholder = "e.g. 120")]
        public int? OwnershipTargetMinutes { get; set; }

        [Description("Service level: the most minutes a Case may stay open, measured from creation to " +
                     "closure. Leave empty for no target.")]
        [FormField(Group = "ServiceLevel", Order = 30, Placeholder = "e.g. 4320")]
        public int? ResolutionTargetMinutes { get; set; }

        [Description("Case ranking: damping from 0 (exclusive) to 1 applied to every piece of evidence that moves " +
                     "a Case's likelihood. Lower trusts the evidence less. Leave empty for the environment default " +
                     "of 0.7.")]
        [FormField(Group = "Ranking", Order = 10, Placeholder = "0.7")]
        public double? RiskKappa { get; set; }

        [Description("Case ranking: the weight from 0 to 1 given to evidence that shares inputs with stronger " +
                     "evidence in the same group; 1 counts it in full and 0 ignores it. Leave empty for the " +
                     "environment default of 0.3.")]
        [FormField(Group = "Ranking", Order = 20, Placeholder = "0.3")]
        public double? RiskRho { get; set; }

        [Description("Case ranking: how many imaginary Cases at the prior rate pull each estimate towards it, so " +
                     "that a few real Cases cannot swing it. Higher is more cautious. Leave empty for the " +
                     "environment default of 20.")]
        [FormField(Group = "Ranking", Order = 30, Placeholder = "20")]
        public double? RiskPriorStrength { get; set; }

        [Description("Case ranking: the confirmed rate assumed before this workflow has closed Cases with " +
                     "outcomes, from 0.001 to 0.999. Leave empty for the environment default of 0.08.")]
        [FormField(Group = "Ranking", Order = 40, Placeholder = "0.08")]
        public double? RiskBaseRatePrior { get; set; }

        [Description("Case ranking: how many days before a diary deadline a Case moves into the deadline near " +
                     "lane. Leave empty for the environment default of 2.")]
        [FormField(Group = "Ranking", Order = 50, Placeholder = "2")]
        public int? RiskDeadlineLaneDays { get; set; }

        [Description("Case ranking: the age in days at which earlier closed Cases on the same case key count " +
                     "for half as much. Leave empty for the environment default of 180.")]
        [FormField(Group = "Ranking", Order = 60, Placeholder = "180")]
        public double? RiskKeyHistoryHalfLifeDays { get; set; }

        [Description("Case ranking: five increasing comma separated weights, one for each impact band from " +
                     "Minimal to Severe, that multiply likelihood into expected harm. Leave empty for the " +
                     "environment default of 1,3,10,30,100.")]
        [FormField(Group = "Ranking", Order = 70, Placeholder = "1,3,10,30,100")]
        public string? RiskImpactWeights { get; set; }

        [Description("Case ranking: four increasing comma separated Case values that separate the five money " +
                     "bands of impact. Leave empty for the environment default of 5000,100000,1000000,10000000.")]
        [FormField(Group = "Ranking", Order = 80, Placeholder = "5000,100000,1000000,10000000")]
        public string? RiskMoneyBands { get; set; }

        [Description("Case ranking: the share, from 0 to 0.5, of this workflow's low-priority Cases that are picked " +
                     "at random for a quality review, so that the ranking is checked against cases it would " +
                     "never have put in front of an analyst. 0 turns sampling off. Leave empty for the " +
                     "environment default of 0.05.")]
        [FormField(Group = "Ranking", Order = 90, Placeholder = "0.05")]
        public double? RiskQaSampleRate { get; set; }

        [Description("Service level: how many Cases each analyst aims to close in a day, from 1 to 1000. It " +
                     "drives the daily progress ring and the streak of days the goal was met on the Cases page. " +
                     "Leave empty for no goal, in which case a streak is a run of days with at least one Case closed.")]
        [FormField(Group = "ServiceLevel", Order = 40, Placeholder = "e.g. 25")]
        public int? DailyGoal { get; set; }

        [Description("Server-assigned globally-unique identifier of this Case Workflow, referenced by cases and " +
                     "activation rules. Read-only.")]
        public Guid Guid { get; set; }

        [Description("Server-assigned identifier. Read-only; required when updating an existing Case Workflow.")]
        public int Id { get; set; }

        [Description("Server-assigned creation timestamp. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("Server-assigned user who created this Case Workflow. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Server-assigned user who most recently updated this Case Workflow. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Server-assigned timestamp of the most recent update. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned version number, incremented on every update. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("Server-assigned user who soft-deleted this Case Workflow, if any. Read-only.")]
        public string? DeletedUser { get; set; }

        [Description("Server-assigned timestamp this Case Workflow was soft-deleted, if any. Read-only.")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}