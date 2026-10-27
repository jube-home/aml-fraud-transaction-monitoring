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
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { keepPreviousData, useQuery } from '@tanstack/vue-query'
import { Building2, Check, ChevronDown, Search } from 'lucide-vue-next'
import { queryKeys } from '../api/queryKeys'
import { useApi } from '../api/useApi'
import { useToasts } from '../stores/toasts'

const props = defineProps<{ current: number }>()
const { t } = useI18n()
const toasts = useToasts()

interface Tenant {
  id: number
  name: string
}

const visibleLimit = 50
const term = ref('')
const debounced = ref('')
const active = ref(0)
const panel = ref<HTMLElement>()
const search = ref<HTMLInputElement>()
const busy = ref(false)
const opened = ref(false)
const currentId = computed(() => props.current)

let timer: ReturnType<typeof setTimeout> | undefined

watch(term, (value) => {
  clearTimeout(timer)
  timer = setTimeout(() => {
    debounced.value = value.trim()
    active.value = 0
  }, 250)
})

const { data: matches } = useQuery({
  queryKey: [...queryKeys.tenants(), debounced],
  enabled: opened,
  queryFn: () =>
    useApi<Tenant[]>('/api/TenantRegistry/ByFilter', {
      query: { 'filter[filters][0][value]': debounced.value },
    }),
  placeholderData: keepPreviousData,
})

const { data: currentTenant } = useQuery({
  queryKey: [...queryKeys.tenants(), 'current', currentId],
  enabled: computed(() => props.current > 0),
  queryFn: () => useApi<Tenant>(`/api/TenantRegistry/${props.current}`),
})

const currentName = computed(
  () =>
    currentTenant.value?.name ??
    (matches.value ?? []).find((tenant) => tenant.id === props.current)?.name,
)

const shown = computed<Tenant[]>(() => {
  const rest = (matches.value ?? []).filter((tenant) => tenant.id !== props.current)
  const pinned =
    currentTenant.value &&
    currentTenant.value.name.toLowerCase().includes(debounced.value.toLowerCase())
      ? [currentTenant.value]
      : (matches.value ?? []).filter((tenant) => tenant.id === props.current)

  return [...pinned, ...rest].slice(0, visibleLimit)
})
const truncated = computed(() => (matches.value?.length ?? 0) > visibleLimit)

function onToggle(event: Event): void {
  opened.value = (event as ToggleEvent).newState === 'open'

  if (!opened.value) return

  term.value = ''
  debounced.value = ''
  active.value = 0
  void nextTick(() => search.value?.focus())
}

async function choose(tenant: Tenant): Promise<void> {
  if (tenant.id === props.current || busy.value) {
    panel.value?.hidePopover()
    return
  }

  busy.value = true

  try {
    await useApi('/api/UserInTenant', { method: 'PUT', body: { tenantRegistryId: tenant.id } })
    window.location.reload()
  } catch (error) {
    busy.value = false
    toasts.apiError(error)
  }
}

function onKeydown(event: KeyboardEvent): void {
  const count = shown.value.length

  if (event.key === 'ArrowDown') {
    event.preventDefault()
    active.value = count === 0 ? 0 : (active.value + 1) % count
  } else if (event.key === 'ArrowUp') {
    event.preventDefault()
    active.value = count === 0 ? 0 : (active.value - 1 + count) % count
  } else if (event.key === 'Enter') {
    event.preventDefault()

    const tenant = shown.value[active.value]

    if (tenant) void choose(tenant)
  }
}
</script>

<template>
  <button
    type="button"
    class="btn btn-sm gap-1.5 rounded-full"
    popovertarget="tenant-panel"
    style="anchor-name: --tenant-panel"
    :aria-label="currentName ? `${t('shell.tenant')}: ${currentName}` : t('shell.tenant')"
    :title="currentName"
    data-testid="tenant-switcher"
  >
    <Building2 aria-hidden="true" class="size-4 shrink-0" />
    <span class="hidden max-w-36 truncate sm:inline">{{ currentName }}</span>
    <ChevronDown aria-hidden="true" class="size-3.5 shrink-0 opacity-70" />
  </button>
  <div
    id="tenant-panel"
    ref="panel"
    popover
    class="dropdown dropdown-end card card-sm z-50 mt-2 w-96 max-w-[calc(100vw-2rem)] bg-base-100 shadow-md"
    style="position-anchor: --tenant-panel"
    @toggle="onToggle"
  >
    <div class="card-body gap-2">
      <label class="input input-sm w-full">
        <Search aria-hidden="true" class="size-4 opacity-70" />
        <input
          ref="search"
          v-model="term"
          type="search"
          class="grow"
          role="combobox"
          aria-expanded="true"
          aria-controls="tenant-options"
          :aria-activedescendant="shown[active] ? `tenant-option-${shown[active]!.id}` : undefined"
          :aria-label="t('shell.tenantSearch')"
          :placeholder="t('shell.tenantSearch')"
          autocomplete="off"
          data-testid="tenant-search"
          @keydown="onKeydown"
        />
      </label>
      <ul
        id="tenant-options"
        role="listbox"
        :aria-label="t('shell.tenant')"
        class="menu max-h-72 w-full flex-nowrap overflow-y-auto p-0"
      >
        <li
          v-for="(tenant, index) in shown"
          :id="`tenant-option-${tenant.id}`"
          :key="tenant.id"
          role="option"
          :aria-selected="tenant.id === props.current"
        >
          <button
            type="button"
            tabindex="-1"
            :class="{
              'menu-active bg-primary! text-primary-content!': index === active,
              'font-semibold': tenant.id === props.current,
            }"
            :disabled="busy"
            @click="choose(tenant)"
            @mousemove="active = index"
          >
            <span class="flex-1 truncate text-start">{{ tenant.name }}</span>
            <Check v-if="tenant.id === props.current" aria-hidden="true" class="size-4 shrink-0" />
          </button>
        </li>
      </ul>
      <p v-if="shown.length === 0" class="px-1 text-sm" data-testid="tenant-none">
        {{ t('shell.tenantNone') }}
      </p>
      <p v-else-if="truncated" class="px-1 text-xs" data-testid="tenant-more">
        {{ t('shell.tenantMore', { count: visibleLimit }) }}
      </p>
    </div>
  </div>
</template>
