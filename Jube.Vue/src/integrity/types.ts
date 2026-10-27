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

export type Severity = 'Error' | 'Warning' | 'Info'

export interface IntegrityModel {
  id: number
  name: string | null
  active: boolean
}

export interface IntegrityCheck {
  code: string
  title: string
  severity: Severity
  category: string
  entityKind: string | null
  entityId: number | null
  entityName: string | null
  message: string
}

export interface IntegrityReport {
  entityAnalysisModelId: number
  modelName: string | null
  checkedDate: string
  engineSource: string
  errors: number
  warnings: number
  infos: number
  checks: IntegrityCheck[]
}

export interface EngineNode {
  instance: string | null
  heartbeatDate: string | null
  synchronisedDate: string | null
}

export interface EngineLoadedEntity {
  name: string
  id: number | null
  guid: string | null
}

export interface EngineLoaded {
  kind: string
  count: number
  entities: EngineLoadedEntity[]
}

export interface EngineInstance {
  instance: string
  started: boolean
  capturedDate: string
  loaded: EngineLoaded[]
}

export interface EngineState {
  source: string
  nodes: EngineNode[]
  instances: EngineInstance[]
}

export interface GraphNode {
  id: string
  label: string
  kind: string
  active: boolean
  detail: string | null
}

export interface GraphEdge {
  from: string
  to: string
  label: string
  dashed: boolean
}

export interface ModelGraph {
  nodes: GraphNode[]
  edges: GraphEdge[]
  truncated: boolean
  maxNodes: number
  focus: string | null
}

export interface PendingRow {
  kind: number
  entityId: number
  name: string
  version: number
  deleted: boolean
  makerUser: string | null
  parentId: number | null
}

export interface SyncNodeStatus {
  id: number
  instance: string
  heartbeatDate: string | null
  synchronisedDate: string | null
  instanceAvailable: boolean
  synchronisationPending: boolean
}
