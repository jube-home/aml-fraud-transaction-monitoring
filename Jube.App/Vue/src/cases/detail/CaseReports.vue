<!-- Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 -->

<script setup vapor lang="ts">
import {computed, reactive, ref, watch} from 'vue'
import {useI18n} from 'vue-i18n'
import {useMutation, useQuery, useQueryClient} from '@tanstack/vue-query'
import {FileText} from 'lucide-vue-next'
import DecideCard from './DecideCard.vue'
import {meQueryOptions} from '../../auth/me'
import {useToasts} from '../../stores/toasts'
import {
  isEditable,
  isValidJsonObject,
  maximumMessage,
  maximumNote,
  maximumReference,
  mayDownload,
  mayWithdraw,
  reportPermission,
  reportsApi,
  reportsKey,
  reportState,
  type CaseReport,
} from './reportsApi'

const props = defineProps<{ caseId: number }>()

const {t} = useI18n()
const toasts = useToasts()
const client = useQueryClient()
const me = useQuery(meQueryOptions)

const held = computed(() =>
  Array.isArray(me.data.value?.permissions) ? me.data.value.permissions : [],
)
const canView = computed(() =>
  [reportPermission.file, reportPermission.approve, reportPermission.view].some((id) =>
    held.value.includes(id),
  ),
)
const canFile = computed(() => held.value.includes(reportPermission.file))
const canApprove = computed(() => held.value.includes(reportPermission.approve))

const reports = useQuery({
  queryKey: computed(() => reportsKey(props.caseId)),
  queryFn: () => reportsApi.list(props.caseId),
  enabled: canView,
  staleTime: 15_000,
  retry: false,
})

const profiles = useQuery({
  queryKey: ['service', 'CaseReportProfile', 'list'] as const,
  queryFn: () => reportsApi.profiles(),
  enabled: canFile,
  staleTime: 60_000,
  retry: false,
})

const profileName = ref('')
const reportCode = ref('')
const fields = reactive<Record<string, string>>({})
const notes = reactive<Record<string, string>>({})
const references = reactive<Record<string, string>>({})
const kinds = reactive<Record<string, number>>({})
const messages = reactive<Record<string, string>>({})
const paths = reactive<Record<string, string>>({})

watch(
  () => reports.data.value,
  (list) => {
    for (const report of list ?? []) fields[report.guid] = report.fieldsJson
  },
  {immediate: true},
)

watch(
  () => profiles.data.value,
  (list) => {
    if (!profileName.value && list && list.length > 0) profileName.value = list[0]!.name
  },
  {immediate: true},
)

function refresh(): void {
  void client.invalidateQueries({queryKey: ['service', 'CaseReport']})
}

function run(work: () => Promise<CaseReport>) {
  return useMutation({
    mutationFn: work,
    onSuccess: () => {
      toasts.success(t('caseDetail.reports.recorded'))
      refresh()
    },
    onError: (error) => toasts.apiError(error),
  })
}

const start = useMutation({
  mutationFn: () => reportsApi.start(props.caseId, profileName.value, reportCode.value.trim()),
  onSuccess: () => {
    reportCode.value = ''
    toasts.success(t('caseDetail.reports.recorded'))
    refresh()
  },
  onError: (error) => toasts.apiError(error),
})

const step = ref<(() => Promise<CaseReport>) | null>(null)
const stepper = run(() => (step.value ? step.value() : Promise.reject(new Error('none'))))

function go(work: () => Promise<CaseReport>): void {
  step.value = work
  stepper.mutate()
}

const stateKeys = [1, 2, 3, 4, 5, 6, 7, 8]

function save(report: CaseReport): void {
  go(() => reportsApi.save(report.guid, fields[report.guid] ?? '{}'))
}

function validate(report: CaseReport): void {
  go(() => reportsApi.validate(report.guid))
}

function requestApproval(report: CaseReport): void {
  go(() => reportsApi.requestApproval(report.guid))
}

function approve(report: CaseReport): void {
  go(() => reportsApi.approve(report.guid, notes[report.guid]?.trim() || null))
}

function reject(report: CaseReport): void {
  go(() => reportsApi.reject(report.guid, (notes[report.guid] ?? '').trim()))
}

function recordSubmission(report: CaseReport): void {
  go(() => reportsApi.recordSubmission(report.guid, (references[report.guid] ?? '').trim()))
}

function recordFeedback(report: CaseReport): void {
  go(() =>
    reportsApi.recordFeedback(
      report.guid,
      kinds[report.guid] ?? 1,
      messages[report.guid]?.trim() || null,
      paths[report.guid]?.trim() || null,
    ),
  )
}

function newVersion(report: CaseReport): void {
  go(() => reportsApi.newVersion(report.guid))
}

function withdraw(report: CaseReport): void {
  go(() => reportsApi.withdraw(report.guid))
}
</script>

<template>
  <DecideCard v-if="canView"
    :title="t('caseDetail.reports.title')"
    :help="t('caseDetail.reports.hint')"
    testid="case-reports"
  >
    <template #icon>
      <FileText class="size-5"/>
    </template>

    <p v-if="reports.isError.value" role="alert" class="text-sm">
      {{ t('caseDetail.reports.failed') }}
    </p>
    <p
      v-else-if="(reports.data.value ?? []).length === 0"
      class="text-sm text-base-content/70"
      data-testid="case-reports-empty"
    >
      {{ t('caseDetail.reports.none') }}
    </p>

    <div
      v-for="report in reports.data.value ?? []"
      :key="report.guid"
      class="border-base-300 flex flex-col gap-2 border-b py-2"
      :data-testid="`case-report-${report.guid}`"
    >
      <div class="flex items-center gap-2 text-sm">
        <span class="font-semibold">{{ report.reportCode }}</span>
        <span class="text-base-content/70"
        >{{ report.profileName }} ({{ report.profileVersion }})</span
        >
        <span class="badge badge-sm" :data-testid="`case-report-state-${report.guid}`">{{
            stateKeys.includes(report.stateId) ? t(`caseDetail.reports.states.${report.stateId}`) : ''
          }}</span>
        <span v-if="report.fiuReference" class="text-base-content/70">{{
            report.fiuReference
          }}</span>
      </div>

      <template v-if="isEditable(report) && canFile">
        <textarea
          v-model="fields[report.guid]"
          class="textarea textarea-sm w-full font-mono"
          rows="5"
          :aria-label="t('caseDetail.reports.fields')"
          :data-testid="`case-report-fields-${report.guid}`"
        ></textarea>
        <div class="flex flex-wrap gap-2">
          <button
            type="button"
            class="btn btn-sm"
            :disabled="!isValidJsonObject(fields[report.guid] ?? '')"
            :data-testid="`case-report-save-${report.guid}`"
            @click="save(report)"
          >
            {{ t('caseDetail.reports.save') }}
          </button>
          <button
            v-if="report.stateId === reportState.draft"
            type="button"
            class="btn btn-sm btn-primary"
            :data-testid="`case-report-validate-${report.guid}`"
            @click="validate(report)"
          >
            {{ t('caseDetail.reports.validate') }}
          </button>
          <button
            v-if="report.stateId === reportState.validated"
            type="button"
            class="btn btn-sm btn-primary"
            :data-testid="`case-report-request-${report.guid}`"
            @click="requestApproval(report)"
          >
            {{ t('caseDetail.reports.requestApproval') }}
          </button>
        </div>
      </template>

      <ul
        v-if="report.issues.length > 0 && report.stateId === reportState.draft"
        class="text-error list-disc pl-5 text-sm"
        :aria-label="t('caseDetail.reports.issues')"
        :data-testid="`case-report-issues-${report.guid}`"
      >
        <li v-for="(issue, index) in report.issues" :key="index">
          {{ issue.message }}
          <span v-if="issue.path" class="font-mono text-xs"> ({{ issue.path }})</span>
        </li>
      </ul>

      <div v-if="report.stateId === reportState.pendingApproval && canApprove" class="join w-full">
        <input
          v-model="notes[report.guid]"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumNote"
          :placeholder="t('caseDetail.reports.rejectReason')"
          :aria-label="t('caseDetail.reports.rejectReason')"
          :data-testid="`case-report-note-${report.guid}`"
        />
        <button
          type="button"
          class="btn btn-sm btn-primary join-item"
          :data-testid="`case-report-approve-${report.guid}`"
          @click="approve(report)"
        >
          {{ t('caseDetail.reports.approve') }}
        </button>
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="!(notes[report.guid] ?? '').trim()"
          :data-testid="`case-report-reject-${report.guid}`"
          @click="reject(report)"
        >
          {{ t('caseDetail.reports.reject') }}
        </button>
      </div>

      <div v-if="report.stateId === reportState.approved && canFile" class="join w-full">
        <input
          v-model="references[report.guid]"
          type="text"
          class="input input-sm join-item grow"
          :maxlength="maximumReference"
          :placeholder="t('caseDetail.reports.fiuReference')"
          :aria-label="t('caseDetail.reports.fiuReference')"
          :data-testid="`case-report-reference-${report.guid}`"
        />
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="!(references[report.guid] ?? '').trim()"
          :data-testid="`case-report-submit-${report.guid}`"
          @click="recordSubmission(report)"
        >
          {{ t('caseDetail.reports.recordSubmission') }}
        </button>
      </div>

      <div
        v-if="
          (report.stateId === reportState.submitted ||
            report.stateId === reportState.acknowledged) &&
          canFile
        "
        class="flex flex-col gap-1"
      >
        <select
          v-model="kinds[report.guid]"
          class="select select-sm"
          :aria-label="t('caseDetail.reports.feedbackKind')"
          :data-testid="`case-report-kind-${report.guid}`"
        >
          <option v-for="id in [1, 2, 3]" :key="id" :value="id">
            {{ t(`caseDetail.reports.kinds.${id}`) }}
          </option>
        </select>
        <input
          v-model="messages[report.guid]"
          type="text"
          class="input input-sm w-full"
          :maxlength="maximumMessage"
          :placeholder="t('caseDetail.reports.message')"
          :aria-label="t('caseDetail.reports.message')"
        />
        <input
          v-model="paths[report.guid]"
          type="text"
          class="input input-sm w-full"
          :placeholder="t('caseDetail.reports.fieldPath')"
          :aria-label="t('caseDetail.reports.fieldPath')"
        />
        <button
          type="button"
          class="btn btn-sm self-start"
          :disabled="!kinds[report.guid]"
          :data-testid="`case-report-feedback-${report.guid}`"
          @click="recordFeedback(report)"
        >
          {{ t('caseDetail.reports.recordFeedback') }}
        </button>
      </div>

      <div class="flex flex-wrap gap-2">
        <a
          v-if="mayDownload(report, held)"
          class="btn btn-sm"
          :href="reportsApi.downloadUrl(report.guid)"
          download
          :data-testid="`case-report-download-${report.guid}`"
        >{{ t('caseDetail.reports.download') }}</a
        >
        <button
          v-if="report.stateId === reportState.rejected && canFile"
          type="button"
          class="btn btn-sm"
          :data-testid="`case-report-new-version-${report.guid}`"
          @click="newVersion(report)"
        >
          {{ t('caseDetail.reports.newVersion') }}
        </button>
        <button
          v-if="mayWithdraw(report, held, me.data.value?.userName)"
          type="button"
          class="btn btn-sm btn-ghost"
          :data-testid="`case-report-withdraw-${report.guid}`"
          @click="withdraw(report)"
        >
          {{ t('caseDetail.reports.withdraw') }}
        </button>
      </div>

      <ul class="text-base-content/70 text-xs">
        <li v-for="version in report.versions" :key="`v${version.version}`">
          {{
            t('caseDetail.reports.versionLine', {
              version: version.version,
              user: version.createdUser,
            })
          }}
        </li>
        <li v-for="(approval, index) in report.approvals" :key="`a${index}`">
          {{
            t('caseDetail.reports.approvalLine', {
              user: approval.createdUser,
              decision:
                approval.decisionId === 1
                  ? t('caseDetail.reports.approved')
                  : t('caseDetail.reports.rejected'),
            })
          }}
          <span v-if="approval.note"> - {{ approval.note }}</span>
        </li>
        <li v-for="(item, index) in report.feedback" :key="`f${index}`">
          {{ t(`caseDetail.reports.kinds.${item.kindId}`) }}
          <span v-if="item.message"> - {{ item.message }}</span>
          <span v-if="item.fieldPath" class="font-mono"> ({{ item.fieldPath }})</span>
        </li>
      </ul>
    </div>

    <div v-if="canFile" class="flex flex-col gap-1 pt-2">
      <p v-if="(profiles.data.value ?? []).length === 0" class="text-sm text-base-content/70">
        {{ t('caseDetail.reports.noProfiles') }}
      </p>
      <div v-else class="join w-full">
        <select
          v-model="profileName"
          class="select select-sm join-item"
          :aria-label="t('caseDetail.reports.profile')"
          data-testid="case-report-profile"
        >
          <option v-for="profile in profiles.data.value" :key="profile.name" :value="profile.name">
            {{ profile.name }}
          </option>
        </select>
        <input
          v-model="reportCode"
          type="text"
          class="input input-sm join-item grow"
          maxlength="50"
          :placeholder="t('caseDetail.reports.code')"
          :aria-label="t('caseDetail.reports.code')"
          data-testid="case-report-code"
        />
        <button
          type="button"
          class="btn btn-sm join-item"
          :disabled="!profileName || !reportCode.trim() || start.isPending.value"
          data-testid="case-report-start"
          @click="start.mutate()"
        >
          {{ t('caseDetail.reports.start') }}
        </button>
      </div>
    </div>
  </DecideCard>
</template>
