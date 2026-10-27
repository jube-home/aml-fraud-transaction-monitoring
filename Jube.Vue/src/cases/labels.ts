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

export type Tone = 'neutral' | 'info' | 'success' | 'warning' | 'error'

export const laneKeys: Record<number, string> = {
  1: 'cases.lane.liveMoney',
  2: 'cases.lane.mandatory',
  3: 'cases.lane.deadlineNear',
  4: 'cases.lane.standard',
}

export const likelihoodKeys: Record<number, string> = {
  1: 'cases.likelihood.unlikely',
  2: 'cases.likelihood.possible',
  3: 'cases.likelihood.likely',
  4: 'cases.likelihood.veryLikely',
}

export const impactKeys: Record<number, string> = {
  1: 'cases.impact.minimal',
  2: 'cases.impact.low',
  3: 'cases.impact.moderate',
  4: 'cases.impact.high',
  5: 'cases.impact.severe',
}

export const viewKeys: Record<number, string> = {
  1: 'cases.view.mine',
  2: 'cases.view.returned',
  3: 'cases.view.free',
  4: 'cases.view.urgent',
  5: 'cases.view.overdue',
  6: 'cases.view.all',
  7: 'cases.view.qaSamples',
}

export const slaStateKeys: Record<number, string> = {
  0: 'cases.sla.noTarget',
  1: 'cases.sla.met',
  2: 'cases.sla.onTrack',
  3: 'cases.sla.atRisk',
  4: 'cases.sla.breached',
}

export const slaDimensionKeys: Record<number, string> = {
  1: 'cases.sla.firstView',
  2: 'cases.sla.ownership',
  3: 'cases.sla.resolution',
  4: 'cases.sla.statusDwell',
}

export const attentionKeys: Record<number, string> = {
  1: 'cases.attention.reopened',
  2: 'cases.attention.escalated',
  3: 'cases.attention.movedByOther',
  4: 'cases.attention.diaryDue',
  5: 'cases.attention.riskRose',
  6: 'cases.attention.newActivity',
}

export const eventKeys: Record<number, string> = {
  1: 'cases.event.automaticUnlock',
  2: 'cases.event.skim',
  3: 'cases.event.automaticLock',
  4: 'cases.event.fullFetch',
  5: 'cases.event.closed',
  6: 'cases.event.manualLock',
  7: 'cases.event.diary',
  8: 'cases.event.workflowChange',
  9: 'cases.event.statusChange',
  10: 'cases.event.diaryDateChange',
  11: 'cases.event.ratingChange',
  12: 'cases.event.suspendClosed',
  13: 'cases.event.suspendOpen',
  14: 'cases.event.allocateLock',
  15: 'cases.event.processExpired',
  16: 'cases.event.peek',
  17: 'cases.event.escalation',
  18: 'cases.event.lockBroken',
  19: 'cases.event.diaryCleared',
  20: 'cases.event.qaSampled',
}

export const enterReasonKeys: Record<number, string> = {
  1: 'cases.stay.created',
  2: 'cases.stay.statusChanged',
  3: 'cases.stay.reopened',
  4: 'cases.stay.escalated',
}

export const exitReasonKeys: Record<number, string> = {
  1: 'cases.stay.movedOn',
  2: 'cases.stay.closed',
}

export const triggerKeys: Record<number, string> = {
  1: 'cases.trigger.initial',
  2: 'cases.trigger.activation',
  3: 'cases.trigger.linkedKey',
  4: 'cases.trigger.neighbour',
  5: 'cases.trigger.diary',
  6: 'cases.trigger.hold',
  7: 'cases.trigger.finding',
  8: 'cases.trigger.typology',
  9: 'cases.trigger.model',
  10: 'cases.trigger.sweep',
}

export const diaryTypeKeys: Record<number, string> = {
  1: 'cases.diary.type.review',
  2: 'cases.diary.type.customerResponse',
  3: 'cases.diary.type.regulatoryDeadline',
  4: 'cases.diary.type.paymentHold',
}

export const sortKeys: Record<number, string> = {
  0: 'cases.sort.triage',
  1: 'cases.sort.priority',
  2: 'cases.sort.age',
  3: 'cases.sort.due',
  4: 'cases.sort.value',
  5: 'cases.sort.likelihood',
  6: 'cases.sort.newest',
}

export const closedStatusKeys: Record<number, string> = {
  0: 'cases.closedStatus.open',
  1: 'cases.closedStatus.suspendOpen',
  2: 'cases.closedStatus.suspendClosed',
  3: 'cases.closedStatus.closed',
  4: 'cases.closedStatus.suspendBypass',
}

export function laneTone(lane: number): Tone {
  if (lane === 1 || lane === 2) return 'error'
  if (lane === 3) return 'warning'

  return 'neutral'
}

export function likelihoodTone(band: number): Tone {
  return band >= 3 ? 'error' : 'neutral'
}

export function slaTone(state: number): Tone {
  if (state === 4) return 'error'
  if (state === 3) return 'warning'

  return 'neutral'
}

export const toneClass: Record<Tone, string> = {
  neutral: 'badge badge-soft badge-sm',
  info: 'badge badge-soft badge-sm badge-info',
  success: 'badge badge-sm badge-success',
  warning: 'badge badge-sm badge-warning',
  error: 'badge badge-sm badge-error',
}

export function caseLabel(caseId: number): string {
  return `C-${caseId}`
}

export function riskSortValue(lane: number, priority: number): number {
  return (5 - lane) * 1000 + priority
}
