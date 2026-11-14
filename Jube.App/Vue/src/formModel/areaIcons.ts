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

import type {Component} from 'vue'
import {
  BarChart3,
  BookA,
  Boxes,
  Building2,
  Braces,
  BrainCircuit,
  Calculator,
  CircleDot,
  ClipboardList,
  Database,
  DoorOpen,
  Filter,
  Folder,
  FolderKanban,
  FunctionSquare,
  Gauge,
  Globe,
  LayoutTemplate,
  List,
  ListChecks,
  MousePointerClick,
  Network,
  Radar,
  RefreshCw,
  ScrollText,
  ShieldAlert,
  ShieldCheck,
  Sigma,
  SlidersHorizontal,
  Tag,
  Timer,
  UserRoundCog,
  Users,
  Waypoints,
  Webhook,
  Workflow,
  Zap,
} from 'lucide-vue-next'

const icons: Record<string, Component> = {
  EntityAnalysisModel: Boxes,
  EntityAnalysisModelRequestXPath: Braces,
  EntityAnalysisModelInlineFunction: FunctionSquare,
  EntityAnalysisModelInlineScript: ScrollText,
  EntityAnalysisModelGatewayRule: DoorOpen,
  EntityAnalysisModelSanction: ShieldAlert,
  EntityAnalysisModelTag: Tag,
  EntityAnalysisModelList: List,
  EntityAnalysisModelDictionary: BookA,
  EntityAnalysisModelTtlCounter: Timer,
  EntityAnalysisModelAbstractionRule: Sigma,
  EntityAnalysisModelAbstractionCalculation: Calculator,
  EntityAnalysisModelAdaptation: Globe,
  EntityAnalysisModelHttpAdaptation: Globe,
  ExhaustiveSearchInstance: BrainCircuit,
  EntityAnalysisModelGraphRecall: Network,
  EntityAnalysisModelSvirfd: Radar,
  EntityAnalysisModelResponseElevationRange: Gauge,
  EntityAnalysisModelActivationRule: Zap,
  CaseWorkflow: FolderKanban,
  EntityAnalysisModelReprocessingRule: RefreshCw,
  CaseWorkflowStatus: CircleDot,
  CaseWorkflowXPath: Braces,
  CaseWorkflowForm: ClipboardList,
  CaseWorkflowAction: MousePointerClick,
  CaseWorkflowDisplay: LayoutTemplate,
  CaseWorkflowMacro: Workflow,
  CaseWorkflowFilter: Filter,
  CaseWorkflowFormValidator: ShieldCheck,
  CaseWorkflowReleaseHook: Webhook,
  CaseWorkflowChecklist: ListChecks,
  CaseWorkflowChecklistItem: ListChecks,
  GraphRegistryEdge: Waypoints,
  UserRegistry: Users,
  VisualisationRegistryParameter: SlidersHorizontal,
  VisualisationRegistryDatasource: Database,
  TenantRegistry: Building2,
  VisualisationRegistry: BarChart3,
  GraphRegistry: Waypoints,
  RoleRegistry: UserRoundCog,
}

export function iconForArea(area: string): Component {
  return icons[area] ?? Folder
}
