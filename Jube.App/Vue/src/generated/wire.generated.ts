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

import { dict, many, rows } from '../api/wireRuntime'

type Decoder = (value: unknown) => unknown

export const typesHash = 'E35D696ABEAD07A3'

function d_Jube_App_Endpoints_ExhaustiveSearchInstanceSetActiveRequest(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    active: a[0] ?? null,
  }
}

function d_Jube_App_Endpoints_MeEndpoints_MeResponse(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    userName: a[0] ?? null,
    landlord: a[1] ?? null,
    permissions: a[2] ?? null,
    tenantRegistryId: a[3] ?? null,
    canChangePassword: a[4] ?? null,
    askJoobyInference: a[5] ?? null,
    sessionExpiry: a[6] ?? null,
    svirfd: a[7] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_AgedCase(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    ageMinutes: a[1] ?? null,
    lane: a[2] ?? null,
    priority: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_AgingBucket(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    label: a[0] ?? null,
    fromMinutes: a[1] ?? null,
    toMinutes: a[2] ?? null,
    total: a[3] ?? null,
    byLane: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_AgingReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    open: a[0] ?? null,
    buckets: ((v) => many(v, d_Jube_CaseRisk_Analytics_AgingBucket))(a[1] ?? null),
    oldest: ((v) => many(v, d_Jube_CaseRisk_Analytics_AgedCase))(a[2] ?? null),
  }
}

function d_Jube_CaseRisk_Analytics_CalibrationBin(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: a[0] ?? null,
    to: a[1] ?? null,
    cases: a[2] ?? null,
    predicted: a[3] ?? null,
    observed: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_ChecklistSlaItem(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    itemGuid: a[0] ?? null,
    checklistGuid: a[1] ?? null,
    checklistName: a[2] ?? null,
    itemName: a[3] ?? null,
    targetMinutes: a[4] ?? null,
    shown: a[5] ?? null,
    hidden: a[6] ?? null,
    completed: a[7] ?? null,
    completedWithinSla: a[8] ?? null,
    completedLate: a[9] ?? null,
    open: a[10] ?? null,
    breached: a[11] ?? null,
    medianMinutes: a[12] ?? null,
    p90Minutes: a[13] ?? null,
    attainment: a[14] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_ChecklistSlaReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    items: ((v) => many(v, d_Jube_CaseRisk_Analytics_ChecklistSlaItem))(a[0] ?? null),
    shown: a[1] ?? null,
    completed: a[2] ?? null,
    completedWithinSla: a[3] ?? null,
    completedLate: a[4] ?? null,
    breached: a[5] ?? null,
    attainment: a[6] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_CycleTimeReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    cases: a[0] ?? null,
    firstView: d_Jube_CaseRisk_Analytics_DistributionStats(a[1] ?? null),
    ownership: d_Jube_CaseRisk_Analytics_DistributionStats(a[2] ?? null),
    resolution: d_Jube_CaseRisk_Analytics_DistributionStats(a[3] ?? null),
    waitingUnviewed: d_Jube_CaseRisk_Analytics_DistributionStats(a[4] ?? null),
    neverViewed: a[5] ?? null,
    neverOwned: a[6] ?? null,
    firstViewHistogram: ((v) => many(v, d_Jube_CaseRisk_Analytics_HistogramBucket))(a[7] ?? null),
    resolutionHistogram: ((v) => many(v, d_Jube_CaseRisk_Analytics_HistogramBucket))(a[8] ?? null),
  }
}

function d_Jube_CaseRisk_Analytics_DistributionStats(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    count: a[0] ?? null,
    mean: a[1] ?? null,
    min: a[2] ?? null,
    p50: a[3] ?? null,
    p75: a[4] ?? null,
    p90: a[5] ?? null,
    p95: a[6] ?? null,
    max: a[7] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_FlowDay(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    date: a[0] ?? null,
    opened: a[1] ?? null,
    closed: a[2] ?? null,
    backlog: a[3] ?? null,
    byStatus: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_GainsPoint(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    worked: a[0] ?? null,
    found: a[1] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_HandoffBucket(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    label: a[0] ?? null,
    cases: a[1] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_HandoffCase(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    handoffs: a[1] ?? null,
    reopens: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_HandoffReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    distribution: ((v) => many(v, d_Jube_CaseRisk_Analytics_HandoffBucket))(a[0] ?? null),
    reopenedCases: a[1] ?? null,
    reopenRate: a[2] ?? null,
    pingPong: ((v) => many(v, d_Jube_CaseRisk_Analytics_HandoffCase))(a[3] ?? null),
  }
}

function d_Jube_CaseRisk_Analytics_HistogramBucket(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    label: a[0] ?? null,
    from: a[1] ?? null,
    to: a[2] ?? null,
    count: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_MyDay(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    date: a[0] ?? null,
    closed: a[1] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_MyDayReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    goal: a[0] ?? null,
    closedToday: a[1] ?? null,
    streak: a[2] ?? null,
    bestDay: a[3] ?? null,
    typicalDay: a[4] ?? null,
    days: ((v) => many(v, d_Jube_CaseRisk_Analytics_MyDay))(a[5] ?? null),
  }
}

function d_Jube_CaseRisk_Analytics_PersonWorkload(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    closed: a[0] ?? null,
    resolution: d_Jube_CaseRisk_Analytics_DistributionStats(a[1] ?? null),
    heldNow: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_QaSampleReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    picked: a[0] ?? null,
    open: a[1] ?? null,
    closed: a[2] ?? null,
    decided: a[3] ?? null,
    confirmed: a[4] ?? null,
    confirmedRate: a[5] ?? null,
    overallConfirmedRate: a[6] ?? null,
    enough: a[7] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_RankingQualityReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    outcomes: a[0] ?? null,
    confirmed: a[1] ?? null,
    baseRate: a[2] ?? null,
    sufficient: a[3] ?? null,
    priorityAuc: a[4] ?? null,
    likelihoodAuc: a[5] ?? null,
    oldestFirstAuc: a[6] ?? null,
    priorityLiftTop10: a[7] ?? null,
    priorityLiftTop20: a[8] ?? null,
    priorityLiftTop30: a[9] ?? null,
    priorityGains: ((v) => many(v, d_Jube_CaseRisk_Analytics_GainsPoint))(a[10] ?? null),
    likelihoodGains: ((v) => many(v, d_Jube_CaseRisk_Analytics_GainsPoint))(a[11] ?? null),
    oldestFirstGains: ((v) => many(v, d_Jube_CaseRisk_Analytics_GainsPoint))(a[12] ?? null),
    calibration: ((v) => many(v, d_Jube_CaseRisk_Analytics_CalibrationBin))(a[13] ?? null),
    brierScore: a[14] ?? null,
    expectedCalibrationError: a[15] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_RhythmReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    arrivalsByWeekdayAndHour: a[0] ?? null,
    medianFirstViewMinutesByHour: a[1] ?? null,
    closuresByHour: a[2] ?? null,
    busiestWeekday: a[3] ?? null,
    busiestHour: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_RuleEffectiveness(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    ruleGuid: a[0] ?? null,
    name: a[1] ?? null,
    cases: a[2] ?? null,
    open: a[3] ?? null,
    confirmed: a[4] ?? null,
    notConfirmed: a[5] ?? null,
    noOutcome: a[6] ?? null,
    precision: a[7] ?? null,
    meanHandlingMinutes: a[8] ?? null,
    wastedMinutes: a[9] ?? null,
    shareOfWastedEffort: a[10] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_SlaDimensionSummary(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dimension: a[0] ?? null,
    met: a[1] ?? null,
    breached: a[2] ?? null,
    atRisk: a[3] ?? null,
    onTrack: a[4] ?? null,
    attainment: a[5] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_SlaReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dimensions: ((v) => many(v, d_Jube_CaseRisk_Analytics_SlaDimensionSummary))(a[0] ?? null),
    weekly: ((v) => many(v, d_Jube_CaseRisk_Analytics_SlaWeek))(a[1] ?? null),
    watch: ((v) => many(v, d_Jube_CaseRisk_Analytics_SlaWatch))(a[2] ?? null),
    openBreached: a[3] ?? null,
    openAtRisk: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_SlaWatch(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    dimension: a[1] ?? null,
    state: a[2] ?? null,
    elapsedFraction: a[3] ?? null,
    dueDate: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_SlaWeek(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    weekStart: a[0] ?? null,
    dimension: a[1] ?? null,
    met: a[2] ?? null,
    breached: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_StatusDwell(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowStatusGuid: a[0] ?? null,
    stays: a[1] ?? null,
    currentlyIn: a[2] ?? null,
    completed: d_Jube_CaseRisk_Analytics_DistributionStats(a[3] ?? null),
    current: d_Jube_CaseRisk_Analytics_DistributionStats(a[4] ?? null),
    totalMinutes: a[5] ?? null,
    shareOfTime: a[6] ?? null,
    maxDwellMinutes: a[7] ?? null,
    breaches: a[8] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_TeamWorkload(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    people: a[0] ?? null,
    medianClosedPerPerson: a[1] ?? null,
    medianResolutionMinutes: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Analytics_WorkloadReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    mine: d_Jube_CaseRisk_Analytics_PersonWorkload(a[0] ?? null),
    team: d_Jube_CaseRisk_Analytics_TeamWorkload(a[1] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_BoxSummary(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    q1: a[0] ?? null,
    median: a[1] ?? null,
    q3: a[2] ?? null,
    lowerFence: a[3] ?? null,
    upperFence: a[4] ?? null,
    outliers: a[5] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_Burst(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    windowHours: a[0] ?? null,
    max: a[1] ?? null,
    at: a[2] ?? null,
    median: a[3] ?? null,
    ratio: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_CorrelationPair(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    first: a[0] ?? null,
    second: a[1] ?? null,
    r: a[2] ?? null,
    pairs: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_CorrelationReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    fields: a[0] ?? null,
    matrix: a[1] ?? null,
    strong: ((v) => many(v, d_Jube_CaseRisk_Journal_CorrelationPair))(a[2] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_CounterpartyEntry(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    count: a[1] ?? null,
    share: a[2] ?? null,
    amount: a[3] ?? null,
    firstSeen: a[4] ?? null,
    lastSeen: a[5] ?? null,
    new: a[6] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_CounterpartyProfile(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    distinct: a[1] ?? null,
    concentration: a[2] ?? null,
    newCount: a[3] ?? null,
    top: ((v) => many(v, d_Jube_CaseRisk_Journal_CounterpartyEntry))(a[4] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_DistributionBin(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: a[0] ?? null,
    to: a[1] ?? null,
    count: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_DistributionReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    count: a[1] ?? null,
    min: a[2] ?? null,
    max: a[3] ?? null,
    mean: a[4] ?? null,
    median: a[5] ?? null,
    p5: a[6] ?? null,
    p95: a[7] ?? null,
    bins: ((v) => many(v, d_Jube_CaseRisk_Journal_DistributionBin))(a[8] ?? null),
    box: d_Jube_CaseRisk_Journal_BoxSummary(a[9] ?? null),
    latest: a[10] ?? null,
    latestPercentile: a[11] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_Dormancy(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hours: a[0] ?? null,
    from: a[1] ?? null,
    to: a[2] ?? null,
    eventsInNextDay: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_DriftReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    splitAt: a[0] ?? null,
    prior: a[1] ?? null,
    recent: a[2] ?? null,
    fields: ((v) => many(v, d_Jube_CaseRisk_Journal_FieldDrift))(a[3] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_FieldDrift(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    statistic: a[1] ?? null,
    critical: a[2] ?? null,
    drifted: a[3] ?? null,
    priorMedian: a[4] ?? null,
    recentMedian: a[5] ?? null,
    shift: a[6] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_FieldNorm(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    count: a[1] ?? null,
    median: a[2] ?? null,
    scale: a[3] ?? null,
    confident: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_FieldSummary(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    count: a[1] ?? null,
    sum: a[2] ?? null,
    mean: a[3] ?? null,
    median: a[4] ?? null,
    max: a[5] ?? null,
    latest: a[6] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_GapBin(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    label: a[0] ?? null,
    fromMinutes: a[1] ?? null,
    toMinutes: a[2] ?? null,
    count: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_GapReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    bins: ((v) => many(v, d_Jube_CaseRisk_Journal_GapBin))(a[0] ?? null),
    medianHours: a[1] ?? null,
    longest: d_Jube_CaseRisk_Journal_Dormancy(a[2] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_JournalSummary(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    count: a[0] ?? null,
    first: a[1] ?? null,
    last: a[2] ?? null,
    activeDays: a[3] ?? null,
    activations: a[4] ?? null,
    activationRows: a[5] ?? null,
    maxElevation: a[6] ?? null,
    fields: ((v) => many(v, d_Jube_CaseRisk_Journal_FieldSummary))(a[7] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_NewValue(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    firstSeen: a[1] ?? null,
    eventId: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_NotableReason(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    value: a[1] ?? null,
    median: a[2] ?? null,
    z: a[3] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_NotableReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    norms: ((v) => many(v, d_Jube_CaseRisk_Journal_FieldNorm))(a[0] ?? null),
    rows: ((v) => many(v, d_Jube_CaseRisk_Journal_NotableRow))(a[1] ?? null),
    lowConfidenceFields: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_NotableRow(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    score: a[1] ?? null,
    worst: a[2] ?? null,
    reasons: ((v) => many(v, d_Jube_CaseRisk_Journal_NotableReason))(a[3] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_NoveltyField(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    distinct: a[1] ?? null,
    recent: ((v) => many(v, d_Jube_CaseRisk_Journal_NewValue))(a[2] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_NoveltyReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    fields: ((v) => many(v, d_Jube_CaseRisk_Journal_NoveltyField))(a[0] ?? null),
    rows: a[1] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_PeerComparison(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    keyMedian: a[1] ?? null,
    keyRows: a[2] ?? null,
    peerMedian: a[3] ?? null,
    peerScale: a[4] ?? null,
    peerQ1: a[5] ?? null,
    peerQ3: a[6] ?? null,
    peerKeys: a[7] ?? null,
    z: a[8] ?? null,
    percentile: a[9] ?? null,
    direction: a[10] ?? null,
    lowConfidence: a[11] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_ProgressionPoint(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    start: a[0] ?? null,
    median: a[1] ?? null,
    count: a[2] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_ProgressionReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    bucket: a[0] ?? null,
    series: ((v) => many(v, d_Jube_CaseRisk_Journal_ProgressionSeries))(a[1] ?? null),
  }
}

function d_Jube_CaseRisk_Journal_ProgressionSeries(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    points: ((v) => many(v, d_Jube_CaseRisk_Journal_ProgressionPoint))(a[1] ?? null),
    latest: a[2] ?? null,
    previousMedian: a[3] ?? null,
    change: a[4] ?? null,
  }
}

function d_Jube_CaseRisk_Journal_VelocityReport(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hourByWeekday: a[0] ?? null,
    bursts: ((v) => many(v, d_Jube_CaseRisk_Journal_Burst))(a[1] ?? null),
  }
}

function d_Jube_Data_Poco_EntityAnalysisModelSynchronisationSchedule(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    scheduleDate: a[2] ?? null,
    tenantRegistryId: a[3] ?? null,
    createdUser: a[4] ?? null,
    tenantRegistry: d_Jube_Data_Poco_TenantRegistry(a[5] ?? null),
    entityAnalysisModelId: a[6] ?? null,
  }
}

function d_Jube_Data_Poco_TenantRegistry(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    createdDate: a[2] ?? null,
    createdUser: a[3] ?? null,
    updatedDate: a[4] ?? null,
    updatedUser: a[5] ?? null,
    deletedDate: a[6] ?? null,
    deletedUser: a[7] ?? null,
    active: a[8] ?? null,
    locked: a[9] ?? null,
    deleted: a[10] ?? null,
    landlord: a[11] ?? null,
    version: a[12] ?? null,
    httpAdaptationUrl: a[13] ?? null,
    httpAdaptationTimeoutMilliseconds: a[14] ?? null,
    httpAdaptationValidateSsl: a[15] ?? null,
    svirfdRemoteLookupsEnabled: a[16] ?? null,
  }
}

function d_Jube_Data_Query_DynamicResultsSchema_DynamicResultsSchemaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    schema: a[0] ?? null,
    rows: ((v) => rows(v, null))(a[1] ?? null),
  }
}

function d_Jube_Data_Query_GetAskJoobyStartupStatsQuery_GetAskJoobyStartupStatsQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    bites: a[0] ?? null,
    trails: a[1] ?? null,
    saltlicks: a[2] ?? null,
    joobriks: a[3] ?? null,
    yokes: a[4] ?? null,
  }
}

function d_Jube_Dto_ApplicationLogEntry_ApplicationLogEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    level: a[2] ?? null,
    loggerName: a[3] ?? null,
    threadContext: a[4] ?? null,
    message: a[5] ?? null,
    exception: a[6] ?? null,
    createdDate: a[7] ?? null,
    instance: a[8] ?? null,
  }
}

function d_Jube_Dto_ArchiverStagePerformanceCounter_ArchiverStagePerformanceCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    entityAnalysisModelName: a[2] ?? null,
    stageId: a[3] ?? null,
    stageName: a[4] ?? null,
    totalMicroseconds: a[5] ?? null,
    minMicroseconds: a[6] ?? null,
    maxMicroseconds: a[7] ?? null,
    invokeCount: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_ArchiverWarning_ArchiverWarningDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    entityAnalysisModelGuid: a[2] ?? null,
    entityAnalysisModelName: a[3] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[4] ?? null,
    stageId: a[5] ?? null,
    stageName: a[6] ?? null,
    durationMicroseconds: a[7] ?? null,
    createdDate: a[8] ?? null,
    instance: a[9] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyApprovalDecisionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    approve: a[0] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyConversationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    title: a[2] ?? null,
    createdUser: a[3] ?? null,
    createdDate: a[4] ?? null,
    updatedUser: a[5] ?? null,
    updatedDate: a[6] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyConversationEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    id: a[1] ?? null,
    role: a[2] ?? null,
    text: a[3] ?? null,
    createdDate: a[4] ?? null,
    inputTokens: a[5] ?? null,
    outputTokens: a[6] ?? null,
    cachedInputTokens: a[7] ?? null,
    durationMs: a[8] ?? null,
    joobrikCount: a[9] ?? null,
    distinctJoobrikCount: a[10] ?? null,
    totalCost: a[11] ?? null,
    iconId: a[12] ?? null,
    detail: a[13] ?? null,
    attachments: ((v) => many(v, d_Jube_Dto_AskJoobyConversation_AskJoobyMessageAttachmentDto))(a[14] ?? null),
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyFeedbackRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    isPositive: a[0] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyFileDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    askJoobyConversationId: a[1] ?? null,
    name: a[2] ?? null,
    contentType: a[3] ?? null,
    size: a[4] ?? null,
    createdUser: a[5] ?? null,
    createdDate: a[6] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyGuidanceRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guidance: a[0] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyMessageAttachmentDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobySendMessageRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    text: a[0] ?? null,
    pageContext: a[1] ?? null,
    attachmentFileIds: a[2] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyStartConversationRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    pageContext: a[0] ?? null,
  }
}

function d_Jube_Dto_AskJoobyConversation_AskJoobyStreamItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    text: a[1] ?? null,
    agentLabel: a[2] ?? null,
    detail: a[3] ?? null,
    iconId: a[4] ?? null,
    suggestions: a[5] ?? null,
    options: a[6] ?? null,
    allowFreeText: a[7] ?? null,
    messageId: a[8] ?? null,
    attachments: ((v) => many(v, d_Jube_Dto_AskJoobyConversation_AskJoobyMessageAttachmentDto))(a[9] ?? null),
    inputTokens: a[10] ?? null,
    outputTokens: a[11] ?? null,
    cachedInputTokens: a[12] ?? null,
    durationMs: a[13] ?? null,
    joobrikCount: a[14] ?? null,
    distinctJoobrikCount: a[15] ?? null,
    totalCost: a[16] ?? null,
    approvalId: a[17] ?? null,
  }
}

function d_Jube_Dto_AskJoobyMemory_AskJoobyMemoryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    scope: a[1] ?? null,
    createdUser: a[2] ?? null,
    content: a[3] ?? null,
    score: a[4] ?? null,
  }
}

function d_Jube_Dto_AskJoobyRagChunk_AskJoobyRagChunkDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    hash: a[1] ?? null,
    parentHash: a[2] ?? null,
    category: a[3] ?? null,
    kind: a[4] ?? null,
    sourceFile: a[5] ?? null,
    startLine: a[6] ?? null,
    endLine: a[7] ?? null,
    qualifiedName: a[8] ?? null,
    content: a[9] ?? null,
    score: a[10] ?? null,
  }
}

function d_Jube_Dto_Authentication_AuthenticationMethodsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    negotiate: a[0] ?? null,
    oauth: a[1] ?? null,
    totp: a[2] ?? null,
  }
}

function d_Jube_Dto_Authentication_AuthenticationRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    userName: a[0] ?? null,
    password: a[1] ?? null,
    newPassword: a[2] ?? null,
    repeatNewPassword: a[3] ?? null,
    remoteIp: a[4] ?? null,
    localIp: a[5] ?? null,
    userAgent: a[6] ?? null,
    passwordChangeState: a[7] ?? null,
    mfa: a[8] ?? null,
  }
}

function d_Jube_Dto_Authentication_AuthenticationResponseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    token: a[0] ?? null,
    expiration: a[1] ?? null,
  }
}

function d_Jube_Dto_Authentication_AuthenticationSchemaRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    userName: a[0] ?? null,
  }
}

function d_Jube_Dto_Authentication_ChangePasswordRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    password: a[0] ?? null,
    newPassword: a[1] ?? null,
  }
}

function d_Jube_Dto_Authentication_PasswordSchemeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    wirePasswordHash: a[0] ?? null,
  }
}

function d_Jube_Dto_Authentication_TotpEnrolmentDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    provisioningUri: a[0] ?? null,
    secret: a[1] ?? null,
  }
}

function d_Jube_Dto_CaptureQueueHealth_CaptureQueueHealthDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    queueId: a[1] ?? null,
    queueName: a[2] ?? null,
    queueDepth: a[3] ?? null,
    droppedCount: a[4] ?? null,
    createdDate: a[5] ?? null,
    instance: a[6] ?? null,
  }
}

function d_Jube_Dto_CaseCreationStagePerformanceCounter_CaseCreationStagePerformanceCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    stageId: a[1] ?? null,
    stageName: a[2] ?? null,
    totalMicroseconds: a[3] ?? null,
    minMicroseconds: a[4] ?? null,
    maxMicroseconds: a[5] ?? null,
    invokeCount: a[6] ?? null,
    createdDate: a[7] ?? null,
    instance: a[8] ?? null,
  }
}

function d_Jube_Dto_CaseCreationWarning_CaseCreationWarningDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[2] ?? null,
    caseWorkflowGuid: a[3] ?? null,
    caseKey: a[4] ?? null,
    caseKeyValue: a[5] ?? null,
    stageId: a[6] ?? null,
    stageName: a[7] ?? null,
    destination: a[8] ?? null,
    durationMicroseconds: a[9] ?? null,
    createdDate: a[10] ?? null,
    instance: a[11] ?? null,
  }
}

function d_Jube_Dto_ContainerLogEntry_ContainerLogEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    containerName: a[2] ?? null,
    streamTypeId: a[3] ?? null,
    streamTypeName: a[4] ?? null,
    message: a[5] ?? null,
    createdDate: a[6] ?? null,
    instance: a[7] ?? null,
  }
}

function d_Jube_Dto_DockerContainerMetric_DockerContainerMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    containerId: a[2] ?? null,
    name: a[3] ?? null,
    image: a[4] ?? null,
    state: a[5] ?? null,
    status: a[6] ?? null,
    restartCount: a[7] ?? null,
    oomKilled: a[8] ?? null,
    exitCode: a[9] ?? null,
    startedAt: a[10] ?? null,
    healthStatus: a[11] ?? null,
    cpuUsagePercent: a[12] ?? null,
    onlineCpus: a[13] ?? null,
    memoryUsageBytes: a[14] ?? null,
    memoryLimitBytes: a[15] ?? null,
    memoryPercent: a[16] ?? null,
    networkRxBytes: a[17] ?? null,
    networkTxBytes: a[18] ?? null,
    blockReadBytes: a[19] ?? null,
    blockWriteBytes: a[20] ?? null,
    pidsCurrent: a[21] ?? null,
    pidsLimit: a[22] ?? null,
    createdDate: a[23] ?? null,
    instance: a[24] ?? null,
  }
}

function d_Jube_Dto_DockerEvent_DockerEventDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    eventType: a[2] ?? null,
    action: a[3] ?? null,
    actorId: a[4] ?? null,
    actorName: a[5] ?? null,
    scope: a[6] ?? null,
    createdDate: a[7] ?? null,
    instance: a[8] ?? null,
  }
}

function d_Jube_Dto_DockerHostMetric_DockerHostMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    containersTotal: a[1] ?? null,
    containersRunning: a[2] ?? null,
    containersPaused: a[3] ?? null,
    containersStopped: a[4] ?? null,
    imagesCount: a[5] ?? null,
    nCpu: a[6] ?? null,
    memTotalBytes: a[7] ?? null,
    dockerVersion: a[8] ?? null,
    apiVersion: a[9] ?? null,
    kernelVersion: a[10] ?? null,
    operatingSystem: a[11] ?? null,
    osType: a[12] ?? null,
    architecture: a[13] ?? null,
    layersSizeBytes: a[14] ?? null,
    imagesSizeBytes: a[15] ?? null,
    reclaimableImagesBytes: a[16] ?? null,
    containersDiskBytes: a[17] ?? null,
    volumesSizeBytes: a[18] ?? null,
    buildCacheSizeBytes: a[19] ?? null,
    createdDate: a[20] ?? null,
    instance: a[21] ?? null,
  }
}

function d_Jube_Dto_DotNetRuntimeMetric_DotNetRuntimeMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    instance: a[2] ?? null,
    processorCount: a[3] ?? null,
    gen0CollectionCount: a[4] ?? null,
    gen1CollectionCount: a[5] ?? null,
    gen2CollectionCount: a[6] ?? null,
    totalAllocatedBytes: a[7] ?? null,
    heapSizeBytes: a[8] ?? null,
    fragmentedBytes: a[9] ?? null,
    memoryLoadBytes: a[10] ?? null,
    highMemoryLoadThresholdBytes: a[11] ?? null,
    workingSetBytes: a[12] ?? null,
    privateMemoryBytes: a[13] ?? null,
    threadCount: a[14] ?? null,
    threadPoolWorkerThreadsAvailable: a[15] ?? null,
    threadPoolWorkerThreadsMax: a[16] ?? null,
    threadPoolCompletionPortThreadsAvailable: a[17] ?? null,
    threadPoolCompletionPortThreadsMax: a[18] ?? null,
    threadPoolQueueLength: a[19] ?? null,
    cpuTimeMicroseconds: a[20] ?? null,
    runtimeAvailableMemoryBytes: a[21] ?? null,
    runtimeCommittedMemoryBytes: a[22] ?? null,
    containerCpuLimitCores: a[23] ?? null,
    containerCpuUsageMicroseconds: a[24] ?? null,
    containerCpuThrottledPeriods: a[25] ?? null,
    containerCpuThrottledMicroseconds: a[26] ?? null,
    containerMemoryLimitBytes: a[27] ?? null,
    containerMemoryUsageBytes: a[28] ?? null,
    gcPauseTimeMicroseconds: a[29] ?? null,
    lockContentionCount: a[30] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisAsynchronousQueueBalance_EntityAnalysisAsynchronousQueueBalanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    instance: a[1] ?? null,
    createdDate: a[2] ?? null,
    asynchronousEntityInvoke: a[3] ?? null,
    caseCreation: a[4] ?? null,
    tagging: a[5] ?? null,
    notification: a[6] ?? null,
    webhook: a[7] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisInlineScript_EntityAnalysisInlineScriptDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModel_EntityAnalysisModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    entryName: a[1] ?? null,
    entryXPath: a[2] ?? null,
    entryPayloadLocationTypeId: a[3] ?? null,
    referenceDateName: a[4] ?? null,
    referenceDatePayloadLocationTypeId: a[5] ?? null,
    referenceDateXPath: a[6] ?? null,
    enableCache: a[7] ?? null,
    cacheFetchLimit: a[8] ?? null,
    cacheTtlInterval: a[9] ?? null,
    cacheTtlIntervalValue: a[10] ?? null,
    enableSanctionCache: a[11] ?? null,
    maxResponseElevation: a[12] ?? null,
    enableResponseElevationLimit: a[13] ?? null,
    maxResponseElevationInterval: a[14] ?? null,
    maxResponseElevationValue: a[15] ?? null,
    maxResponseElevationThreshold: a[16] ?? null,
    enableActivationWatcher: a[17] ?? null,
    maxActivationWatcherInterval: a[18] ?? null,
    maxActivationWatcherValue: a[19] ?? null,
    maxActivationWatcherThreshold: a[20] ?? null,
    activationWatcherSample: a[21] ?? null,
    enableTtlCounter: a[22] ?? null,
    enableRdbmsArchive: a[23] ?? null,
    enableActivationArchive: a[24] ?? null,
    enableElasticsearchArchive: a[25] ?? null,
    enableImplicitAsync: a[26] ?? null,
    implicitAsyncTimeoutMilliseconds: a[27] ?? null,
    enableTrace: a[28] ?? null,
    enableLogs: a[29] ?? null,
    enableLogsInfo: a[30] ?? null,
    enableSampling: a[31] ?? null,
    samplePercentage: a[32] ?? null,
    enableLogsWarnThreshold: a[33] ?? null,
    logsWarnThresholdMilliseconds: a[34] ?? null,
    enableLogsInResponse: a[35] ?? null,
    active: a[36] ?? null,
    guid: a[37] ?? null,
    locked: a[38] ?? null,
    id: a[39] ?? null,
    createdUser: a[40] ?? null,
    createdDate: a[41] ?? null,
    updatedUser: a[42] ?? null,
    updatedDate: a[43] ?? null,
    version: a[44] ?? null,
    deletedUser: a[45] ?? null,
    deletedDate: a[46] ?? null,
    enablePayloadJObject: a[47] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelAbstractionCalculation_EntityAnalysisModelAbstractionCalculationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    functionScript: a[2] ?? null,
    reportTable: a[3] ?? null,
    responsePayload: a[4] ?? null,
    guid: a[5] ?? null,
    active: a[6] ?? null,
    locked: a[7] ?? null,
    id: a[8] ?? null,
    createdUser: a[9] ?? null,
    createdDate: a[10] ?? null,
    updatedUser: a[11] ?? null,
    updatedDate: a[12] ?? null,
    version: a[13] ?? null,
    deletedUser: a[14] ?? null,
    deletedDate: a[15] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelAbstractionRule_EntityAnalysisModelAbstractionRuleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    ruleScriptTypeId: a[2] ?? null,
    coderRuleScript: a[3] ?? null,
    search: a[4] ?? null,
    searchKey: a[5] ?? null,
    searchValue: a[6] ?? null,
    searchInterval: a[7] ?? null,
    searchFunctionTypeId: a[8] ?? null,
    searchFunctionKey: a[9] ?? null,
    offset: a[10] ?? null,
    offsetTypeId: a[11] ?? null,
    offsetValue: a[12] ?? null,
    reportTable: a[13] ?? null,
    responsePayload: a[14] ?? null,
    inheritedId: a[15] ?? null,
    guid: a[16] ?? null,
    active: a[17] ?? null,
    locked: a[18] ?? null,
    builderRuleScript: a[19] ?? null,
    json: a[20] ?? null,
    id: a[21] ?? null,
    createdUser: a[22] ?? null,
    createdDate: a[23] ?? null,
    updatedUser: a[24] ?? null,
    updatedDate: a[25] ?? null,
    version: a[26] ?? null,
    deletedUser: a[27] ?? null,
    deletedDate: a[28] ?? null,
    iterationOrderId: a[29] ?? null,
    searchFunctionParameters: a[30] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelActivationRule_EntityAnalysisModelActivationRuleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    priority: a[2] ?? null,
    ruleScriptTypeId: a[3] ?? null,
    coderRuleScript: a[4] ?? null,
    enableOverride: a[5] ?? null,
    enableForce: a[6] ?? null,
    overrideKey: a[7] ?? null,
    enableCaseWorkflow: a[8] ?? null,
    caseWorkflowGuid: a[9] ?? null,
    caseWorkflowStatusGuid: a[10] ?? null,
    caseKey: a[11] ?? null,
    enableBypass: a[12] ?? null,
    bypassSuspendSample: a[13] ?? null,
    bypassSuspendInterval: a[14] ?? null,
    bypassSuspendValue: a[15] ?? null,
    enableResponseElevation: a[16] ?? null,
    responseElevation: a[17] ?? null,
    responseElevationContent: a[18] ?? null,
    responseElevationRedirect: a[19] ?? null,
    sendToActivationWatcher: a[20] ?? null,
    responseElevationKey: a[21] ?? null,
    responseElevationForeColor: a[22] ?? null,
    responseElevationBackColor: a[23] ?? null,
    enableTtlCounter: a[24] ?? null,
    entityAnalysisModelGuidTtlCounter: a[25] ?? null,
    entityAnalysisModelTtlCounterGuid: a[26] ?? null,
    activationSample: a[27] ?? null,
    visible: a[28] ?? null,
    enableReprocessing: a[29] ?? null,
    reportTable: a[30] ?? null,
    responsePayload: a[31] ?? null,
    evaluationCounter: a[32] ?? null,
    activationCounter: a[33] ?? null,
    activationCounterDate: a[34] ?? null,
    guid: a[35] ?? null,
    active: a[36] ?? null,
    locked: a[37] ?? null,
    builderRuleScript: a[38] ?? null,
    json: a[39] ?? null,
    id: a[40] ?? null,
    createdUser: a[41] ?? null,
    createdDate: a[42] ?? null,
    updatedUser: a[43] ?? null,
    updatedDate: a[44] ?? null,
    version: a[45] ?? null,
    deletedUser: a[46] ?? null,
    deletedDate: a[47] ?? null,
    shadow: a[48] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelActivationRule_ShadowVariantRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    baseRuleId: a[0] ?? null,
    ruleText: a[1] ?? null,
    placeholder: a[2] ?? null,
    value: a[3] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelActivationRule_ShadowVariantResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelActivationRuleOverride_EntityAnalysisModelActivationRuleOverrideDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    overrideKey: a[1] ?? null,
    overrideKeyValue: a[2] ?? null,
    entityAnalysisModelActivationRuleName: a[3] ?? null,
    active: a[4] ?? null,
    deleteExpiryDate: a[5] ?? null,
    overrideKind: a[6] ?? null,
    id: a[7] ?? null,
    createdDate: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    createdUser: a[11] ?? null,
    version: a[12] ?? null,
    deletedUser: a[13] ?? null,
    deletedDate: a[14] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelAsynchronousQueueBalance_EntityAnalysisModelAsynchronousQueueBalanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    instance: a[1] ?? null,
    createdDate: a[2] ?? null,
    archive: a[3] ?? null,
    activationWatcher: a[4] ?? null,
    entityAnalysisModelGuid: a[5] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelDictionary_EntityAnalysisModelDictionaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    name: a[1] ?? null,
    dataName: a[2] ?? null,
    responsePayload: a[3] ?? null,
    guid: a[4] ?? null,
    active: a[5] ?? null,
    locked: a[6] ?? null,
    id: a[7] ?? null,
    createdDate: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    createdUser: a[11] ?? null,
    version: a[12] ?? null,
    deletedUser: a[13] ?? null,
    deletedDate: a[14] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelDictionaryKvp_EntityAnalysisModelDictionaryKvpCsvUploadResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    fileName: a[1] ?? null,
    records: a[2] ?? null,
    errors: a[3] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelDictionaryKvp_EntityAnalysisModelDictionaryKvpDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelDictionaryId: a[0] ?? null,
    kvpKey: a[1] ?? null,
    kvpValue: a[2] ?? null,
    deleteExpiryDate: a[3] ?? null,
    guid: a[4] ?? null,
    id: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    version: a[10] ?? null,
    deletedUser: a[11] ?? null,
    deletedDate: a[12] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelGatewayRule_EntityAnalysisModelGatewayRuleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    priority: a[3] ?? null,
    ruleScriptTypeId: a[4] ?? null,
    coderRuleScript: a[5] ?? null,
    maxResponseElevation: a[6] ?? null,
    gatewaySample: a[7] ?? null,
    responsePayload: a[8] ?? null,
    activationCounter: a[9] ?? null,
    activationCounterDate: a[10] ?? null,
    evaluationCounter: a[11] ?? null,
    guid: a[12] ?? null,
    active: a[13] ?? null,
    locked: a[14] ?? null,
    builderRuleScript: a[15] ?? null,
    json: a[16] ?? null,
    id: a[17] ?? null,
    createdUser: a[18] ?? null,
    createdDate: a[19] ?? null,
    updatedUser: a[20] ?? null,
    updatedDate: a[21] ?? null,
    version: a[22] ?? null,
    deletedUser: a[23] ?? null,
    deletedDate: a[24] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelHttpAdaptation_EntityAnalysisModelHttpAdaptationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    httpEndpoint: a[2] ?? null,
    priority: a[3] ?? null,
    reportTable: a[4] ?? null,
    responsePayload: a[5] ?? null,
    inheritedId: a[6] ?? null,
    guid: a[7] ?? null,
    active: a[8] ?? null,
    locked: a[9] ?? null,
    id: a[10] ?? null,
    createdUser: a[11] ?? null,
    createdDate: a[12] ?? null,
    updatedUser: a[13] ?? null,
    updatedDate: a[14] ?? null,
    version: a[15] ?? null,
    deletedUser: a[16] ?? null,
    deletedDate: a[17] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelInlineFunction_EntityAnalysisModelInlineFunctionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    returnDataTypeId: a[2] ?? null,
    functionScript: a[3] ?? null,
    encryptionId: a[4] ?? null,
    reportTable: a[5] ?? null,
    responsePayload: a[6] ?? null,
    guid: a[7] ?? null,
    active: a[8] ?? null,
    locked: a[9] ?? null,
    id: a[10] ?? null,
    createdUser: a[11] ?? null,
    createdDate: a[12] ?? null,
    updatedUser: a[13] ?? null,
    updatedDate: a[14] ?? null,
    version: a[15] ?? null,
    deletedUser: a[16] ?? null,
    deletedDate: a[17] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelInlineScript_EntityAnalysisModelInlineScriptDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    entityAnalysisInlineScriptId: a[2] ?? null,
    guid: a[3] ?? null,
    active: a[4] ?? null,
    locked: a[5] ?? null,
    id: a[6] ?? null,
    createdUser: a[7] ?? null,
    createdDate: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    version: a[11] ?? null,
    deletedUser: a[12] ?? null,
    deletedDate: a[13] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelList_EntityAnalysisModelListDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    name: a[1] ?? null,
    guid: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    id: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    version: a[10] ?? null,
    deletedUser: a[11] ?? null,
    deletedDate: a[12] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelListValue_EntityAnalysisModelListValueCsvUploadResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    fileName: a[1] ?? null,
    entityAnalysisModelListId: a[2] ?? null,
    records: a[3] ?? null,
    errors: a[4] ?? null,
    length: a[5] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelListValue_EntityAnalysisModelListValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelListId: a[0] ?? null,
    listValue: a[1] ?? null,
    deleteExpiryDate: a[2] ?? null,
    guid: a[3] ?? null,
    id: a[4] ?? null,
    createdUser: a[5] ?? null,
    createdDate: a[6] ?? null,
    updatedUser: a[7] ?? null,
    updatedDate: a[8] ?? null,
    version: a[9] ?? null,
    deletedUser: a[10] ?? null,
    deletedDate: a[11] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelOverride_EntityAnalysisModelOverrideDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    overrideKey: a[1] ?? null,
    overrideKeyValue: a[2] ?? null,
    active: a[3] ?? null,
    deleteExpiryDate: a[4] ?? null,
    overrideKind: a[5] ?? null,
    id: a[6] ?? null,
    createdDate: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    createdUser: a[10] ?? null,
    version: a[11] ?? null,
    deletedUser: a[12] ?? null,
    deletedDate: a[13] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelProcessingCounter_EntityAnalysisModelProcessingCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    id: a[1] ?? null,
    createdDate: a[2] ?? null,
    instance: a[3] ?? null,
    modelInvoke: a[4] ?? null,
    gatewayMatch: a[5] ?? null,
    responseElevation: a[6] ?? null,
    responseElevationSum: a[7] ?? null,
    activationWatcher: a[8] ?? null,
    responseElevationLimit: a[9] ?? null,
    modelTotalResponseTime: a[10] ?? null,
    minResponseTimeMicroseconds: a[11] ?? null,
    maxResponseTimeMicroseconds: a[12] ?? null,
    archiveWalPendingCount: a[13] ?? null,
    entityAnalysisModelGuid: a[14] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelReprocessingRule_EntityAnalysisModelReprocessingRuleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    priority: a[2] ?? null,
    ruleScriptTypeId: a[3] ?? null,
    builderRuleScript: a[4] ?? null,
    json: a[5] ?? null,
    coderRuleScript: a[6] ?? null,
    active: a[7] ?? null,
    locked: a[8] ?? null,
    reprocessingSample: a[9] ?? null,
    reprocessingValue: a[10] ?? null,
    reprocessingInterval: a[11] ?? null,
    id: a[12] ?? null,
    createdUser: a[13] ?? null,
    createdDate: a[14] ?? null,
    updatedUser: a[15] ?? null,
    updatedDate: a[16] ?? null,
    version: a[17] ?? null,
    deletedUser: a[18] ?? null,
    deletedDate: a[19] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelReprocessingRuleInstance_EntityAnalysisModelReprocessingRuleInstanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelReprocessingRuleId: a[0] ?? null,
    statusId: a[1] ?? null,
    startedDate: a[2] ?? null,
    availableCount: a[3] ?? null,
    sampledCount: a[4] ?? null,
    matchedCount: a[5] ?? null,
    processedCount: a[6] ?? null,
    completedDate: a[7] ?? null,
    errorCount: a[8] ?? null,
    referenceDate: a[9] ?? null,
    id: a[10] ?? null,
    createdUser: a[11] ?? null,
    createdDate: a[12] ?? null,
    updatedUser: a[13] ?? null,
    updatedDate: a[14] ?? null,
    version: a[15] ?? null,
    deletedUser: a[16] ?? null,
    deletedDate: a[17] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelRequestXPath_EntityAnalysisModelRequestXPathDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    dataTypeId: a[2] ?? null,
    xPath: a[3] ?? null,
    defaultValue: a[4] ?? null,
    encryptionId: a[5] ?? null,
    enableOverride: a[6] ?? null,
    cache: a[7] ?? null,
    reportTable: a[8] ?? null,
    responsePayload: a[9] ?? null,
    searchKey: a[10] ?? null,
    searchKeyTtlInterval: a[11] ?? null,
    searchKeyTtlIntervalValue: a[12] ?? null,
    searchKeyFetchLimit: a[13] ?? null,
    searchKeyCache: a[14] ?? null,
    searchKeyCacheInterval: a[15] ?? null,
    searchKeyCacheValue: a[16] ?? null,
    searchKeyCacheSample: a[17] ?? null,
    searchKeyCacheFetchLimit: a[18] ?? null,
    searchKeyCacheTtlInterval: a[19] ?? null,
    searchKeyCacheTtlValue: a[20] ?? null,
    encryptOffDeploymentRegion: a[21] ?? null,
    xPathExpression: a[22] ?? null,
    hashEntityKeyComposite: a[23] ?? null,
    hashEntryKeyComposite: a[24] ?? null,
    active: a[25] ?? null,
    locked: a[26] ?? null,
    id: a[27] ?? null,
    createdUser: a[28] ?? null,
    createdDate: a[29] ?? null,
    updatedUser: a[30] ?? null,
    updatedDate: a[31] ?? null,
    version: a[32] ?? null,
    deletedUser: a[33] ?? null,
    deletedDate: a[34] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelResponseElevationRange_EntityAnalysisModelResponseElevationRangeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    guid: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    minimum: a[5] ?? null,
    maximum: a[6] ?? null,
    decline: a[7] ?? null,
    hold: a[8] ?? null,
    content: a[9] ?? null,
    redirect: a[10] ?? null,
    foreColor: a[11] ?? null,
    backColor: a[12] ?? null,
    enableHttpEndpoint: a[13] ?? null,
    httpEndpoint: a[14] ?? null,
    httpEndpointTypeId: a[15] ?? null,
    httpEndpointHeaderName: a[16] ?? null,
    httpEndpointCredentialVariable: a[17] ?? null,
    enableNotification: a[18] ?? null,
    notificationTypeId: a[19] ?? null,
    notificationDestination: a[20] ?? null,
    notificationSubject: a[21] ?? null,
    notificationBody: a[22] ?? null,
    label: a[23] ?? null,
    id: a[24] ?? null,
    createdUser: a[25] ?? null,
    createdDate: a[26] ?? null,
    updatedUser: a[27] ?? null,
    updatedDate: a[28] ?? null,
    version: a[29] ?? null,
    deletedUser: a[30] ?? null,
    deletedDate: a[31] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelResponseTimePipelineCounter_EntityAnalysisModelResponseTimePipelineCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    entityAnalysisModelName: a[2] ?? null,
    stageId: a[3] ?? null,
    stageName: a[4] ?? null,
    sequenceNumber: a[5] ?? null,
    totalMicroseconds: a[6] ?? null,
    minMicroseconds: a[7] ?? null,
    maxMicroseconds: a[8] ?? null,
    totalAllocatedBytes: a[9] ?? null,
    minAllocatedBytes: a[10] ?? null,
    maxAllocatedBytes: a[11] ?? null,
    invokeCount: a[12] ?? null,
    createdDate: a[13] ?? null,
    instance: a[14] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelSanction_EntityAnalysisModelSanctionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    multipartStringDataName: a[2] ?? null,
    distance: a[3] ?? null,
    aggregationTypeId: a[4] ?? null,
    maxDistanceRatio: a[5] ?? null,
    maxCoverageRatio: a[6] ?? null,
    cacheInterval: a[7] ?? null,
    cacheValue: a[8] ?? null,
    reportTable: a[9] ?? null,
    responsePayload: a[10] ?? null,
    guid: a[11] ?? null,
    active: a[12] ?? null,
    locked: a[13] ?? null,
    id: a[14] ?? null,
    createdUser: a[15] ?? null,
    createdDate: a[16] ?? null,
    updatedUser: a[17] ?? null,
    updatedDate: a[18] ?? null,
    version: a[19] ?? null,
    deletedUser: a[20] ?? null,
    deletedDate: a[21] ?? null,
    matchAlgorithmId: a[22] ?? null,
    minimumSimilarity: a[23] ?? null,
    transliterate: a[24] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelStagePerformanceCounter_EntityAnalysisModelStagePerformanceCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    entityAnalysisModelName: a[2] ?? null,
    stageId: a[3] ?? null,
    stageName: a[4] ?? null,
    totalMicroseconds: a[5] ?? null,
    minMicroseconds: a[6] ?? null,
    maxMicroseconds: a[7] ?? null,
    invokeCount: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelTag_EntityAnalysisModelTagDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    guid: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    id: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    version: a[10] ?? null,
    deletedUser: a[11] ?? null,
    deletedDate: a[12] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelTaskPerformanceCounter_EntityAnalysisModelTaskPerformanceCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    entityAnalysisModelName: a[2] ?? null,
    directionId: a[3] ?? null,
    directionName: a[4] ?? null,
    taskTypeId: a[5] ?? null,
    taskName: a[6] ?? null,
    totalMicroseconds: a[7] ?? null,
    minMicroseconds: a[8] ?? null,
    maxMicroseconds: a[9] ?? null,
    totalAllocatedBytes: a[10] ?? null,
    minAllocatedBytes: a[11] ?? null,
    maxAllocatedBytes: a[12] ?? null,
    invokeCount: a[13] ?? null,
    createdDate: a[14] ?? null,
    instance: a[15] ?? null,
  }
}

function d_Jube_Dto_EntityAnalysisModelTtlCounter_EntityAnalysisModelTtlCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    ttlCounterDataName: a[2] ?? null,
    enableLiveForever: a[3] ?? null,
    ttlCounterInterval: a[4] ?? null,
    ttlCounterValue: a[5] ?? null,
    resolutionInterval: a[6] ?? null,
    enableSum: a[7] ?? null,
    ttlCounterDataValue: a[8] ?? null,
    responsePayload: a[9] ?? null,
    reportTable: a[10] ?? null,
    onlineAggregation: a[11] ?? null,
    active: a[12] ?? null,
    guid: a[13] ?? null,
    locked: a[14] ?? null,
    id: a[15] ?? null,
    createdUser: a[16] ?? null,
    createdDate: a[17] ?? null,
    updatedUser: a[18] ?? null,
    updatedDate: a[19] ?? null,
    version: a[20] ?? null,
    deletedUser: a[21] ?? null,
    deletedDate: a[22] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalActionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    note: a[0] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalBulkDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    entityId: a[1] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalBulkResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    approved: a[0] ?? null,
    refusals: a[1] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalChangeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    before: a[1] ?? null,
    after: a[2] ?? null,
    status: a[3] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalChangesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hasEarlierVersion: a[0] ?? null,
    changes: ((v) => many(v, d_Jube_Dto_EntityApproval_EntityApprovalChangeDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    entityId: a[1] ?? null,
    version: a[2] ?? null,
    note: a[3] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    kind: a[1] ?? null,
    entityId: a[2] ?? null,
    entityVersion: a[3] ?? null,
    state: a[4] ?? null,
    note: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    returnedDate: a[8] ?? null,
    returnedUser: a[9] ?? null,
    returnedNote: a[10] ?? null,
    returnKindId: a[11] ?? null,
    carriedForwardFromVersion: a[12] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_EntityApprovalStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    entityId: a[1] ?? null,
    name: a[2] ?? null,
    currentVersion: a[3] ?? null,
    effectiveVersion: a[4] ?? null,
    deleted: a[5] ?? null,
    pending: a[6] ?? null,
    rejected: a[7] ?? null,
    makerUser: a[8] ?? null,
    approvalsRecorded: a[9] ?? null,
    approvalsRequired: a[10] ?? null,
    canApprove: a[11] ?? null,
    note: a[12] ?? null,
    returnedNote: a[13] ?? null,
  }
}

function d_Jube_Dto_EntityApproval_PendingApprovalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    entityId: a[1] ?? null,
    name: a[2] ?? null,
    version: a[3] ?? null,
    deleted: a[4] ?? null,
    makerUser: a[5] ?? null,
    modelId: a[6] ?? null,
    parentId: a[7] ?? null,
  }
}

function d_Jube_Dto_EtcdClusterEvent_EtcdClusterEventDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    endpoint: a[2] ?? null,
    memberId: a[3] ?? null,
    name: a[4] ?? null,
    eventTypeId: a[5] ?? null,
    eventTypeName: a[6] ?? null,
    previousValue: a[7] ?? null,
    newValue: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_EtcdMemberStatus_EtcdMemberStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    endpoint: a[2] ?? null,
    memberId: a[3] ?? null,
    name: a[4] ?? null,
    peerUrls: a[5] ?? null,
    clientUrls: a[6] ?? null,
    isLearner: a[7] ?? null,
    version: a[8] ?? null,
    clusterVersion: a[9] ?? null,
    healthOk: a[10] ?? null,
    healthReason: a[11] ?? null,
    leaderId: a[12] ?? null,
    isLeader: a[13] ?? null,
    hasLeader: a[14] ?? null,
    leaderChangesTotal: a[15] ?? null,
    dbSizeBytes: a[16] ?? null,
    dbSizeInUseBytes: a[17] ?? null,
    raftIndex: a[18] ?? null,
    raftTerm: a[19] ?? null,
    raftAppliedIndex: a[20] ?? null,
    proposalsCommittedTotal: a[21] ?? null,
    proposalsAppliedTotal: a[22] ?? null,
    proposalsPendingCount: a[23] ?? null,
    proposalsFailedTotal: a[24] ?? null,
    walFsyncAvgMicroseconds: a[25] ?? null,
    backendCommitAvgMicroseconds: a[26] ?? null,
    slowApplyTotal: a[27] ?? null,
    slowReadIndexesTotal: a[28] ?? null,
    alarmCount: a[29] ?? null,
    alarms: a[30] ?? null,
    createdDate: a[31] ?? null,
    instance: a[32] ?? null,
  }
}

function d_Jube_Dto_ExhaustiveInstanceDataset_DatasetDistinctValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    count: a[1] ?? null,
  }
}

function d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    index: a[0] ?? null,
    header: a[1] ?? null,
    kind: a[2] ?? null,
    numericCount: a[3] ?? null,
    textCount: a[4] ?? null,
    missingCount: a[5] ?? null,
    min: a[6] ?? null,
    max: a[7] ?? null,
    mean: a[8] ?? null,
    zeroCount: a[9] ?? null,
    oneCount: a[10] ?? null,
    isBinary: a[11] ?? null,
    isConstant: a[12] ?? null,
    formulaLikeCount: a[13] ?? null,
    include: a[14] ?? null,
    name: a[15] ?? null,
    processingTypeId: a[16] ?? null,
    isCategorical: a[17] ?? null,
    distinctValues: ((v) => many(v, d_Jube_Dto_ExhaustiveInstanceDataset_DatasetDistinctValueDto))(a[18] ?? null),
  }
}

function d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    kind: a[3] ?? null,
    fileName: a[4] ?? null,
    sourceLabel: a[5] ?? null,
    contentHash: a[6] ?? null,
    rowCount: a[7] ?? null,
    sizeBytes: a[8] ?? null,
    columnCount: a[9] ?? null,
    statusId: a[10] ?? null,
    delimiter: a[11] ?? null,
    columns: ((v) => many(v, d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetColumnDto))(a[12] ?? null),
    warnings: a[13] ?? null,
    createdUser: a[14] ?? null,
    createdDate: a[15] ?? null,
    version: a[16] ?? null,
  }
}

function d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetMappingColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    index: a[0] ?? null,
    include: a[1] ?? null,
    name: a[2] ?? null,
    processingTypeId: a[3] ?? null,
  }
}

function d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetMappingDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    columns: ((v) => many(v, d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetMappingColumnDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_ExhaustiveSearchInstance_ExhaustiveSearchInstanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    dataSourceId: a[2] ?? null,
    exhaustiveInstanceCsvId: a[3] ?? null,
    exhaustiveInstanceJsonId: a[4] ?? null,
    baseRate: a[5] ?? null,
    bootstrap: a[6] ?? null,
    bootstrapTo: a[7] ?? null,
    bootstrapWeightFactor: a[8] ?? null,
    challengerModelBaseProbability: a[9] ?? null,
    calibrationMethod: a[10] ?? null,
    anomaly: a[11] ?? null,
    anomalyProbability: a[12] ?? null,
    anomalyAlgorithmId: a[13] ?? null,
    anomalyParametersJson: a[14] ?? null,
    anomalySummary: a[15] ?? null,
    anomalyReportJson: a[16] ?? null,
    filter: a[17] ?? null,
    filterJson: a[18] ?? null,
    filterSql: a[19] ?? null,
    filterTokens: a[20] ?? null,
    categoricalOptionsJson: a[21] ?? null,
    reportTable: a[22] ?? null,
    responsePayload: a[23] ?? null,
    statusId: a[24] ?? null,
    models: a[25] ?? null,
    modelsSinceBest: a[26] ?? null,
    score: a[27] ?? null,
    topologyComplexity: a[28] ?? null,
    completedDate: a[29] ?? null,
    active: a[30] ?? null,
    guid: a[31] ?? null,
    locked: a[32] ?? null,
    id: a[33] ?? null,
    createdUser: a[34] ?? null,
    createdDate: a[35] ?? null,
    updatedUser: a[36] ?? null,
    updatedDate: a[37] ?? null,
    version: a[38] ?? null,
    deletedUser: a[39] ?? null,
    deletedDate: a[40] ?? null,
  }
}

function d_Jube_Dto_Filter_FilterCountGroupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    count: a[1] ?? null,
  }
}

function d_Jube_Dto_Filter_FilterCountResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    count: a[1] ?? null,
    groups: ((v) => many(v, d_Jube_Dto_Filter_FilterCountGroupDto))(a[2] ?? null),
    groupsTruncated: a[3] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Filter_FilterFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    dataType: a[1] ?? null,
    operators: a[2] ?? null,
    description: a[3] ?? null,
  }
}

function d_Jube_Dto_HaProxyReachabilityProbe_HaProxyReachabilityProbeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    target: a[2] ?? null,
    haProxyAddress: a[3] ?? null,
    success: a[4] ?? null,
    connectMicroseconds: a[5] ?? null,
    httpStatusCode: a[6] ?? null,
    errorMessage: a[7] ?? null,
    createdDate: a[8] ?? null,
    instance: a[9] ?? null,
  }
}

function d_Jube_Dto_HaProxyServerStatus_HaProxyServerStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    pxName: a[2] ?? null,
    svName: a[3] ?? null,
    status: a[4] ?? null,
    addr: a[5] ?? null,
    checkStatus: a[6] ?? null,
    checkCode: a[7] ?? null,
    chkFail: a[8] ?? null,
    chkDown: a[9] ?? null,
    lastChg: a[10] ?? null,
    scur: a[11] ?? null,
    qcur: a[12] ?? null,
    weight: a[13] ?? null,
    act: a[14] ?? null,
    bck: a[15] ?? null,
    hrsp2Xx: a[16] ?? null,
    hrsp5Xx: a[17] ?? null,
    mode: a[18] ?? null,
    createdDate: a[19] ?? null,
    instance: a[20] ?? null,
  }
}

function d_Jube_Dto_Hold_ResponseHold(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    stateId: a[0] ?? null,
    rangeId: a[1] ?? null,
    rangeName: a[2] ?? null,
    rangeMinimum: a[3] ?? null,
    rangeMaximum: a[4] ?? null,
    elevation: a[5] ?? null,
    stampedDate: a[6] ?? null,
    decision: d_Jube_Dto_Hold_ResponseHoldDecision(a[7] ?? null),
    legs: ((v) => many(v, d_Jube_Dto_Hold_ResponseHoldLeg))(a[8] ?? null),
  }
}

function d_Jube_Dto_Hold_ResponseHoldDecision(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    actor: a[0] ?? null,
    date: a[1] ?? null,
    reason: a[2] ?? null,
    releaseHookId: a[3] ?? null,
    releaseHookName: a[4] ?? null,
    httpStatus: a[5] ?? null,
  }
}

function d_Jube_Dto_Hold_ResponseHoldLeg(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    outcomeId: a[1] ?? null,
    date: a[2] ?? null,
    destination: a[3] ?? null,
    httpStatus: a[4] ?? null,
    detail: a[5] ?? null,
  }
}

function d_Jube_Dto_HttpProcessingCounter_HttpProcessingCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    instance: a[0] ?? null,
    createdDate: a[1] ?? null,
    model: a[2] ?? null,
    asynchronousModel: a[3] ?? null,
    tag: a[4] ?? null,
    error: a[5] ?? null,
    sanction: a[6] ?? null,
    exhaustive: a[7] ?? null,
    all: a[8] ?? null,
  }
}

function d_Jube_Dto_Invoke_AdvancedAnalyticsRecallContributionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    weight: a[1] ?? null,
    significance: a[2] ?? null,
    source: a[3] ?? null,
  }
}

function d_Jube_Dto_Invoke_AdvancedAnalyticsRecallDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    score: a[0] ?? null,
    baseValue: a[1] ?? null,
    contribution: ((v) => many(v, d_Jube_Dto_Invoke_AdvancedAnalyticsRecallContributionDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Invoke_ExhaustiveBatchRecallResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rowCount: a[0] ?? null,
    scoredCount: a[1] ?? null,
    mappedVariables: a[2] ?? null,
    unmappedVariables: a[3] ?? null,
    warnings: a[4] ?? null,
    rows: ((v) => many(v, d_Jube_Dto_Invoke_ExhaustiveBatchRecallRowDto))(a[5] ?? null),
  }
}

function d_Jube_Dto_Invoke_ExhaustiveBatchRecallRowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rowNumber: a[0] ?? null,
    score: a[1] ?? null,
    baseValue: a[2] ?? null,
    relevance: ((v) => many(v, d_Jube_Dto_Invoke_ExhaustiveRecallVariableRelevanceDto))(a[3] ?? null),
    error: a[4] ?? null,
  }
}

function d_Jube_Dto_Invoke_ExhaustiveRecallDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    score: a[0] ?? null,
    baseValue: a[1] ?? null,
    relevance: ((v) => many(v, d_Jube_Dto_Invoke_ExhaustiveRecallVariableRelevanceDto))(a[2] ?? null),
    nodeRelevance: a[3] ?? null,
  }
}

function d_Jube_Dto_Invoke_ExhaustiveRecallVariableRelevanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    connectionWeight: a[1] ?? null,
    lrpRelevance: a[2] ?? null,
  }
}

function d_Jube_Dto_Invoke_SanctionAggregationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    sum: a[0] ?? null,
    average: a[1] ?? null,
    count: a[2] ?? null,
    max: a[3] ?? null,
    min: a[4] ?? null,
    first: a[5] ?? null,
    last: a[6] ?? null,
    confidence: a[7] ?? null,
  }
}

function d_Jube_Dto_Invoke_SanctionAggregationsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    total: d_Jube_Dto_Invoke_SanctionAggregationDto(a[0] ?? null),
    bySource: ((v) => many(v, d_Jube_Dto_Invoke_SanctionSourceAggregationDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Invoke_SanctionEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    sanctionEntrySourceId: a[1] ?? null,
    source: a[2] ?? null,
    reference: a[3] ?? null,
    id: a[4] ?? null,
    distance: a[5] ?? null,
  }
}

function d_Jube_Dto_Invoke_SanctionSearchRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    multiPartString: a[0] ?? null,
    distance: a[1] ?? null,
    maxDistanceRatio: a[2] ?? null,
    maxCoverageRatio: a[3] ?? null,
  }
}

function d_Jube_Dto_Invoke_SanctionSearchResponseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    aggregations: d_Jube_Dto_Invoke_SanctionAggregationsDto(a[0] ?? null),
    entries: ((v) => many(v, d_Jube_Dto_Invoke_SanctionEntryDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Invoke_SanctionSourceAggregationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    sum: a[0] ?? null,
    average: a[1] ?? null,
    count: a[2] ?? null,
    max: a[3] ?? null,
    min: a[4] ?? null,
    first: a[5] ?? null,
    last: a[6] ?? null,
    confidence: a[7] ?? null,
    sourceId: a[8] ?? null,
    sourceName: a[9] ?? null,
  }
}

function d_Jube_Dto_ModelInvokeWarning_ModelInvokeWarningDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    entityAnalysisModelGuid: a[2] ?? null,
    entityAnalysisModelName: a[3] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[4] ?? null,
    message: a[5] ?? null,
    elapsedMicroseconds: a[6] ?? null,
    sinceLastEntryMicroseconds: a[7] ?? null,
    threadId: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_OpenTelemetryExclude_OpenTelemetryExcludeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    active: a[1] ?? null,
    locked: a[2] ?? null,
    id: a[3] ?? null,
    createdUser: a[4] ?? null,
    createdDate: a[5] ?? null,
    updatedUser: a[6] ?? null,
    updatedDate: a[7] ?? null,
    version: a[8] ?? null,
    deletedUser: a[9] ?? null,
    deletedDate: a[10] ?? null,
  }
}

function d_Jube_Dto_OpenTelemetryLogCounter_OpenTelemetryLogCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    regex: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    id: a[4] ?? null,
    createdUser: a[5] ?? null,
    createdDate: a[6] ?? null,
    updatedUser: a[7] ?? null,
    updatedDate: a[8] ?? null,
    version: a[9] ?? null,
    deletedUser: a[10] ?? null,
    deletedDate: a[11] ?? null,
  }
}

function d_Jube_Dto_OpenTelemetryMetric_OpenTelemetryMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    metricName: a[2] ?? null,
    instrumentType: a[3] ?? null,
    tags: a[4] ?? null,
    count: a[5] ?? null,
    sum: a[6] ?? null,
    min: a[7] ?? null,
    max: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_OtlpDispatchCounter_OtlpDispatchCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    signalId: a[1] ?? null,
    signalName: a[2] ?? null,
    count: a[3] ?? null,
    successCount: a[4] ?? null,
    failureCount: a[5] ?? null,
    itemCount: a[6] ?? null,
    droppedCount: a[7] ?? null,
    totalMicroseconds: a[8] ?? null,
    minMicroseconds: a[9] ?? null,
    maxMicroseconds: a[10] ?? null,
    createdDate: a[11] ?? null,
    instance: a[12] ?? null,
  }
}

function d_Jube_Dto_OverlayNetworkTaskDrift_OverlayNetworkTaskDriftDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    serviceName: a[2] ?? null,
    dnsResolvedAddresses: a[3] ?? null,
    swarmTaskAddresses: a[4] ?? null,
    addressesOnlyInDns: a[5] ?? null,
    addressesOnlyInSwarm: a[6] ?? null,
    isConsistent: a[7] ?? null,
    createdDate: a[8] ?? null,
    instance: a[9] ?? null,
  }
}

function d_Jube_Dto_PatroniClusterEvent_PatroniClusterEventDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    scope: a[2] ?? null,
    name: a[3] ?? null,
    eventTypeId: a[4] ?? null,
    eventTypeName: a[5] ?? null,
    previousValue: a[6] ?? null,
    newValue: a[7] ?? null,
    reason: a[8] ?? null,
    timelineId: a[9] ?? null,
    lsnBytes: a[10] ?? null,
    createdDate: a[11] ?? null,
    instance: a[12] ?? null,
  }
}

function d_Jube_Dto_PatroniMemberStatus_PatroniMemberStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    name: a[2] ?? null,
    host: a[3] ?? null,
    port: a[4] ?? null,
    apiUrl: a[5] ?? null,
    role: a[6] ?? null,
    state: a[7] ?? null,
    timelineId: a[8] ?? null,
    lagBytes: a[9] ?? null,
    pendingRestart: a[10] ?? null,
    patroniVersion: a[11] ?? null,
    scope: a[12] ?? null,
    postgresServerVersion: a[13] ?? null,
    databaseSystemIdentifier: a[14] ?? null,
    xlogLocationBytes: a[15] ?? null,
    receivedLocationBytes: a[16] ?? null,
    replayPaused: a[17] ?? null,
    clusterUnlocked: a[18] ?? null,
    syncStandby: a[19] ?? null,
    createdDate: a[20] ?? null,
    instance: a[21] ?? null,
  }
}

function d_Jube_Dto_Payload_BacktestChartRequest(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dark: a[0] ?? null,
    scanned: a[1] ?? null,
    evaluated: a[2] ?? null,
    fired: a[3] ?? null,
    positives: a[4] ?? null,
    truePositives: a[5] ?? null,
    falsePositives: a[6] ?? null,
    falseNegatives: a[7] ?? null,
    trueNegatives: a[8] ?? null,
    classDefined: a[9] ?? null,
    tags: ((v) => many(v, d_Jube_Dto_Payload_BacktestChartTag))(a[10] ?? null),
  }
}

function d_Jube_Dto_Payload_BacktestChartTag(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    count: a[1] ?? null,
  }
}

function d_Jube_Dto_Payload_ColumnStatistics(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    min: a[0] ?? null,
    max: a[1] ?? null,
    mean: a[2] ?? null,
    median: a[3] ?? null,
    standardDeviation: a[4] ?? null,
    histogram: ((v) => many(v, d_Jube_Dto_Payload_HistogramBucket))(a[5] ?? null),
  }
}

function d_Jube_Dto_Payload_HistogramBucket(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    bin: a[0] ?? null,
    frequency: a[1] ?? null,
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ApplicationLogEntry_ApplicationLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_ApplicationLogEntry_ApplicationLogEntryDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ArchiverStagePerformanceCounter_ArchiverStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_ArchiverStagePerformanceCounter_ArchiverStagePerformanceCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ArchiverWarning_ArchiverWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_ArchiverWarning_ArchiverWarningDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaptureQueueHealth_CaptureQueueHealthDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_CaptureQueueHealth_CaptureQueueHealthDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaseCreationStagePerformanceCounter_CaseCreationStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_CaseCreationStagePerformanceCounter_CaseCreationStagePerformanceCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaseCreationWarning_CaseCreationWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_CaseCreationWarning_CaseCreationWarningDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ContainerLogEntry_ContainerLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_ContainerLogEntry_ContainerLogEntryDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerContainerMetric_DockerContainerMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_DockerContainerMetric_DockerContainerMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerEvent_DockerEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_DockerEvent_DockerEventDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerHostMetric_DockerHostMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_DockerHostMetric_DockerHostMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DotNetRuntimeMetric_DotNetRuntimeMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_DotNetRuntimeMetric_DotNetRuntimeMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisAsynchronousQueueBalance_EntityAnalysisAsynchronousQueueBalanceDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisAsynchronousQueueBalance_EntityAnalysisAsynchronousQueueBalanceDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelAsynchronousQueueBalance_EntityAnalysisModelAsynchronousQueueBalanceDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisModelAsynchronousQueueBalance_EntityAnalysisModelAsynchronousQueueBalanceDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelProcessingCounter_EntityAnalysisModelProcessingCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisModelProcessingCounter_EntityAnalysisModelProcessingCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelResponseTimePipelineCounter_EntityAnalysisModelResponseTimePipelineCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisModelResponseTimePipelineCounter_EntityAnalysisModelResponseTimePipelineCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelStagePerformanceCounter_EntityAnalysisModelStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisModelStagePerformanceCounter_EntityAnalysisModelStagePerformanceCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelTaskPerformanceCounter_EntityAnalysisModelTaskPerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EntityAnalysisModelTaskPerformanceCounter_EntityAnalysisModelTaskPerformanceCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EtcdClusterEvent_EtcdClusterEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EtcdClusterEvent_EtcdClusterEventDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EtcdMemberStatus_EtcdMemberStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_EtcdMemberStatus_EtcdMemberStatusDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HaProxyReachabilityProbe_HaProxyReachabilityProbeDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_HaProxyReachabilityProbe_HaProxyReachabilityProbeDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HaProxyServerStatus_HaProxyServerStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_HaProxyServerStatus_HaProxyServerStatusDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HttpProcessingCounter_HttpProcessingCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_HttpProcessingCounter_HttpProcessingCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ModelInvokeWarning_ModelInvokeWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_ModelInvokeWarning_ModelInvokeWarningDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OpenTelemetryMetric_OpenTelemetryMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_OpenTelemetryMetric_OpenTelemetryMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OtlpDispatchCounter_OtlpDispatchCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_OtlpDispatchCounter_OtlpDispatchCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OverlayNetworkTaskDrift_OverlayNetworkTaskDriftDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_OverlayNetworkTaskDrift_OverlayNetworkTaskDriftDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PatroniClusterEvent_PatroniClusterEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PatroniClusterEvent_PatroniClusterEventDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PatroniMemberStatus_PatroniMemberStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PatroniMemberStatus_PatroniMemberStatusDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresActivity_PostgresActivityDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresActivity_PostgresActivityDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresIndexStatistics_PostgresIndexStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresIndexStatistics_PostgresIndexStatisticsDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresLogEntry_PostgresLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresLogEntry_PostgresLogEntryDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresMetric_PostgresMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresMetric_PostgresMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresReplicationStatus_PostgresReplicationStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresReplicationStatus_PostgresReplicationStatusDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresStatementStatistics_PostgresStatementStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresStatementStatistics_PostgresStatementStatisticsDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresTableStatistics_PostgresTableStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_PostgresTableStatistics_PostgresTableStatisticsDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespCallCounter_RespCallCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespCallCounter_RespCallCounterDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespConnectionEvent_RespConnectionEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespConnectionEvent_RespConnectionEventDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespConnectionMultiplexerMetric_RespConnectionMultiplexerMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespConnectionMultiplexerMetric_RespConnectionMultiplexerMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespMetric_RespMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespMetric_RespMetricDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSentinelEvent_RespSentinelEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespSentinelEvent_RespSentinelEventDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSentinelStatus_RespSentinelStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespSentinelStatus_RespSentinelStatusDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSlowOperation_RespSlowOperationDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_RespSlowOperation_RespSlowOperationDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_UserLogin_UserLoginDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_UserLogin_UserLoginDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_UserLogout_UserLogoutDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_UserLogout_UserLogoutDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_WafAttack_WafAttackDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    rows: ((v) => many(v, d_Jube_Dto_WafAttack_WafAttackDto))(a[0] ?? null),
    total: a[1] ?? null,
    statistics: d_Jube_Dto_Payload_PayloadStatistics(a[2] ?? null),
  }
}

function d_Jube_Dto_Payload_PayloadStatistics(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    columns: ((v) => dict(v, d_Jube_Dto_Payload_ColumnStatistics))(a[0] ?? null),
  }
}

function d_Jube_Dto_Payload_StatisticsHistogramChartRequest(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    title: a[0] ?? null,
    unit: a[1] ?? null,
    dark: a[2] ?? null,
    color: a[3] ?? null,
    statistics: d_Jube_Dto_Payload_ColumnStatistics(a[4] ?? null),
  }
}

function d_Jube_Dto_Payload_WatcherSummaryChartRequest(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dark: a[0] ?? null,
    columnColor: a[1] ?? null,
    lineColor: a[2] ?? null,
    rules: ((v) => many(v, d_Jube_Dto_Payload_WatcherSummaryRule))(a[3] ?? null),
  }
}

function d_Jube_Dto_Payload_WatcherSummaryRule(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    frequency: a[1] ?? null,
    aggregate: a[2] ?? null,
  }
}

function d_Jube_Dto_PostgresActivity_PostgresActivityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    pid: a[0] ?? null,
    databaseName: a[1] ?? null,
    userName: a[2] ?? null,
    applicationName: a[3] ?? null,
    clientAddress: a[4] ?? null,
    backendType: a[5] ?? null,
    state: a[6] ?? null,
    waitEventType: a[7] ?? null,
    waitEvent: a[8] ?? null,
    blockedByPids: a[9] ?? null,
    backendStartDate: a[10] ?? null,
    transactionStartDate: a[11] ?? null,
    transactionDurationSeconds: a[12] ?? null,
    queryStartDate: a[13] ?? null,
    queryDurationSeconds: a[14] ?? null,
    query: a[15] ?? null,
  }
}

function d_Jube_Dto_PostgresIndexStatistics_PostgresIndexStatisticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    schemaName: a[0] ?? null,
    tableName: a[1] ?? null,
    indexName: a[2] ?? null,
    indexScans: a[3] ?? null,
    indexTuplesRead: a[4] ?? null,
    indexTuplesFetched: a[5] ?? null,
    indexSizeBytes: a[6] ?? null,
    isUnique: a[7] ?? null,
    isPrimary: a[8] ?? null,
    isUnused: a[9] ?? null,
  }
}

function d_Jube_Dto_PostgresLock_PostgresLockDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    pid: a[0] ?? null,
    userName: a[1] ?? null,
    query: a[2] ?? null,
    lockType: a[3] ?? null,
    mode: a[4] ?? null,
    granted: a[5] ?? null,
    relationName: a[6] ?? null,
    virtualTransactionId: a[7] ?? null,
    fastPath: a[8] ?? null,
  }
}

function d_Jube_Dto_PostgresLogEntry_PostgresLogEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    level: a[2] ?? null,
    pid: a[3] ?? null,
    message: a[4] ?? null,
    createdDate: a[5] ?? null,
    instance: a[6] ?? null,
  }
}

function d_Jube_Dto_PostgresMetric_PostgresMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    instance: a[2] ?? null,
    activeConnections: a[3] ?? null,
    transactionsCommitted: a[4] ?? null,
    transactionsRolledBack: a[5] ?? null,
    blocksRead: a[6] ?? null,
    blocksHit: a[7] ?? null,
    cacheHitRatioPercent: a[8] ?? null,
    rowsReturned: a[9] ?? null,
    rowsFetched: a[10] ?? null,
    rowsInserted: a[11] ?? null,
    rowsUpdated: a[12] ?? null,
    rowsDeleted: a[13] ?? null,
    deadlocks: a[14] ?? null,
    tempFilesCreated: a[15] ?? null,
    tempBytesWritten: a[16] ?? null,
    conflicts: a[17] ?? null,
    isInRecovery: a[18] ?? null,
    replicationLagSeconds: a[19] ?? null,
    replicaCount: a[20] ?? null,
    databaseSizeBytes: a[21] ?? null,
    longestRunningQuerySeconds: a[22] ?? null,
    waitingBackends: a[23] ?? null,
    walBytesGenerated: a[24] ?? null,
  }
}

function d_Jube_Dto_PostgresReplicationStatus_PostgresReplicationStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    pid: a[2] ?? null,
    userName: a[3] ?? null,
    applicationName: a[4] ?? null,
    clientAddress: a[5] ?? null,
    state: a[6] ?? null,
    sentLsn: a[7] ?? null,
    writeLsn: a[8] ?? null,
    flushLsn: a[9] ?? null,
    replayLsn: a[10] ?? null,
    writeLagSeconds: a[11] ?? null,
    flushLagSeconds: a[12] ?? null,
    replayLagSeconds: a[13] ?? null,
    syncState: a[14] ?? null,
    syncPriority: a[15] ?? null,
    createdDate: a[16] ?? null,
    instance: a[17] ?? null,
  }
}

function d_Jube_Dto_PostgresStatementStatistics_PostgresStatementStatisticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    queryId: a[0] ?? null,
    databaseName: a[1] ?? null,
    userName: a[2] ?? null,
    query: a[3] ?? null,
    calls: a[4] ?? null,
    rows: a[5] ?? null,
    totalExecTimeMilliseconds: a[6] ?? null,
    meanExecTimeMilliseconds: a[7] ?? null,
    minExecTimeMilliseconds: a[8] ?? null,
    maxExecTimeMilliseconds: a[9] ?? null,
    stddevExecTimeMilliseconds: a[10] ?? null,
    sharedBlksHitBytes: a[11] ?? null,
    sharedBlksReadBytes: a[12] ?? null,
    tempBlksReadBytes: a[13] ?? null,
    tempBlksWrittenBytes: a[14] ?? null,
    walBytes: a[15] ?? null,
  }
}

function d_Jube_Dto_PostgresTableStatistics_PostgresTableStatisticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    schemaName: a[0] ?? null,
    tableName: a[1] ?? null,
    sequentialScans: a[2] ?? null,
    sequentialTuplesRead: a[3] ?? null,
    indexScans: a[4] ?? null,
    indexTuplesFetched: a[5] ?? null,
    liveTupleCount: a[6] ?? null,
    deadTupleCount: a[7] ?? null,
    tableSizeBytes: a[8] ?? null,
    indexesSizeBytes: a[9] ?? null,
    totalSizeBytes: a[10] ?? null,
    lastVacuum: a[11] ?? null,
    lastAutoVacuum: a[12] ?? null,
    lastAnalyze: a[13] ?? null,
    lastAutoAnalyze: a[14] ?? null,
    likelyMissingIndex: a[15] ?? null,
  }
}

function d_Jube_Dto_Preservation_PreservationSnapshotDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    bytes: a[2] ?? null,
    takenAtUtc: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_AdvancedAnalyticsNarrative_AdvancedAnalyticsNarrativeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    holistic: a[0] ?? null,
    groups: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseAnalytics_CaseAnalyticsHeadlineDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    opened: a[0] ?? null,
    closed: a[1] ?? null,
    openNow: a[2] ?? null,
    netChange: a[3] ?? null,
    unassessedOpen: a[4] ?? null,
    urgentOpen: a[5] ?? null,
    decided: a[6] ?? null,
    confirmedRate: a[7] ?? null,
    medianFirstViewMinutes: a[8] ?? null,
    medianResolutionMinutes: a[9] ?? null,
    slaBreachedOpen: a[10] ?? null,
    slaAtRiskOpen: a[11] ?? null,
    reopenRate: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_CaseAnalytics_CaseAnalyticsRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    from: a[1] ?? null,
    to: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBulk_CaseBulkCloseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseIds: a[0] ?? null,
    closedReasonId: a[1] ?? null,
    closedReasonNote: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBulk_CaseBulkItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    done: a[1] ?? null,
    skipReasonId: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBulk_CaseBulkReassignDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseIds: a[0] ?? null,
    toUser: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBulk_CaseBulkResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    items: ((v) => many(v, d_Jube_Dto_Query_CaseBulk_CaseBulkItemDto))(a[0] ?? null),
    done: a[1] ?? null,
    skipped: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseByCaseKeyValue_CaseByCaseKeyValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    diaryDate: a[2] ?? null,
    caseWorkflowGuid: a[3] ?? null,
    caseWorkflowStatusName: a[4] ?? null,
    createdDate: a[5] ?? null,
    locked: a[6] ?? null,
    lockedUser: a[7] ?? null,
    lockedDate: a[8] ?? null,
    closedStatusId: a[9] ?? null,
    closedDate: a[10] ?? null,
    closedUser: a[11] ?? null,
    caseKey: a[12] ?? null,
    diary: a[13] ?? null,
    diaryUser: a[14] ?? null,
    rating: a[15] ?? null,
    caseKeyValue: a[16] ?? null,
    lastClosedStatus: a[17] ?? null,
    closedStatusMigrationDate: a[18] ?? null,
    foreColor: a[19] ?? null,
    backColor: a[20] ?? null,
  }
}

function d_Jube_Dto_Query_CaseById_CaseByIdActivationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseById_CaseByIdDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    diaryDate: a[2] ?? null,
    caseWorkflowGuid: a[3] ?? null,
    caseWorkflowStatusGuid: a[4] ?? null,
    createdDate: a[5] ?? null,
    locked: a[6] ?? null,
    lockedUser: a[7] ?? null,
    lockedDate: a[8] ?? null,
    closedStatusId: a[9] ?? null,
    closedDate: a[10] ?? null,
    closedUser: a[11] ?? null,
    caseKey: a[12] ?? null,
    diary: a[13] ?? null,
    diaryUser: a[14] ?? null,
    rating: a[15] ?? null,
    caseKeyValue: a[16] ?? null,
    lastClosedStatus: a[17] ?? null,
    closedStatusMigrationDate: a[18] ?? null,
    foreColor: a[19] ?? null,
    backColor: a[20] ?? null,
    json: a[21] ?? null,
    formattedPayload: ((v) => many(v, d_Jube_Dto_Query_CaseById_CaseByIdFieldEntryDto))(a[22] ?? null),
    activation: ((v) => many(v, d_Jube_Dto_Query_CaseById_CaseByIdActivationDto))(a[23] ?? null),
    enableVisualisation: a[24] ?? null,
    visualisationRegistryGuid: a[25] ?? null,
    enableGraph: a[26] ?? null,
    graphRegistryGuid: a[27] ?? null,
    entityAnalysisModelId: a[28] ?? null,
  }
}

function d_Jube_Dto_Query_CaseById_CaseByIdFieldEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    value: a[1] ?? null,
    cellFormatForeColor: a[2] ?? null,
    cellFormatForeRow: a[3] ?? null,
    cellFormatBackColor: a[4] ?? null,
    cellFormatBackRow: a[5] ?? null,
    existsMatch: a[6] ?? null,
    conditionalRegularExpressionFormatting: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileActivationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    diaryDate: a[2] ?? null,
    caseWorkflowGuid: a[3] ?? null,
    caseWorkflowStatusGuid: a[4] ?? null,
    createdDate: a[5] ?? null,
    locked: a[6] ?? null,
    lockedUser: a[7] ?? null,
    lockedDate: a[8] ?? null,
    closedStatusId: a[9] ?? null,
    closedDate: a[10] ?? null,
    closedUser: a[11] ?? null,
    caseKey: a[12] ?? null,
    diary: a[13] ?? null,
    diaryUser: a[14] ?? null,
    rating: a[15] ?? null,
    caseKeyValue: a[16] ?? null,
    lastClosedStatus: a[17] ?? null,
    closedStatusMigrationDate: a[18] ?? null,
    foreColor: a[19] ?? null,
    backColor: a[20] ?? null,
    json: a[21] ?? null,
    formattedPayload: ((v) => many(v, d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileFieldEntryDto))(a[22] ?? null),
    activation: ((v) => many(v, d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileActivationDto))(a[23] ?? null),
    enableVisualisation: a[24] ?? null,
    visualisationRegistryGuid: a[25] ?? null,
    enableGraph: a[26] ?? null,
    graphRegistryGuid: a[27] ?? null,
    entityAnalysisModelId: a[28] ?? null,
  }
}

function d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileFieldEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    value: a[1] ?? null,
    cellFormatForeColor: a[2] ?? null,
    cellFormatForeRow: a[3] ?? null,
    cellFormatBackColor: a[4] ?? null,
    cellFormatBackRow: a[5] ?? null,
    existsMatch: a[6] ?? null,
    conditionalRegularExpressionFormatting: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    checklists: ((v) => many(v, d_Jube_Dto_Query_CaseChecklist_CaseChecklistGroupDto))(a[1] ?? null),
    required: a[2] ?? null,
    done: a[3] ?? null,
    open: a[4] ?? null,
    breached: a[5] ?? null,
    reconciledDate: a[6] ?? null,
    override: d_Jube_Dto_Query_CaseChecklist_CaseChecklistOverrideDto(a[7] ?? null),
    complete: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistGroupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowChecklistGuid: a[0] ?? null,
    name: a[1] ?? null,
    items: ((v) => many(v, d_Jube_Dto_Query_CaseChecklist_CaseChecklistItemDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistHistoryEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    actionId: a[1] ?? null,
    itemName: a[2] ?? null,
    actor: a[3] ?? null,
    createdDate: a[4] ?? null,
    detail: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowChecklistItemGuid: a[1] ?? null,
    name: a[2] ?? null,
    priority: a[3] ?? null,
    stateId: a[4] ?? null,
    firstShownDate: a[5] ?? null,
    shownDate: a[6] ?? null,
    dueDate: a[7] ?? null,
    slaBreachedDate: a[8] ?? null,
    completedDate: a[9] ?? null,
    completedUser: a[10] ?? null,
    slaStateId: a[11] ?? null,
    minutesRemaining: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistOverrideDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    reason: a[2] ?? null,
    grantedUser: a[3] ?? null,
    grantedDate: a[4] ?? null,
    openItems: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseChecklist_CaseChecklistOverrideRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    reason: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseDiary_CaseDiaryClearDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseDiary_CaseDiarySetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    diaryTypeId: a[1] ?? null,
    dueDate: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseEventByCaseKeyValue_CaseEventByCaseKeyValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    caseEventTypeId: a[2] ?? null,
    caseEventType: a[3] ?? null,
    createdDate: a[4] ?? null,
    createdUser: a[5] ?? null,
    before: a[6] ?? null,
    after: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseHold_CaseHoldDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseHold_CaseHoldDecisionResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    stateId: a[0] ?? null,
    releaseHookName: a[1] ?? null,
    httpStatus: a[2] ?? null,
    version: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseHold_CaseHoldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelInstanceEntryGuid: a[0] ?? null,
    entryKeyValue: a[1] ?? null,
    referenceDate: a[2] ?? null,
    stateId: a[3] ?? null,
    rangeName: a[4] ?? null,
    rangeMinimum: a[5] ?? null,
    rangeMaximum: a[6] ?? null,
    elevation: a[7] ?? null,
    stampedDate: a[8] ?? null,
    deadlineDate: a[9] ?? null,
    decision: d_Jube_Dto_Hold_ResponseHoldDecision(a[10] ?? null),
    legs: ((v) => many(v, d_Jube_Dto_Hold_ResponseHoldLeg))(a[11] ?? null),
    releaseHookName: a[12] ?? null,
    refuseCallsHook: a[13] ?? null,
  }
}

function d_Jube_Dto_Query_CaseJournal_CaseJournalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    schema: a[0] ?? null,
    rows: ((v) => rows(v, null))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalActivationsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    meta: d_Jube_Dto_Query_CaseKeyJournal_JournalActivationsMetaDto(a[0] ?? null),
    cards: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalActivationCardDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAnalyticsRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    lens: a[3] ?? null,
    from: a[4] ?? null,
    to: a[5] ?? null,
    limit: a[6] ?? null,
    activationsOnly: a[7] ?? null,
    responseElevation: a[8] ?? null,
    caseId: a[9] ?? null,
    pinnedOnly: a[10] ?? null,
    merge: a[11] ?? null,
    filter: a[12] ?? null,
    archiveIds: a[13] ?? null,
    sections: a[14] ?? null,
    fields: a[15] ?? null,
    amountField: a[16] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: a[0] ?? null,
    to: a[1] ?? null,
    groups: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundGroupDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundGroupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    truncated: a[3] ?? null,
    rows: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundRowDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundRowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    referenceDate: a[1] ?? null,
    responseElevation: a[2] ?? null,
    activationCount: a[3] ?? null,
    activations: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    title: a[1] ?? null,
    type: a[2] ?? null,
    roleId: a[3] ?? null,
    roleInferred: a[4] ?? null,
    lens: a[5] ?? null,
    group: a[6] ?? null,
    drill: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalDrillKeyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    title: a[1] ?? null,
    dataTypeId: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    meta: d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalMetaDto(a[0] ?? null),
    columns: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalColumnDto))(a[1] ?? null),
    lenses: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLensDto))(a[2] ?? null),
    rows: ((v) => rows(v, null))(a[3] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLensDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    lens: a[0] ?? null,
    rows: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLocationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    latitude: a[0] ?? null,
    longitude: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalMetaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    drillKeys: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalDrillKeyDto))(a[0] ?? null),
    key: a[1] ?? null,
    value: a[2] ?? null,
    caseWorkflowGuid: a[3] ?? null,
    lens: a[4] ?? null,
    rowCount: a[5] ?? null,
    limit: a[6] ?? null,
    truncated: a[7] ?? null,
    total: a[8] ?? null,
    location: d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLocationDto(a[9] ?? null),
    from: a[10] ?? null,
    to: a[11] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPayloadDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    fields: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPinRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    transactions: a[1] ?? null,
    pinned: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPinsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    transactions: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    caseId: a[1] ?? null,
    name: a[2] ?? null,
    days: a[3] ?? null,
    merge: a[4] ?? null,
    filter: a[5] ?? null,
    limitKmh: a[6] ?? null,
    minimumKilometres: a[7] ?? null,
    impossibleOnly: a[8] ?? null,
    showAll: a[9] ?? null,
    lens: a[10] ?? null,
    createdUser: a[11] ?? null,
    createdDate: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplayRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    name: a[1] ?? null,
    days: a[2] ?? null,
    merge: a[3] ?? null,
    filter: a[4] ?? null,
    limitKmh: a[5] ?? null,
    minimumKilometres: a[6] ?? null,
    impossibleOnly: a[7] ?? null,
    showAll: a[8] ?? null,
    lens: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplaysDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    replays: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplayDto))(a[0] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    lens: a[3] ?? null,
    from: a[4] ?? null,
    to: a[5] ?? null,
    limit: a[6] ?? null,
    activationsOnly: a[7] ?? null,
    responseElevation: a[8] ?? null,
    caseId: a[9] ?? null,
    pinnedOnly: a[10] ?? null,
    merge: a[11] ?? null,
    filter: a[12] ?? null,
    archiveIds: a[13] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalSvirfdDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    ip: a[1] ?? null,
    storedJson: a[2] ?? null,
    storedAt: a[3] ?? null,
    fresh: d_Jube_Dto_Query_Svirfd_SvirfdLookupDto(a[4] ?? null),
    drift: ((v) => many(v, d_Jube_Dto_Query_Svirfd_SvirfdDriftItemDto))(a[5] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalSvirfdRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    id: a[1] ?? null,
    field: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    previous: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagSetDto))(a[0] ?? null),
    current: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagSetDto))(a[1] ?? null),
    skipped: a[2] ?? null,
    locked: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    caseWorkflowGuid: a[1] ?? null,
    kind: a[2] ?? null,
    state: a[3] ?? null,
    key: a[4] ?? null,
    value: a[5] ?? null,
    add: a[6] ?? null,
    remove: a[7] ?? null,
    undoOf: a[8] ?? null,
    total: a[9] ?? null,
    processed: a[10] ?? null,
    changed: a[11] ?? null,
    skipped: a[12] ?? null,
    locked: a[13] ?? null,
    error: a[14] ?? null,
    canUndo: a[15] ?? null,
    createdDate: a[16] ?? null,
    startedDate: a[17] ?? null,
    finishedDate: a[18] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobPreviewDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    matching: a[0] ?? null,
    limit: a[1] ?? null,
    tooMany: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobPreviewRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    from: a[3] ?? null,
    to: a[4] ?? null,
    activationsOnly: a[5] ?? null,
    responseElevation: a[6] ?? null,
    merge: a[7] ?? null,
    filter: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    from: a[3] ?? null,
    to: a[4] ?? null,
    activationsOnly: a[5] ?? null,
    responseElevation: a[6] ?? null,
    merge: a[7] ?? null,
    filter: a[8] ?? null,
    add: a[9] ?? null,
    remove: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagOptionsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    tag: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    transactions: a[1] ?? null,
    add: a[2] ?? null,
    remove: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagRestoreRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    sets: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagSetDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagSetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    transaction: a[0] ?? null,
    tag: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    meta: d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackMetaDto(a[0] ?? null),
    summary: d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelSummaryDto(a[1] ?? null),
    points: ((v) => rows(v, null))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackMetaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    value: a[1] ?? null,
    from: a[2] ?? null,
    to: a[3] ?? null,
    total: a[4] ?? null,
    matched: a[5] ?? null,
    located: a[6] ?? null,
    unlocated: a[7] ?? null,
    limit: a[8] ?? null,
    truncated: a[9] ?? null,
    location: d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLocationDto(a[10] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    from: a[3] ?? null,
    to: a[4] ?? null,
    activationsOnly: a[5] ?? null,
    responseElevation: a[6] ?? null,
    merge: a[7] ?? null,
    filter: a[8] ?? null,
    summaryOnly: a[9] ?? null,
    caseId: a[10] ?? null,
    includeUnmatched: a[11] ?? null,
    limitKmh: a[12] ?? null,
    minimumKilometres: a[13] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTransactionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    referenceDate: a[2] ?? null,
    createdDate: a[3] ?? null,
    responseElevation: a[4] ?? null,
    responseElevationName: a[5] ?? null,
    hold: d_Jube_Dto_Hold_ResponseHold(a[6] ?? null),
    entryKeyValue: a[7] ?? null,
    modelName: a[8] ?? null,
    tags: a[9] ?? null,
    activations: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionActivationDto))(a[10] ?? null),
    values: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionValueDto))(a[11] ?? null),
    keys: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionKeyDto))(a[12] ?? null),
    adaptations: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationDto))(a[13] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTransactionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    id: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelLegDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    lane: a[0] ?? null,
    fromTransaction: a[1] ?? null,
    toTransaction: a[2] ?? null,
    fromDate: a[3] ?? null,
    toDate: a[4] ?? null,
    kilometres: a[5] ?? null,
    hours: a[6] ?? null,
    speedKmh: a[7] ?? null,
    impossible: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    legs: a[0] ?? null,
    impossible: a[1] ?? null,
    limitKmh: a[2] ?? null,
    minimumKilometres: a[3] ?? null,
    worst: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelLegDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalActivationCardDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    source: a[1] ?? null,
    name: a[2] ?? null,
    humanLabel: a[3] ?? null,
    fired: a[4] ?? null,
    forced: a[5] ?? null,
    anchorId: a[6] ?? null,
    anchorDate: a[7] ?? null,
    value: a[8] ?? null,
    threshold: a[9] ?? null,
    calibrated: a[10] ?? null,
    bandId: a[11] ?? null,
    observedRate: a[12] ?? null,
    expectedRate: a[13] ?? null,
    bandSample: a[14] ?? null,
    modelName: a[15] ?? null,
    modelVersion: a[16] ?? null,
    drivers: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalActivationDriverDto))(a[17] ?? null),
    otherDrivers: a[18] ?? null,
    path: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalActivationPathDto))(a[19] ?? null),
    hiddenPath: a[20] ?? null,
    shadow: a[21] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalActivationDriverDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    label: a[0] ?? null,
    signed: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalActivationPathDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    group: a[0] ?? null,
    name: a[1] ?? null,
    value: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalActivationsMetaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    value: a[1] ?? null,
    caseWorkflowGuid: a[2] ?? null,
    rowCount: a[3] ?? null,
    limit: a[4] ?? null,
    truncated: a[5] ?? null,
    from: a[6] ?? null,
    to: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    source: a[0] ?? null,
    name: a[1] ?? null,
    value: a[2] ?? null,
    error: a[3] ?? null,
    humanLabel: a[4] ?? null,
    narrative: a[5] ?? null,
    model: d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationModelDto(a[6] ?? null),
    result: d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationResultDto(a[7] ?? null),
    calibration: d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationDto(a[8] ?? null),
    contribution: d_Jube_Dto_Query_CaseKeyJournal_JournalContributionDto(a[9] ?? null),
    journey: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalJourneyNodeDto))(a[10] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    family: a[1] ?? null,
    version: a[2] ?? null,
    artifactHash: a[3] ?? null,
    trainedDate: a[4] ?? null,
    featureCount: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    threshold: a[0] ?? null,
    activated: a[1] ?? null,
    expectedPositiveRate: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationBandDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    lower: a[0] ?? null,
    upper: a[1] ?? null,
    expected: a[2] ?? null,
    observed: a[3] ?? null,
    count: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    space: a[0] ?? null,
    calibrated: a[1] ?? null,
    method: a[2] ?? null,
    validatedDate: a[3] ?? null,
    sample: a[4] ?? null,
    brier: a[5] ?? null,
    intercept: a[6] ?? null,
    slope: a[7] ?? null,
    bands: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationBandDto))(a[8] ?? null),
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalContributionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    space: a[0] ?? null,
    method: a[1] ?? null,
    exact: a[2] ?? null,
    baseValue: a[3] ?? null,
    items: ((v) => many(v, d_Jube_Dto_Query_CaseKeyJournal_JournalContributionItemDto))(a[4] ?? null),
    hiddenItems: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalContributionItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    weight: a[1] ?? null,
    direction: a[2] ?? null,
    significance: a[3] ?? null,
    source: a[4] ?? null,
    humanLabel: a[5] ?? null,
    signed: a[6] ?? null,
    hidden: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalJourneyNodeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    feature: a[0] ?? null,
    operator: a[1] ?? null,
    threshold: a[2] ?? null,
    thresholdCategory: a[3] ?? null,
    source: a[4] ?? null,
    humanLabel: a[5] ?? null,
    hidden: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionActivationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    forced: a[1] ?? null,
    shadow: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionKeyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    value: a[1] ?? null,
    drill: a[2] ?? null,
    payloadKey: a[3] ?? null,
    dataTypeId: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    group: a[0] ?? null,
    name: a[1] ?? null,
    value: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseNoteByCaseKeyValue_CaseNoteByCaseKeyValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    createdDate: a[2] ?? null,
    createdUser: a[3] ?? null,
    note: a[4] ?? null,
    actionId: a[5] ?? null,
    action: a[6] ?? null,
    priorityId: a[7] ?? null,
    priority: a[8] ?? null,
    sourceId: a[9] ?? null,
    kindId: a[10] ?? null,
    isFinding: a[11] ?? null,
    version: a[12] ?? null,
    supersedesNoteId: a[13] ?? null,
    files: ((v) => many(v, d_Jube_Dto_Repository_CaseFile_CaseFileDto))(a[14] ?? null),
    askJoobyConversationId: a[15] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseAttentionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    needed: a[0] ?? null,
    reasons: ((v) => many(v, d_Jube_Dto_Query_CaseSearch_CaseAttentionReasonDto))(a[1] ?? null),
    lastViewedDate: a[2] ?? null,
    changesSince: a[3] ?? null,
    priorityRise: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseAttentionReasonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    text: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseRiskMatrixCellDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    likelihoodBand: a[0] ?? null,
    impact: a[1] ?? null,
    cases: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseRiskMatrixDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    total: a[0] ?? null,
    cells: ((v) => many(v, d_Jube_Dto_Query_CaseSearch_CaseRiskMatrixCellDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseRiskSnapshotDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    assessed: a[0] ?? null,
    sequence: a[1] ?? null,
    lane: a[2] ?? null,
    laneName: a[3] ?? null,
    laneReason: a[4] ?? null,
    likelihood: a[5] ?? null,
    likelihoodBand: a[6] ?? null,
    likelihoodBandName: a[7] ?? null,
    impact: a[8] ?? null,
    impactName: a[9] ?? null,
    priority: a[10] ?? null,
    expectedHarm: a[11] ?? null,
    method: a[12] ?? null,
    evidenceCount: a[13] ?? null,
    nextDeadline: a[14] ?? null,
    deadlineBreached: a[15] ?? null,
    topDriver: a[16] ?? null,
    topDriverFamily: a[17] ?? null,
    assessedDate: a[18] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseSearchCountDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    key: a[1] ?? null,
    name: a[2] ?? null,
    count: a[3] ?? null,
    mine: a[4] ?? null,
    failed: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseSearchPageDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    total: a[0] ?? null,
    take: a[1] ?? null,
    skip: a[2] ?? null,
    sort: a[3] ?? null,
    generatedAt: a[4] ?? null,
    rows: ((v) => many(v, d_Jube_Dto_Query_CaseSearch_CaseSearchRowDto))(a[5] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseSearchRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    view: a[1] ?? null,
    caseWorkflowFilterGuid: a[2] ?? null,
    caseSearchFilterGuid: a[3] ?? null,
    sessionGuid: a[4] ?? null,
    sort: a[5] ?? null,
    take: a[6] ?? null,
    skip: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseSearchRowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseKey: a[1] ?? null,
    caseKeyValue: a[2] ?? null,
    createdDate: a[3] ?? null,
    ageMinutes: a[4] ?? null,
    caseWorkflowGuid: a[5] ?? null,
    caseWorkflowName: a[6] ?? null,
    caseWorkflowStatusGuid: a[7] ?? null,
    statusName: a[8] ?? null,
    foreColor: a[9] ?? null,
    backColor: a[10] ?? null,
    statusEnteredDate: a[11] ?? null,
    locked: a[12] ?? null,
    lockedUser: a[13] ?? null,
    mine: a[14] ?? null,
    closedStatusId: a[15] ?? null,
    diary: a[16] ?? null,
    diaryDate: a[17] ?? null,
    diaryTypeId: a[18] ?? null,
    diaryType: a[19] ?? null,
    qaSample: a[20] ?? null,
    value: a[21] ?? null,
    risk: d_Jube_Dto_Query_CaseSearch_CaseRiskSnapshotDto(a[22] ?? null),
    attention: d_Jube_Dto_Query_CaseSearch_CaseAttentionDto(a[23] ?? null),
    serviceLevel: d_Jube_Dto_Query_CaseSearch_CaseServiceLevelDto(a[24] ?? null),
    firstViewedDate: a[25] ?? null,
    firstViewedBy: a[26] ?? null,
    firstOwnedDate: a[27] ?? null,
    firstOwnedBy: a[28] ?? null,
    checklistOpen: a[29] ?? null,
    checklistBreached: a[30] ?? null,
    lastEventTypeId: a[31] ?? null,
    lastEventUser: a[32] ?? null,
    lastEventDate: a[33] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseServiceLevelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    state: a[0] ?? null,
    stateName: a[1] ?? null,
    worst: d_Jube_Dto_Query_CaseSearch_CaseServiceLevelMeasureDto(a[2] ?? null),
    measures: ((v) => many(v, d_Jube_Dto_Query_CaseSearch_CaseServiceLevelMeasureDto))(a[3] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseServiceLevelMeasureDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dimension: a[0] ?? null,
    dimensionName: a[1] ?? null,
    state: a[2] ?? null,
    stateName: a[3] ?? null,
    dueDate: a[4] ?? null,
    elapsedFraction: a[5] ?? null,
    actualMinutes: a[6] ?? null,
    targetMinutes: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseSkimDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    case: d_Jube_Dto_Query_CaseSearch_CaseSearchRowDto(a[0] ?? null),
    nextCaseIds: a[1] ?? null,
    remaining: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSearch_CaseWorkflowChoiceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    name: a[1] ?? null,
    modelName: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSimilarity_CaseSimilarNeighbourDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseKey: a[1] ?? null,
    caseKeyValue: a[2] ?? null,
    status: a[3] ?? null,
    closedStatusId: a[4] ?? null,
    outcomeId: a[5] ?? null,
    createdDate: a[6] ?? null,
    closedDate: a[7] ?? null,
    bandId: a[8] ?? null,
    score: a[9] ?? null,
    linked: a[10] ?? null,
    reasons: ((v) => many(v, d_Jube_Dto_Query_CaseSimilarity_CaseSimilarReasonDto))(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSimilarity_CaseSimilarReasonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSimilarity_CaseSimilarityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    built: a[0] ?? null,
    checkedDate: a[1] ?? null,
    similar: a[2] ?? null,
    closed: a[3] ?? null,
    confirmed: a[4] ?? null,
    open: a[5] ?? null,
    enoughToJudge: a[6] ?? null,
    neighbours: ((v) => many(v, d_Jube_Dto_Query_CaseSimilarity_CaseSimilarNeighbourDto))(a[7] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseChecklistSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    required: a[0] ?? null,
    done: a[1] ?? null,
    open: a[2] ?? null,
    breached: a[3] ?? null,
    nextDueDate: a[4] ?? null,
    complete: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseContributionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    family: a[0] ?? null,
    label: a[1] ?? null,
    source: a[2] ?? null,
    confirmed: a[3] ?? null,
    closed: a[4] ?? null,
    precision: a[5] ?? null,
    logLikelihoodRatio: a[6] ?? null,
    decay: a[7] ?? null,
    weight: a[8] ?? null,
    logOdds: a[9] ?? null,
    sharesInputs: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseDeltaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    firstLook: a[0] ?? null,
    lastViewedDate: a[1] ?? null,
    priorityThen: a[2] ?? null,
    priorityNow: a[3] ?? null,
    changes: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseEventSummaryDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseEventSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    eventTypeId: a[0] ?? null,
    eventType: a[1] ?? null,
    user: a[2] ?? null,
    date: a[3] ?? null,
    before: a[4] ?? null,
    after: a[5] ?? null,
    new: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseImpactComponentsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    typologySeverity: a[0] ?? null,
    moneyBand: a[1] ?? null,
    value: a[2] ?? null,
    exposureUplift: a[3] ?? null,
    breadthUplift: a[4] ?? null,
    floor: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseKeyHistoryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    closed: a[0] ?? null,
    confirmed: a[1] ?? null,
    newestClosedDate: a[2] ?? null,
    otherOpenCases: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseNoteSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    note: a[0] ?? null,
    user: a[1] ?? null,
    date: a[2] ?? null,
    files: ((v) => many(v, d_Jube_Dto_Repository_CaseFile_CaseFileDto))(a[3] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseRiskHistoryItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    sequence: a[0] ?? null,
    createdDate: a[1] ?? null,
    trigger: a[2] ?? null,
    lane: a[3] ?? null,
    likelihood: a[4] ?? null,
    impact: a[5] ?? null,
    priority: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseStayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowStatusGuid: a[0] ?? null,
    statusName: a[1] ?? null,
    enterReason: a[2] ?? null,
    enteredDate: a[3] ?? null,
    enteredUser: a[4] ?? null,
    exitedDate: a[5] ?? null,
    exitReason: a[6] ?? null,
    minutes: a[7] ?? null,
    maxMinutes: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSummary_CaseSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    case: d_Jube_Dto_Query_CaseSearch_CaseSearchRowDto(a[0] ?? null),
    contributions: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseContributionDto))(a[1] ?? null),
    baseRate: a[2] ?? null,
    impact: d_Jube_Dto_Query_CaseSummary_CaseImpactComponentsDto(a[3] ?? null),
    riskHistory: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseRiskHistoryItemDto))(a[4] ?? null),
    stays: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseStayDto))(a[5] ?? null),
    recentEvents: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseEventSummaryDto))(a[6] ?? null),
    notes: ((v) => many(v, d_Jube_Dto_Query_CaseSummary_CaseNoteSummaryDto))(a[7] ?? null),
    research: ((v) => many(v, d_Jube_Dto_Repository_CaseResearch_CaseResearchDto))(a[8] ?? null),
    keyHistory: d_Jube_Dto_Query_CaseSummary_CaseKeyHistoryDto(a[9] ?? null),
    checklist: d_Jube_Dto_Query_CaseSummary_CaseChecklistSummaryDto(a[10] ?? null),
    since: d_Jube_Dto_Query_CaseSummary_CaseDeltaDto(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseAnalystWorkloadDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    user: a[0] ?? null,
    holding: a[1] ?? null,
    oldestHoldMinutes: a[2] ?? null,
    taken: a[3] ?? null,
    peeks: a[4] ?? null,
    closed: a[5] ?? null,
    medianResolutionMinutes: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseBreakLockDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    reason: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseLegalHoldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    hold: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseLegalHoldStateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hold: a[0] ?? null,
    reason: a[1] ?? null,
    user: a[2] ?? null,
    date: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseReassignDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    toUser: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseStuckLockDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseKey: a[1] ?? null,
    caseKeyValue: a[2] ?? null,
    statusName: a[3] ?? null,
    lockedUser: a[4] ?? null,
    lockedDate: a[5] ?? null,
    lastActivityDate: a[6] ?? null,
    idleMinutes: a[7] ?? null,
    priority: a[8] ?? null,
    lane: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSupervision_CaseSupervisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    from: a[1] ?? null,
    to: a[2] ?? null,
    unassigned: a[3] ?? null,
    held: a[4] ?? null,
    analysts: ((v) => many(v, d_Jube_Dto_Query_CaseSupervision_CaseAnalystWorkloadDto))(a[5] ?? null),
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionCandidateTransactionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    archiveId: a[0] ?? null,
    referenceDate: a[1] ?? null,
    responseElevation: a[2] ?? null,
    activationCount: a[3] ?? null,
    activations: a[4] ?? null,
    laneKey: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionInForceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    isRule: a[1] ?? null,
    overrideKey: a[2] ?? null,
    overrideKeyValue: a[3] ?? null,
    entityAnalysisModelName: a[4] ?? null,
    ruleName: a[5] ?? null,
    overrideKindId: a[6] ?? null,
    expiryDate: a[7] ?? null,
    createdUser: a[8] ?? null,
    createdDate: a[9] ?? null,
    originCaseId: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionKeyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    isCaseKey: a[1] ?? null,
    enabled: a[2] ?? null,
    reason: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionLaneCountDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    laneKey: a[0] ?? null,
    laneValue: a[1] ?? null,
    count: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionReasonDraftDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    reason: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    count: a[1] ?? null,
    firstSeenDate: a[2] ?? null,
    lastSeenDate: a[3] ?? null,
    lanes: ((v) => many(v, d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionLaneCountDto))(a[4] ?? null),
    widerCountStateId: a[5] ?? null,
    widerTransactions: a[6] ?? null,
    widerKeys: a[7] ?? null,
    widerCapped: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDispatchDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    sourceKindId: a[2] ?? null,
    sourceName: a[3] ?? null,
    channelId: a[4] ?? null,
    modeId: a[5] ?? null,
    destination: a[6] ?? null,
    subject: a[7] ?? null,
    body: a[8] ?? null,
    outcomeId: a[9] ?? null,
    detail: a[10] ?? null,
    responseStatus: a[11] ?? null,
    correlationId: a[12] ?? null,
    rows: a[13] ?? null,
    createdUser: a[14] ?? null,
    createdDate: a[15] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDispatchesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDisplayExecutionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseWorkflowDisplayId: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntriesRenderDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntryValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    value: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntryViewDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    formName: a[2] ?? null,
    createdUser: a[3] ?? null,
    createdDate: a[4] ?? null,
    html: a[5] ?? null,
    otherValues: ((v) => many(v, d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntryValueDto))(a[6] ?? null),
    error: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    options: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormRenderDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseWorkflowFormId: a[1] ?? null,
    values: a[2] ?? null,
    readOnly: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormRenderResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    html: a[0] ?? null,
    visibleFields: a[1] ?? null,
    fields: ((v) => many(v, d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormFieldDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    dataType: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlTestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    sql: a[0] ?? null,
    caseId: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlTestResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    ok: a[0] ?? null,
    error: a[1] ?? null,
    columns: ((v) => many(v, d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlColumnDto))(a[2] ?? null),
    rows: ((v) => rows(v, null))(a[3] ?? null),
    rowCount: a[4] ?? null,
    truncated: a[5] ?? null,
    milliseconds: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowFormEntryByCaseKeyValue_CaseWorkflowFormEntryByCaseKeyValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    createdDate: a[2] ?? null,
    createdUser: a[3] ?? null,
    responseStatusId: a[4] ?? null,
    name: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_CaseWorkflowMacroExecution_CaseWorkflowMacroExecutionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    caseWorkflowMacroId: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_Completions_CompletionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    name: a[1] ?? null,
    meta: a[2] ?? null,
    score: a[3] ?? null,
    dataType: a[4] ?? null,
    group: a[5] ?? null,
    field: a[6] ?? null,
    xPath: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelActivationRuleOverrideQuery_EntityAnalysisModelActivationRuleOverrideQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    hasOverride: a[1] ?? null,
    entityAnalysisModelActivationRuleOverrideId: a[2] ?? null,
    entityAnalysisModelGuid: a[3] ?? null,
    enableForce: a[4] ?? null,
    overrideKind: a[5] ?? null,
    deleteExpiryDate: a[6] ?? null,
    overrideId: a[7] ?? null,
    pendingRemoval: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestExplainRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    request: d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto(a[0] ?? null),
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestExplanationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    entryKeyValue: a[2] ?? null,
    referenceDate: a[3] ?? null,
    tags: a[4] ?? null,
    passedFilter: a[5] ?? null,
    filterError: a[6] ?? null,
    fired: a[7] ?? null,
    ruleError: a[8] ?? null,
    positive: a[9] ?? null,
    values: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestValueDto))(a[10] ?? null),
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestFieldsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    filterFields: ((v) => many(v, d_Jube_Dto_Filter_FilterFieldDto))(a[0] ?? null),
    classFields: ((v) => many(v, d_Jube_Dto_Filter_FilterFieldDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestInstanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    entityAnalysisModelId: a[2] ?? null,
    ruleType: a[3] ?? null,
    ruleId: a[4] ?? null,
    status: a[5] ?? null,
    progress: a[6] ?? null,
    scanned: a[7] ?? null,
    evaluated: a[8] ?? null,
    createdDate: a[9] ?? null,
    createdUser: a[10] ?? null,
    startedDate: a[11] ?? null,
    completedDate: a[12] ?? null,
    error: a[13] ?? null,
    request: d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto(a[14] ?? null),
    fired: a[15] ?? null,
    positives: a[16] ?? null,
    truePositives: a[17] ?? null,
    falsePositives: a[18] ?? null,
    falseNegatives: a[19] ?? null,
    trueNegatives: a[20] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    ruleType: a[1] ?? null,
    ruleId: a[2] ?? null,
    ruleText: a[3] ?? null,
    filterJson: a[4] ?? null,
    filterRuleText: a[5] ?? null,
    classJson: a[6] ?? null,
    from: a[7] ?? null,
    to: a[8] ?? null,
    limit: a[9] ?? null,
    sampleSize: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    scanned: a[1] ?? null,
    filteredOut: a[2] ?? null,
    filterErrors: a[3] ?? null,
    evaluated: a[4] ?? null,
    limitReached: a[5] ?? null,
    earliestReferenceDate: a[6] ?? null,
    latestReferenceDate: a[7] ?? null,
    fired: a[8] ?? null,
    notFired: a[9] ?? null,
    runtimeErrors: a[10] ?? null,
    aborted: a[11] ?? null,
    abortReason: a[12] ?? null,
    classDefined: a[13] ?? null,
    positives: a[14] ?? null,
    truePositives: a[15] ?? null,
    falsePositives: a[16] ?? null,
    falseNegatives: a[17] ?? null,
    trueNegatives: a[18] ?? null,
    durationMicroseconds: a[19] ?? null,
    tagsInSample: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTagCountDto))(a[20] ?? null),
    truePositiveSamples: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto))(a[21] ?? null),
    falsePositiveSamples: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto))(a[22] ?? null),
    falseNegativeSamples: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto))(a[23] ?? null),
    errorSamples: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto))(a[24] ?? null),
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[25] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelInstanceEntryGuid: a[0] ?? null,
    entryKeyValue: a[1] ?? null,
    referenceDate: a[2] ?? null,
    fired: a[3] ?? null,
    positive: a[4] ?? null,
    error: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSubmitRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    request: d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto(a[0] ?? null),
    windowInterval: a[1] ?? null,
    windowValue: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSubmitResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    instance: d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestInstanceDto(a[1] ?? null),
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTagCountDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    count: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    request: d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto(a[0] ?? null),
    placeholder: a[1] ?? null,
    candidates: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    variants: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneVariantDto))(a[1] ?? null),
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneVariantDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    evaluated: a[1] ?? null,
    fired: a[2] ?? null,
    runtimeErrors: a[3] ?? null,
    truePositives: a[4] ?? null,
    falsePositives: a[5] ?? null,
    falseNegatives: a[6] ?? null,
    trueNegatives: a[7] ?? null,
    precision: a[8] ?? null,
    recall: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    value: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyImpactDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entity: d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto(a[0] ?? null),
    dependents: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto))(a[1] ?? null),
    directDependents: a[2] ?? null,
    deleteBlocked: a[3] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyIssuesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dangling: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto))(a[0] ?? null),
    onInactiveTargets: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto))(a[1] ?? null),
    unreferenced: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyListDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entity: d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto(a[0] ?? null),
    dependencies: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto))(a[1] ?? null),
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    dependent: d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto(a[0] ?? null),
    target: d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto(a[1] ?? null),
    dependencyKind: a[2] ?? null,
    name: a[3] ?? null,
    line: a[4] ?? null,
    depth: a[5] ?? null,
    via: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    id: a[1] ?? null,
    name: a[2] ?? null,
    active: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelHistorySimulation_AbstractionSimulationResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    simulated: a[0] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[1] ?? null),
    cacheBacked: a[2] ?? null,
    searchKey: a[3] ?? null,
    searchKeyValue: a[4] ?? null,
    fetchLimit: a[5] ?? null,
    documentsFetched: a[6] ?? null,
    fetchLimitReached: a[7] ?? null,
    documentsEvaluated: a[8] ?? null,
    matched: a[9] ?? null,
    inWindow: a[10] ?? null,
    windowFrom: a[11] ?? null,
    windowTo: a[12] ?? null,
    windowShortenedBySearchKey: a[13] ?? null,
    value: a[14] ?? null,
    sample: ((v) => rows(v, null))(a[15] ?? null),
    runtimeError: a[16] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelHistorySimulation_TtlCounterValueResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    read: a[0] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[1] ?? null),
    name: a[2] ?? null,
    dataName: a[3] ?? null,
    dataValue: a[4] ?? null,
    onlineAggregation: a[5] ?? null,
    windowFrom: a[6] ?? null,
    value: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineInstanceStateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    instance: a[0] ?? null,
    started: a[1] ?? null,
    capturedDate: a[2] ?? null,
    loaded: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedCountDto))(a[3] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedCountDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    count: a[1] ?? null,
    entities: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedEntityDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedEntityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    guid: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineNodeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    instance: a[0] ?? null,
    heartbeatDate: a[1] ?? null,
    synchronisedDate: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineStateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    source: a[0] ?? null,
    nodes: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineNodeDto))(a[1] ?? null),
    instances: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineInstanceStateDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_IntegrityCheckDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    title: a[1] ?? null,
    severity: a[2] ?? null,
    category: a[3] ?? null,
    entityKind: a[4] ?? null,
    entityId: a[5] ?? null,
    entityName: a[6] ?? null,
    message: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_IntegrityModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    nodes: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphNodeDto))(a[0] ?? null),
    edges: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphEdgeDto))(a[1] ?? null),
    truncated: a[2] ?? null,
    maxNodes: a[3] ?? null,
    focus: a[4] ?? null,
    collapseAbove: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphEdgeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: a[0] ?? null,
    to: a[1] ?? null,
    label: a[2] ?? null,
    dashed: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphNodeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    label: a[1] ?? null,
    kind: a[2] ?? null,
    active: a[3] ?? null,
    detail: a[4] ?? null,
    errors: a[5] ?? null,
    warnings: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelIntegrityReportDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    modelName: a[1] ?? null,
    checkedDate: a[2] ?? null,
    engineSource: a[3] ?? null,
    errors: a[4] ?? null,
    warnings: a[5] ?? null,
    infos: a[6] ?? null,
    checks: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelIntegrity_IntegrityCheckDto))(a[7] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    source: a[1] ?? null,
    referenceDate: a[2] ?? null,
    entryId: a[3] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[4] ?? null,
    values: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextValueDto))(a[5] ?? null),
    stages: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextStageDto))(a[6] ?? null),
    errors: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextFromArchiveDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextFromRequestJsonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    requestJson: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextOverlayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    context: d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextDto(a[0] ?? null),
    values: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextStageDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    stage: a[0] ?? null,
    status: a[1] ?? null,
    note: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    group: a[1] ?? null,
    dataType: a[2] ?? null,
    value: a[3] ?? null,
    origin: a[4] ?? null,
    note: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideKeyQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    overrideKey: a[0] ?? null,
    enabledOnModels: a[1] ?? null,
    values: a[2] ?? null,
    overrides: a[3] ?? null,
    forced: a[4] ?? null,
    activationRules: a[5] ?? null,
    allActivationRules: a[6] ?? null,
    nextExpiryDate: a[7] ?? null,
    lastCreatedDate: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    hasOverride: a[2] ?? null,
    overrideKind: a[3] ?? null,
    deleteExpiryDate: a[4] ?? null,
    overrideId: a[5] ?? null,
    pendingRemoval: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideValueQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    overrideKeyValue: a[0] ?? null,
    name: a[1] ?? null,
    entityAnalysisModelGuid: a[2] ?? null,
    overrideKind: a[3] ?? null,
    createdUser: a[4] ?? null,
    createdDate: a[5] ?? null,
    deleteExpiryDate: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelSample_EntityAnalysisModelSampleOptionsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    dateFrom: a[1] ?? null,
    dateTo: a[2] ?? null,
    sample: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelSynchronisationNodeStatusEntries_EntityAnalysisModelSynchronisationNodeStatusEntriesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    synchronisationPending: a[0] ?? null,
    instanceAvailable: a[1] ?? null,
    instance: a[2] ?? null,
    synchronisedDate: a[3] ?? null,
    heartbeatDate: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    kind: a[1] ?? null,
    interval: a[2] ?? null,
    intervalName: a[3] ?? null,
    value: a[4] ?? null,
    from: a[5] ?? null,
    to: a[6] ?? null,
    lengthSeconds: a[7] ?? null,
    searchKeyShortensTheWindow: a[8] ?? null,
    liveForever: a[9] ?? null,
    note: a[10] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowIntervalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowKindDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    intervals: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowIntervalDto))(a[1] ?? null),
    arithmetic: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType_EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    dataTypeId: a[1] ?? null,
    searchKey: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveNarrative_ExhaustiveNarrativeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    holistic: a[0] ?? null,
    groups: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceConfusion_ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    score: a[1] ?? null,
    falsePositive: a[2] ?? null,
    truePositive: a[3] ?? null,
    falseNegative: a[4] ?? null,
    trueNegative: a[5] ?? null,
    tableTotal: a[6] ?? null,
    positiveRowTotal: a[7] ?? null,
    positiveColumnTotal: a[8] ?? null,
    negativeRowTotal: a[9] ?? null,
    negativeColumnTotal: a[10] ?? null,
    positiveRowTableTotal: a[11] ?? null,
    positiveColumnTableTotal: a[12] ?? null,
    negativeRowTableTotal: a[13] ?? null,
    negativeColumnTableTotal: a[14] ?? null,
    truePositiveRowTotal: a[15] ?? null,
    truePositiveColumnTotal: a[16] ?? null,
    truePositiveTableTotal: a[17] ?? null,
    falsePositiveRowTotal: a[18] ?? null,
    falsePositiveColumnTotal: a[19] ?? null,
    falsePositiveTableTotal: a[20] ?? null,
    falseNegativeRowTotal: a[21] ?? null,
    falseNegativeColumnTotal: a[22] ?? null,
    falseNegativeTableTotal: a[23] ?? null,
    trueNegativeRowTotal: a[24] ?? null,
    trueNegativeColumnTotal: a[25] ?? null,
    trueNegativeTableTotal: a[26] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram_ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    bin: a[0] ?? null,
    frequency: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve_ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    score: a[0] ?? null,
    createdDate: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstancePredictedActual_ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    predicted: a[0] ?? null,
    actual: a[1] ?? null,
    error: a[2] ?? null,
    value: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceQuery_ExhaustiveSearchInstancePromotedTrialInstanceQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    exhaustiveSearchInstanceTrialInstanceId: a[1] ?? null,
    active: a[2] ?? null,
    score: a[3] ?? null,
    topologyComplexity: a[4] ?? null,
    kind: a[5] ?? null,
    json: a[6] ?? null,
    createdDate: a[7] ?? null,
    calibrationMethod: a[8] ?? null,
    calibrationInput: a[9] ?? null,
    calibrationSlope: a[10] ?? null,
    calibrationIntercept: a[11] ?? null,
    calibrationEpsilon: a[12] ?? null,
    trainingPrior: a[13] ?? null,
    calibrationPrior: a[14] ?? null,
    baseRate: a[15] ?? null,
    baseRateSource: a[16] ?? null,
    calibrationSampleSize: a[17] ?? null,
    calibrationStatus: a[18] ?? null,
    calibrationReason: a[19] ?? null,
    calibrationQualityJson: a[20] ?? null,
    calibrationParametersJson: a[21] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceRoc_ExhaustiveSearchInstancePromotedTrialInstanceRocDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    score: a[1] ?? null,
    fpr: a[2] ?? null,
    tpr: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    variableMean: a[2] ?? null,
    variableStandardDeviation: a[3] ?? null,
    variableMaximum: a[4] ?? null,
    variableMinimum: a[5] ?? null,
    prescriptionMean: a[6] ?? null,
    prescriptionStandardDeviation: a[7] ?? null,
    prescriptionMaximum: a[8] ?? null,
    prescriptionMinimum: a[9] ?? null,
    sensitivity: a[10] ?? null,
    positive: a[11] ?? null,
    promotedTrialInstanceVariableId: a[12] ?? null,
    histogramValues: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionHistogramValueDto))(a[13] ?? null),
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionHistogramValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    frequency: a[0] ?? null,
    bin: a[1] ?? null,
    binEnd: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstancePromotionHistory_ExhaustiveSearchInstancePromotionHistoryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    exhaustiveSearchInstanceTrialInstanceId: a[1] ?? null,
    score: a[2] ?? null,
    topologyComplexity: a[3] ?? null,
    createdDate: a[4] ?? null,
    active: a[5] ?? null,
    trialInstances: a[6] ?? null,
    trialInstancesToDate: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceListQuery_ExhaustiveSearchInstanceTrialInstanceDetailDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    kind: a[1] ?? null,
    score: a[2] ?? null,
    hasDiagram: a[3] ?? null,
    evidence: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceListQuery_ExhaustiveSearchInstanceTrialInstanceListQueryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    kind: a[2] ?? null,
    score: a[3] ?? null,
    promoted: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceVariable_ExhaustiveSearchInstanceTrialInstanceVariableDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    mean: a[2] ?? null,
    maximum: a[3] ?? null,
    minimum: a[4] ?? null,
    standardDeviation: a[5] ?? null,
    normalisationTypeId: a[6] ?? null,
    emptyRange: a[7] ?? null,
    variableSequence: a[8] ?? null,
    processingTypeId: a[9] ?? null,
    categoricalSourceName: a[10] ?? null,
    categoricalKind: a[11] ?? null,
    categoricalLevel: a[12] ?? null,
    categoricalReferenceLabel: a[13] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceVariableVariance_ExhaustiveSearchInstanceTrialInstanceVariableVarianceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    correlation: a[1] ?? null,
    correlationAbsRank: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    mean: a[1] ?? null,
    standardDeviation: a[2] ?? null,
    name: a[3] ?? null,
    processingTypeId: a[4] ?? null,
    kurtosis: a[5] ?? null,
    skewness: a[6] ?? null,
    maximum: a[7] ?? null,
    minimum: a[8] ?? null,
    iqr: a[9] ?? null,
    normalisationType: a[10] ?? null,
    distinctValues: a[11] ?? null,
    correlation: a[12] ?? null,
    correlationAbsRank: a[13] ?? null,
    categoricalSourceName: a[14] ?? null,
    categoricalKind: a[15] ?? null,
    categoricalLevel: a[16] ?? null,
    histogramValues: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableHistogramValueDto))(a[17] ?? null),
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableHistogramValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    frequency: a[0] ?? null,
    bin: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableReallocateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    processingTypeId: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    start: a[0] ?? null,
    end: a[1] ?? null,
    frequency: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    population: d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto(a[2] ?? null),
    populationBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[3] ?? null),
    classificationBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[4] ?? null),
    anomalyBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[5] ?? null),
    negativeBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[6] ?? null),
    positiveBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[7] ?? null),
    prescriptionBins: ((v) => many(v, d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto))(a[8] ?? null),
    classification: d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto(a[9] ?? null),
    anomaly: d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto(a[10] ?? null),
    prescription: d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    mean: a[0] ?? null,
    standardDeviation: a[1] ?? null,
    minimum: a[2] ?? null,
    maximum: a[3] ?? null,
    iqr: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormApprovalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    historyRoute: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormChildDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    area: a[0] ?? null,
    label: a[1] ?? null,
    hub: a[2] ?? null,
    order: a[3] ?? null,
    parentKey: a[4] ?? null,
    listRoute: a[5] ?? null,
    parentField: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormConditionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    property: a[0] ?? null,
    op: a[1] ?? null,
    values: a[2] ?? null,
    matchMode: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormEditorDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kind: a[0] ?? null,
    parseType: a[1] ?? null,
    roles: a[2] ?? null,
    reads: a[3] ?? null,
    parameterSets: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormParameterSetDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_FormModel_FormFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    label: a[1] ?? null,
    description: a[2] ?? null,
    type: a[3] ?? null,
    nullable: a[4] ?? null,
    group: a[5] ?? null,
    order: a[6] ?? null,
    widget: a[7] ?? null,
    span: a[8] ?? null,
    readOnly: a[9] ?? null,
    placeholder: a[10] ?? null,
    help: a[11] ?? null,
    editor: a[12] ?? null,
    lookup: d_Jube_Dto_Query_FormModel_FormLookupDto(a[13] ?? null),
    newDefault: a[14] ?? null,
    required: a[15] ?? null,
    minLength: a[16] ?? null,
    maxLength: a[17] ?? null,
    min: a[18] ?? null,
    max: a[19] ?? null,
    exclusiveMin: a[20] ?? null,
    exclusiveMax: a[21] ?? null,
    patterns: a[22] ?? null,
    options: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormOptionDto))(a[23] ?? null),
    visibleWhen: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormConditionDto))(a[24] ?? null),
    requiredWhen: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormConditionDto))(a[25] ?? null),
    enabledWhen: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormConditionDto))(a[26] ?? null),
  }
}

function d_Jube_Dto_Query_FormModel_FormGroupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    path: a[0] ?? null,
    label: a[1] ?? null,
    order: a[2] ?? null,
    collapsed: a[3] ?? null,
    widget: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormKeysDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    parent: a[1] ?? null,
    naturalKey: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormListColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    title: a[1] ?? null,
    order: a[2] ?? null,
    sortable: a[3] ?? null,
    filterable: a[4] ?? null,
    hidden: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormLookupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    source: a[0] ?? null,
    textField: a[1] ?? null,
    valueField: a[2] ?? null,
    parentField: a[3] ?? null,
    upperField: a[4] ?? null,
    allowEmpty: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    area: a[0] ?? null,
    operations: a[1] ?? null,
    endpoint: a[2] ?? null,
    approval: d_Jube_Dto_Query_FormModel_FormApprovalDto(a[3] ?? null),
    roles: d_Jube_Dto_Query_FormModel_FormRolesDto(a[4] ?? null),
    values: d_Jube_Dto_Query_FormModel_FormValuesDto(a[5] ?? null),
    editors: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormEditorDto))(a[6] ?? null),
    keys: d_Jube_Dto_Query_FormModel_FormKeysDto(a[7] ?? null),
    lockFields: a[8] ?? null,
    groups: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormGroupDto))(a[9] ?? null),
    fields: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormFieldDto))(a[10] ?? null),
    listColumns: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormListColumnDto))(a[11] ?? null),
    children: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormChildDto))(a[12] ?? null),
  }
}

function d_Jube_Dto_Query_FormModel_FormOptionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    value: a[1] ?? null,
    label: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormParameterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    default: a[2] ?? null,
    minimum: a[3] ?? null,
    maximum: a[4] ?? null,
    minimumIsExclusive: a[5] ?? null,
    maximumIsExclusive: a[6] ?? null,
    description: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormParameterSetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    value: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    orderSensitive: a[3] ?? null,
    parameters: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormParameterDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_FormModel_FormRolesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    endpoint: a[0] ?? null,
    parentKey: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormValueFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    title: a[1] ?? null,
    type: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_FormModel_FormValuesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    area: a[0] ?? null,
    endpoint: a[1] ?? null,
    parentField: a[2] ?? null,
    parentKind: a[3] ?? null,
    fields: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormValueFieldDto))(a[4] ?? null),
  }
}

function d_Jube_Dto_Query_Graph_GraphAnchorDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    x: a[1] ?? null,
    y: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphCaptureRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    render: d_Jube_Dto_Query_Graph_GraphRenderRequestDto(a[1] ?? null),
    note: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphCaptureResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    svgCaseFileId: a[0] ?? null,
    definitionCaseFileId: a[1] ?? null,
    caseNoteId: a[2] ?? null,
    hash: a[3] ?? null,
    nodes: a[4] ?? null,
    edges: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphComboDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    viaNodeId: a[1] ?? null,
    key: a[2] ?? null,
    count: a[3] ?? null,
    nodeIds: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphCommunityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    size: a[1] ?? null,
    density: a[2] ?? null,
    confirmedCount: a[3] ?? null,
    closedCount: a[4] ?? null,
    confirmedRate: a[5] ?? null,
    tenantRateRatio: a[6] ?? null,
    label: a[7] ?? null,
    computedDate: a[8] ?? null,
    members: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphNodeDto))(a[9] ?? null),
    sentence: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEdgeDefinitionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    name: a[1] ?? null,
    kindId: a[2] ?? null,
    restricted: a[3] ?? null,
    visible: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEdgeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    definitionGuid: a[1] ?? null,
    definitionName: a[2] ?? null,
    kindId: a[3] ?? null,
    fromNodeId: a[4] ?? null,
    toNodeId: a[5] ?? null,
    count: a[6] ?? null,
    amountSum: a[7] ?? null,
    windowCount: a[8] ?? null,
    windowAmount: a[9] ?? null,
    firstSeen: a[10] ?? null,
    lastSeen: a[11] ?? null,
    windowFirst: a[12] ?? null,
    windowLast: a[13] ?? null,
    weight: a[14] ?? null,
    specificity: a[15] ?? null,
    restricted: a[16] ?? null,
    archiveIds: a[17] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEdgeTransactionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    archiveId: a[0] ?? null,
    entityAnalysisModelInstanceEntryGuid: a[1] ?? null,
    occurredDate: a[2] ?? null,
    amount: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEdgeTransactionsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    edge: d_Jube_Dto_Query_Graph_GraphEdgeDto(a[0] ?? null),
    transactions: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphEdgeTransactionDto))(a[1] ?? null),
    archiveIds: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEdgeTransactionsRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    edgeId: a[0] ?? null,
    from: a[1] ?? null,
    to: a[2] ?? null,
    limit: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEntityMatchDecideDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    decisionId: a[1] ?? null,
    note: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphEntityMatchDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    nodeIdA: a[1] ?? null,
    keyA: a[2] ?? null,
    valueA: a[3] ?? null,
    nodeIdB: a[4] ?? null,
    keyB: a[5] ?? null,
    valueB: a[6] ?? null,
    sourceId: a[7] ?? null,
    posterior: a[8] ?? null,
    evidence: a[9] ?? null,
    decisionId: a[10] ?? null,
    decidedUser: a[11] ?? null,
    decidedDate: a[12] ?? null,
    decisionNote: a[13] ?? null,
    resolvedEntityId: a[14] ?? null,
    createdDate: a[15] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphExplodeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    node: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    lanes: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphExplodeLaneDto))(a[1] ?? null),
    from: a[2] ?? null,
    to: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphExplodeLaneDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    node: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    count: a[1] ?? null,
    total: a[2] ?? null,
    first: a[3] ?? null,
    last: a[4] ?? null,
    definitions: a[5] ?? null,
    edgeIds: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphExplodeRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    registryGuid: a[0] ?? null,
    nodeId: a[1] ?? null,
    from: a[2] ?? null,
    to: a[3] ?? null,
    edges: a[4] ?? null,
    limit: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphExplorationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    graphRegistryGuid: a[2] ?? null,
    address: a[3] ?? null,
    pinned: a[4] ?? null,
    createdDate: a[5] ?? null,
    updatedDate: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphFlowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    forward: a[0] ?? null,
    graph: d_Jube_Dto_Query_Graph_GraphNeighbourhoodDto(a[1] ?? null),
    tranches: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphFlowTrancheDto))(a[2] ?? null),
    passThroughNodeIds: a[3] ?? null,
    sentences: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphFlowRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    registryGuid: a[0] ?? null,
    nodeId: a[1] ?? null,
    archiveId: a[2] ?? null,
    at: a[3] ?? null,
    amount: a[4] ?? null,
    forward: a[5] ?? null,
    hopWindowHours: a[6] ?? null,
    tolerance: a[7] ?? null,
    depth: a[8] ?? null,
    fanOut: a[9] ?? null,
    edges: a[10] ?? null,
    from: a[11] ?? null,
    to: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphFlowTrancheDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    depth: a[0] ?? null,
    at: a[1] ?? null,
    amount: a[2] ?? null,
    nodeId: a[3] ?? null,
    nodeIds: a[4] ?? null,
    edgeIds: a[5] ?? null,
    archiveIds: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphHubOverrideRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    nodeId: a[0] ?? null,
    overrideId: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNeighbourhoodDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    seed: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    nodes: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphNodeDto))(a[1] ?? null),
    edges: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphEdgeDto))(a[2] ?? null),
    combos: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphComboDto))(a[3] ?? null),
    definitions: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphEdgeDefinitionDto))(a[4] ?? null),
    hiddenHubs: a[5] ?? null,
    overCap: a[6] ?? null,
    from: a[7] ?? null,
    to: a[8] ?? null,
    hops: a[9] ?? null,
    builtDate: a[10] ?? null,
    noDefinitions: a[11] ?? null,
    relevance: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNeighbourhoodRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    registryGuid: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    hops: a[3] ?? null,
    fanOut: a[4] ?? null,
    from: a[5] ?? null,
    to: a[6] ?? null,
    minimumAmount: a[7] ?? null,
    edges: a[8] ?? null,
    includeHubs: a[9] ?? null,
    expanded: a[10] ?? null,
    relevance: a[11] ?? null,
    cap: a[12] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeCasesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    open: a[0] ?? null,
    closed: a[1] ?? null,
    confirmed: a[2] ?? null,
    latestCaseId: a[3] ?? null,
    latestOpenCaseId: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeDefinitionCountDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    name: a[1] ?? null,
    counterparts: a[2] ?? null,
    count: a[3] ?? null,
    total: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
    hub: a[3] ?? null,
    hubOverrideId: a[4] ?? null,
    degree: a[5] ?? null,
    degreeMoney: a[6] ?? null,
    degreeIdentity: a[7] ?? null,
    hubNeighbourCount: a[8] ?? null,
    componentId: a[9] ?? null,
    componentSize: a[10] ?? null,
    communityId: a[11] ?? null,
    coreNumber: a[12] ?? null,
    pageRank: a[13] ?? null,
    clusteringCoefficient: a[14] ?? null,
    distanceToConfirmed: a[15] ?? null,
    confirmedWithin1: a[16] ?? null,
    confirmedWithin2: a[17] ?? null,
    confirmedWithin3: a[18] ?? null,
    propagationScore: a[19] ?? null,
    hubScore: a[20] ?? null,
    authorityScore: a[21] ?? null,
    distanceToNotConfirmed: a[22] ?? null,
    interest: a[23] ?? null,
    computedDate: a[24] ?? null,
    nearestTag: a[25] ?? null,
    nearestTagDistance: a[26] ?? null,
    tags: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphNodeTagDto))(a[27] ?? null),
    firstSeen: a[28] ?? null,
    lastSeen: a[29] ?? null,
    depth: a[30] ?? null,
    viaNodeId: a[31] ?? null,
    seed: a[32] ?? null,
    cases: d_Jube_Dto_Query_Graph_GraphNodeCasesDto(a[33] ?? null),
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeFactsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    node: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    definitions: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphNodeDefinitionCountDto))(a[1] ?? null),
    community: d_Jube_Dto_Query_Graph_GraphCommunityDto(a[2] ?? null),
    patterns: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphPatternDto))(a[3] ?? null),
    sentences: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    nodeId: a[0] ?? null,
    key: a[1] ?? null,
    value: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphNodeTagDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    tag: a[0] ?? null,
    distance: a[1] ?? null,
    within1: a[2] ?? null,
    within2: a[3] ?? null,
    within3: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPathDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hops: a[0] ?? null,
    weight: a[1] ?? null,
    nodeIds: a[2] ?? null,
    edgeIds: a[3] ?? null,
    sentence: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPathsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    to: d_Jube_Dto_Query_Graph_GraphNodeDto(a[1] ?? null),
    paths: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphPathDto))(a[2] ?? null),
    nodes: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphNodeDto))(a[3] ?? null),
    edges: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphEdgeDto))(a[4] ?? null),
    sentences: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPathsRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    registryGuid: a[0] ?? null,
    fromNodeId: a[1] ?? null,
    toNodeId: a[2] ?? null,
    maxHops: a[3] ?? null,
    k: a[4] ?? null,
    includeHubs: a[5] ?? null,
    edges: a[6] ?? null,
    from: a[7] ?? null,
    to: a[8] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPatternDecideRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    stateId: a[1] ?? null,
    caseId: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPatternDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    kindId: a[1] ?? null,
    kind: a[2] ?? null,
    anchor: d_Jube_Dto_Query_Graph_GraphNodeDto(a[3] ?? null),
    score: a[4] ?? null,
    windowFrom: a[5] ?? null,
    windowTo: a[6] ?? null,
    parameters: a[7] ?? null,
    archiveIds: a[8] ?? null,
    memberCount: a[9] ?? null,
    members: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphPatternMemberDto))(a[10] ?? null),
    stateId: a[11] ?? null,
    caseId: a[12] ?? null,
    firstDetectedDate: a[13] ?? null,
    lastDetectedDate: a[14] ?? null,
    decidedUser: a[15] ?? null,
    decidedDate: a[16] ?? null,
    sentence: a[17] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPatternListRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    stateId: a[0] ?? null,
    kindId: a[1] ?? null,
    limit: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphPatternMemberDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    node: d_Jube_Dto_Query_Graph_GraphNodeDto(a[0] ?? null),
    role: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphRenderRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    query: d_Jube_Dto_Query_Graph_GraphNeighbourhoodRequestDto(a[0] ?? null),
    flow: d_Jube_Dto_Query_Graph_GraphFlowRequestDto(a[1] ?? null),
    layoutId: a[2] ?? null,
    measureId: a[3] ?? null,
    anchors: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphAnchorDto))(a[4] ?? null),
    dark: a[5] ?? null,
    captions: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_Graph_GraphRenderResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    svg: a[0] ?? null,
    positions: ((v) => many(v, d_Jube_Dto_Query_Graph_GraphAnchorDto))(a[1] ?? null),
    graph: d_Jube_Dto_Query_Graph_GraphNeighbourhoodDto(a[2] ?? null),
    layoutId: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_GridModel_GridColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    field: a[0] ?? null,
    title: a[1] ?? null,
    description: a[2] ?? null,
    type: a[3] ?? null,
    format: a[4] ?? null,
    unit: a[5] ?? null,
    order: a[6] ?? null,
    sortable: a[7] ?? null,
    hidden: a[8] ?? null,
    numerator: a[9] ?? null,
    denominator: a[10] ?? null,
  }
}

function d_Jube_Dto_Query_GridModel_GridFilterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    param: a[0] ?? null,
    label: a[1] ?? null,
    kind: a[2] ?? null,
    options: ((v) => many(v, d_Jube_Dto_Query_FormModel_FormOptionDto))(a[3] ?? null),
    lookup: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_GridModel_GridModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    area: a[0] ?? null,
    endpoint: a[1] ?? null,
    title: a[2] ?? null,
    snapshot: a[3] ?? null,
    sortField: a[4] ?? null,
    sortDirection: a[5] ?? null,
    take: a[6] ?? null,
    timeField: a[7] ?? null,
    windowMinutes: a[8] ?? null,
    search: d_Jube_Dto_Query_GridModel_GridFilterDto(a[9] ?? null),
    filters: ((v) => many(v, d_Jube_Dto_Query_GridModel_GridFilterDto))(a[10] ?? null),
    columns: ((v) => many(v, d_Jube_Dto_Query_GridModel_GridColumnDto))(a[11] ?? null),
  }
}

function d_Jube_Dto_Query_Icons_IconDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
  }
}

function d_Jube_Dto_Query_InboundQueue_InboundQueueDeadDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    attempts: a[2] ?? null,
    lastError: a[3] ?? null,
    enqueuedBy: a[4] ?? null,
    createdDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_InboundQueue_InboundQueueDepthDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    pending: a[0] ?? null,
    processing: a[1] ?? null,
    dead: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_ModelGovernance_ModelGovernanceHistoryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    current: d_Jube_Dto_Query_ModelGovernance_ModelGovernanceRecordDto(a[0] ?? null),
    history: ((v) => many(v, d_Jube_Dto_Query_ModelGovernance_ModelGovernanceRecordDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_ModelGovernance_ModelGovernanceInventoryRowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    modelGuid: a[1] ?? null,
    name: a[2] ?? null,
    recorded: a[3] ?? null,
    ownerUser: a[4] ?? null,
    riskTierId: a[5] ?? null,
    nextReviewDate: a[6] ?? null,
    reviewStatusId: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_ModelGovernance_ModelGovernanceRecordDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    modelGuid: a[1] ?? null,
    version: a[2] ?? null,
    ownerUser: a[3] ?? null,
    intendedPurpose: a[4] ?? null,
    riskTierId: a[5] ?? null,
    humanOversight: a[6] ?? null,
    nextReviewDate: a[7] ?? null,
    createdUser: a[8] ?? null,
    createdDate: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_Parser_ParseRuleErrorSpanDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    start: a[0] ?? null,
    length: a[1] ?? null,
    message: a[2] ?? null,
    line: a[3] ?? null,
    column: a[4] ?? null,
    endColumn: a[5] ?? null,
    code: a[6] ?? null,
  }
}

function d_Jube_Dto_Query_Parser_ParseRuleRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    ruleParseType: a[0] ?? null,
    ruleText: a[1] ?? null,
    entityAnalysisModelId: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Parser_ParseRuleResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    message: a[0] ?? null,
    errorSpans: ((v) => many(v, d_Jube_Dto_Query_Parser_ParseRuleErrorSpanDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Query_QueryBuilder_BuilderOperatorArgumentDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    many: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_QueryBuilder_BuilderOperatorDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    type: a[0] ?? null,
    label: a[1] ?? null,
    group: a[2] ?? null,
    arguments: ((v) => many(v, d_Jube_Dto_Query_QueryBuilder_BuilderOperatorArgumentDto))(a[3] ?? null),
    applyTo: a[4] ?? null,
    template: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_RuleVocabulary_RuleWordDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    overloads: ((v) => many(v, d_Jube_Dto_Query_RuleVocabulary_RuleWordOverloadDto))(a[2] ?? null),
    description: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_RuleVocabulary_RuleWordOverloadDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    signature: a[0] ?? null,
    receiverType: a[1] ?? null,
    returnType: a[2] ?? null,
    parameters: ((v) => many(v, d_Jube_Dto_Query_RuleVocabulary_RuleWordParameterDto))(a[3] ?? null),
    description: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_RuleVocabulary_RuleWordPageDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    items: ((v) => many(v, d_Jube_Dto_Query_RuleVocabulary_RuleWordSummaryDto))(a[0] ?? null),
    more: a[1] ?? null,
  }
}

function d_Jube_Dto_Query_RuleVocabulary_RuleWordParameterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    type: a[1] ?? null,
    optional: a[2] ?? null,
    defaultValue: a[3] ?? null,
    description: a[4] ?? null,
  }
}

function d_Jube_Dto_Query_RuleVocabulary_RuleWordSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    receiverTypes: a[2] ?? null,
    overloads: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_Svirfd_SvirfdDriftItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    path: a[0] ?? null,
    stored: a[1] ?? null,
    fresh: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Svirfd_SvirfdLookupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    ip: a[0] ?? null,
    recordJson: a[1] ?? null,
    headline: a[2] ?? null,
    narrative: ((v) => many(v, d_Jube_Dto_Query_Svirfd_SvirfdNarrativeGroupDto))(a[3] ?? null),
    narrativeVersion: a[4] ?? null,
    attribution: a[5] ?? null,
    licenceUrl: a[6] ?? null,
    mode: a[7] ?? null,
    seq: a[8] ?? null,
    rttChartSvg: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_Svirfd_SvirfdNarrativeGroupDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    title: a[1] ?? null,
    paragraphs: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_Svirfd_SvirfdStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    enabled: a[0] ?? null,
    ready: a[1] ?? null,
    mode: a[2] ?? null,
    seq: a[3] ?? null,
    remoteEngineLookup: a[4] ?? null,
    attribution: a[5] ?? null,
    licenceUrl: a[6] ?? null,
    state: a[7] ?? null,
    lastError: a[8] ?? null,
    nextRetryAt: a[9] ?? null,
    stalenessSeconds: a[10] ?? null,
    tenantRemoteLookupsEnabled: a[11] ?? null,
  }
}

function d_Jube_Dto_Query_TreeChildren_CasesWorkflowTreeChildDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    casesWorkflowId: a[0] ?? null,
    key: a[1] ?? null,
    name: a[2] ?? null,
    color: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_TreeChildren_EntityAnalysisModelTreeChildDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    key: a[2] ?? null,
    name: a[3] ?? null,
    color: a[4] ?? null,
    deleted: a[5] ?? null,
  }
}

function d_Jube_Dto_Query_TreeChildren_RoleRegistryTreeChildDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    roleRegistryGuid: a[0] ?? null,
    key: a[1] ?? null,
    name: a[2] ?? null,
    color: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_TreeChildren_VisualisationRegistryTreeChildDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    visualisationRegistryId: a[0] ?? null,
    key: a[1] ?? null,
    name: a[2] ?? null,
    color: a[3] ?? null,
  }
}

function d_Jube_Dto_Query_VisualisationRegistrySummary_VisualisationRegistrySummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    registryId: a[0] ?? null,
    datasourceCount: a[1] ?? null,
    parameterCount: a[2] ?? null,
    displayKindIds: a[3] ?? null,
    executionsInWindow: a[4] ?? null,
    failuresInWindow: a[5] ?? null,
    averageResponseMilliseconds: a[6] ?? null,
    lastExecutionDate: a[7] ?? null,
    lastExecutionUser: a[8] ?? null,
    windowDays: a[9] ?? null,
  }
}

function d_Jube_Dto_Query_WireSchema_WireFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    index: a[0] ?? null,
    name: a[1] ?? null,
    type: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_WireSchema_WireRouteDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    method: a[0] ?? null,
    template: a[1] ?? null,
    shape: a[2] ?? null,
  }
}

function d_Jube_Dto_Query_WireSchema_WireSchemaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    hash: a[0] ?? null,
    types: ((v) => dict(v, d_Jube_Dto_Query_WireSchema_WireTypeDto))(a[1] ?? null),
    routes: ((v) => many(v, d_Jube_Dto_Query_WireSchema_WireRouteDto))(a[2] ?? null),
    mode: a[3] ?? null,
    encryption: a[4] ?? null,
    encryptionPublicKey: a[5] ?? null,
    typesHash: a[6] ?? null,
    generatedDecoders: a[7] ?? null,
  }
}

function d_Jube_Dto_Query_WireSchema_WireTypeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    fields: ((v) => many(v, d_Jube_Dto_Query_WireSchema_WireFieldDto))(a[0] ?? null),
  }
}

function d_Jube_Dto_Repository_ActivationWatcher_ActivationWatcherDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    tenantRegistryId: a[1] ?? null,
    key: a[2] ?? null,
    keyValue: a[3] ?? null,
    longitude: a[4] ?? null,
    latitude: a[5] ?? null,
    activationRuleSummary: a[6] ?? null,
    responseElevationContent: a[7] ?? null,
    responseElevation: a[8] ?? null,
    backColor: a[9] ?? null,
    foreColor: a[10] ?? null,
    createdDate: a[11] ?? null,
    previousLatitude: a[12] ?? null,
    previousLongitude: a[13] ?? null,
    previousCreatedDate: a[14] ?? null,
    responseElevationName: a[15] ?? null,
  }
}

function d_Jube_Dto_Repository_Archive_ArchiveTagDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelInstanceEntryGuid: a[0] ?? null,
    tag: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Case_CaseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    diaryDate: a[1] ?? null,
    caseWorkflowStatusGuid: a[2] ?? null,
    createdDate: a[3] ?? null,
    locked: a[4] ?? null,
    lockedUser: a[5] ?? null,
    lockedDate: a[6] ?? null,
    closedStatusId: a[7] ?? null,
    closedDate: a[8] ?? null,
    closedUser: a[9] ?? null,
    caseKey: a[10] ?? null,
    diary: a[11] ?? null,
    diaryUser: a[12] ?? null,
    rating: a[13] ?? null,
    json: a[14] ?? null,
    caseKeyValue: a[15] ?? null,
    lastClosedStatus: a[16] ?? null,
    payload: a[17] ?? null,
    closedReasonId: a[18] ?? null,
    closedReasonNote: a[19] ?? null,
  }
}

function d_Jube_Dto_Repository_Case_CreateCaseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowGuid: a[0] ?? null,
    caseWorkflowStatusGuid: a[1] ?? null,
    caseKey: a[2] ?? null,
    caseKeyValue: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayComparisonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    first: d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayDto(a[0] ?? null),
    second: d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayDto(a[1] ?? null),
    commonCases: a[2] ?? null,
    sameRecommendation: a[3] ?? null,
    firstAgreed: a[4] ?? null,
    secondAgreed: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    statusId: a[1] ?? null,
    agentDefinitionHash: a[2] ?? null,
    modelName: a[3] ?? null,
    fromDate: a[4] ?? null,
    toDate: a[5] ?? null,
    asOfMinutes: a[6] ?? null,
    casesSelected: a[7] ?? null,
    scored: a[8] ?? null,
    unusable: a[9] ?? null,
    agreed: a[10] ?? null,
    missedRisk: a[11] ?? null,
    overEscalation: a[12] ?? null,
    inconclusive: a[13] ?? null,
    agreementRate: a[14] ?? null,
    error: a[15] ?? null,
    createdDate: a[16] ?? null,
    createdUser: a[17] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    fromDate: a[0] ?? null,
    toDate: a[1] ?? null,
    maximumCases: a[2] ?? null,
    asOfMinutes: a[3] ?? null,
    caseWorkflowGuid: a[4] ?? null,
    prompt: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAgentRun_CaseAgentRunDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    accept: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAgentRun_CaseAgentRunDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    recommendedOutcomeId: a[1] ?? null,
    rationale: a[2] ?? null,
    evidence: a[3] ?? null,
    stateId: a[4] ?? null,
    overrideReason: a[5] ?? null,
    decidedUser: a[6] ?? null,
    decidedDate: a[7] ?? null,
    createdDate: a[8] ?? null,
    modelName: a[9] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    caseId: a[1] ?? null,
    likelihoodId: a[2] ?? null,
    reason: a[3] ?? null,
    computedLikelihoodProbability: a[4] ?? null,
    createdUser: a[5] ?? null,
    createdDate: a[6] ?? null,
    withdrawnDate: a[7] ?? null,
    mine: a[8] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentStateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    current: d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentDto(a[0] ?? null),
    history: ((v) => many(v, d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentWithdrawDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseFile_CaseFileDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    caseKey: a[2] ?? null,
    createdUser: a[3] ?? null,
    caseKeyValue: a[4] ?? null,
    caseId: a[5] ?? null,
    caseNoteId: a[6] ?? null,
    caseResearchId: a[7] ?? null,
    deletedDate: a[8] ?? null,
    deletedUser: a[9] ?? null,
    deleted: a[10] ?? null,
    name: a[11] ?? null,
    extension: a[12] ?? null,
    contentType: a[13] ?? null,
    size: a[14] ?? null,
    hash: a[15] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseFile_CaseFileUploadRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseKey: a[0] ?? null,
    caseKeyValue: a[1] ?? null,
    caseId: a[2] ?? null,
    caseNoteId: a[3] ?? null,
    caseResearchId: a[4] ?? null,
    fileName: a[5] ?? null,
    contentType: a[6] ?? null,
    content: a[7] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseLink_CaseLinkDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    otherCaseId: a[0] ?? null,
    caseKey: a[1] ?? null,
    caseKeyValue: a[2] ?? null,
    closedStatusId: a[3] ?? null,
    reason: a[4] ?? null,
    createdUser: a[5] ?? null,
    createdDate: a[6] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseLink_CaseLinkRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    otherCaseId: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseNote_CaseNoteDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    note: a[1] ?? null,
    actionId: a[2] ?? null,
    priorityId: a[3] ?? null,
    createdDate: a[4] ?? null,
    caseKey: a[5] ?? null,
    createdUser: a[6] ?? null,
    caseKeyValue: a[7] ?? null,
    caseId: a[8] ?? null,
    isFinding: a[9] ?? null,
    sourceId: a[10] ?? null,
    kindId: a[11] ?? null,
    version: a[12] ?? null,
    files: ((v) => many(v, d_Jube_Dto_Repository_CaseFile_CaseFileDto))(a[13] ?? null),
    payload: a[14] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseNote_CaseNoteFindingRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    isFinding: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseQaReview_CaseQaReviewDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    analystVerdictId: a[1] ?? null,
    agentVerdictId: a[2] ?? null,
    comment: a[3] ?? null,
    reviewerUser: a[4] ?? null,
    createdDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseQaReview_CaseQaReviewStateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    eligible: a[0] ?? null,
    hasRecommendation: a[1] ?? null,
    reviews: ((v) => many(v, d_Jube_Dto_Repository_CaseQaReview_CaseQaReviewDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportApprovalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    reportVersion: a[0] ?? null,
    decisionId: a[1] ?? null,
    note: a[2] ?? null,
    createdUser: a[3] ?? null,
    createdDate: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    note: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    caseId: a[1] ?? null,
    profileName: a[2] ?? null,
    profileVersion: a[3] ?? null,
    reportCode: a[4] ?? null,
    stateId: a[5] ?? null,
    currentVersion: a[6] ?? null,
    fiuReference: a[7] ?? null,
    createdUser: a[8] ?? null,
    createdDate: a[9] ?? null,
    submittedUser: a[10] ?? null,
    submittedDate: a[11] ?? null,
    fieldsJson: a[12] ?? null,
    versions: ((v) => many(v, d_Jube_Dto_Repository_CaseReport_CaseReportVersionDto))(a[13] ?? null),
    approvals: ((v) => many(v, d_Jube_Dto_Repository_CaseReport_CaseReportApprovalDto))(a[14] ?? null),
    feedback: ((v) => many(v, d_Jube_Dto_Repository_CaseReport_CaseReportFeedbackDto))(a[15] ?? null),
    issues: ((v) => many(v, d_Jube_Dto_Repository_CaseReport_CaseReportIssueDto))(a[16] ?? null),
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportFeedbackDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    reportVersion: a[0] ?? null,
    kindId: a[1] ?? null,
    message: a[2] ?? null,
    fieldPath: a[3] ?? null,
    receivedDate: a[4] ?? null,
    recordedUser: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportFeedbackRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    kindId: a[1] ?? null,
    message: a[2] ?? null,
    fieldPath: a[3] ?? null,
    receivedDate: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportIssueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    stage: a[0] ?? null,
    code: a[1] ?? null,
    message: a[2] ?? null,
    path: a[3] ?? null,
    line: a[4] ?? null,
    position: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportProfileDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    version: a[1] ?? null,
    description: a[2] ?? null,
    templateXml: a[3] ?? null,
    schemaDocuments: a[4] ?? null,
    codeLists: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportProfileResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    version: a[1] ?? null,
    issues: ((v) => many(v, d_Jube_Dto_Repository_CaseReport_CaseReportIssueDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportProfileSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    version: a[1] ?? null,
    description: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportSaveDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    fieldsJson: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportStartDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    profileName: a[1] ?? null,
    reportCode: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportSubmissionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    fiuReference: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseReport_CaseReportVersionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    version: a[0] ?? null,
    supersedesVersion: a[1] ?? null,
    payloadSha256: a[2] ?? null,
    hasXml: a[3] ?? null,
    createdUser: a[4] ?? null,
    createdDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseResearch_CaseResearchDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    caseKey: a[2] ?? null,
    caseKeyValue: a[3] ?? null,
    topic: a[4] ?? null,
    findings: a[5] ?? null,
    sourceId: a[6] ?? null,
    sources: ((v) => many(v, d_Jube_Dto_Repository_CaseResearch_CaseResearchSourceDto))(a[7] ?? null),
    files: ((v) => many(v, d_Jube_Dto_Repository_CaseFile_CaseFileDto))(a[8] ?? null),
    createdUser: a[9] ?? null,
    createdDate: a[10] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseResearch_CaseResearchSourceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    title: a[1] ?? null,
    url: a[2] ?? null,
    retrievedDate: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSearchFilter_CaseSearchFilterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    caseWorkflowGuid: a[1] ?? null,
    name: a[2] ?? null,
    filterJson: a[3] ?? null,
    selectJson: a[4] ?? null,
    sharedRoleRegistryGuid: a[5] ?? null,
    mine: a[6] ?? null,
    createdUser: a[7] ?? null,
    createdDate: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    version: a[11] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionApprovalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    suppressionVersion: a[1] ?? null,
    decisionId: a[2] ?? null,
    note: a[3] ?? null,
    createdUser: a[4] ?? null,
    createdDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionCountsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    countInCase: a[0] ?? null,
    firstSeenDate: a[1] ?? null,
    lastSeenDate: a[2] ?? null,
    widerCountStateId: a[3] ?? null,
    widerTransactions: a[4] ?? null,
    widerKeys: a[5] ?? null,
    widerCapped: a[6] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    note: a[1] ?? null,
    expiryDate: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    caseKey: a[2] ?? null,
    caseKeyValue: a[3] ?? null,
    overrideKey: a[4] ?? null,
    overrideKeyValue: a[5] ?? null,
    pathId: a[6] ?? null,
    reason: a[7] ?? null,
    expiryDate: a[8] ?? null,
    stateId: a[9] ?? null,
    standingId: a[10] ?? null,
    sourceId: a[11] ?? null,
    version: a[12] ?? null,
    approvalsRequired: a[13] ?? null,
    approvalsRecorded: a[14] ?? null,
    canDecide: a[15] ?? null,
    canRetract: a[16] ?? null,
    canResubmit: a[17] ?? null,
    then: d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionCountsDto(a[18] ?? null),
    now: d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionCountsDto(a[19] ?? null),
    targets: ((v) => many(v, d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetDto))(a[20] ?? null),
    transactions: ((v) => many(v, d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTransactionDto))(a[21] ?? null),
    approvals: ((v) => many(v, d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionApprovalDto))(a[22] ?? null),
    createdUser: a[23] ?? null,
    createdDate: a[24] ?? null,
    decidedUser: a[25] ?? null,
    decidedDate: a[26] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    pathId: a[1] ?? null,
    overrideKey: a[2] ?? null,
    overrideKeyValue: a[3] ?? null,
    reason: a[4] ?? null,
    expiryDate: a[5] ?? null,
    targets: ((v) => many(v, d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetRequestDto))(a[6] ?? null),
    archiveIds: a[7] ?? null,
    merge: a[8] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    entityAnalysisModelName: a[2] ?? null,
    ruleName: a[3] ?? null,
    overrideKindId: a[4] ?? null,
    appliedOverrideId: a[5] ?? null,
    appliedDate: a[6] ?? null,
    standingId: a[7] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelGuid: a[0] ?? null,
    ruleName: a[1] ?? null,
    overrideKindId: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTransactionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    archiveId: a[1] ?? null,
    referenceDate: a[2] ?? null,
    laneKey: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionVersionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    version: a[0] ?? null,
    stateId: a[1] ?? null,
    reason: a[2] ?? null,
    expiryDate: a[3] ?? null,
    updatedUser: a[4] ?? null,
    updatedDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseTag_CaseTagDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    name: a[2] ?? null,
    createdUser: a[3] ?? null,
    createdDate: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseTag_CaseTagMergeRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseId: a[0] ?? null,
    tags: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflow_CaseWorkflowDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    enableVisualisation: a[5] ?? null,
    visualisationRegistryGuid: a[6] ?? null,
    enableGraph: a[7] ?? null,
    graphRegistryGuid: a[8] ?? null,
    valueField: a[9] ?? null,
    typologySeverityId: a[10] ?? null,
    firstViewTargetMinutes: a[11] ?? null,
    ownershipTargetMinutes: a[12] ?? null,
    resolutionTargetMinutes: a[13] ?? null,
    riskKappa: a[14] ?? null,
    riskRho: a[15] ?? null,
    riskPriorStrength: a[16] ?? null,
    riskBaseRatePrior: a[17] ?? null,
    riskDeadlineLaneDays: a[18] ?? null,
    riskKeyHistoryHalfLifeDays: a[19] ?? null,
    riskImpactWeights: a[20] ?? null,
    riskMoneyBands: a[21] ?? null,
    riskQaSampleRate: a[22] ?? null,
    dailyGoal: a[23] ?? null,
    enableNarrative: a[24] ?? null,
    guid: a[25] ?? null,
    id: a[26] ?? null,
    createdDate: a[27] ?? null,
    createdUser: a[28] ?? null,
    updatedUser: a[29] ?? null,
    updatedDate: a[30] ?? null,
    version: a[31] ?? null,
    deletedUser: a[32] ?? null,
    deletedDate: a[33] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowAction_CaseWorkflowActionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    enableHttpEndpoint: a[5] ?? null,
    httpEndpoint: a[6] ?? null,
    httpEndpointTypeId: a[7] ?? null,
    enableNotification: a[8] ?? null,
    notificationTypeId: a[9] ?? null,
    notificationDestination: a[10] ?? null,
    notificationSubject: a[11] ?? null,
    notificationBody: a[12] ?? null,
    sql: a[13] ?? null,
    guid: a[14] ?? null,
    id: a[15] ?? null,
    createdDate: a[16] ?? null,
    createdUser: a[17] ?? null,
    updatedUser: a[18] ?? null,
    updatedDate: a[19] ?? null,
    version: a[20] ?? null,
    deletedUser: a[21] ?? null,
    deletedDate: a[22] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowActionRole_CaseWorkflowActionRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowActionGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowChecklist_CaseWorkflowChecklistDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowStatusId: a[0] ?? null,
    caseWorkflowStatusGuid: a[1] ?? null,
    name: a[2] ?? null,
    priority: a[3] ?? null,
    active: a[4] ?? null,
    guid: a[5] ?? null,
    id: a[6] ?? null,
    createdDate: a[7] ?? null,
    createdUser: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    version: a[11] ?? null,
    deletedUser: a[12] ?? null,
    deletedDate: a[13] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    caseWorkflowChecklistGuid: a[1] ?? null,
    name: a[2] ?? null,
    priority: a[3] ?? null,
    active: a[4] ?? null,
    enableShow: a[5] ?? null,
    showSql: a[6] ?? null,
    enableValidation: a[7] ?? null,
    validateSql: a[8] ?? null,
    validateMessage: a[9] ?? null,
    slaMinutes: a[10] ?? null,
    guid: a[11] ?? null,
    id: a[12] ?? null,
    createdDate: a[13] ?? null,
    createdUser: a[14] ?? null,
    updatedUser: a[15] ?? null,
    updatedDate: a[16] ?? null,
    version: a[17] ?? null,
    deletedUser: a[18] ?? null,
    deletedDate: a[19] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemTestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    conditionId: a[1] ?? null,
    caseId: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemTestResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    passed: a[0] ?? null,
    code: a[1] ?? null,
    message: a[2] ?? null,
    milliseconds: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowDisplay_CaseWorkflowDisplayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    html: a[5] ?? null,
    enableHttpEndpoint: a[6] ?? null,
    httpEndpoint: a[7] ?? null,
    httpEndpointTypeId: a[8] ?? null,
    enableNotification: a[9] ?? null,
    notificationType: a[10] ?? null,
    notificationDestination: a[11] ?? null,
    notificationSubject: a[12] ?? null,
    notificationBody: a[13] ?? null,
    sql: a[14] ?? null,
    guid: a[15] ?? null,
    id: a[16] ?? null,
    createdDate: a[17] ?? null,
    createdUser: a[18] ?? null,
    updatedUser: a[19] ?? null,
    updatedDate: a[20] ?? null,
    version: a[21] ?? null,
    deletedUser: a[22] ?? null,
    deletedDate: a[23] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowDisplayRole_CaseWorkflowDisplayRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowDisplayGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFilter_CaseWorkflowFilterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    selectJson: a[5] ?? null,
    filterJson: a[6] ?? null,
    filterTokens: a[7] ?? null,
    enableHttpEndpoint: a[8] ?? null,
    httpEndpoint: a[9] ?? null,
    httpEndpointTypeId: a[10] ?? null,
    enableNotification: a[11] ?? null,
    notificationType: a[12] ?? null,
    notificationDestination: a[13] ?? null,
    notificationSubject: a[14] ?? null,
    notificationBody: a[15] ?? null,
    guid: a[16] ?? null,
    id: a[17] ?? null,
    createdDate: a[18] ?? null,
    createdUser: a[19] ?? null,
    updatedUser: a[20] ?? null,
    updatedDate: a[21] ?? null,
    version: a[22] ?? null,
    deletedUser: a[23] ?? null,
    deletedDate: a[24] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFilterRole_CaseWorkflowFilterRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowFilterGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowForm_CaseWorkflowFormDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    html: a[5] ?? null,
    enableHttpEndpoint: a[6] ?? null,
    httpEndpoint: a[7] ?? null,
    httpEndpointTypeId: a[8] ?? null,
    enableNotification: a[9] ?? null,
    notificationTypeId: a[10] ?? null,
    notificationDestination: a[11] ?? null,
    notificationSubject: a[12] ?? null,
    notificationBody: a[13] ?? null,
    sql: a[14] ?? null,
    guid: a[15] ?? null,
    id: a[16] ?? null,
    createdDate: a[17] ?? null,
    createdUser: a[18] ?? null,
    updatedUser: a[19] ?? null,
    updatedDate: a[20] ?? null,
    version: a[21] ?? null,
    deletedUser: a[22] ?? null,
    deletedDate: a[23] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormEntry_CaseWorkflowFormEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    payload: a[1] ?? null,
    createdDate: a[2] ?? null,
    createdUser: a[3] ?? null,
    caseKey: a[4] ?? null,
    caseId: a[5] ?? null,
    caseWorkflowFormId: a[6] ?? null,
    caseKeyValue: a[7] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormEntryValue_CaseWorkflowFormEntryValueDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    value: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormRole_CaseWorkflowFormRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowFormGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    caseWorkflowFormGuid: a[1] ?? null,
    name: a[2] ?? null,
    active: a[3] ?? null,
    fieldName: a[4] ?? null,
    kindId: a[5] ?? null,
    argument1: a[6] ?? null,
    argument2: a[7] ?? null,
    sql: a[8] ?? null,
    message: a[9] ?? null,
    guid: a[10] ?? null,
    id: a[11] ?? null,
    createdDate: a[12] ?? null,
    createdUser: a[13] ?? null,
    updatedUser: a[14] ?? null,
    updatedDate: a[15] ?? null,
    version: a[16] ?? null,
    deletedUser: a[17] ?? null,
    deletedDate: a[18] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorTestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseId: a[1] ?? null,
    values: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorTestResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    passed: a[0] ?? null,
    code: a[1] ?? null,
    message: a[2] ?? null,
    milliseconds: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowMacro_CaseWorkflowMacroDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    javascript: a[5] ?? null,
    imageLocation: a[6] ?? null,
    enableHttpEndpoint: a[7] ?? null,
    httpEndpoint: a[8] ?? null,
    httpEndpointTypeId: a[9] ?? null,
    enableNotification: a[10] ?? null,
    notificationTypeId: a[11] ?? null,
    notificationDestination: a[12] ?? null,
    notificationSubject: a[13] ?? null,
    notificationBody: a[14] ?? null,
    sql: a[15] ?? null,
    guid: a[16] ?? null,
    id: a[17] ?? null,
    createdDate: a[18] ?? null,
    createdUser: a[19] ?? null,
    updatedUser: a[20] ?? null,
    updatedDate: a[21] ?? null,
    version: a[22] ?? null,
    deletedUser: a[23] ?? null,
    deletedDate: a[24] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowMacroRole_CaseWorkflowMacroRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowMacroGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowPriority_CaseWorkflowPriorityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowReleaseHook_CaseWorkflowReleaseHookDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    sourceField: a[4] ?? null,
    sourceValue: a[5] ?? null,
    releaseUrl: a[6] ?? null,
    refuseUrl: a[7] ?? null,
    httpEndpointTypeId: a[8] ?? null,
    body: a[9] ?? null,
    headerName: a[10] ?? null,
    credentialVariable: a[11] ?? null,
    guid: a[12] ?? null,
    id: a[13] ?? null,
    createdDate: a[14] ?? null,
    createdUser: a[15] ?? null,
    updatedUser: a[16] ?? null,
    updatedDate: a[17] ?? null,
    version: a[18] ?? null,
    deletedUser: a[19] ?? null,
    deletedDate: a[20] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowRole_CaseWorkflowRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowStatus_CaseWorkflowStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    priority: a[4] ?? null,
    outcomeId: a[5] ?? null,
    maxDwellMinutes: a[6] ?? null,
    escalationTriggerId: a[7] ?? null,
    escalationThreshold: a[8] ?? null,
    escalationTargetCaseWorkflowStatusGuid: a[9] ?? null,
    escalationReleaseLock: a[10] ?? null,
    foreColor: a[11] ?? null,
    backColor: a[12] ?? null,
    enableHttpEndpoint: a[13] ?? null,
    httpEndpoint: a[14] ?? null,
    httpEndpointTypeId: a[15] ?? null,
    enableNotification: a[16] ?? null,
    notificationTypeId: a[17] ?? null,
    notificationDestination: a[18] ?? null,
    notificationSubject: a[19] ?? null,
    notificationBody: a[20] ?? null,
    sql: a[21] ?? null,
    guid: a[22] ?? null,
    id: a[23] ?? null,
    createdDate: a[24] ?? null,
    createdUser: a[25] ?? null,
    updatedUser: a[26] ?? null,
    updatedDate: a[27] ?? null,
    version: a[28] ?? null,
    deletedUser: a[29] ?? null,
    deletedDate: a[30] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowStatusRole_CaseWorkflowStatusRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowStatusGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowXPath_CaseWorkflowXPathDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    caseWorkflowId: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    sourceGuid: a[4] ?? null,
    displayName: a[5] ?? null,
    sourceKindId: a[6] ?? null,
    roleId: a[7] ?? null,
    segment: a[8] ?? null,
    boldLineMatched: a[9] ?? null,
    boldLineFormatForeColor: a[10] ?? null,
    boldLineFormatBackColor: a[11] ?? null,
    conditionalRegularExpressionFormatting: a[12] ?? null,
    regularExpression: a[13] ?? null,
    conditionalFormatForeColor: a[14] ?? null,
    foreRowColorScope: a[15] ?? null,
    conditionalFormatBackColor: a[16] ?? null,
    backRowColorScope: a[17] ?? null,
    guid: a[18] ?? null,
    id: a[19] ?? null,
    createdDate: a[20] ?? null,
    createdUser: a[21] ?? null,
    updatedUser: a[22] ?? null,
    updatedDate: a[23] ?? null,
    version: a[24] ?? null,
    deletedUser: a[25] ?? null,
    deletedDate: a[26] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowXPath_CaseWorkflowXPathFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    name: a[1] ?? null,
    label: a[2] ?? null,
    kindId: a[3] ?? null,
    dataTypeId: a[4] ?? null,
    archiveKey: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_CaseWorkflowXPathRole_CaseWorkflowXPathRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    caseWorkflowXPathGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelGraphRecall_EntityAnalysisModelGraphRecallDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    requestXPathName: a[5] ?? null,
    nodeKey: a[6] ?? null,
    counterpartName: a[7] ?? null,
    graphRegistryGuid: a[8] ?? null,
    responsePayload: a[9] ?? null,
    reportTable: a[10] ?? null,
    id: a[11] ?? null,
    createdDate: a[12] ?? null,
    createdUser: a[13] ?? null,
    updatedUser: a[14] ?? null,
    updatedDate: a[15] ?? null,
    version: a[16] ?? null,
    deletedUser: a[17] ?? null,
    deletedDate: a[18] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelGraphRecall_EntityAnalysisModelGraphRecallFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    label: a[1] ?? null,
    reportTable: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelRole_EntityAnalysisModelRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    entityAnalysisModelGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelSvirfd_EntityAnalysisModelSvirfdDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entityAnalysisModelId: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    requestXPathName: a[5] ?? null,
    responsePayload: a[6] ?? null,
    reportTable: a[7] ?? null,
    id: a[8] ?? null,
    createdDate: a[9] ?? null,
    createdUser: a[10] ?? null,
    updatedUser: a[11] ?? null,
    updatedDate: a[12] ?? null,
    version: a[13] ?? null,
    deletedUser: a[14] ?? null,
    deletedDate: a[15] ?? null,
    narrative: a[16] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelSvirfd_EntityAnalysisModelSvirfdFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    label: a[1] ?? null,
    ip: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityAnalysisModelSynchronisationSchedule_EntityAnalysisModelSynchronisationScheduleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    scheduleDate: a[1] ?? null,
    entityAnalysisModelId: a[2] ?? null,
    tenantRegistryId: a[3] ?? null,
    createdUser: a[4] ?? null,
    createdDate: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    note: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    keyName: a[0] ?? null,
    keyValue: a[1] ?? null,
    version: a[2] ?? null,
    tierId: a[3] ?? null,
    score: a[4] ?? null,
    sourceId: a[5] ?? null,
    reason: a[6] ?? null,
    reviewByDate: a[7] ?? null,
    createdUser: a[8] ?? null,
    createdDate: a[9] ?? null,
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingHistoryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    current: d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDto(a[0] ?? null),
    history: ((v) => many(v, d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingImportDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    ratings: ((v) => many(v, d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDto))(a[0] ?? null),
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingImportResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    written: a[0] ?? null,
    unchanged: a[1] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    keyName: a[1] ?? null,
    keyValue: a[2] ?? null,
    tierId: a[3] ?? null,
    score: a[4] ?? null,
    reviewByDate: a[5] ?? null,
    reason: a[6] ?? null,
    basedOnVersion: a[7] ?? null,
    stateId: a[8] ?? null,
    resultingVersion: a[9] ?? null,
    requestedUser: a[10] ?? null,
    requestedDate: a[11] ?? null,
    decidedUser: a[12] ?? null,
    decidedDate: a[13] ?? null,
    decisionNote: a[14] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_ErasureDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    note: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_ErasurePreviewDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    archiveRows: a[0] ?? null,
    refusalReasonId: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_ErasureRequestCreateDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    keyName: a[0] ?? null,
    keyValue: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_ErasureRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    keyName: a[1] ?? null,
    keyValue: a[2] ?? null,
    reason: a[3] ?? null,
    stateId: a[4] ?? null,
    refusalReasonId: a[5] ?? null,
    archiveRowsFound: a[6] ?? null,
    archiveRowsRemoved: a[7] ?? null,
    graphNodesRemoved: a[8] ?? null,
    graphEdgesRemoved: a[9] ?? null,
    casePayloadsNulled: a[10] ?? null,
    requestedUser: a[11] ?? null,
    requestedDate: a[12] ?? null,
    decidedUser: a[13] ?? null,
    decidedDate: a[14] ?? null,
    decisionNote: a[15] ?? null,
    executedDate: a[16] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_ErasureSubjectDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    keyName: a[0] ?? null,
    keyValue: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Erasure_SubjectExportDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    keyName: a[0] ?? null,
    keyValue: a[1] ?? null,
    generatedDate: a[2] ?? null,
    generatedUser: a[3] ?? null,
    truncated: a[4] ?? null,
    transactions: ((v) => many(v, d_Jube_Dto_Repository_Erasure_SubjectExportTransactionDto))(a[5] ?? null),
  }
}

function d_Jube_Dto_Repository_Erasure_SubjectExportTransactionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    entryGuid: a[0] ?? null,
    modelId: a[1] ?? null,
    referenceDate: a[2] ?? null,
    createdDate: a[3] ?? null,
    json: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_ExhaustiveSearchInstancePromotedTrialInstance_ExhaustiveSearchInstancePromotedTrialInstanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    active: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_GraphRegistry_GraphRegistryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    active: a[1] ?? null,
    locked: a[2] ?? null,
    contextKindId: a[3] ?? null,
    defaultLayoutId: a[4] ?? null,
    defaultHops: a[5] ?? null,
    defaultWindowDays: a[6] ?? null,
    renderCap: a[7] ?? null,
    hubPolicyId: a[8] ?? null,
    guid: a[9] ?? null,
    id: a[10] ?? null,
    createdDate: a[11] ?? null,
    createdUser: a[12] ?? null,
    updatedUser: a[13] ?? null,
    updatedDate: a[14] ?? null,
    version: a[15] ?? null,
    deletedUser: a[16] ?? null,
    deletedDate: a[17] ?? null,
  }
}

function d_Jube_Dto_Repository_GraphRegistryEdge_GraphRegistryEdgeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    graphRegistryId: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    kindId: a[5] ?? null,
    fromKey: a[6] ?? null,
    toKey: a[7] ?? null,
    fromNodeKey: a[8] ?? null,
    toNodeKey: a[9] ?? null,
    amountKey: a[10] ?? null,
    minimumAmount: a[11] ?? null,
    halfLifeDays: a[12] ?? null,
    kindPrior: a[13] ?? null,
    hubThreshold: a[14] ?? null,
    hard: a[15] ?? null,
    restricted: a[16] ?? null,
    id: a[17] ?? null,
    createdDate: a[18] ?? null,
    createdUser: a[19] ?? null,
    updatedUser: a[20] ?? null,
    updatedDate: a[21] ?? null,
    version: a[22] ?? null,
    deletedUser: a[23] ?? null,
    deletedDate: a[24] ?? null,
  }
}

function d_Jube_Dto_Repository_GraphRegistryEdge_GraphRegistryEdgeKeyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    label: a[1] ?? null,
    models: a[2] ?? null,
    unflaggedModels: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_GraphRegistryRole_GraphRegistryRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    graphRegistryGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_PermissionSpecification_PermissionSpecificationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_ImportExportOptionsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    password: a[0] ?? null,
    exhaustive: a[1] ?? null,
    lists: a[2] ?? null,
    dictionaries: a[3] ?? null,
    visualisations: a[4] ?? null,
    roles: a[5] ?? null,
    advancedAnalytics: a[6] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffChangeDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    property: a[0] ?? null,
    before: a[1] ?? null,
    after: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    diffKindId: a[1] ?? null,
    left: d_Jube_Dto_Repository_Preservation_PreservationDiffSideDto(a[2] ?? null),
    right: d_Jube_Dto_Repository_Preservation_PreservationDiffSideDto(a[3] ?? null),
    identical: a[4] ?? null,
    added: a[5] ?? null,
    removed: a[6] ?? null,
    changed: a[7] ?? null,
    unchanged: a[8] ?? null,
    referencesIgnored: a[9] ?? null,
    truncated: a[10] ?? null,
    sections: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationDiffSectionDto))(a[11] ?? null),
    entries: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationDiffEntryDto))(a[12] ?? null),
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    section: a[0] ?? null,
    path: a[1] ?? null,
    kindId: a[2] ?? null,
    changes: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationDiffChangeDto))(a[3] ?? null),
    nestedCount: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    leftSnapshotId: a[0] ?? null,
    rightSnapshotId: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffSectionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    added: a[1] ?? null,
    removed: a[2] ?? null,
    changed: a[3] ?? null,
    unchanged: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationDiffSideDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    snapshotId: a[0] ?? null,
    label: a[1] ?? null,
    createdDate: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationImportPreviewDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    diff: d_Jube_Dto_Repository_Preservation_PreservationDiffDto(a[0] ?? null),
    packageVersion: a[1] ?? null,
    packageGuid: a[2] ?? null,
    warnings: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationImportWarningDto))(a[3] ?? null),
    blockers: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationImportWarningDto))(a[4] ?? null),
    orphanedUsers: d_Jube_Dto_Repository_Preservation_PreservationOrphanedUsersDto(a[5] ?? null),
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationImportRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    password: a[0] ?? null,
    fileName: a[1] ?? null,
    package: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationImportWarningDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    message: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationOrphanedUsersDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    count: a[0] ?? null,
    names: a[1] ?? null,
    importerStaysActive: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationSummaryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    sections: ((v) => many(v, d_Jube_Dto_Repository_Preservation_PreservationSummarySectionDto))(a[0] ?? null),
    totalBytes: a[1] ?? null,
    minimumPasswordLength: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_Preservation_PreservationSummarySectionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    count: a[1] ?? null,
    rows: a[2] ?? null,
    bytes: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_PreservationSnapshot_PreservationSnapshotDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    snapshotSourceId: a[2] ?? null,
    name: a[3] ?? null,
    exportVersion: a[4] ?? null,
    exportGuid: a[5] ?? null,
    entityAnalysisModelCount: a[6] ?? null,
    bytes: a[7] ?? null,
    inError: a[8] ?? null,
    createdUser: a[9] ?? null,
    createdDate: a[10] ?? null,
    completedDate: a[11] ?? null,
  }
}

function d_Jube_Dto_Repository_PreservationSnapshot_PreservationSnapshotRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    snapshotSourceId: a[1] ?? null,
    lists: a[2] ?? null,
    dictionaries: a[3] ?? null,
    visualisations: a[4] ?? null,
    roles: a[5] ?? null,
    advancedAnalytics: a[6] ?? null,
  }
}

function d_Jube_Dto_Repository_RoleRegistry_RoleRegistryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    active: a[1] ?? null,
    locked: a[2] ?? null,
    description: a[3] ?? null,
    guid: a[4] ?? null,
    id: a[5] ?? null,
    createdDate: a[6] ?? null,
    createdUser: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    version: a[10] ?? null,
    deletedUser: a[11] ?? null,
    deletedDate: a[12] ?? null,
  }
}

function d_Jube_Dto_Repository_RoleRegistryPermission_RoleRegistryPermissionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    roleRegistryId: a[0] ?? null,
    permissionSpecificationId: a[1] ?? null,
    guid: a[2] ?? null,
    active: a[3] ?? null,
    locked: a[4] ?? null,
    id: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    updatedUser: a[8] ?? null,
    updatedDate: a[9] ?? null,
    version: a[10] ?? null,
    deletedUser: a[11] ?? null,
    deletedDate: a[12] ?? null,
  }
}

function d_Jube_Dto_Repository_SanctionEntrySource_SanctionEntrySourceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionDecisionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    note: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    screenedName: a[1] ?? null,
    sanctionEntrySourceId: a[2] ?? null,
    sanctionEntryReference: a[3] ?? null,
    reason: a[4] ?? null,
    expiryDate: a[5] ?? null,
    stateId: a[6] ?? null,
    requestedUser: a[7] ?? null,
    requestedDate: a[8] ?? null,
    decidedUser: a[9] ?? null,
    decidedDate: a[10] ?? null,
    decisionNote: a[11] ?? null,
  }
}

function d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    screenedName: a[0] ?? null,
    sanctionEntrySourceId: a[1] ?? null,
    sanctionEntryReference: a[2] ?? null,
    reason: a[3] ?? null,
    expiryDate: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_SessionCaseJournal_SessionCaseJournalDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    json: a[1] ?? null,
    createdUser: a[2] ?? null,
    createdDate: a[3] ?? null,
    caseWorkflowGuid: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_SessionCaseSearchCompiledSql_SessionCaseSearchCompiledSqlDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    filterJson: a[2] ?? null,
    filterTokens: a[3] ?? null,
    selectJson: a[4] ?? null,
    prepared: a[5] ?? null,
    error: a[6] ?? null,
    caseWorkflowGuid: a[7] ?? null,
    caseWorkflowFilterGuid: a[8] ?? null,
    createdUser: a[9] ?? null,
    createdDate: a[10] ?? null,
    rebuild: a[11] ?? null,
    rebuildDate: a[12] ?? null,
    notFound: a[13] ?? null,
  }
}

function d_Jube_Dto_Repository_TenantRegistry_TenantRegistryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    description: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    id: a[4] ?? null,
    createdDate: a[5] ?? null,
    createdUser: a[6] ?? null,
    updatedUser: a[7] ?? null,
    updatedDate: a[8] ?? null,
    version: a[9] ?? null,
    deletedUser: a[10] ?? null,
    deletedDate: a[11] ?? null,
    httpAdaptationUrl: a[12] ?? null,
    httpAdaptationTimeoutMilliseconds: a[13] ?? null,
    httpAdaptationValidateSsl: a[14] ?? null,
    svirfdRemoteLookupsEnabled: a[15] ?? null,
  }
}

function d_Jube_Dto_Repository_UserInTenant_UserInTenantDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    user: a[0] ?? null,
    tenantRegistryId: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_UserRegistry_UserRegistryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    roleRegistryGuid: a[1] ?? null,
    email: a[2] ?? null,
    wirePasswordHash: a[3] ?? null,
    passwordLocked: a[4] ?? null,
    id: a[5] ?? null,
    guid: a[6] ?? null,
    createdUser: a[7] ?? null,
    createdDate: a[8] ?? null,
    version: a[9] ?? null,
    inheritedId: a[10] ?? null,
    active: a[11] ?? null,
  }
}

function d_Jube_Dto_Repository_UserRegistry_UserRegistryPasswordResetDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    wirePasswordHash: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_UserRegistry_UserRegistryPasswordResponseDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    password: a[0] ?? null,
    passwordExpiryDate: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_UserRegistryApiKey_UserRegistryApiKeyDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    userRegistryId: a[1] ?? null,
    name: a[2] ?? null,
    description: a[3] ?? null,
    createdDate: a[4] ?? null,
    apiKeyDisplay: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistry_VisualisationRegistryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    active: a[1] ?? null,
    locked: a[2] ?? null,
    contextKindId: a[3] ?? null,
    columns: a[4] ?? null,
    columnWidth: a[5] ?? null,
    rowHeight: a[6] ?? null,
    guid: a[7] ?? null,
    id: a[8] ?? null,
    createdDate: a[9] ?? null,
    createdUser: a[10] ?? null,
    updatedUser: a[11] ?? null,
    updatedDate: a[12] ?? null,
    version: a[13] ?? null,
    deletedUser: a[14] ?? null,
    deletedDate: a[15] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardDisplayDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    markupKindId: a[1] ?? null,
    markup: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    columnSpan: a[3] ?? null,
    rowSpan: a[4] ?? null,
    priority: a[5] ?? null,
    stateId: a[6] ?? null,
    message: a[7] ?? null,
    rowCount: a[8] ?? null,
    truncated: a[9] ?? null,
    executionMilliseconds: a[10] ?? null,
    display: d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardDisplayDto(a[11] ?? null),
    grid: d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridDto(a[12] ?? null),
    style: d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardStyleDto(a[13] ?? null),
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    ordinal: a[1] ?? null,
    dataTypeId: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    columns: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridColumnDto))(a[0] ?? null),
    rows: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardParameterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    value: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    parameters: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardParameterDto))(a[0] ?? null),
    contextId: a[1] ?? null,
    themeId: a[2] ?? null,
    styleModeId: a[3] ?? null,
    knownStylesheetVersion: a[4] ?? null,
    width: a[5] ?? null,
    height: a[6] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardStyleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    modeId: a[0] ?? null,
    version: a[1] ?? null,
    css: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceChartKindDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    roles: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceChartRoleDto))(a[3] ?? null),
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceChartRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    roleId: a[0] ?? null,
    name: a[1] ?? null,
    description: a[2] ?? null,
    many: a[3] ?? null,
    optional: a[4] ?? null,
    acceptsId: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceKindFitDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    kindId: a[0] ?? null,
    fitsByPosition: a[1] ?? null,
    reason: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceMappingEntryDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    roleId: a[0] ?? null,
    columns: a[1] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourcePreviewRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    visualisationRegistryId: a[0] ?? null,
    command: a[1] ?? null,
    displayKindId: a[2] ?? null,
    mapping: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceMappingEntryDto))(a[3] ?? null),
    templateText: a[4] ?? null,
    includeGrid: a[5] ?? null,
    card: d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardRequestDto(a[6] ?? null),
    priority: a[7] ?? null,
    columnSpan: a[8] ?? null,
    rowSpan: a[9] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceResolvedRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    roleId: a[0] ?? null,
    columns: a[1] ?? null,
    explicit: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    visualisationRegistryId: a[0] ?? null,
    guid: a[1] ?? null,
    name: a[2] ?? null,
    locked: a[3] ?? null,
    includeGrid: a[4] ?? null,
    active: a[5] ?? null,
    description: a[6] ?? null,
    command: a[7] ?? null,
    priority: a[8] ?? null,
    columns: a[9] ?? null,
    rowSpan: a[10] ?? null,
    columnSpan: a[11] ?? null,
    displayKindId: a[12] ?? null,
    templateText: a[13] ?? null,
    mapping: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceMappingEntryDto))(a[14] ?? null),
    id: a[15] ?? null,
    createdDate: a[16] ?? null,
    updatedUser: a[17] ?? null,
    updatedDate: a[18] ?? null,
    createdUser: a[19] ?? null,
    version: a[20] ?? null,
    deletedUser: a[21] ?? null,
    deletedDate: a[22] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionColumnDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    ordinal: a[1] ?? null,
    dataTypeName: a[2] ?? null,
    dataTypeId: a[3] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    columns: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionColumnDto))(a[1] ?? null),
    error: a[2] ?? null,
    errorLine: a[3] ?? null,
    errorColumn: a[4] ?? null,
    kinds: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceKindFitDto))(a[5] ?? null),
    resolvedMapping: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceResolvedRoleDto))(a[6] ?? null),
    mappingError: a[7] ?? null,
    templateError: a[8] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionRequestDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    visualisationRegistryId: a[0] ?? null,
    command: a[1] ?? null,
    displayKindId: a[2] ?? null,
    mapping: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceMappingEntryDto))(a[3] ?? null),
    templateText: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceSqlCheckDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    findings: ((v) => many(v, d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceSqlFindingDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceSqlFindingDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    message: a[1] ?? null,
    line: a[2] ?? null,
    column: a[3] ?? null,
    suggestion: a[4] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasourceRole_VisualisationRegistryDatasourceRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    visualisationRegistryDatasourceGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryDatasourceSeries_VisualisationRegistryDatasourceSeriesDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    name: a[1] ?? null,
    dataTypeId: a[2] ?? null,
    ordinal: a[3] ?? null,
    roleId: a[4] ?? null,
    roleOrder: a[5] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryParameter_VisualisationRegistryParameterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    visualisationRegistryId: a[0] ?? null,
    name: a[1] ?? null,
    active: a[2] ?? null,
    locked: a[3] ?? null,
    dataTypeId: a[4] ?? null,
    defaultValue: a[5] ?? null,
    required: a[6] ?? null,
    guid: a[7] ?? null,
    id: a[8] ?? null,
    createdUser: a[9] ?? null,
    createdDate: a[10] ?? null,
    updatedUser: a[11] ?? null,
    updatedDate: a[12] ?? null,
    version: a[13] ?? null,
    deletedUser: a[14] ?? null,
    deletedDate: a[15] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryParameterRole_VisualisationRegistryParameterRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    visualisationRegistryParameterGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_Repository_VisualisationRegistryRole_VisualisationRegistryRoleDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    visualisationRegistryGuid: a[1] ?? null,
    roleRegistryGuid: a[2] ?? null,
  }
}

function d_Jube_Dto_RespCallCounter_RespCallCounterDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    call: a[1] ?? null,
    count: a[2] ?? null,
    totalMicroseconds: a[3] ?? null,
    minMicroseconds: a[4] ?? null,
    maxMicroseconds: a[5] ?? null,
    createdDate: a[6] ?? null,
    instance: a[7] ?? null,
  }
}

function d_Jube_Dto_RespConnectionEvent_RespConnectionEventDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    eventTypeId: a[2] ?? null,
    eventTypeName: a[3] ?? null,
    endPoint: a[4] ?? null,
    connectionTypeId: a[5] ?? null,
    connectionTypeName: a[6] ?? null,
    failureTypeId: a[7] ?? null,
    failureTypeName: a[8] ?? null,
    origin: a[9] ?? null,
    message: a[10] ?? null,
    exception: a[11] ?? null,
    createdDate: a[12] ?? null,
    instance: a[13] ?? null,
    retryCount: a[14] ?? null,
    backoffMilliseconds: a[15] ?? null,
    transactionsImpacted: a[16] ?? null,
  }
}

function d_Jube_Dto_RespConnectionMultiplexerMetric_RespConnectionMultiplexerMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    instance: a[2] ?? null,
    clientName: a[3] ?? null,
    timeoutMilliseconds: a[4] ?? null,
    isConnected: a[5] ?? null,
    isConnecting: a[6] ?? null,
    endPointCount: a[7] ?? null,
    connectedEndPointCount: a[8] ?? null,
    totalOperationCount: a[9] ?? null,
    totalOutstanding: a[10] ?? null,
    interactivePendingUnsentItems: a[11] ?? null,
    interactiveSentItemsAwaitingResponse: a[12] ?? null,
    interactiveResponsesAwaitingAsyncCompletion: a[13] ?? null,
    interactiveTotalOutstanding: a[14] ?? null,
    interactiveCompletedAsynchronously: a[15] ?? null,
    interactiveCompletedSynchronously: a[16] ?? null,
    interactiveFailedAsynchronously: a[17] ?? null,
    interactiveNonPreferredEndpointCount: a[18] ?? null,
    interactiveSocketCount: a[19] ?? null,
    interactiveWriterCount: a[20] ?? null,
    subscriptionPendingUnsentItems: a[21] ?? null,
    subscriptionSentItemsAwaitingResponse: a[22] ?? null,
    subscriptionResponsesAwaitingAsyncCompletion: a[23] ?? null,
    subscriptionTotalOutstanding: a[24] ?? null,
    subscriptionCompletedAsynchronously: a[25] ?? null,
    subscriptionCompletedSynchronously: a[26] ?? null,
    subscriptionFailedAsynchronously: a[27] ?? null,
    subscriptionSocketCount: a[28] ?? null,
    subscriptionCount: a[29] ?? null,
    connectionFailedCount: a[30] ?? null,
    connectionRestoredCount: a[31] ?? null,
    errorMessageCount: a[32] ?? null,
    internalErrorCount: a[33] ?? null,
    configurationChangedCount: a[34] ?? null,
    configurationChangedBroadcastCount: a[35] ?? null,
  }
}

function d_Jube_Dto_RespKeyActivity_RespKeyActivityDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    type: a[1] ?? null,
    idleTimeSeconds: a[2] ?? null,
    frequency: a[3] ?? null,
    timeToLiveSeconds: a[4] ?? null,
  }
}

function d_Jube_Dto_RespMetric_RespMetricDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    instance: a[2] ?? null,
    connectedClients: a[3] ?? null,
    blockedClients: a[4] ?? null,
    usedMemoryBytes: a[5] ?? null,
    usedMemoryRssBytes: a[6] ?? null,
    maxMemoryBytes: a[7] ?? null,
    instantaneousOpsPerSecond: a[8] ?? null,
    totalCommandsProcessed: a[9] ?? null,
    totalConnectionsReceived: a[10] ?? null,
    keyspaceHits: a[11] ?? null,
    keyspaceMisses: a[12] ?? null,
    hitRatePercent: a[13] ?? null,
    evictedKeys: a[14] ?? null,
    expiredKeys: a[15] ?? null,
    connectedReplicas: a[16] ?? null,
    masterReplicationOffset: a[17] ?? null,
    uptimeSeconds: a[18] ?? null,
    totalKeys: a[19] ?? null,
    memoryFragmentationRatio: a[20] ?? null,
    rdbLastSaveAgeSeconds: a[21] ?? null,
    aofEnabled: a[22] ?? null,
    lastAofRewriteDate: a[23] ?? null,
    lastBgSaveDate: a[24] ?? null,
  }
}

function d_Jube_Dto_RespSentinelEvent_RespSentinelEventDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    channel: a[2] ?? null,
    message: a[3] ?? null,
    createdDate: a[4] ?? null,
    instance: a[5] ?? null,
  }
}

function d_Jube_Dto_RespSentinelStatus_RespSentinelStatusDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    occurredDate: a[1] ?? null,
    entityTypeId: a[2] ?? null,
    entityTypeName: a[3] ?? null,
    name: a[4] ?? null,
    ip: a[5] ?? null,
    port: a[6] ?? null,
    flags: a[7] ?? null,
    masterLinkStatus: a[8] ?? null,
    masterHost: a[9] ?? null,
    masterPort: a[10] ?? null,
    slaveReplOffset: a[11] ?? null,
    replicationLagSeconds: a[12] ?? null,
    numSlaves: a[13] ?? null,
    numOtherSentinels: a[14] ?? null,
    quorum: a[15] ?? null,
    downAfterMilliseconds: a[16] ?? null,
    runId: a[17] ?? null,
    createdDate: a[18] ?? null,
    instance: a[19] ?? null,
  }
}

function d_Jube_Dto_RespSlowOperation_RespSlowOperationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    respSlowLogId: a[1] ?? null,
    occurredDate: a[2] ?? null,
    durationMicroseconds: a[3] ?? null,
    command: a[4] ?? null,
    commandName: a[5] ?? null,
    keyName: a[6] ?? null,
    clientAddress: a[7] ?? null,
    clientName: a[8] ?? null,
    createdDate: a[9] ?? null,
    instance: a[10] ?? null,
  }
}

function d_Jube_Dto_RuleExecution_BuilderRuleResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    valid: a[0] ?? null,
    ruleText: a[1] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[2] ?? null),
  }
}

function d_Jube_Dto_RuleExecution_InlineScriptExecutionResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    compiled: a[0] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[1] ?? null),
    succeeded: a[2] ?? null,
    properties: ((v) => many(v, d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextValueDto))(a[3] ?? null),
    runtimeError: a[4] ?? null,
    timedOut: a[5] ?? null,
    durationMicroseconds: a[6] ?? null,
    engineBehaviour: a[7] ?? null,
  }
}

function d_Jube_Dto_RuleExecution_RuleExecutionResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    compiled: a[0] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[1] ?? null),
    result: a[2] ?? null,
    resultType: a[3] ?? null,
    runtimeError: a[4] ?? null,
    timedOut: a[5] ?? null,
    durationMicroseconds: a[6] ?? null,
    namesRead: a[7] ?? null,
    unsetNamesRead: a[8] ?? null,
    engineBehaviour: a[9] ?? null,
  }
}

function d_Jube_Dto_UserLogin_UserLoginDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    createdUser: a[2] ?? null,
    failed: a[3] ?? null,
    authenticationTypeId: a[4] ?? null,
    authenticationScheme: a[5] ?? null,
    failureTypeId: a[6] ?? null,
    failureReason: a[7] ?? null,
    failureMessage: a[8] ?? null,
    remoteIp: a[9] ?? null,
    localIp: a[10] ?? null,
    userAgent: a[11] ?? null,
  }
}

function d_Jube_Dto_UserLogout_UserLogoutDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    createdUser: a[2] ?? null,
    tenantRegistryId: a[3] ?? null,
    reasonId: a[4] ?? null,
    reason: a[5] ?? null,
    outcomeId: a[6] ?? null,
    outcome: a[7] ?? null,
    remoteIp: a[8] ?? null,
    localIp: a[9] ?? null,
    userAgent: a[10] ?? null,
    sessionStartDate: a[11] ?? null,
    sessionSeconds: a[12] ?? null,
    cutByUser: a[13] ?? null,
    message: a[14] ?? null,
  }
}

function d_Jube_Dto_UserNotification_UserNotificationDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    guid: a[0] ?? null,
    kindId: a[1] ?? null,
    priorityId: a[2] ?? null,
    caseId: a[3] ?? null,
    argumentsJson: a[4] ?? null,
    createdDate: a[5] ?? null,
    read: a[6] ?? null,
  }
}

function d_Jube_Dto_Validation_ValidationErrorDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    propertyName: a[0] ?? null,
    errorCode: a[1] ?? null,
    message: a[2] ?? null,
    line: a[3] ?? null,
    start: a[4] ?? null,
    length: a[5] ?? null,
  }
}

function d_Jube_Dto_Validation_ValidationResultDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    isValid: a[0] ?? null,
    errors: ((v) => many(v, d_Jube_Dto_Validation_ValidationErrorDto))(a[1] ?? null),
  }
}

function d_Jube_Dto_WafAttack_WafAttackDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    createdDate: a[1] ?? null,
    transport: a[2] ?? null,
    route: a[3] ?? null,
    method: a[4] ?? null,
    remoteIp: a[5] ?? null,
    userName: a[6] ?? null,
    signatureName: a[7] ?? null,
    category: a[8] ?? null,
    matchedField: a[9] ?? null,
    matchedValue: a[10] ?? null,
    action: a[11] ?? null,
    correlationId: a[12] ?? null,
    instance: a[13] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCalibrationInfo(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    applied: a[0] ?? null,
    slope: a[1] ?? null,
    intercept: a[2] ?? null,
    rowsUsed: a[3] ?? null,
    reason: a[4] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCoefficient(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    kind: a[1] ?? null,
    estimate: a[2] ?? null,
    standardError: a[3] ?? null,
    waldZ: a[4] ?? null,
    pValue: a[5] ?? null,
    oddsRatio: a[6] ?? null,
    oddsRatioLower95: a[7] ?? null,
    oddsRatioUpper95: a[8] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticFitInfo(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    converged: a[0] ?? null,
    iterations: a[1] ?? null,
    maximumIterations: a[2] ?? null,
    maximumGradient: a[3] ?? null,
    penalised: a[4] ?? null,
    ridgeLambda: a[5] ?? null,
    predictorCount: a[6] ?? null,
    eventsPerVariable: a[7] ?? null,
    standardErrorBasis: a[8] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticPriors(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    trainingPositiveRate: a[0] ?? null,
    effectivePrior: a[1] ?? null,
    baseRate: a[2] ?? null,
    baseRateSource: a[3] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticReliabilityBin(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    lower: a[0] ?? null,
    upper: a[1] ?? null,
    count: a[2] ?? null,
    meanPredicted: a[3] ?? null,
    observedRate: a[4] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitMetrics(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    split: a[0] ?? null,
    rows: a[1] ?? null,
    positives: a[2] ?? null,
    rocAuc: a[3] ?? null,
    rocAucStandardError: a[4] ?? null,
    rocAucLower95: a[5] ?? null,
    rocAucUpper95: a[6] ?? null,
    averagePrecision: a[7] ?? null,
    logLoss: a[8] ?? null,
    brierScore: a[9] ?? null,
    expectedCalibrationError: a[10] ?? null,
    reliabilityBins: ((v) => many(v, d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticReliabilityBin))(a[11] ?? null),
    threshold: a[12] ?? null,
    thresholdSource: a[13] ?? null,
    truePositives: a[14] ?? null,
    falsePositives: a[15] ?? null,
    trueNegatives: a[16] ?? null,
    falseNegatives: a[17] ?? null,
    precision: a[18] ?? null,
    recall: a[19] ?? null,
    f1: a[20] ?? null,
    matthewsCorrelation: a[21] ?? null,
    expectedCostAtThreshold: a[22] ?? null,
    calibrationSlope: a[23] ?? null,
    calibrationIntercept: a[24] ?? null,
    meanPredictedProbability: a[25] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitSizes(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    train: a[0] ?? null,
    validation: a[1] ?? null,
    test: a[2] ?? null,
    trainPositives: a[3] ?? null,
    validationPositives: a[4] ?? null,
    testPositives: a[5] ?? null,
  }
}

function d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticWarning(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    code: a[0] ?? null,
    message: a[1] ?? null,
  }
}

function d_Jube_Service_Charts_Models_ExhaustiveStatisticsChartGridItem(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    variableId: a[0] ?? null,
    svg: a[1] ?? null,
  }
}

function d_Jube_Service_Query_CaseAnalytics_CaseAnalyticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    from: a[0] ?? null,
    to: a[1] ?? null,
    generatedAt: a[2] ?? null,
    truncated: a[3] ?? null,
    casesAnalysed: a[4] ?? null,
    statusNames: a[5] ?? null,
    headline: d_Jube_Dto_Query_CaseAnalytics_CaseAnalyticsHeadlineDto(a[6] ?? null),
    cycleTimes: d_Jube_CaseRisk_Analytics_CycleTimeReport(a[7] ?? null),
    flow: ((v) => many(v, d_Jube_CaseRisk_Analytics_FlowDay))(a[8] ?? null),
    statusDwell: ((v) => many(v, d_Jube_CaseRisk_Analytics_StatusDwell))(a[9] ?? null),
    aging: d_Jube_CaseRisk_Analytics_AgingReport(a[10] ?? null),
    rules: ((v) => many(v, d_Jube_CaseRisk_Analytics_RuleEffectiveness))(a[11] ?? null),
    ranking: d_Jube_CaseRisk_Analytics_RankingQualityReport(a[12] ?? null),
    qaSample: d_Jube_CaseRisk_Analytics_QaSampleReport(a[13] ?? null),
    serviceLevels: d_Jube_CaseRisk_Analytics_SlaReport(a[14] ?? null),
    rhythm: d_Jube_CaseRisk_Analytics_RhythmReport(a[15] ?? null),
    workload: d_Jube_CaseRisk_Analytics_WorkloadReport(a[16] ?? null),
    myDay: d_Jube_CaseRisk_Analytics_MyDayReport(a[17] ?? null),
    checklistSla: d_Jube_CaseRisk_Analytics_ChecklistSlaReport(a[18] ?? null),
    handoffs: d_Jube_CaseRisk_Analytics_HandoffReport(a[19] ?? null),
  }
}

function d_Jube_Service_Query_CaseKeyJournal_CaseKeyJournalAnalyticsDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    meta: d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsMetaDto(a[0] ?? null),
    fields: ((v) => many(v, d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsFieldDto))(a[1] ?? null),
    summary: d_Jube_CaseRisk_Journal_JournalSummary(a[2] ?? null),
    notable: d_Jube_CaseRisk_Journal_NotableReport(a[3] ?? null),
    progression: d_Jube_CaseRisk_Journal_ProgressionReport(a[4] ?? null),
    distributions: ((v) => many(v, d_Jube_CaseRisk_Journal_DistributionReport))(a[5] ?? null),
    velocity: d_Jube_CaseRisk_Journal_VelocityReport(a[6] ?? null),
    gaps: d_Jube_CaseRisk_Journal_GapReport(a[7] ?? null),
    novelty: d_Jube_CaseRisk_Journal_NoveltyReport(a[8] ?? null),
    counterparties: ((v) => many(v, d_Jube_CaseRisk_Journal_CounterpartyProfile))(a[9] ?? null),
    correlation: d_Jube_CaseRisk_Journal_CorrelationReport(a[10] ?? null),
    drift: d_Jube_CaseRisk_Journal_DriftReport(a[11] ?? null),
    peers: d_Jube_Service_Query_CaseKeyJournal_JournalPeersDto(a[12] ?? null),
  }
}

function d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsFieldDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    title: a[1] ?? null,
    lens: a[2] ?? null,
    group: a[3] ?? null,
    roleId: a[4] ?? null,
    roleInferred: a[5] ?? null,
    kind: a[6] ?? null,
  }
}

function d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsMetaDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    key: a[0] ?? null,
    value: a[1] ?? null,
    caseWorkflowGuid: a[2] ?? null,
    rowCount: a[3] ?? null,
    limit: a[4] ?? null,
    truncated: a[5] ?? null,
    from: a[6] ?? null,
    to: a[7] ?? null,
    lowConfidence: a[8] ?? null,
    sections: a[9] ?? null,
  }
}

function d_Jube_Service_Query_CaseKeyJournal_JournalPeersDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    available: a[0] ?? null,
    reason: a[1] ?? null,
    computedDate: a[2] ?? null,
    windowDays: a[3] ?? null,
    segment: a[4] ?? null,
    segmentValue: a[5] ?? null,
    segmentFallback: a[6] ?? null,
    comparisons: ((v) => many(v, d_Jube_CaseRisk_Journal_PeerComparison))(a[7] ?? null),
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsInstance_Models_AskJoobyAnalyticsInstanceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    entityAnalysisModelId: a[1] ?? null,
    responsePayload: a[2] ?? null,
    reportTable: a[3] ?? null,
    active: a[4] ?? null,
    guid: a[5] ?? null,
    id: a[6] ?? null,
    createdUser: a[7] ?? null,
    createdDate: a[8] ?? null,
    updatedUser: a[9] ?? null,
    updatedDate: a[10] ?? null,
    version: a[11] ?? null,
    deletedUser: a[12] ?? null,
    deletedDate: a[13] ?? null,
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsMetricComparisonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    name: a[0] ?? null,
    valueA: a[1] ?? null,
    valueB: a[2] ?? null,
    difference: a[3] ?? null,
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsPromotionEvidenceDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    versionId: a[0] ?? null,
    versionNumber: a[1] ?? null,
    trainedModelId: a[2] ?? null,
    modelName: a[3] ?? null,
    isActiveVersion: a[4] ?? null,
    instanceAllocated: a[5] ?? null,
    allocatedEntityAnalysisModelId: a[6] ?? null,
    producingRunId: a[7] ?? null,
    seed: a[8] ?? null,
    hasHeldOutTestSet: a[9] ?? null,
    splitSizes: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitSizes(a[10] ?? null),
    testMetrics: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitMetrics(a[11] ?? null),
    trainMetrics: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitMetrics(a[12] ?? null),
    calibration: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCalibrationInfo(a[13] ?? null),
    fit: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticFitInfo(a[14] ?? null),
    priors: d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticPriors(a[15] ?? null),
    coefficients: ((v) => many(v, d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCoefficient))(a[16] ?? null),
    warnings: ((v) => many(v, d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticWarning))(a[17] ?? null),
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsVersionComparisonDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    versionA: d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsModelVersionDto(a[0] ?? null),
    versionB: d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsModelVersionDto(a[1] ?? null),
    sameSeed: a[2] ?? null,
    sameSplitSizes: a[3] ?? null,
    sameVariables: a[4] ?? null,
    metrics: ((v) => many(v, d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsMetricComparisonDto))(a[5] ?? null),
    warningsOnlyInA: a[6] ?? null,
    warningsOnlyInB: a[7] ?? null,
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsModelVersionDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    trainedModelId: a[2] ?? null,
    versionNumber: a[3] ?? null,
    isActive: a[4] ?? null,
    producingRunId: a[5] ?? null,
    createdUser: a[6] ?? null,
    createdDate: a[7] ?? null,
    testRows: a[8] ?? null,
    testRocAuc: a[9] ?? null,
    testAveragePrecision: a[10] ?? null,
    testLogLoss: a[11] ?? null,
    testBrierScore: a[12] ?? null,
    testExpectedCalibrationError: a[13] ?? null,
    warningCodes: a[14] ?? null,
    transformedVariableCount: a[15] ?? null,
    evidence: a[16] ?? null,
  }
}

function d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsTrainedModelDto(value: unknown): unknown {
  if (value == null) return value
  const a = value as unknown[]
  return {
    id: a[0] ?? null,
    guid: a[1] ?? null,
    askJoobyAnalyticsInstanceId: a[2] ?? null,
    name: a[3] ?? null,
    kind: a[4] ?? null,
    source: a[5] ?? null,
    active: a[6] ?? null,
    activeVersionId: a[7] ?? null,
    activeVersionNumber: a[8] ?? null,
    correlationKey: a[9] ?? null,
    createdUser: a[10] ?? null,
    createdDate: a[11] ?? null,
    updatedDate: a[12] ?? null,
    version: a[13] ?? null,
    versions: ((v) => many(v, d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsModelVersionDto))(a[14] ?? null),
  }
}

export const decoders: Record<string, Decoder> = {
  'Jube.App.Endpoints.ExhaustiveSearchInstanceSetActiveRequest': d_Jube_App_Endpoints_ExhaustiveSearchInstanceSetActiveRequest,
  'Jube.App.Endpoints.MeEndpoints+MeResponse': d_Jube_App_Endpoints_MeEndpoints_MeResponse,
  'Jube.CaseRisk.Analytics.AgedCase': d_Jube_CaseRisk_Analytics_AgedCase,
  'Jube.CaseRisk.Analytics.AgingBucket': d_Jube_CaseRisk_Analytics_AgingBucket,
  'Jube.CaseRisk.Analytics.AgingReport': d_Jube_CaseRisk_Analytics_AgingReport,
  'Jube.CaseRisk.Analytics.CalibrationBin': d_Jube_CaseRisk_Analytics_CalibrationBin,
  'Jube.CaseRisk.Analytics.ChecklistSlaItem': d_Jube_CaseRisk_Analytics_ChecklistSlaItem,
  'Jube.CaseRisk.Analytics.ChecklistSlaReport': d_Jube_CaseRisk_Analytics_ChecklistSlaReport,
  'Jube.CaseRisk.Analytics.CycleTimeReport': d_Jube_CaseRisk_Analytics_CycleTimeReport,
  'Jube.CaseRisk.Analytics.DistributionStats': d_Jube_CaseRisk_Analytics_DistributionStats,
  'Jube.CaseRisk.Analytics.FlowDay': d_Jube_CaseRisk_Analytics_FlowDay,
  'Jube.CaseRisk.Analytics.GainsPoint': d_Jube_CaseRisk_Analytics_GainsPoint,
  'Jube.CaseRisk.Analytics.HandoffBucket': d_Jube_CaseRisk_Analytics_HandoffBucket,
  'Jube.CaseRisk.Analytics.HandoffCase': d_Jube_CaseRisk_Analytics_HandoffCase,
  'Jube.CaseRisk.Analytics.HandoffReport': d_Jube_CaseRisk_Analytics_HandoffReport,
  'Jube.CaseRisk.Analytics.HistogramBucket': d_Jube_CaseRisk_Analytics_HistogramBucket,
  'Jube.CaseRisk.Analytics.MyDay': d_Jube_CaseRisk_Analytics_MyDay,
  'Jube.CaseRisk.Analytics.MyDayReport': d_Jube_CaseRisk_Analytics_MyDayReport,
  'Jube.CaseRisk.Analytics.PersonWorkload': d_Jube_CaseRisk_Analytics_PersonWorkload,
  'Jube.CaseRisk.Analytics.QaSampleReport': d_Jube_CaseRisk_Analytics_QaSampleReport,
  'Jube.CaseRisk.Analytics.RankingQualityReport': d_Jube_CaseRisk_Analytics_RankingQualityReport,
  'Jube.CaseRisk.Analytics.RhythmReport': d_Jube_CaseRisk_Analytics_RhythmReport,
  'Jube.CaseRisk.Analytics.RuleEffectiveness': d_Jube_CaseRisk_Analytics_RuleEffectiveness,
  'Jube.CaseRisk.Analytics.SlaDimensionSummary': d_Jube_CaseRisk_Analytics_SlaDimensionSummary,
  'Jube.CaseRisk.Analytics.SlaReport': d_Jube_CaseRisk_Analytics_SlaReport,
  'Jube.CaseRisk.Analytics.SlaWatch': d_Jube_CaseRisk_Analytics_SlaWatch,
  'Jube.CaseRisk.Analytics.SlaWeek': d_Jube_CaseRisk_Analytics_SlaWeek,
  'Jube.CaseRisk.Analytics.StatusDwell': d_Jube_CaseRisk_Analytics_StatusDwell,
  'Jube.CaseRisk.Analytics.TeamWorkload': d_Jube_CaseRisk_Analytics_TeamWorkload,
  'Jube.CaseRisk.Analytics.WorkloadReport': d_Jube_CaseRisk_Analytics_WorkloadReport,
  'Jube.CaseRisk.Journal.BoxSummary': d_Jube_CaseRisk_Journal_BoxSummary,
  'Jube.CaseRisk.Journal.Burst': d_Jube_CaseRisk_Journal_Burst,
  'Jube.CaseRisk.Journal.CorrelationPair': d_Jube_CaseRisk_Journal_CorrelationPair,
  'Jube.CaseRisk.Journal.CorrelationReport': d_Jube_CaseRisk_Journal_CorrelationReport,
  'Jube.CaseRisk.Journal.CounterpartyEntry': d_Jube_CaseRisk_Journal_CounterpartyEntry,
  'Jube.CaseRisk.Journal.CounterpartyProfile': d_Jube_CaseRisk_Journal_CounterpartyProfile,
  'Jube.CaseRisk.Journal.DistributionBin': d_Jube_CaseRisk_Journal_DistributionBin,
  'Jube.CaseRisk.Journal.DistributionReport': d_Jube_CaseRisk_Journal_DistributionReport,
  'Jube.CaseRisk.Journal.Dormancy': d_Jube_CaseRisk_Journal_Dormancy,
  'Jube.CaseRisk.Journal.DriftReport': d_Jube_CaseRisk_Journal_DriftReport,
  'Jube.CaseRisk.Journal.FieldDrift': d_Jube_CaseRisk_Journal_FieldDrift,
  'Jube.CaseRisk.Journal.FieldNorm': d_Jube_CaseRisk_Journal_FieldNorm,
  'Jube.CaseRisk.Journal.FieldSummary': d_Jube_CaseRisk_Journal_FieldSummary,
  'Jube.CaseRisk.Journal.GapBin': d_Jube_CaseRisk_Journal_GapBin,
  'Jube.CaseRisk.Journal.GapReport': d_Jube_CaseRisk_Journal_GapReport,
  'Jube.CaseRisk.Journal.JournalSummary': d_Jube_CaseRisk_Journal_JournalSummary,
  'Jube.CaseRisk.Journal.NewValue': d_Jube_CaseRisk_Journal_NewValue,
  'Jube.CaseRisk.Journal.NotableReason': d_Jube_CaseRisk_Journal_NotableReason,
  'Jube.CaseRisk.Journal.NotableReport': d_Jube_CaseRisk_Journal_NotableReport,
  'Jube.CaseRisk.Journal.NotableRow': d_Jube_CaseRisk_Journal_NotableRow,
  'Jube.CaseRisk.Journal.NoveltyField': d_Jube_CaseRisk_Journal_NoveltyField,
  'Jube.CaseRisk.Journal.NoveltyReport': d_Jube_CaseRisk_Journal_NoveltyReport,
  'Jube.CaseRisk.Journal.PeerComparison': d_Jube_CaseRisk_Journal_PeerComparison,
  'Jube.CaseRisk.Journal.ProgressionPoint': d_Jube_CaseRisk_Journal_ProgressionPoint,
  'Jube.CaseRisk.Journal.ProgressionReport': d_Jube_CaseRisk_Journal_ProgressionReport,
  'Jube.CaseRisk.Journal.ProgressionSeries': d_Jube_CaseRisk_Journal_ProgressionSeries,
  'Jube.CaseRisk.Journal.VelocityReport': d_Jube_CaseRisk_Journal_VelocityReport,
  'Jube.Data.Poco.EntityAnalysisModelSynchronisationSchedule': d_Jube_Data_Poco_EntityAnalysisModelSynchronisationSchedule,
  'Jube.Data.Poco.TenantRegistry': d_Jube_Data_Poco_TenantRegistry,
  'Jube.Data.Query.DynamicResultsSchema.DynamicResultsSchemaDto': d_Jube_Data_Query_DynamicResultsSchema_DynamicResultsSchemaDto,
  'Jube.Data.Query.GetAskJoobyStartupStatsQuery.GetAskJoobyStartupStatsQueryDto': d_Jube_Data_Query_GetAskJoobyStartupStatsQuery_GetAskJoobyStartupStatsQueryDto,
  'Jube.Dto.ApplicationLogEntry.ApplicationLogEntryDto': d_Jube_Dto_ApplicationLogEntry_ApplicationLogEntryDto,
  'Jube.Dto.ArchiverStagePerformanceCounter.ArchiverStagePerformanceCounterDto': d_Jube_Dto_ArchiverStagePerformanceCounter_ArchiverStagePerformanceCounterDto,
  'Jube.Dto.ArchiverWarning.ArchiverWarningDto': d_Jube_Dto_ArchiverWarning_ArchiverWarningDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyApprovalDecisionRequestDto': d_Jube_Dto_AskJoobyConversation_AskJoobyApprovalDecisionRequestDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyConversationDto': d_Jube_Dto_AskJoobyConversation_AskJoobyConversationDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyConversationEntryDto': d_Jube_Dto_AskJoobyConversation_AskJoobyConversationEntryDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyFeedbackRequestDto': d_Jube_Dto_AskJoobyConversation_AskJoobyFeedbackRequestDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyFileDto': d_Jube_Dto_AskJoobyConversation_AskJoobyFileDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyGuidanceRequestDto': d_Jube_Dto_AskJoobyConversation_AskJoobyGuidanceRequestDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyMessageAttachmentDto': d_Jube_Dto_AskJoobyConversation_AskJoobyMessageAttachmentDto,
  'Jube.Dto.AskJoobyConversation.AskJoobySendMessageRequestDto': d_Jube_Dto_AskJoobyConversation_AskJoobySendMessageRequestDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyStartConversationRequestDto': d_Jube_Dto_AskJoobyConversation_AskJoobyStartConversationRequestDto,
  'Jube.Dto.AskJoobyConversation.AskJoobyStreamItemDto': d_Jube_Dto_AskJoobyConversation_AskJoobyStreamItemDto,
  'Jube.Dto.AskJoobyMemory.AskJoobyMemoryDto': d_Jube_Dto_AskJoobyMemory_AskJoobyMemoryDto,
  'Jube.Dto.AskJoobyRagChunk.AskJoobyRagChunkDto': d_Jube_Dto_AskJoobyRagChunk_AskJoobyRagChunkDto,
  'Jube.Dto.Authentication.AuthenticationMethodsDto': d_Jube_Dto_Authentication_AuthenticationMethodsDto,
  'Jube.Dto.Authentication.AuthenticationRequestDto': d_Jube_Dto_Authentication_AuthenticationRequestDto,
  'Jube.Dto.Authentication.AuthenticationResponseDto': d_Jube_Dto_Authentication_AuthenticationResponseDto,
  'Jube.Dto.Authentication.AuthenticationSchemaRequestDto': d_Jube_Dto_Authentication_AuthenticationSchemaRequestDto,
  'Jube.Dto.Authentication.ChangePasswordRequestDto': d_Jube_Dto_Authentication_ChangePasswordRequestDto,
  'Jube.Dto.Authentication.PasswordSchemeDto': d_Jube_Dto_Authentication_PasswordSchemeDto,
  'Jube.Dto.Authentication.TotpEnrolmentDto': d_Jube_Dto_Authentication_TotpEnrolmentDto,
  'Jube.Dto.CaptureQueueHealth.CaptureQueueHealthDto': d_Jube_Dto_CaptureQueueHealth_CaptureQueueHealthDto,
  'Jube.Dto.CaseCreationStagePerformanceCounter.CaseCreationStagePerformanceCounterDto': d_Jube_Dto_CaseCreationStagePerformanceCounter_CaseCreationStagePerformanceCounterDto,
  'Jube.Dto.CaseCreationWarning.CaseCreationWarningDto': d_Jube_Dto_CaseCreationWarning_CaseCreationWarningDto,
  'Jube.Dto.ContainerLogEntry.ContainerLogEntryDto': d_Jube_Dto_ContainerLogEntry_ContainerLogEntryDto,
  'Jube.Dto.DockerContainerMetric.DockerContainerMetricDto': d_Jube_Dto_DockerContainerMetric_DockerContainerMetricDto,
  'Jube.Dto.DockerEvent.DockerEventDto': d_Jube_Dto_DockerEvent_DockerEventDto,
  'Jube.Dto.DockerHostMetric.DockerHostMetricDto': d_Jube_Dto_DockerHostMetric_DockerHostMetricDto,
  'Jube.Dto.DotNetRuntimeMetric.DotNetRuntimeMetricDto': d_Jube_Dto_DotNetRuntimeMetric_DotNetRuntimeMetricDto,
  'Jube.Dto.EntityAnalysisAsynchronousQueueBalance.EntityAnalysisAsynchronousQueueBalanceDto': d_Jube_Dto_EntityAnalysisAsynchronousQueueBalance_EntityAnalysisAsynchronousQueueBalanceDto,
  'Jube.Dto.EntityAnalysisInlineScript.EntityAnalysisInlineScriptDto': d_Jube_Dto_EntityAnalysisInlineScript_EntityAnalysisInlineScriptDto,
  'Jube.Dto.EntityAnalysisModel.EntityAnalysisModelDto': d_Jube_Dto_EntityAnalysisModel_EntityAnalysisModelDto,
  'Jube.Dto.EntityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionCalculationDto': d_Jube_Dto_EntityAnalysisModelAbstractionCalculation_EntityAnalysisModelAbstractionCalculationDto,
  'Jube.Dto.EntityAnalysisModelAbstractionRule.EntityAnalysisModelAbstractionRuleDto': d_Jube_Dto_EntityAnalysisModelAbstractionRule_EntityAnalysisModelAbstractionRuleDto,
  'Jube.Dto.EntityAnalysisModelActivationRule.EntityAnalysisModelActivationRuleDto': d_Jube_Dto_EntityAnalysisModelActivationRule_EntityAnalysisModelActivationRuleDto,
  'Jube.Dto.EntityAnalysisModelActivationRule.ShadowVariantRequestDto': d_Jube_Dto_EntityAnalysisModelActivationRule_ShadowVariantRequestDto,
  'Jube.Dto.EntityAnalysisModelActivationRule.ShadowVariantResultDto': d_Jube_Dto_EntityAnalysisModelActivationRule_ShadowVariantResultDto,
  'Jube.Dto.EntityAnalysisModelActivationRuleOverride.EntityAnalysisModelActivationRuleOverrideDto': d_Jube_Dto_EntityAnalysisModelActivationRuleOverride_EntityAnalysisModelActivationRuleOverrideDto,
  'Jube.Dto.EntityAnalysisModelAsynchronousQueueBalance.EntityAnalysisModelAsynchronousQueueBalanceDto': d_Jube_Dto_EntityAnalysisModelAsynchronousQueueBalance_EntityAnalysisModelAsynchronousQueueBalanceDto,
  'Jube.Dto.EntityAnalysisModelDictionary.EntityAnalysisModelDictionaryDto': d_Jube_Dto_EntityAnalysisModelDictionary_EntityAnalysisModelDictionaryDto,
  'Jube.Dto.EntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryKvpCsvUploadResultDto': d_Jube_Dto_EntityAnalysisModelDictionaryKvp_EntityAnalysisModelDictionaryKvpCsvUploadResultDto,
  'Jube.Dto.EntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryKvpDto': d_Jube_Dto_EntityAnalysisModelDictionaryKvp_EntityAnalysisModelDictionaryKvpDto,
  'Jube.Dto.EntityAnalysisModelGatewayRule.EntityAnalysisModelGatewayRuleDto': d_Jube_Dto_EntityAnalysisModelGatewayRule_EntityAnalysisModelGatewayRuleDto,
  'Jube.Dto.EntityAnalysisModelHttpAdaptation.EntityAnalysisModelHttpAdaptationDto': d_Jube_Dto_EntityAnalysisModelHttpAdaptation_EntityAnalysisModelHttpAdaptationDto,
  'Jube.Dto.EntityAnalysisModelInlineFunction.EntityAnalysisModelInlineFunctionDto': d_Jube_Dto_EntityAnalysisModelInlineFunction_EntityAnalysisModelInlineFunctionDto,
  'Jube.Dto.EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptDto': d_Jube_Dto_EntityAnalysisModelInlineScript_EntityAnalysisModelInlineScriptDto,
  'Jube.Dto.EntityAnalysisModelList.EntityAnalysisModelListDto': d_Jube_Dto_EntityAnalysisModelList_EntityAnalysisModelListDto,
  'Jube.Dto.EntityAnalysisModelListValue.EntityAnalysisModelListValueCsvUploadResultDto': d_Jube_Dto_EntityAnalysisModelListValue_EntityAnalysisModelListValueCsvUploadResultDto,
  'Jube.Dto.EntityAnalysisModelListValue.EntityAnalysisModelListValueDto': d_Jube_Dto_EntityAnalysisModelListValue_EntityAnalysisModelListValueDto,
  'Jube.Dto.EntityAnalysisModelOverride.EntityAnalysisModelOverrideDto': d_Jube_Dto_EntityAnalysisModelOverride_EntityAnalysisModelOverrideDto,
  'Jube.Dto.EntityAnalysisModelProcessingCounter.EntityAnalysisModelProcessingCounterDto': d_Jube_Dto_EntityAnalysisModelProcessingCounter_EntityAnalysisModelProcessingCounterDto,
  'Jube.Dto.EntityAnalysisModelReprocessingRule.EntityAnalysisModelReprocessingRuleDto': d_Jube_Dto_EntityAnalysisModelReprocessingRule_EntityAnalysisModelReprocessingRuleDto,
  'Jube.Dto.EntityAnalysisModelReprocessingRuleInstance.EntityAnalysisModelReprocessingRuleInstanceDto': d_Jube_Dto_EntityAnalysisModelReprocessingRuleInstance_EntityAnalysisModelReprocessingRuleInstanceDto,
  'Jube.Dto.EntityAnalysisModelRequestXPath.EntityAnalysisModelRequestXPathDto': d_Jube_Dto_EntityAnalysisModelRequestXPath_EntityAnalysisModelRequestXPathDto,
  'Jube.Dto.EntityAnalysisModelResponseElevationRange.EntityAnalysisModelResponseElevationRangeDto': d_Jube_Dto_EntityAnalysisModelResponseElevationRange_EntityAnalysisModelResponseElevationRangeDto,
  'Jube.Dto.EntityAnalysisModelResponseTimePipelineCounter.EntityAnalysisModelResponseTimePipelineCounterDto': d_Jube_Dto_EntityAnalysisModelResponseTimePipelineCounter_EntityAnalysisModelResponseTimePipelineCounterDto,
  'Jube.Dto.EntityAnalysisModelSanction.EntityAnalysisModelSanctionDto': d_Jube_Dto_EntityAnalysisModelSanction_EntityAnalysisModelSanctionDto,
  'Jube.Dto.EntityAnalysisModelStagePerformanceCounter.EntityAnalysisModelStagePerformanceCounterDto': d_Jube_Dto_EntityAnalysisModelStagePerformanceCounter_EntityAnalysisModelStagePerformanceCounterDto,
  'Jube.Dto.EntityAnalysisModelTag.EntityAnalysisModelTagDto': d_Jube_Dto_EntityAnalysisModelTag_EntityAnalysisModelTagDto,
  'Jube.Dto.EntityAnalysisModelTaskPerformanceCounter.EntityAnalysisModelTaskPerformanceCounterDto': d_Jube_Dto_EntityAnalysisModelTaskPerformanceCounter_EntityAnalysisModelTaskPerformanceCounterDto,
  'Jube.Dto.EntityAnalysisModelTtlCounter.EntityAnalysisModelTtlCounterDto': d_Jube_Dto_EntityAnalysisModelTtlCounter_EntityAnalysisModelTtlCounterDto,
  'Jube.Dto.EntityApproval.EntityApprovalActionRequestDto': d_Jube_Dto_EntityApproval_EntityApprovalActionRequestDto,
  'Jube.Dto.EntityApproval.EntityApprovalBulkDto': d_Jube_Dto_EntityApproval_EntityApprovalBulkDto,
  'Jube.Dto.EntityApproval.EntityApprovalBulkResultDto': d_Jube_Dto_EntityApproval_EntityApprovalBulkResultDto,
  'Jube.Dto.EntityApproval.EntityApprovalChangeDto': d_Jube_Dto_EntityApproval_EntityApprovalChangeDto,
  'Jube.Dto.EntityApproval.EntityApprovalChangesDto': d_Jube_Dto_EntityApproval_EntityApprovalChangesDto,
  'Jube.Dto.EntityApproval.EntityApprovalDecisionDto': d_Jube_Dto_EntityApproval_EntityApprovalDecisionDto,
  'Jube.Dto.EntityApproval.EntityApprovalDto': d_Jube_Dto_EntityApproval_EntityApprovalDto,
  'Jube.Dto.EntityApproval.EntityApprovalStatusDto': d_Jube_Dto_EntityApproval_EntityApprovalStatusDto,
  'Jube.Dto.EntityApproval.PendingApprovalDto': d_Jube_Dto_EntityApproval_PendingApprovalDto,
  'Jube.Dto.EtcdClusterEvent.EtcdClusterEventDto': d_Jube_Dto_EtcdClusterEvent_EtcdClusterEventDto,
  'Jube.Dto.EtcdMemberStatus.EtcdMemberStatusDto': d_Jube_Dto_EtcdMemberStatus_EtcdMemberStatusDto,
  'Jube.Dto.ExhaustiveInstanceDataset.DatasetDistinctValueDto': d_Jube_Dto_ExhaustiveInstanceDataset_DatasetDistinctValueDto,
  'Jube.Dto.ExhaustiveInstanceDataset.ExhaustiveInstanceDatasetColumnDto': d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetColumnDto,
  'Jube.Dto.ExhaustiveInstanceDataset.ExhaustiveInstanceDatasetDto': d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetDto,
  'Jube.Dto.ExhaustiveInstanceDataset.ExhaustiveInstanceDatasetMappingColumnDto': d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetMappingColumnDto,
  'Jube.Dto.ExhaustiveInstanceDataset.ExhaustiveInstanceDatasetMappingDto': d_Jube_Dto_ExhaustiveInstanceDataset_ExhaustiveInstanceDatasetMappingDto,
  'Jube.Dto.ExhaustiveSearchInstance.ExhaustiveSearchInstanceDto': d_Jube_Dto_ExhaustiveSearchInstance_ExhaustiveSearchInstanceDto,
  'Jube.Dto.Filter.FilterCountGroupDto': d_Jube_Dto_Filter_FilterCountGroupDto,
  'Jube.Dto.Filter.FilterCountResultDto': d_Jube_Dto_Filter_FilterCountResultDto,
  'Jube.Dto.Filter.FilterFieldDto': d_Jube_Dto_Filter_FilterFieldDto,
  'Jube.Dto.HaProxyReachabilityProbe.HaProxyReachabilityProbeDto': d_Jube_Dto_HaProxyReachabilityProbe_HaProxyReachabilityProbeDto,
  'Jube.Dto.HaProxyServerStatus.HaProxyServerStatusDto': d_Jube_Dto_HaProxyServerStatus_HaProxyServerStatusDto,
  'Jube.Dto.Hold.ResponseHold': d_Jube_Dto_Hold_ResponseHold,
  'Jube.Dto.Hold.ResponseHoldDecision': d_Jube_Dto_Hold_ResponseHoldDecision,
  'Jube.Dto.Hold.ResponseHoldLeg': d_Jube_Dto_Hold_ResponseHoldLeg,
  'Jube.Dto.HttpProcessingCounter.HttpProcessingCounterDto': d_Jube_Dto_HttpProcessingCounter_HttpProcessingCounterDto,
  'Jube.Dto.Invoke.AdvancedAnalyticsRecallContributionDto': d_Jube_Dto_Invoke_AdvancedAnalyticsRecallContributionDto,
  'Jube.Dto.Invoke.AdvancedAnalyticsRecallDto': d_Jube_Dto_Invoke_AdvancedAnalyticsRecallDto,
  'Jube.Dto.Invoke.ExhaustiveBatchRecallResultDto': d_Jube_Dto_Invoke_ExhaustiveBatchRecallResultDto,
  'Jube.Dto.Invoke.ExhaustiveBatchRecallRowDto': d_Jube_Dto_Invoke_ExhaustiveBatchRecallRowDto,
  'Jube.Dto.Invoke.ExhaustiveRecallDto': d_Jube_Dto_Invoke_ExhaustiveRecallDto,
  'Jube.Dto.Invoke.ExhaustiveRecallVariableRelevanceDto': d_Jube_Dto_Invoke_ExhaustiveRecallVariableRelevanceDto,
  'Jube.Dto.Invoke.SanctionAggregationDto': d_Jube_Dto_Invoke_SanctionAggregationDto,
  'Jube.Dto.Invoke.SanctionAggregationsDto': d_Jube_Dto_Invoke_SanctionAggregationsDto,
  'Jube.Dto.Invoke.SanctionEntryDto': d_Jube_Dto_Invoke_SanctionEntryDto,
  'Jube.Dto.Invoke.SanctionSearchRequestDto': d_Jube_Dto_Invoke_SanctionSearchRequestDto,
  'Jube.Dto.Invoke.SanctionSearchResponseDto': d_Jube_Dto_Invoke_SanctionSearchResponseDto,
  'Jube.Dto.Invoke.SanctionSourceAggregationDto': d_Jube_Dto_Invoke_SanctionSourceAggregationDto,
  'Jube.Dto.ModelInvokeWarning.ModelInvokeWarningDto': d_Jube_Dto_ModelInvokeWarning_ModelInvokeWarningDto,
  'Jube.Dto.OpenTelemetryExclude.OpenTelemetryExcludeDto': d_Jube_Dto_OpenTelemetryExclude_OpenTelemetryExcludeDto,
  'Jube.Dto.OpenTelemetryLogCounter.OpenTelemetryLogCounterDto': d_Jube_Dto_OpenTelemetryLogCounter_OpenTelemetryLogCounterDto,
  'Jube.Dto.OpenTelemetryMetric.OpenTelemetryMetricDto': d_Jube_Dto_OpenTelemetryMetric_OpenTelemetryMetricDto,
  'Jube.Dto.OtlpDispatchCounter.OtlpDispatchCounterDto': d_Jube_Dto_OtlpDispatchCounter_OtlpDispatchCounterDto,
  'Jube.Dto.OverlayNetworkTaskDrift.OverlayNetworkTaskDriftDto': d_Jube_Dto_OverlayNetworkTaskDrift_OverlayNetworkTaskDriftDto,
  'Jube.Dto.PatroniClusterEvent.PatroniClusterEventDto': d_Jube_Dto_PatroniClusterEvent_PatroniClusterEventDto,
  'Jube.Dto.PatroniMemberStatus.PatroniMemberStatusDto': d_Jube_Dto_PatroniMemberStatus_PatroniMemberStatusDto,
  'Jube.Dto.Payload.BacktestChartRequest': d_Jube_Dto_Payload_BacktestChartRequest,
  'Jube.Dto.Payload.BacktestChartTag': d_Jube_Dto_Payload_BacktestChartTag,
  'Jube.Dto.Payload.ColumnStatistics': d_Jube_Dto_Payload_ColumnStatistics,
  'Jube.Dto.Payload.HistogramBucket': d_Jube_Dto_Payload_HistogramBucket,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.ApplicationLogEntry.ApplicationLogEntryDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ApplicationLogEntry_ApplicationLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.ArchiverStagePerformanceCounter.ArchiverStagePerformanceCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ArchiverStagePerformanceCounter_ArchiverStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.ArchiverWarning.ArchiverWarningDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ArchiverWarning_ArchiverWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.CaptureQueueHealth.CaptureQueueHealthDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaptureQueueHealth_CaptureQueueHealthDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.CaseCreationStagePerformanceCounter.CaseCreationStagePerformanceCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaseCreationStagePerformanceCounter_CaseCreationStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.CaseCreationWarning.CaseCreationWarningDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_CaseCreationWarning_CaseCreationWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.ContainerLogEntry.ContainerLogEntryDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ContainerLogEntry_ContainerLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.DockerContainerMetric.DockerContainerMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerContainerMetric_DockerContainerMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.DockerEvent.DockerEventDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerEvent_DockerEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.DockerHostMetric.DockerHostMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DockerHostMetric_DockerHostMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.DotNetRuntimeMetric.DotNetRuntimeMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_DotNetRuntimeMetric_DotNetRuntimeMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisAsynchronousQueueBalance.EntityAnalysisAsynchronousQueueBalanceDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisAsynchronousQueueBalance_EntityAnalysisAsynchronousQueueBalanceDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisModelAsynchronousQueueBalance.EntityAnalysisModelAsynchronousQueueBalanceDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelAsynchronousQueueBalance_EntityAnalysisModelAsynchronousQueueBalanceDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisModelProcessingCounter.EntityAnalysisModelProcessingCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelProcessingCounter_EntityAnalysisModelProcessingCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisModelResponseTimePipelineCounter.EntityAnalysisModelResponseTimePipelineCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelResponseTimePipelineCounter_EntityAnalysisModelResponseTimePipelineCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisModelStagePerformanceCounter.EntityAnalysisModelStagePerformanceCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelStagePerformanceCounter_EntityAnalysisModelStagePerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EntityAnalysisModelTaskPerformanceCounter.EntityAnalysisModelTaskPerformanceCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EntityAnalysisModelTaskPerformanceCounter_EntityAnalysisModelTaskPerformanceCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EtcdClusterEvent.EtcdClusterEventDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EtcdClusterEvent_EtcdClusterEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.EtcdMemberStatus.EtcdMemberStatusDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_EtcdMemberStatus_EtcdMemberStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.HaProxyReachabilityProbe.HaProxyReachabilityProbeDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HaProxyReachabilityProbe_HaProxyReachabilityProbeDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.HaProxyServerStatus.HaProxyServerStatusDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HaProxyServerStatus_HaProxyServerStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.HttpProcessingCounter.HttpProcessingCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_HttpProcessingCounter_HttpProcessingCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.ModelInvokeWarning.ModelInvokeWarningDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_ModelInvokeWarning_ModelInvokeWarningDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.OpenTelemetryMetric.OpenTelemetryMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OpenTelemetryMetric_OpenTelemetryMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.OtlpDispatchCounter.OtlpDispatchCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OtlpDispatchCounter_OtlpDispatchCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.OverlayNetworkTaskDrift.OverlayNetworkTaskDriftDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_OverlayNetworkTaskDrift_OverlayNetworkTaskDriftDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PatroniClusterEvent.PatroniClusterEventDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PatroniClusterEvent_PatroniClusterEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PatroniMemberStatus.PatroniMemberStatusDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PatroniMemberStatus_PatroniMemberStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresActivity.PostgresActivityDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresActivity_PostgresActivityDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresIndexStatistics.PostgresIndexStatisticsDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresIndexStatistics_PostgresIndexStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresLogEntry.PostgresLogEntryDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresLogEntry_PostgresLogEntryDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresMetric.PostgresMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresMetric_PostgresMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresReplicationStatus.PostgresReplicationStatusDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresReplicationStatus_PostgresReplicationStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresStatementStatistics.PostgresStatementStatisticsDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresStatementStatistics_PostgresStatementStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.PostgresTableStatistics.PostgresTableStatisticsDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_PostgresTableStatistics_PostgresTableStatisticsDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespCallCounter.RespCallCounterDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespCallCounter_RespCallCounterDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespConnectionEvent.RespConnectionEventDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespConnectionEvent_RespConnectionEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespConnectionMultiplexerMetric.RespConnectionMultiplexerMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespConnectionMultiplexerMetric_RespConnectionMultiplexerMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespMetric.RespMetricDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespMetric_RespMetricDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespSentinelEvent.RespSentinelEventDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSentinelEvent_RespSentinelEventDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespSentinelStatus.RespSentinelStatusDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSentinelStatus_RespSentinelStatusDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.RespSlowOperation.RespSlowOperationDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_RespSlowOperation_RespSlowOperationDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.UserLogin.UserLoginDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_UserLogin_UserLoginDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.UserLogout.UserLogoutDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_UserLogout_UserLogoutDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadResult`1[[Jube.Dto.WafAttack.WafAttackDto, Jube.Dto, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]': d_Jube_Dto_Payload_PayloadResult_1__Jube_Dto_WafAttack_WafAttackDto__Jube_Dto__Version_1_0_0_0__Culture_neutral__PublicKeyToken_null__,
  'Jube.Dto.Payload.PayloadStatistics': d_Jube_Dto_Payload_PayloadStatistics,
  'Jube.Dto.Payload.StatisticsHistogramChartRequest': d_Jube_Dto_Payload_StatisticsHistogramChartRequest,
  'Jube.Dto.Payload.WatcherSummaryChartRequest': d_Jube_Dto_Payload_WatcherSummaryChartRequest,
  'Jube.Dto.Payload.WatcherSummaryRule': d_Jube_Dto_Payload_WatcherSummaryRule,
  'Jube.Dto.PostgresActivity.PostgresActivityDto': d_Jube_Dto_PostgresActivity_PostgresActivityDto,
  'Jube.Dto.PostgresIndexStatistics.PostgresIndexStatisticsDto': d_Jube_Dto_PostgresIndexStatistics_PostgresIndexStatisticsDto,
  'Jube.Dto.PostgresLock.PostgresLockDto': d_Jube_Dto_PostgresLock_PostgresLockDto,
  'Jube.Dto.PostgresLogEntry.PostgresLogEntryDto': d_Jube_Dto_PostgresLogEntry_PostgresLogEntryDto,
  'Jube.Dto.PostgresMetric.PostgresMetricDto': d_Jube_Dto_PostgresMetric_PostgresMetricDto,
  'Jube.Dto.PostgresReplicationStatus.PostgresReplicationStatusDto': d_Jube_Dto_PostgresReplicationStatus_PostgresReplicationStatusDto,
  'Jube.Dto.PostgresStatementStatistics.PostgresStatementStatisticsDto': d_Jube_Dto_PostgresStatementStatistics_PostgresStatementStatisticsDto,
  'Jube.Dto.PostgresTableStatistics.PostgresTableStatisticsDto': d_Jube_Dto_PostgresTableStatistics_PostgresTableStatisticsDto,
  'Jube.Dto.Preservation.PreservationSnapshotDto': d_Jube_Dto_Preservation_PreservationSnapshotDto,
  'Jube.Dto.Query.AdvancedAnalyticsNarrative.AdvancedAnalyticsNarrativeDto': d_Jube_Dto_Query_AdvancedAnalyticsNarrative_AdvancedAnalyticsNarrativeDto,
  'Jube.Dto.Query.CaseAnalytics.CaseAnalyticsHeadlineDto': d_Jube_Dto_Query_CaseAnalytics_CaseAnalyticsHeadlineDto,
  'Jube.Dto.Query.CaseAnalytics.CaseAnalyticsRequestDto': d_Jube_Dto_Query_CaseAnalytics_CaseAnalyticsRequestDto,
  'Jube.Dto.Query.CaseBulk.CaseBulkCloseDto': d_Jube_Dto_Query_CaseBulk_CaseBulkCloseDto,
  'Jube.Dto.Query.CaseBulk.CaseBulkItemDto': d_Jube_Dto_Query_CaseBulk_CaseBulkItemDto,
  'Jube.Dto.Query.CaseBulk.CaseBulkReassignDto': d_Jube_Dto_Query_CaseBulk_CaseBulkReassignDto,
  'Jube.Dto.Query.CaseBulk.CaseBulkResultDto': d_Jube_Dto_Query_CaseBulk_CaseBulkResultDto,
  'Jube.Dto.Query.CaseByCaseKeyValue.CaseByCaseKeyValueDto': d_Jube_Dto_Query_CaseByCaseKeyValue_CaseByCaseKeyValueDto,
  'Jube.Dto.Query.CaseById.CaseByIdActivationDto': d_Jube_Dto_Query_CaseById_CaseByIdActivationDto,
  'Jube.Dto.Query.CaseById.CaseByIdDto': d_Jube_Dto_Query_CaseById_CaseByIdDto,
  'Jube.Dto.Query.CaseById.CaseByIdFieldEntryDto': d_Jube_Dto_Query_CaseById_CaseByIdFieldEntryDto,
  'Jube.Dto.Query.CaseBySessionCaseSearchCompile.CaseBySessionCaseSearchCompileActivationDto': d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileActivationDto,
  'Jube.Dto.Query.CaseBySessionCaseSearchCompile.CaseBySessionCaseSearchCompileDto': d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileDto,
  'Jube.Dto.Query.CaseBySessionCaseSearchCompile.CaseBySessionCaseSearchCompileFieldEntryDto': d_Jube_Dto_Query_CaseBySessionCaseSearchCompile_CaseBySessionCaseSearchCompileFieldEntryDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistGroupDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistGroupDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistHistoryEntryDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistHistoryEntryDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistItemDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistItemDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistOverrideDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistOverrideDto,
  'Jube.Dto.Query.CaseChecklist.CaseChecklistOverrideRequestDto': d_Jube_Dto_Query_CaseChecklist_CaseChecklistOverrideRequestDto,
  'Jube.Dto.Query.CaseDiary.CaseDiaryClearDto': d_Jube_Dto_Query_CaseDiary_CaseDiaryClearDto,
  'Jube.Dto.Query.CaseDiary.CaseDiarySetDto': d_Jube_Dto_Query_CaseDiary_CaseDiarySetDto,
  'Jube.Dto.Query.CaseEventByCaseKeyValue.CaseEventByCaseKeyValueDto': d_Jube_Dto_Query_CaseEventByCaseKeyValue_CaseEventByCaseKeyValueDto,
  'Jube.Dto.Query.CaseHold.CaseHoldDecisionDto': d_Jube_Dto_Query_CaseHold_CaseHoldDecisionDto,
  'Jube.Dto.Query.CaseHold.CaseHoldDecisionResultDto': d_Jube_Dto_Query_CaseHold_CaseHoldDecisionResultDto,
  'Jube.Dto.Query.CaseHold.CaseHoldDto': d_Jube_Dto_Query_CaseHold_CaseHoldDto,
  'Jube.Dto.Query.CaseJournal.CaseJournalDto': d_Jube_Dto_Query_CaseJournal_CaseJournalDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalActivationsDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalActivationsDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalAnalyticsRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAnalyticsRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalAroundDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalAroundGroupDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundGroupDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalAroundRowDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalAroundRowDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalColumnDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalColumnDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalDrillKeyDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalDrillKeyDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalLensDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLensDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalLocationDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalLocationDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalMetaDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalMetaDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalPayloadDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPayloadDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalPinRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPinRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalPinsDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalPinsDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalReplayDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplayDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalReplayRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplayRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalReplaysDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalReplaysDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalSvirfdDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalSvirfdDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalSvirfdRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalSvirfdRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagJobDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagJobPreviewDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobPreviewDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagJobPreviewRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobPreviewRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagJobRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagJobRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagOptionsDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagOptionsDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagRestoreRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagRestoreRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTagSetDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTagSetDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTrackDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTrackMetaDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackMetaDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTrackRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTrackRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTransactionDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTransactionDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTransactionRequestDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTransactionRequestDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTravelLegDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelLegDto,
  'Jube.Dto.Query.CaseKeyJournal.CaseKeyJournalTravelSummaryDto': d_Jube_Dto_Query_CaseKeyJournal_CaseKeyJournalTravelSummaryDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalActivationCardDto': d_Jube_Dto_Query_CaseKeyJournal_JournalActivationCardDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalActivationDriverDto': d_Jube_Dto_Query_CaseKeyJournal_JournalActivationDriverDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalActivationPathDto': d_Jube_Dto_Query_CaseKeyJournal_JournalActivationPathDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalActivationsMetaDto': d_Jube_Dto_Query_CaseKeyJournal_JournalActivationsMetaDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalAdaptationDto': d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalAdaptationModelDto': d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationModelDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalAdaptationResultDto': d_Jube_Dto_Query_CaseKeyJournal_JournalAdaptationResultDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalCalibrationBandDto': d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationBandDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalCalibrationDto': d_Jube_Dto_Query_CaseKeyJournal_JournalCalibrationDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalContributionDto': d_Jube_Dto_Query_CaseKeyJournal_JournalContributionDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalContributionItemDto': d_Jube_Dto_Query_CaseKeyJournal_JournalContributionItemDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalJourneyNodeDto': d_Jube_Dto_Query_CaseKeyJournal_JournalJourneyNodeDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalTransactionActivationDto': d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionActivationDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalTransactionKeyDto': d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionKeyDto,
  'Jube.Dto.Query.CaseKeyJournal.JournalTransactionValueDto': d_Jube_Dto_Query_CaseKeyJournal_JournalTransactionValueDto,
  'Jube.Dto.Query.CaseNoteByCaseKeyValue.CaseNoteByCaseKeyValueDto': d_Jube_Dto_Query_CaseNoteByCaseKeyValue_CaseNoteByCaseKeyValueDto,
  'Jube.Dto.Query.CaseSearch.CaseAttentionDto': d_Jube_Dto_Query_CaseSearch_CaseAttentionDto,
  'Jube.Dto.Query.CaseSearch.CaseAttentionReasonDto': d_Jube_Dto_Query_CaseSearch_CaseAttentionReasonDto,
  'Jube.Dto.Query.CaseSearch.CaseRiskMatrixCellDto': d_Jube_Dto_Query_CaseSearch_CaseRiskMatrixCellDto,
  'Jube.Dto.Query.CaseSearch.CaseRiskMatrixDto': d_Jube_Dto_Query_CaseSearch_CaseRiskMatrixDto,
  'Jube.Dto.Query.CaseSearch.CaseRiskSnapshotDto': d_Jube_Dto_Query_CaseSearch_CaseRiskSnapshotDto,
  'Jube.Dto.Query.CaseSearch.CaseSearchCountDto': d_Jube_Dto_Query_CaseSearch_CaseSearchCountDto,
  'Jube.Dto.Query.CaseSearch.CaseSearchPageDto': d_Jube_Dto_Query_CaseSearch_CaseSearchPageDto,
  'Jube.Dto.Query.CaseSearch.CaseSearchRequestDto': d_Jube_Dto_Query_CaseSearch_CaseSearchRequestDto,
  'Jube.Dto.Query.CaseSearch.CaseSearchRowDto': d_Jube_Dto_Query_CaseSearch_CaseSearchRowDto,
  'Jube.Dto.Query.CaseSearch.CaseServiceLevelDto': d_Jube_Dto_Query_CaseSearch_CaseServiceLevelDto,
  'Jube.Dto.Query.CaseSearch.CaseServiceLevelMeasureDto': d_Jube_Dto_Query_CaseSearch_CaseServiceLevelMeasureDto,
  'Jube.Dto.Query.CaseSearch.CaseSkimDto': d_Jube_Dto_Query_CaseSearch_CaseSkimDto,
  'Jube.Dto.Query.CaseSearch.CaseWorkflowChoiceDto': d_Jube_Dto_Query_CaseSearch_CaseWorkflowChoiceDto,
  'Jube.Dto.Query.CaseSimilarity.CaseSimilarNeighbourDto': d_Jube_Dto_Query_CaseSimilarity_CaseSimilarNeighbourDto,
  'Jube.Dto.Query.CaseSimilarity.CaseSimilarReasonDto': d_Jube_Dto_Query_CaseSimilarity_CaseSimilarReasonDto,
  'Jube.Dto.Query.CaseSimilarity.CaseSimilarityDto': d_Jube_Dto_Query_CaseSimilarity_CaseSimilarityDto,
  'Jube.Dto.Query.CaseSummary.CaseChecklistSummaryDto': d_Jube_Dto_Query_CaseSummary_CaseChecklistSummaryDto,
  'Jube.Dto.Query.CaseSummary.CaseContributionDto': d_Jube_Dto_Query_CaseSummary_CaseContributionDto,
  'Jube.Dto.Query.CaseSummary.CaseDeltaDto': d_Jube_Dto_Query_CaseSummary_CaseDeltaDto,
  'Jube.Dto.Query.CaseSummary.CaseEventSummaryDto': d_Jube_Dto_Query_CaseSummary_CaseEventSummaryDto,
  'Jube.Dto.Query.CaseSummary.CaseImpactComponentsDto': d_Jube_Dto_Query_CaseSummary_CaseImpactComponentsDto,
  'Jube.Dto.Query.CaseSummary.CaseKeyHistoryDto': d_Jube_Dto_Query_CaseSummary_CaseKeyHistoryDto,
  'Jube.Dto.Query.CaseSummary.CaseNoteSummaryDto': d_Jube_Dto_Query_CaseSummary_CaseNoteSummaryDto,
  'Jube.Dto.Query.CaseSummary.CaseRiskHistoryItemDto': d_Jube_Dto_Query_CaseSummary_CaseRiskHistoryItemDto,
  'Jube.Dto.Query.CaseSummary.CaseStayDto': d_Jube_Dto_Query_CaseSummary_CaseStayDto,
  'Jube.Dto.Query.CaseSummary.CaseSummaryDto': d_Jube_Dto_Query_CaseSummary_CaseSummaryDto,
  'Jube.Dto.Query.CaseSupervision.CaseAnalystWorkloadDto': d_Jube_Dto_Query_CaseSupervision_CaseAnalystWorkloadDto,
  'Jube.Dto.Query.CaseSupervision.CaseBreakLockDto': d_Jube_Dto_Query_CaseSupervision_CaseBreakLockDto,
  'Jube.Dto.Query.CaseSupervision.CaseLegalHoldDto': d_Jube_Dto_Query_CaseSupervision_CaseLegalHoldDto,
  'Jube.Dto.Query.CaseSupervision.CaseLegalHoldStateDto': d_Jube_Dto_Query_CaseSupervision_CaseLegalHoldStateDto,
  'Jube.Dto.Query.CaseSupervision.CaseReassignDto': d_Jube_Dto_Query_CaseSupervision_CaseReassignDto,
  'Jube.Dto.Query.CaseSupervision.CaseStuckLockDto': d_Jube_Dto_Query_CaseSupervision_CaseStuckLockDto,
  'Jube.Dto.Query.CaseSupervision.CaseSupervisionDto': d_Jube_Dto_Query_CaseSupervision_CaseSupervisionDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionCandidateTransactionDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionCandidateTransactionDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionInForceDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionInForceDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionKeyDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionKeyDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionLaneCountDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionLaneCountDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionReasonDraftDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionReasonDraftDto,
  'Jube.Dto.Query.CaseSuppressionCandidates.CaseSuppressionValueDto': d_Jube_Dto_Query_CaseSuppressionCandidates_CaseSuppressionValueDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowDispatchDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDispatchDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowDispatchesDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDispatchesDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowDisplayExecutionDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowDisplayExecutionDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormEntriesRenderDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntriesRenderDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormEntryValueDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntryValueDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormEntryViewDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormEntryViewDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormFieldDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormFieldDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormRenderDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormRenderDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowFormRenderResultDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowFormRenderResultDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowSqlColumnDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlColumnDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowSqlTestDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlTestDto,
  'Jube.Dto.Query.CaseWorkflowDisplayExecution.CaseWorkflowSqlTestResultDto': d_Jube_Dto_Query_CaseWorkflowDisplayExecution_CaseWorkflowSqlTestResultDto,
  'Jube.Dto.Query.CaseWorkflowFormEntryByCaseKeyValue.CaseWorkflowFormEntryByCaseKeyValueDto': d_Jube_Dto_Query_CaseWorkflowFormEntryByCaseKeyValue_CaseWorkflowFormEntryByCaseKeyValueDto,
  'Jube.Dto.Query.CaseWorkflowMacroExecution.CaseWorkflowMacroExecutionDto': d_Jube_Dto_Query_CaseWorkflowMacroExecution_CaseWorkflowMacroExecutionDto,
  'Jube.Dto.Query.Completions.CompletionDto': d_Jube_Dto_Query_Completions_CompletionDto,
  'Jube.Dto.Query.EntityAnalysisModelActivationRuleOverrideQuery.EntityAnalysisModelActivationRuleOverrideQueryDto': d_Jube_Dto_Query_EntityAnalysisModelActivationRuleOverrideQuery_EntityAnalysisModelActivationRuleOverrideQueryDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestExplainRequestDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestExplainRequestDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestExplanationDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestExplanationDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestFieldsDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestFieldsDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestInstanceDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestInstanceDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestRequestDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestRequestDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestResultDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestResultDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestSampleDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSampleDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestSubmitRequestDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSubmitRequestDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestSubmitResultDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestSubmitResultDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestTagCountDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTagCountDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestTuneRequestDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneRequestDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestTuneResultDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneResultDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestTuneVariantDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestTuneVariantDto,
  'Jube.Dto.Query.EntityAnalysisModelBacktest.BacktestValueDto': d_Jube_Dto_Query_EntityAnalysisModelBacktest_BacktestValueDto,
  'Jube.Dto.Query.EntityAnalysisModelDependency.DependencyImpactDto': d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyImpactDto,
  'Jube.Dto.Query.EntityAnalysisModelDependency.DependencyIssuesDto': d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyIssuesDto,
  'Jube.Dto.Query.EntityAnalysisModelDependency.DependencyListDto': d_Jube_Dto_Query_EntityAnalysisModelDependency_DependencyListDto,
  'Jube.Dto.Query.EntityAnalysisModelDependency.ModelDependencyDto': d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelDependencyDto,
  'Jube.Dto.Query.EntityAnalysisModelDependency.ModelEntityDto': d_Jube_Dto_Query_EntityAnalysisModelDependency_ModelEntityDto,
  'Jube.Dto.Query.EntityAnalysisModelHistorySimulation.AbstractionSimulationResultDto': d_Jube_Dto_Query_EntityAnalysisModelHistorySimulation_AbstractionSimulationResultDto,
  'Jube.Dto.Query.EntityAnalysisModelHistorySimulation.TtlCounterValueResultDto': d_Jube_Dto_Query_EntityAnalysisModelHistorySimulation_TtlCounterValueResultDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.EngineInstanceStateDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineInstanceStateDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.EngineLoadedCountDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedCountDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.EngineLoadedEntityDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineLoadedEntityDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.EngineNodeDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineNodeDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.EngineStateDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_EngineStateDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.IntegrityCheckDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_IntegrityCheckDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.IntegrityModelDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_IntegrityModelDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.ModelGraphDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.ModelGraphEdgeDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphEdgeDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.ModelGraphNodeDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelGraphNodeDto,
  'Jube.Dto.Query.EntityAnalysisModelIntegrity.ModelIntegrityReportDto': d_Jube_Dto_Query_EntityAnalysisModelIntegrity_ModelIntegrityReportDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextFromArchiveDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextFromArchiveDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextFromRequestJsonDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextFromRequestJsonDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextOverlayDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextOverlayDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextStageDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextStageDto,
  'Jube.Dto.Query.EntityAnalysisModelInvocationContext.InvocationContextValueDto': d_Jube_Dto_Query_EntityAnalysisModelInvocationContext_InvocationContextValueDto,
  'Jube.Dto.Query.EntityAnalysisModelOverrideQuery.EntityAnalysisModelOverrideKeyQueryDto': d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideKeyQueryDto,
  'Jube.Dto.Query.EntityAnalysisModelOverrideQuery.EntityAnalysisModelOverrideQueryDto': d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideQueryDto,
  'Jube.Dto.Query.EntityAnalysisModelOverrideQuery.EntityAnalysisModelOverrideValueQueryDto': d_Jube_Dto_Query_EntityAnalysisModelOverrideQuery_EntityAnalysisModelOverrideValueQueryDto,
  'Jube.Dto.Query.EntityAnalysisModelSample.EntityAnalysisModelSampleOptionsDto': d_Jube_Dto_Query_EntityAnalysisModelSample_EntityAnalysisModelSampleOptionsDto,
  'Jube.Dto.Query.EntityAnalysisModelSynchronisationNodeStatusEntries.EntityAnalysisModelSynchronisationNodeStatusEntriesDto': d_Jube_Dto_Query_EntityAnalysisModelSynchronisationNodeStatusEntries_EntityAnalysisModelSynchronisationNodeStatusEntriesDto,
  'Jube.Dto.Query.EntityAnalysisModelTimeWindow.TimeWindowDto': d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowDto,
  'Jube.Dto.Query.EntityAnalysisModelTimeWindow.TimeWindowIntervalDto': d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowIntervalDto,
  'Jube.Dto.Query.EntityAnalysisModelTimeWindow.TimeWindowKindDto': d_Jube_Dto_Query_EntityAnalysisModelTimeWindow_TimeWindowKindDto,
  'Jube.Dto.Query.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType.EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto': d_Jube_Dto_Query_EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataType_EntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeDto,
  'Jube.Dto.Query.ExhaustiveNarrative.ExhaustiveNarrativeDto': d_Jube_Dto_Query_ExhaustiveNarrative_ExhaustiveNarrativeDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceConfusion.ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceConfusion_ExhaustiveSearchInstancePromotedTrialInstanceConfusionDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram.ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogram_ExhaustiveSearchInstancePromotedTrialInstanceErrorHistogramDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve.ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceLearningCurve_ExhaustiveSearchInstancePromotedTrialInstanceLearningCurveDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstancePredictedActual.ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstancePredictedActual_ExhaustiveSearchInstancePromotedTrialInstancePredictedActualDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceQuery.ExhaustiveSearchInstancePromotedTrialInstanceQueryDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceQuery_ExhaustiveSearchInstancePromotedTrialInstanceQueryDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceRoc.ExhaustiveSearchInstancePromotedTrialInstanceRocDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceRoc_ExhaustiveSearchInstancePromotedTrialInstanceRocDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription.ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionHistogramValueDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescription_ExhaustiveSearchInstancePromotedTrialInstanceVariablePrescriptionHistogramValueDto,
  'Jube.Dto.Query.ExhaustiveSearchInstancePromotionHistory.ExhaustiveSearchInstancePromotionHistoryDto': d_Jube_Dto_Query_ExhaustiveSearchInstancePromotionHistory_ExhaustiveSearchInstancePromotionHistoryDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery.ExhaustiveSearchInstanceTrialInstanceDetailDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceListQuery_ExhaustiveSearchInstanceTrialInstanceDetailDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceListQuery.ExhaustiveSearchInstanceTrialInstanceListQueryDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceListQuery_ExhaustiveSearchInstanceTrialInstanceListQueryDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariable.ExhaustiveSearchInstanceTrialInstanceVariableDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceVariable_ExhaustiveSearchInstanceTrialInstanceVariableDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceTrialInstanceVariableVariance.ExhaustiveSearchInstanceTrialInstanceVariableVarianceDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceTrialInstanceVariableVariance_ExhaustiveSearchInstanceTrialInstanceVariableVarianceDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramValueDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableHistogramValueDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableReallocateDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariable_ExhaustiveSearchInstanceVariableReallocateDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics.ExhaustiveSearchInstanceVariableStatisticsBinDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsBinDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics.ExhaustiveSearchInstanceVariableStatisticsDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsDto,
  'Jube.Dto.Query.ExhaustiveSearchInstanceVariableStatistics.ExhaustiveSearchInstanceVariableStatisticsSummaryDto': d_Jube_Dto_Query_ExhaustiveSearchInstanceVariableStatistics_ExhaustiveSearchInstanceVariableStatisticsSummaryDto,
  'Jube.Dto.Query.FormModel.FormApprovalDto': d_Jube_Dto_Query_FormModel_FormApprovalDto,
  'Jube.Dto.Query.FormModel.FormChildDto': d_Jube_Dto_Query_FormModel_FormChildDto,
  'Jube.Dto.Query.FormModel.FormConditionDto': d_Jube_Dto_Query_FormModel_FormConditionDto,
  'Jube.Dto.Query.FormModel.FormEditorDto': d_Jube_Dto_Query_FormModel_FormEditorDto,
  'Jube.Dto.Query.FormModel.FormFieldDto': d_Jube_Dto_Query_FormModel_FormFieldDto,
  'Jube.Dto.Query.FormModel.FormGroupDto': d_Jube_Dto_Query_FormModel_FormGroupDto,
  'Jube.Dto.Query.FormModel.FormKeysDto': d_Jube_Dto_Query_FormModel_FormKeysDto,
  'Jube.Dto.Query.FormModel.FormListColumnDto': d_Jube_Dto_Query_FormModel_FormListColumnDto,
  'Jube.Dto.Query.FormModel.FormLookupDto': d_Jube_Dto_Query_FormModel_FormLookupDto,
  'Jube.Dto.Query.FormModel.FormModelDto': d_Jube_Dto_Query_FormModel_FormModelDto,
  'Jube.Dto.Query.FormModel.FormOptionDto': d_Jube_Dto_Query_FormModel_FormOptionDto,
  'Jube.Dto.Query.FormModel.FormParameterDto': d_Jube_Dto_Query_FormModel_FormParameterDto,
  'Jube.Dto.Query.FormModel.FormParameterSetDto': d_Jube_Dto_Query_FormModel_FormParameterSetDto,
  'Jube.Dto.Query.FormModel.FormRolesDto': d_Jube_Dto_Query_FormModel_FormRolesDto,
  'Jube.Dto.Query.FormModel.FormValueFieldDto': d_Jube_Dto_Query_FormModel_FormValueFieldDto,
  'Jube.Dto.Query.FormModel.FormValuesDto': d_Jube_Dto_Query_FormModel_FormValuesDto,
  'Jube.Dto.Query.Graph.GraphAnchorDto': d_Jube_Dto_Query_Graph_GraphAnchorDto,
  'Jube.Dto.Query.Graph.GraphCaptureRequestDto': d_Jube_Dto_Query_Graph_GraphCaptureRequestDto,
  'Jube.Dto.Query.Graph.GraphCaptureResultDto': d_Jube_Dto_Query_Graph_GraphCaptureResultDto,
  'Jube.Dto.Query.Graph.GraphComboDto': d_Jube_Dto_Query_Graph_GraphComboDto,
  'Jube.Dto.Query.Graph.GraphCommunityDto': d_Jube_Dto_Query_Graph_GraphCommunityDto,
  'Jube.Dto.Query.Graph.GraphEdgeDefinitionDto': d_Jube_Dto_Query_Graph_GraphEdgeDefinitionDto,
  'Jube.Dto.Query.Graph.GraphEdgeDto': d_Jube_Dto_Query_Graph_GraphEdgeDto,
  'Jube.Dto.Query.Graph.GraphEdgeTransactionDto': d_Jube_Dto_Query_Graph_GraphEdgeTransactionDto,
  'Jube.Dto.Query.Graph.GraphEdgeTransactionsDto': d_Jube_Dto_Query_Graph_GraphEdgeTransactionsDto,
  'Jube.Dto.Query.Graph.GraphEdgeTransactionsRequestDto': d_Jube_Dto_Query_Graph_GraphEdgeTransactionsRequestDto,
  'Jube.Dto.Query.Graph.GraphEntityMatchDecideDto': d_Jube_Dto_Query_Graph_GraphEntityMatchDecideDto,
  'Jube.Dto.Query.Graph.GraphEntityMatchDto': d_Jube_Dto_Query_Graph_GraphEntityMatchDto,
  'Jube.Dto.Query.Graph.GraphExplodeDto': d_Jube_Dto_Query_Graph_GraphExplodeDto,
  'Jube.Dto.Query.Graph.GraphExplodeLaneDto': d_Jube_Dto_Query_Graph_GraphExplodeLaneDto,
  'Jube.Dto.Query.Graph.GraphExplodeRequestDto': d_Jube_Dto_Query_Graph_GraphExplodeRequestDto,
  'Jube.Dto.Query.Graph.GraphExplorationDto': d_Jube_Dto_Query_Graph_GraphExplorationDto,
  'Jube.Dto.Query.Graph.GraphFlowDto': d_Jube_Dto_Query_Graph_GraphFlowDto,
  'Jube.Dto.Query.Graph.GraphFlowRequestDto': d_Jube_Dto_Query_Graph_GraphFlowRequestDto,
  'Jube.Dto.Query.Graph.GraphFlowTrancheDto': d_Jube_Dto_Query_Graph_GraphFlowTrancheDto,
  'Jube.Dto.Query.Graph.GraphHubOverrideRequestDto': d_Jube_Dto_Query_Graph_GraphHubOverrideRequestDto,
  'Jube.Dto.Query.Graph.GraphNeighbourhoodDto': d_Jube_Dto_Query_Graph_GraphNeighbourhoodDto,
  'Jube.Dto.Query.Graph.GraphNeighbourhoodRequestDto': d_Jube_Dto_Query_Graph_GraphNeighbourhoodRequestDto,
  'Jube.Dto.Query.Graph.GraphNodeCasesDto': d_Jube_Dto_Query_Graph_GraphNodeCasesDto,
  'Jube.Dto.Query.Graph.GraphNodeDefinitionCountDto': d_Jube_Dto_Query_Graph_GraphNodeDefinitionCountDto,
  'Jube.Dto.Query.Graph.GraphNodeDto': d_Jube_Dto_Query_Graph_GraphNodeDto,
  'Jube.Dto.Query.Graph.GraphNodeFactsDto': d_Jube_Dto_Query_Graph_GraphNodeFactsDto,
  'Jube.Dto.Query.Graph.GraphNodeRequestDto': d_Jube_Dto_Query_Graph_GraphNodeRequestDto,
  'Jube.Dto.Query.Graph.GraphNodeTagDto': d_Jube_Dto_Query_Graph_GraphNodeTagDto,
  'Jube.Dto.Query.Graph.GraphPathDto': d_Jube_Dto_Query_Graph_GraphPathDto,
  'Jube.Dto.Query.Graph.GraphPathsDto': d_Jube_Dto_Query_Graph_GraphPathsDto,
  'Jube.Dto.Query.Graph.GraphPathsRequestDto': d_Jube_Dto_Query_Graph_GraphPathsRequestDto,
  'Jube.Dto.Query.Graph.GraphPatternDecideRequestDto': d_Jube_Dto_Query_Graph_GraphPatternDecideRequestDto,
  'Jube.Dto.Query.Graph.GraphPatternDto': d_Jube_Dto_Query_Graph_GraphPatternDto,
  'Jube.Dto.Query.Graph.GraphPatternListRequestDto': d_Jube_Dto_Query_Graph_GraphPatternListRequestDto,
  'Jube.Dto.Query.Graph.GraphPatternMemberDto': d_Jube_Dto_Query_Graph_GraphPatternMemberDto,
  'Jube.Dto.Query.Graph.GraphRenderRequestDto': d_Jube_Dto_Query_Graph_GraphRenderRequestDto,
  'Jube.Dto.Query.Graph.GraphRenderResultDto': d_Jube_Dto_Query_Graph_GraphRenderResultDto,
  'Jube.Dto.Query.GridModel.GridColumnDto': d_Jube_Dto_Query_GridModel_GridColumnDto,
  'Jube.Dto.Query.GridModel.GridFilterDto': d_Jube_Dto_Query_GridModel_GridFilterDto,
  'Jube.Dto.Query.GridModel.GridModelDto': d_Jube_Dto_Query_GridModel_GridModelDto,
  'Jube.Dto.Query.Icons.IconDto': d_Jube_Dto_Query_Icons_IconDto,
  'Jube.Dto.Query.InboundQueue.InboundQueueDeadDto': d_Jube_Dto_Query_InboundQueue_InboundQueueDeadDto,
  'Jube.Dto.Query.InboundQueue.InboundQueueDepthDto': d_Jube_Dto_Query_InboundQueue_InboundQueueDepthDto,
  'Jube.Dto.Query.ModelGovernance.ModelGovernanceHistoryDto': d_Jube_Dto_Query_ModelGovernance_ModelGovernanceHistoryDto,
  'Jube.Dto.Query.ModelGovernance.ModelGovernanceInventoryRowDto': d_Jube_Dto_Query_ModelGovernance_ModelGovernanceInventoryRowDto,
  'Jube.Dto.Query.ModelGovernance.ModelGovernanceRecordDto': d_Jube_Dto_Query_ModelGovernance_ModelGovernanceRecordDto,
  'Jube.Dto.Query.Parser.ParseRuleErrorSpanDto': d_Jube_Dto_Query_Parser_ParseRuleErrorSpanDto,
  'Jube.Dto.Query.Parser.ParseRuleRequestDto': d_Jube_Dto_Query_Parser_ParseRuleRequestDto,
  'Jube.Dto.Query.Parser.ParseRuleResultDto': d_Jube_Dto_Query_Parser_ParseRuleResultDto,
  'Jube.Dto.Query.QueryBuilder.BuilderOperatorArgumentDto': d_Jube_Dto_Query_QueryBuilder_BuilderOperatorArgumentDto,
  'Jube.Dto.Query.QueryBuilder.BuilderOperatorDto': d_Jube_Dto_Query_QueryBuilder_BuilderOperatorDto,
  'Jube.Dto.Query.RuleVocabulary.RuleWordDto': d_Jube_Dto_Query_RuleVocabulary_RuleWordDto,
  'Jube.Dto.Query.RuleVocabulary.RuleWordOverloadDto': d_Jube_Dto_Query_RuleVocabulary_RuleWordOverloadDto,
  'Jube.Dto.Query.RuleVocabulary.RuleWordPageDto': d_Jube_Dto_Query_RuleVocabulary_RuleWordPageDto,
  'Jube.Dto.Query.RuleVocabulary.RuleWordParameterDto': d_Jube_Dto_Query_RuleVocabulary_RuleWordParameterDto,
  'Jube.Dto.Query.RuleVocabulary.RuleWordSummaryDto': d_Jube_Dto_Query_RuleVocabulary_RuleWordSummaryDto,
  'Jube.Dto.Query.Svirfd.SvirfdDriftItemDto': d_Jube_Dto_Query_Svirfd_SvirfdDriftItemDto,
  'Jube.Dto.Query.Svirfd.SvirfdLookupDto': d_Jube_Dto_Query_Svirfd_SvirfdLookupDto,
  'Jube.Dto.Query.Svirfd.SvirfdNarrativeGroupDto': d_Jube_Dto_Query_Svirfd_SvirfdNarrativeGroupDto,
  'Jube.Dto.Query.Svirfd.SvirfdStatusDto': d_Jube_Dto_Query_Svirfd_SvirfdStatusDto,
  'Jube.Dto.Query.TreeChildren.CasesWorkflowTreeChildDto': d_Jube_Dto_Query_TreeChildren_CasesWorkflowTreeChildDto,
  'Jube.Dto.Query.TreeChildren.EntityAnalysisModelTreeChildDto': d_Jube_Dto_Query_TreeChildren_EntityAnalysisModelTreeChildDto,
  'Jube.Dto.Query.TreeChildren.RoleRegistryTreeChildDto': d_Jube_Dto_Query_TreeChildren_RoleRegistryTreeChildDto,
  'Jube.Dto.Query.TreeChildren.VisualisationRegistryTreeChildDto': d_Jube_Dto_Query_TreeChildren_VisualisationRegistryTreeChildDto,
  'Jube.Dto.Query.VisualisationRegistrySummary.VisualisationRegistrySummaryDto': d_Jube_Dto_Query_VisualisationRegistrySummary_VisualisationRegistrySummaryDto,
  'Jube.Dto.Query.WireSchema.WireFieldDto': d_Jube_Dto_Query_WireSchema_WireFieldDto,
  'Jube.Dto.Query.WireSchema.WireRouteDto': d_Jube_Dto_Query_WireSchema_WireRouteDto,
  'Jube.Dto.Query.WireSchema.WireSchemaDto': d_Jube_Dto_Query_WireSchema_WireSchemaDto,
  'Jube.Dto.Query.WireSchema.WireTypeDto': d_Jube_Dto_Query_WireSchema_WireTypeDto,
  'Jube.Dto.Repository.ActivationWatcher.ActivationWatcherDto': d_Jube_Dto_Repository_ActivationWatcher_ActivationWatcherDto,
  'Jube.Dto.Repository.Archive.ArchiveTagDto': d_Jube_Dto_Repository_Archive_ArchiveTagDto,
  'Jube.Dto.Repository.Case.CaseDto': d_Jube_Dto_Repository_Case_CaseDto,
  'Jube.Dto.Repository.Case.CreateCaseDto': d_Jube_Dto_Repository_Case_CreateCaseDto,
  'Jube.Dto.Repository.CaseAgentReplay.CaseAgentReplayComparisonDto': d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayComparisonDto,
  'Jube.Dto.Repository.CaseAgentReplay.CaseAgentReplayDto': d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayDto,
  'Jube.Dto.Repository.CaseAgentReplay.CaseAgentReplayRequestDto': d_Jube_Dto_Repository_CaseAgentReplay_CaseAgentReplayRequestDto,
  'Jube.Dto.Repository.CaseAgentRun.CaseAgentRunDecisionDto': d_Jube_Dto_Repository_CaseAgentRun_CaseAgentRunDecisionDto,
  'Jube.Dto.Repository.CaseAgentRun.CaseAgentRunDto': d_Jube_Dto_Repository_CaseAgentRun_CaseAgentRunDto,
  'Jube.Dto.Repository.CaseAnalystAssessment.CaseAnalystAssessmentDto': d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentDto,
  'Jube.Dto.Repository.CaseAnalystAssessment.CaseAnalystAssessmentStateDto': d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentStateDto,
  'Jube.Dto.Repository.CaseAnalystAssessment.CaseAnalystAssessmentWithdrawDto': d_Jube_Dto_Repository_CaseAnalystAssessment_CaseAnalystAssessmentWithdrawDto,
  'Jube.Dto.Repository.CaseFile.CaseFileDto': d_Jube_Dto_Repository_CaseFile_CaseFileDto,
  'Jube.Dto.Repository.CaseFile.CaseFileUploadRequestDto': d_Jube_Dto_Repository_CaseFile_CaseFileUploadRequestDto,
  'Jube.Dto.Repository.CaseLink.CaseLinkDto': d_Jube_Dto_Repository_CaseLink_CaseLinkDto,
  'Jube.Dto.Repository.CaseLink.CaseLinkRequestDto': d_Jube_Dto_Repository_CaseLink_CaseLinkRequestDto,
  'Jube.Dto.Repository.CaseNote.CaseNoteDto': d_Jube_Dto_Repository_CaseNote_CaseNoteDto,
  'Jube.Dto.Repository.CaseNote.CaseNoteFindingRequestDto': d_Jube_Dto_Repository_CaseNote_CaseNoteFindingRequestDto,
  'Jube.Dto.Repository.CaseQaReview.CaseQaReviewDto': d_Jube_Dto_Repository_CaseQaReview_CaseQaReviewDto,
  'Jube.Dto.Repository.CaseQaReview.CaseQaReviewStateDto': d_Jube_Dto_Repository_CaseQaReview_CaseQaReviewStateDto,
  'Jube.Dto.Repository.CaseReport.CaseReportApprovalDto': d_Jube_Dto_Repository_CaseReport_CaseReportApprovalDto,
  'Jube.Dto.Repository.CaseReport.CaseReportDecisionDto': d_Jube_Dto_Repository_CaseReport_CaseReportDecisionDto,
  'Jube.Dto.Repository.CaseReport.CaseReportDto': d_Jube_Dto_Repository_CaseReport_CaseReportDto,
  'Jube.Dto.Repository.CaseReport.CaseReportFeedbackDto': d_Jube_Dto_Repository_CaseReport_CaseReportFeedbackDto,
  'Jube.Dto.Repository.CaseReport.CaseReportFeedbackRequestDto': d_Jube_Dto_Repository_CaseReport_CaseReportFeedbackRequestDto,
  'Jube.Dto.Repository.CaseReport.CaseReportIssueDto': d_Jube_Dto_Repository_CaseReport_CaseReportIssueDto,
  'Jube.Dto.Repository.CaseReport.CaseReportProfileDto': d_Jube_Dto_Repository_CaseReport_CaseReportProfileDto,
  'Jube.Dto.Repository.CaseReport.CaseReportProfileResultDto': d_Jube_Dto_Repository_CaseReport_CaseReportProfileResultDto,
  'Jube.Dto.Repository.CaseReport.CaseReportProfileSummaryDto': d_Jube_Dto_Repository_CaseReport_CaseReportProfileSummaryDto,
  'Jube.Dto.Repository.CaseReport.CaseReportSaveDto': d_Jube_Dto_Repository_CaseReport_CaseReportSaveDto,
  'Jube.Dto.Repository.CaseReport.CaseReportStartDto': d_Jube_Dto_Repository_CaseReport_CaseReportStartDto,
  'Jube.Dto.Repository.CaseReport.CaseReportSubmissionDto': d_Jube_Dto_Repository_CaseReport_CaseReportSubmissionDto,
  'Jube.Dto.Repository.CaseReport.CaseReportVersionDto': d_Jube_Dto_Repository_CaseReport_CaseReportVersionDto,
  'Jube.Dto.Repository.CaseResearch.CaseResearchDto': d_Jube_Dto_Repository_CaseResearch_CaseResearchDto,
  'Jube.Dto.Repository.CaseResearch.CaseResearchSourceDto': d_Jube_Dto_Repository_CaseResearch_CaseResearchSourceDto,
  'Jube.Dto.Repository.CaseSearchFilter.CaseSearchFilterDto': d_Jube_Dto_Repository_CaseSearchFilter_CaseSearchFilterDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionApprovalDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionApprovalDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionCountsDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionCountsDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionDecisionDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionDecisionDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionRequestDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionRequestDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionTargetDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionTargetRequestDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTargetRequestDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionTransactionDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionTransactionDto,
  'Jube.Dto.Repository.CaseSuppression.CaseSuppressionVersionDto': d_Jube_Dto_Repository_CaseSuppression_CaseSuppressionVersionDto,
  'Jube.Dto.Repository.CaseTag.CaseTagDto': d_Jube_Dto_Repository_CaseTag_CaseTagDto,
  'Jube.Dto.Repository.CaseTag.CaseTagMergeRequestDto': d_Jube_Dto_Repository_CaseTag_CaseTagMergeRequestDto,
  'Jube.Dto.Repository.CaseWorkflow.CaseWorkflowDto': d_Jube_Dto_Repository_CaseWorkflow_CaseWorkflowDto,
  'Jube.Dto.Repository.CaseWorkflowAction.CaseWorkflowActionDto': d_Jube_Dto_Repository_CaseWorkflowAction_CaseWorkflowActionDto,
  'Jube.Dto.Repository.CaseWorkflowActionRole.CaseWorkflowActionRoleDto': d_Jube_Dto_Repository_CaseWorkflowActionRole_CaseWorkflowActionRoleDto,
  'Jube.Dto.Repository.CaseWorkflowChecklist.CaseWorkflowChecklistDto': d_Jube_Dto_Repository_CaseWorkflowChecklist_CaseWorkflowChecklistDto,
  'Jube.Dto.Repository.CaseWorkflowChecklistItem.CaseWorkflowChecklistItemDto': d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemDto,
  'Jube.Dto.Repository.CaseWorkflowChecklistItem.CaseWorkflowChecklistItemTestDto': d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemTestDto,
  'Jube.Dto.Repository.CaseWorkflowChecklistItem.CaseWorkflowChecklistItemTestResultDto': d_Jube_Dto_Repository_CaseWorkflowChecklistItem_CaseWorkflowChecklistItemTestResultDto,
  'Jube.Dto.Repository.CaseWorkflowDisplay.CaseWorkflowDisplayDto': d_Jube_Dto_Repository_CaseWorkflowDisplay_CaseWorkflowDisplayDto,
  'Jube.Dto.Repository.CaseWorkflowDisplayRole.CaseWorkflowDisplayRoleDto': d_Jube_Dto_Repository_CaseWorkflowDisplayRole_CaseWorkflowDisplayRoleDto,
  'Jube.Dto.Repository.CaseWorkflowFilter.CaseWorkflowFilterDto': d_Jube_Dto_Repository_CaseWorkflowFilter_CaseWorkflowFilterDto,
  'Jube.Dto.Repository.CaseWorkflowFilterRole.CaseWorkflowFilterRoleDto': d_Jube_Dto_Repository_CaseWorkflowFilterRole_CaseWorkflowFilterRoleDto,
  'Jube.Dto.Repository.CaseWorkflowForm.CaseWorkflowFormDto': d_Jube_Dto_Repository_CaseWorkflowForm_CaseWorkflowFormDto,
  'Jube.Dto.Repository.CaseWorkflowFormEntry.CaseWorkflowFormEntryDto': d_Jube_Dto_Repository_CaseWorkflowFormEntry_CaseWorkflowFormEntryDto,
  'Jube.Dto.Repository.CaseWorkflowFormEntryValue.CaseWorkflowFormEntryValueDto': d_Jube_Dto_Repository_CaseWorkflowFormEntryValue_CaseWorkflowFormEntryValueDto,
  'Jube.Dto.Repository.CaseWorkflowFormRole.CaseWorkflowFormRoleDto': d_Jube_Dto_Repository_CaseWorkflowFormRole_CaseWorkflowFormRoleDto,
  'Jube.Dto.Repository.CaseWorkflowFormValidator.CaseWorkflowFormValidatorDto': d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorDto,
  'Jube.Dto.Repository.CaseWorkflowFormValidator.CaseWorkflowFormValidatorTestDto': d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorTestDto,
  'Jube.Dto.Repository.CaseWorkflowFormValidator.CaseWorkflowFormValidatorTestResultDto': d_Jube_Dto_Repository_CaseWorkflowFormValidator_CaseWorkflowFormValidatorTestResultDto,
  'Jube.Dto.Repository.CaseWorkflowMacro.CaseWorkflowMacroDto': d_Jube_Dto_Repository_CaseWorkflowMacro_CaseWorkflowMacroDto,
  'Jube.Dto.Repository.CaseWorkflowMacroRole.CaseWorkflowMacroRoleDto': d_Jube_Dto_Repository_CaseWorkflowMacroRole_CaseWorkflowMacroRoleDto,
  'Jube.Dto.Repository.CaseWorkflowPriority.CaseWorkflowPriorityDto': d_Jube_Dto_Repository_CaseWorkflowPriority_CaseWorkflowPriorityDto,
  'Jube.Dto.Repository.CaseWorkflowReleaseHook.CaseWorkflowReleaseHookDto': d_Jube_Dto_Repository_CaseWorkflowReleaseHook_CaseWorkflowReleaseHookDto,
  'Jube.Dto.Repository.CaseWorkflowRole.CaseWorkflowRoleDto': d_Jube_Dto_Repository_CaseWorkflowRole_CaseWorkflowRoleDto,
  'Jube.Dto.Repository.CaseWorkflowStatus.CaseWorkflowStatusDto': d_Jube_Dto_Repository_CaseWorkflowStatus_CaseWorkflowStatusDto,
  'Jube.Dto.Repository.CaseWorkflowStatusRole.CaseWorkflowStatusRoleDto': d_Jube_Dto_Repository_CaseWorkflowStatusRole_CaseWorkflowStatusRoleDto,
  'Jube.Dto.Repository.CaseWorkflowXPath.CaseWorkflowXPathDto': d_Jube_Dto_Repository_CaseWorkflowXPath_CaseWorkflowXPathDto,
  'Jube.Dto.Repository.CaseWorkflowXPath.CaseWorkflowXPathFieldDto': d_Jube_Dto_Repository_CaseWorkflowXPath_CaseWorkflowXPathFieldDto,
  'Jube.Dto.Repository.CaseWorkflowXPathRole.CaseWorkflowXPathRoleDto': d_Jube_Dto_Repository_CaseWorkflowXPathRole_CaseWorkflowXPathRoleDto,
  'Jube.Dto.Repository.EntityAnalysisModelGraphRecall.EntityAnalysisModelGraphRecallDto': d_Jube_Dto_Repository_EntityAnalysisModelGraphRecall_EntityAnalysisModelGraphRecallDto,
  'Jube.Dto.Repository.EntityAnalysisModelGraphRecall.EntityAnalysisModelGraphRecallFieldDto': d_Jube_Dto_Repository_EntityAnalysisModelGraphRecall_EntityAnalysisModelGraphRecallFieldDto,
  'Jube.Dto.Repository.EntityAnalysisModelRole.EntityAnalysisModelRoleDto': d_Jube_Dto_Repository_EntityAnalysisModelRole_EntityAnalysisModelRoleDto,
  'Jube.Dto.Repository.EntityAnalysisModelSvirfd.EntityAnalysisModelSvirfdDto': d_Jube_Dto_Repository_EntityAnalysisModelSvirfd_EntityAnalysisModelSvirfdDto,
  'Jube.Dto.Repository.EntityAnalysisModelSvirfd.EntityAnalysisModelSvirfdFieldDto': d_Jube_Dto_Repository_EntityAnalysisModelSvirfd_EntityAnalysisModelSvirfdFieldDto,
  'Jube.Dto.Repository.EntityAnalysisModelSynchronisationSchedule.EntityAnalysisModelSynchronisationScheduleDto': d_Jube_Dto_Repository_EntityAnalysisModelSynchronisationSchedule_EntityAnalysisModelSynchronisationScheduleDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingDecisionDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDecisionDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingHistoryDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingHistoryDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingImportDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingImportDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingImportResultDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingImportResultDto,
  'Jube.Dto.Repository.EntityRiskRating.EntityRiskRatingRequestDto': d_Jube_Dto_Repository_EntityRiskRating_EntityRiskRatingRequestDto,
  'Jube.Dto.Repository.Erasure.ErasureDecisionDto': d_Jube_Dto_Repository_Erasure_ErasureDecisionDto,
  'Jube.Dto.Repository.Erasure.ErasurePreviewDto': d_Jube_Dto_Repository_Erasure_ErasurePreviewDto,
  'Jube.Dto.Repository.Erasure.ErasureRequestCreateDto': d_Jube_Dto_Repository_Erasure_ErasureRequestCreateDto,
  'Jube.Dto.Repository.Erasure.ErasureRequestDto': d_Jube_Dto_Repository_Erasure_ErasureRequestDto,
  'Jube.Dto.Repository.Erasure.ErasureSubjectDto': d_Jube_Dto_Repository_Erasure_ErasureSubjectDto,
  'Jube.Dto.Repository.Erasure.SubjectExportDto': d_Jube_Dto_Repository_Erasure_SubjectExportDto,
  'Jube.Dto.Repository.Erasure.SubjectExportTransactionDto': d_Jube_Dto_Repository_Erasure_SubjectExportTransactionDto,
  'Jube.Dto.Repository.ExhaustiveSearchInstancePromotedTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceDto': d_Jube_Dto_Repository_ExhaustiveSearchInstancePromotedTrialInstance_ExhaustiveSearchInstancePromotedTrialInstanceDto,
  'Jube.Dto.Repository.GraphRegistry.GraphRegistryDto': d_Jube_Dto_Repository_GraphRegistry_GraphRegistryDto,
  'Jube.Dto.Repository.GraphRegistryEdge.GraphRegistryEdgeDto': d_Jube_Dto_Repository_GraphRegistryEdge_GraphRegistryEdgeDto,
  'Jube.Dto.Repository.GraphRegistryEdge.GraphRegistryEdgeKeyDto': d_Jube_Dto_Repository_GraphRegistryEdge_GraphRegistryEdgeKeyDto,
  'Jube.Dto.Repository.GraphRegistryRole.GraphRegistryRoleDto': d_Jube_Dto_Repository_GraphRegistryRole_GraphRegistryRoleDto,
  'Jube.Dto.Repository.PermissionSpecification.PermissionSpecificationDto': d_Jube_Dto_Repository_PermissionSpecification_PermissionSpecificationDto,
  'Jube.Dto.Repository.Preservation.ImportExportOptionsDto': d_Jube_Dto_Repository_Preservation_ImportExportOptionsDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffChangeDto': d_Jube_Dto_Repository_Preservation_PreservationDiffChangeDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffDto': d_Jube_Dto_Repository_Preservation_PreservationDiffDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffEntryDto': d_Jube_Dto_Repository_Preservation_PreservationDiffEntryDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffRequestDto': d_Jube_Dto_Repository_Preservation_PreservationDiffRequestDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffSectionDto': d_Jube_Dto_Repository_Preservation_PreservationDiffSectionDto,
  'Jube.Dto.Repository.Preservation.PreservationDiffSideDto': d_Jube_Dto_Repository_Preservation_PreservationDiffSideDto,
  'Jube.Dto.Repository.Preservation.PreservationImportPreviewDto': d_Jube_Dto_Repository_Preservation_PreservationImportPreviewDto,
  'Jube.Dto.Repository.Preservation.PreservationImportRequestDto': d_Jube_Dto_Repository_Preservation_PreservationImportRequestDto,
  'Jube.Dto.Repository.Preservation.PreservationImportWarningDto': d_Jube_Dto_Repository_Preservation_PreservationImportWarningDto,
  'Jube.Dto.Repository.Preservation.PreservationOrphanedUsersDto': d_Jube_Dto_Repository_Preservation_PreservationOrphanedUsersDto,
  'Jube.Dto.Repository.Preservation.PreservationSummaryDto': d_Jube_Dto_Repository_Preservation_PreservationSummaryDto,
  'Jube.Dto.Repository.Preservation.PreservationSummarySectionDto': d_Jube_Dto_Repository_Preservation_PreservationSummarySectionDto,
  'Jube.Dto.Repository.PreservationSnapshot.PreservationSnapshotDto': d_Jube_Dto_Repository_PreservationSnapshot_PreservationSnapshotDto,
  'Jube.Dto.Repository.PreservationSnapshot.PreservationSnapshotRequestDto': d_Jube_Dto_Repository_PreservationSnapshot_PreservationSnapshotRequestDto,
  'Jube.Dto.Repository.RoleRegistry.RoleRegistryDto': d_Jube_Dto_Repository_RoleRegistry_RoleRegistryDto,
  'Jube.Dto.Repository.RoleRegistryPermission.RoleRegistryPermissionDto': d_Jube_Dto_Repository_RoleRegistryPermission_RoleRegistryPermissionDto,
  'Jube.Dto.Repository.SanctionEntrySource.SanctionEntrySourceDto': d_Jube_Dto_Repository_SanctionEntrySource_SanctionEntrySourceDto,
  'Jube.Dto.Repository.SanctionExemption.SanctionExemptionDecisionDto': d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionDecisionDto,
  'Jube.Dto.Repository.SanctionExemption.SanctionExemptionDto': d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionDto,
  'Jube.Dto.Repository.SanctionExemption.SanctionExemptionRequestDto': d_Jube_Dto_Repository_SanctionExemption_SanctionExemptionRequestDto,
  'Jube.Dto.Repository.SessionCaseJournal.SessionCaseJournalDto': d_Jube_Dto_Repository_SessionCaseJournal_SessionCaseJournalDto,
  'Jube.Dto.Repository.SessionCaseSearchCompiledSql.SessionCaseSearchCompiledSqlDto': d_Jube_Dto_Repository_SessionCaseSearchCompiledSql_SessionCaseSearchCompiledSqlDto,
  'Jube.Dto.Repository.TenantRegistry.TenantRegistryDto': d_Jube_Dto_Repository_TenantRegistry_TenantRegistryDto,
  'Jube.Dto.Repository.UserInTenant.UserInTenantDto': d_Jube_Dto_Repository_UserInTenant_UserInTenantDto,
  'Jube.Dto.Repository.UserRegistry.UserRegistryDto': d_Jube_Dto_Repository_UserRegistry_UserRegistryDto,
  'Jube.Dto.Repository.UserRegistry.UserRegistryPasswordResetDto': d_Jube_Dto_Repository_UserRegistry_UserRegistryPasswordResetDto,
  'Jube.Dto.Repository.UserRegistry.UserRegistryPasswordResponseDto': d_Jube_Dto_Repository_UserRegistry_UserRegistryPasswordResponseDto,
  'Jube.Dto.Repository.UserRegistryApiKey.UserRegistryApiKeyDto': d_Jube_Dto_Repository_UserRegistryApiKey_UserRegistryApiKeyDto,
  'Jube.Dto.Repository.VisualisationRegistry.VisualisationRegistryDto': d_Jube_Dto_Repository_VisualisationRegistry_VisualisationRegistryDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardDisplayDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardDisplayDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardGridColumnDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridColumnDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardGridDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardGridDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardParameterDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardParameterDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardRequestDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardRequestDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceCardStyleDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceCardStyleDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceChartKindDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceChartKindDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceChartRoleDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceChartRoleDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceKindFitDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceKindFitDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceMappingEntryDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceMappingEntryDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourcePreviewRequestDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourcePreviewRequestDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.DatasourceResolvedRoleDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_DatasourceResolvedRoleDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceIntrospectionColumnDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionColumnDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceIntrospectionDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceIntrospectionRequestDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceIntrospectionRequestDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceSqlCheckDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceSqlCheckDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceSqlFindingDto': d_Jube_Dto_Repository_VisualisationRegistryDatasource_VisualisationRegistryDatasourceSqlFindingDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasourceRole.VisualisationRegistryDatasourceRoleDto': d_Jube_Dto_Repository_VisualisationRegistryDatasourceRole_VisualisationRegistryDatasourceRoleDto,
  'Jube.Dto.Repository.VisualisationRegistryDatasourceSeries.VisualisationRegistryDatasourceSeriesDto': d_Jube_Dto_Repository_VisualisationRegistryDatasourceSeries_VisualisationRegistryDatasourceSeriesDto,
  'Jube.Dto.Repository.VisualisationRegistryParameter.VisualisationRegistryParameterDto': d_Jube_Dto_Repository_VisualisationRegistryParameter_VisualisationRegistryParameterDto,
  'Jube.Dto.Repository.VisualisationRegistryParameterRole.VisualisationRegistryParameterRoleDto': d_Jube_Dto_Repository_VisualisationRegistryParameterRole_VisualisationRegistryParameterRoleDto,
  'Jube.Dto.Repository.VisualisationRegistryRole.VisualisationRegistryRoleDto': d_Jube_Dto_Repository_VisualisationRegistryRole_VisualisationRegistryRoleDto,
  'Jube.Dto.RespCallCounter.RespCallCounterDto': d_Jube_Dto_RespCallCounter_RespCallCounterDto,
  'Jube.Dto.RespConnectionEvent.RespConnectionEventDto': d_Jube_Dto_RespConnectionEvent_RespConnectionEventDto,
  'Jube.Dto.RespConnectionMultiplexerMetric.RespConnectionMultiplexerMetricDto': d_Jube_Dto_RespConnectionMultiplexerMetric_RespConnectionMultiplexerMetricDto,
  'Jube.Dto.RespKeyActivity.RespKeyActivityDto': d_Jube_Dto_RespKeyActivity_RespKeyActivityDto,
  'Jube.Dto.RespMetric.RespMetricDto': d_Jube_Dto_RespMetric_RespMetricDto,
  'Jube.Dto.RespSentinelEvent.RespSentinelEventDto': d_Jube_Dto_RespSentinelEvent_RespSentinelEventDto,
  'Jube.Dto.RespSentinelStatus.RespSentinelStatusDto': d_Jube_Dto_RespSentinelStatus_RespSentinelStatusDto,
  'Jube.Dto.RespSlowOperation.RespSlowOperationDto': d_Jube_Dto_RespSlowOperation_RespSlowOperationDto,
  'Jube.Dto.RuleExecution.BuilderRuleResultDto': d_Jube_Dto_RuleExecution_BuilderRuleResultDto,
  'Jube.Dto.RuleExecution.InlineScriptExecutionResultDto': d_Jube_Dto_RuleExecution_InlineScriptExecutionResultDto,
  'Jube.Dto.RuleExecution.RuleExecutionResultDto': d_Jube_Dto_RuleExecution_RuleExecutionResultDto,
  'Jube.Dto.UserLogin.UserLoginDto': d_Jube_Dto_UserLogin_UserLoginDto,
  'Jube.Dto.UserLogout.UserLogoutDto': d_Jube_Dto_UserLogout_UserLogoutDto,
  'Jube.Dto.UserNotification.UserNotificationDto': d_Jube_Dto_UserNotification_UserNotificationDto,
  'Jube.Dto.Validation.ValidationErrorDto': d_Jube_Dto_Validation_ValidationErrorDto,
  'Jube.Dto.Validation.ValidationResultDto': d_Jube_Dto_Validation_ValidationResultDto,
  'Jube.Dto.WafAttack.WafAttackDto': d_Jube_Dto_WafAttack_WafAttackDto,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticCalibrationInfo': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCalibrationInfo,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticCoefficient': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticCoefficient,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticFitInfo': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticFitInfo,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticPriors': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticPriors,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticReliabilityBin': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticReliabilityBin,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticSplitMetrics': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitMetrics,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticSplitSizes': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticSplitSizes,
  'Jube.Engine.AdvancedAnalytics.Regression.Models.LogisticWarning': d_Jube_Engine_AdvancedAnalytics_Regression_Models_LogisticWarning,
  'Jube.Service.Charts.Models.ExhaustiveStatisticsChartGridItem': d_Jube_Service_Charts_Models_ExhaustiveStatisticsChartGridItem,
  'Jube.Service.Query.CaseAnalytics.CaseAnalyticsDto': d_Jube_Service_Query_CaseAnalytics_CaseAnalyticsDto,
  'Jube.Service.Query.CaseKeyJournal.CaseKeyJournalAnalyticsDto': d_Jube_Service_Query_CaseKeyJournal_CaseKeyJournalAnalyticsDto,
  'Jube.Service.Query.CaseKeyJournal.JournalAnalyticsFieldDto': d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsFieldDto,
  'Jube.Service.Query.CaseKeyJournal.JournalAnalyticsMetaDto': d_Jube_Service_Query_CaseKeyJournal_JournalAnalyticsMetaDto,
  'Jube.Service.Query.CaseKeyJournal.JournalPeersDto': d_Jube_Service_Query_CaseKeyJournal_JournalPeersDto,
  'Jube.Service.Repository.AskJoobyAnalyticsInstance.Models.AskJoobyAnalyticsInstanceDto': d_Jube_Service_Repository_AskJoobyAnalyticsInstance_Models_AskJoobyAnalyticsInstanceDto,
  'Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsMetricComparisonDto': d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsMetricComparisonDto,
  'Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsPromotionEvidenceDto': d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsPromotionEvidenceDto,
  'Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AdvancedAnalyticsVersionComparisonDto': d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AdvancedAnalyticsVersionComparisonDto,
  'Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AskJoobyAnalyticsModelVersionDto': d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsModelVersionDto,
  'Jube.Service.Repository.AskJoobyAnalyticsModel.Models.AskJoobyAnalyticsTrainedModelDto': d_Jube_Service_Repository_AskJoobyAnalyticsModel_Models_AskJoobyAnalyticsTrainedModelDto,
}
