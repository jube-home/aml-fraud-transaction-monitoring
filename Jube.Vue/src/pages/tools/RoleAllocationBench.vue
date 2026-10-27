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
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import RoleAllocation from '../../components/RoleAllocation.vue'
import { useFormModels } from '../../formModel/useFormModels'

const { t } = useI18n()

const { withRoles } = useFormModels()

const parent = ref('')
const guid = ref('')
const loaded = ref<{ parent: string; guid: string }>()

const chosen = computed(() => parent.value || withRoles.value[0]?.area || '')
const target = computed(() => {
  const roles = withRoles.value.find((candidate) => candidate.area === loaded.value?.parent)?.roles

  return roles && loaded.value
    ? { baseEndpoint: roles.endpoint, parentKeyField: roles.parentKey, guid: loaded.value.guid }
    : undefined
})

const valid = computed(
  () =>
    chosen.value !== '' && /^[0-9a-f]{8}-([0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(guid.value.trim()),
)

function load(): void {
  loaded.value = { parent: chosen.value, guid: guid.value.trim() }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-3 overflow-y-auto">
    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('roleAllocation.bench.parent') }}</span>
        <select v-model="parent" class="select select-sm" data-testid="bench-parent">
          <option v-for="model in withRoles" :key="model.area" :value="model.area">
            {{ model.area }}
          </option>
        </select>
      </label>
      <label class="flex flex-col gap-1 text-sm">
        <span class="label">{{ t('roleAllocation.bench.guid') }}</span>
        <input
          v-model="guid"
          class="input input-sm w-96 font-mono"
          data-testid="bench-guid"
          @keydown.enter="valid && load()"
        />
      </label>
      <button
        type="button"
        class="btn btn-sm"
        :disabled="!valid"
        data-testid="bench-load"
        @click="load"
      >
        {{ t('roleAllocation.bench.load') }}
      </button>
    </div>
    <RoleAllocation v-if="target" :key="`${target.baseEndpoint}:${target.guid}`" v-bind="target" />
    <p v-else class="text-sm" data-testid="bench-empty">{{ t('roleAllocation.bench.choose') }}</p>
  </div>
</template>
