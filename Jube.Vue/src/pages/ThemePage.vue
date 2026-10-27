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
import { computed, nextTick, onMounted, reactive, ref, useTemplateRef, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Bell, Search } from 'lucide-vue-next'
import { useUiStore } from '../stores/ui'
import { contrastRatio, parseCssColor } from '../composables/contrast'
import { useFormat } from '../composables/useFormat'
import DataGrid from '../components/DataGrid.vue'
import FeatureCard from '../components/FeatureCard.vue'
import NestedGroup from '../components/NestedGroup.vue'
import { directionFor } from '../i18n'
import type { GridBadge, GridColumn } from '../components/dataGridTypes'
import { sampleCases, type Risk } from './sampleCases'

const { t, locale } = useI18n()
const ui = useUiStore()

const colours = ['primary', 'secondary', 'accent', 'neutral', 'info', 'success', 'warning', 'error']
const surfaces = ['base-100', 'base-200', 'base-300']

const swatchClass: Record<string, string> = {
  primary: 'bg-primary text-primary-content',
  secondary: 'bg-secondary text-secondary-content',
  accent: 'bg-accent text-accent-content',
  neutral: 'bg-neutral text-neutral-content',
  info: 'bg-info text-info-content',
  success: 'bg-success text-success-content',
  warning: 'bg-warning text-warning-content',
  error: 'bg-error text-error-content',
  'base-100': 'bg-base-100 text-base-content',
  'base-200': 'bg-base-200 text-base-content',
  'base-300': 'bg-base-300 text-base-content',
}

const buttonClass: Record<string, string[]> = {
  solid: [
    'btn btn-neutral',
    'btn btn-primary',
    'btn btn-secondary',
    'btn btn-accent',
    'btn btn-info',
    'btn btn-success',
    'btn btn-warning',
    'btn btn-error',
  ],
  outline: [
    'btn btn-outline btn-neutral',
    'btn btn-outline btn-primary',
    'btn btn-outline btn-secondary',
    'btn btn-outline btn-info',
    'btn btn-outline btn-error',
  ],
  soft: [
    'btn btn-soft btn-neutral',
    'btn btn-soft btn-primary',
    'btn btn-soft btn-secondary',
    'btn btn-soft btn-info',
    'btn btn-soft btn-error',
  ],
  ghost: ['btn btn-ghost', 'btn btn-link'],
}

const badgeClass: Record<string, string[]> = {
  solid: [
    'badge badge-neutral',
    'badge badge-primary',
    'badge badge-secondary',
    'badge badge-accent',
    'badge badge-info',
    'badge badge-success',
    'badge badge-warning',
    'badge badge-error',
  ],
  soft: [
    'badge badge-soft badge-neutral',
    'badge badge-soft badge-primary',
    'badge badge-soft badge-secondary',
    'badge badge-soft badge-info',
    'badge badge-soft badge-error',
  ],
  outline: [
    'badge badge-outline badge-neutral',
    'badge badge-outline badge-primary',
    'badge badge-outline badge-secondary',
    'badge badge-outline badge-info',
    'badge badge-outline badge-error',
  ],
}

const pages = [1, 2, 3]
const softLabel = 'error soft'
const shortcut = ['Ctrl', 'K']
const { number } = useFormat()
const statValue = number(1284)
const sizes = [
  'btn btn-primary btn-xs',
  'btn btn-primary btn-sm',
  'btn btn-primary',
  'btn btn-primary btn-lg',
]

const alerts = [
  { cls: 'alert alert-info', key: 'info' },
  { cls: 'alert alert-success', key: 'success' },
  { cls: 'alert alert-warning', key: 'warning' },
  { cls: 'alert alert-error', key: 'error' },
]

const direction = computed(() => directionFor(locale.value))

const gridColumns = computed<GridColumn[]>(() => {
  void locale.value

  const risk: Record<Risk, GridBadge> = {
    low: { label: t('grid.risk.low'), tone: 'success' },
    medium: { label: t('grid.risk.medium'), tone: 'warning' },
    high: { label: t('grid.risk.high'), tone: 'error' },
  }

  return [
    { title: t('grid.columns.id'), field: 'id', mono: true },
    { title: t('grid.columns.customer'), field: 'customer' },
    { title: t('grid.columns.risk'), field: 'risk', badge: (value) => risk[value as Risk] },
  ]
})

const nested = reactive({
  override: false,
  cases: false,
  bypass: false,
  elevation: false,
  watcher: false,
  notification: false,
})

const swatchHost = useTemplateRef<HTMLElement>('swatchHost')
const ratios = ref<Record<string, number>>({})
const tab = ref(0)
const step = ref(1)
const dialog = useTemplateRef<HTMLDialogElement>('dialog')

function variantLabel(cls: string): string {
  const parts = cls
    .split(' ')
    .filter((part) => /^(btn|badge)-/.test(part))
    .map((part) => part.replace(/^(btn|badge)-/, ''))

  return parts.join(' ') || 'default'
}

function measure(): void {
  const next: Record<string, number> = {}

  for (const element of swatchHost.value?.querySelectorAll<HTMLElement>('[data-swatch]') ?? []) {
    const style = getComputedStyle(element)
    const foreground = parseCssColor(style.color)
    const background = parseCssColor(style.backgroundColor)

    if (foreground && background) {
      next[element.dataset.swatch!] = contrastRatio(foreground, background)
    }
  }

  ratios.value = next
}

function passes(name: string): boolean {
  return (ratios.value[name] ?? 0) >= 4.5
}

function ratioText(name: string): string {
  return ratios.value[name] ? `${ratios.value[name].toFixed(2)}:1` : '–'
}

async function measureWhenSettled(): Promise<void> {
  await nextTick()
  await Promise.all(
    document
      .getAnimations()
      .filter((animation) => animation.effect?.getComputedTiming().iterations !== Infinity)
      .map((animation) => animation.finished.catch(() => null)),
  )

  measure()
}

onMounted(() => void measureWhenSettled())
watch(
  () => ui.resolvedTheme,
  () => void measureWhenSettled(),
)
</script>

<template>
  <section class="flex flex-col gap-10" data-testid="theme-ready">
    <header class="flex flex-col gap-2">
      <h1 class="text-2xl font-semibold text-primary">{{ t('theme.title') }}</h1>
      <p>{{ t('theme.intro') }}</p>
    </header>

    <section class="flex flex-col gap-3" aria-labelledby="theme-colours">
      <h2 id="theme-colours" class="text-xl font-semibold">{{ t('theme.colours') }}</h2>
      <p class="text-sm">{{ t('theme.contrastHint') }}</p>
      <p class="text-sm font-semibold">{{ t('theme.accentRule') }}</p>
      <ul ref="swatchHost" class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
        <li
          v-for="name in [...colours, ...surfaces]"
          :key="name"
          :data-swatch="name"
          :class="['rounded-box border border-base-300 p-4 flex flex-col gap-1', swatchClass[name]]"
        >
          <span class="font-semibold font-mono">{{ name }}</span>
          <span>{{ t('theme.sample') }}</span>
          <span class="font-mono text-sm">
            {{ ratioText(name) }}
            <strong>{{ passes(name) ? t('theme.pass') : t('theme.fail') }}</strong>
          </span>
        </li>
      </ul>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-buttons">
      <h2 id="theme-buttons" class="text-xl font-semibold">{{ t('theme.buttons') }}</h2>
      <div
        v-for="(group, style) in buttonClass"
        :key="style"
        class="flex flex-wrap items-center gap-2"
      >
        <button v-for="cls in group" :key="cls" type="button" :class="cls">
          {{ variantLabel(cls) }}
        </button>
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <button v-for="cls in sizes" :key="cls" type="button" :class="cls">
          {{ variantLabel(cls) }}
        </button>
        <button type="button" class="btn btn-primary" disabled>{{ t('theme.disabled') }}</button>
        <button type="button" class="btn btn-primary">
          <span class="loading loading-spinner loading-sm" aria-hidden="true"></span>
          {{ t('api.loading') }}
        </button>
        <button
          type="button"
          class="btn btn-square btn-ghost"
          :aria-label="t('theme.notifications')"
        >
          <Bell aria-hidden="true" />
        </button>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-badges">
      <h2 id="theme-badges" class="text-xl font-semibold">{{ t('theme.badges') }}</h2>
      <div
        v-for="(group, style) in badgeClass"
        :key="style"
        class="flex flex-wrap items-center gap-2"
      >
        <span v-for="cls in group" :key="cls" :class="cls">{{
          cls.split(' ').pop()!.replace('badge-', '')
        }}</span>
      </div>
      <p class="flex flex-wrap items-center gap-2">
        <span class="badge badge-success">{{ t('grid.risk.low') }}</span>
        <span class="badge badge-warning">{{ t('grid.risk.medium') }}</span>
        <span class="badge badge-error">{{ t('grid.risk.high') }}</span>
      </p>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-alerts">
      <h2 id="theme-alerts" class="text-xl font-semibold">{{ t('theme.alerts') }}</h2>
      <div v-for="alert in alerts" :key="alert.key" role="status" :class="alert.cls">
        <span>{{ t('theme.sample') }} ({{ alert.key }})</span>
      </div>
      <div role="status" class="alert alert-error alert-soft">
        <span>{{ t('theme.sample') }} ({{ softLabel }})</span>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-forms">
      <h2 id="theme-forms" class="text-xl font-semibold">{{ t('theme.forms') }}</h2>
      <div class="grid gap-4 sm:grid-cols-2">
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('theme.name') }}</span>
          <input type="text" class="input w-full" :placeholder="t('theme.placeholder')" />
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('theme.name') }} ({{ t('theme.invalid') }})</span>
          <input type="text" class="input input-error w-full" value="x" aria-invalid="true" />
          <span class="text-sm text-error">{{ t('theme.helperError') }}</span>
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('prefs.theme') }}</span>
          <select class="select w-full">
            <option>{{ t('prefs.themeSystem') }}</option>
            <option>{{ t('prefs.themeLight') }}</option>
            <option>{{ t('prefs.themeDark') }}</option>
          </select>
        </label>
        <label class="flex flex-col gap-1">
          <span class="text-sm">{{ t('theme.name') }} ({{ t('theme.disabled') }})</span>
          <input type="text" class="input w-full" disabled value="x" />
        </label>
        <label class="flex flex-col gap-1 sm:col-span-2">
          <span class="text-sm">{{ t('theme.notes') }}</span>
          <textarea class="textarea w-full" rows="2"></textarea>
        </label>
      </div>
      <div class="flex flex-wrap items-center gap-6">
        <label class="flex items-center gap-2"
          ><input type="checkbox" class="checkbox" checked />{{ t('theme.sample') }}</label
        >
        <label class="flex items-center gap-2"
          ><input type="checkbox" class="checkbox checkbox-primary" checked />{{
            t('theme.sample')
          }}</label
        >
        <label class="flex items-center gap-2"
          ><input type="radio" name="r" class="radio" checked />{{ t('theme.sample') }}</label
        >
        <label class="flex items-center gap-2"
          ><input type="radio" name="r" class="radio radio-primary" />{{ t('theme.sample') }}</label
        >
        <label class="flex items-center gap-2"
          ><input type="checkbox" class="toggle" checked />{{ t('theme.sample') }}</label
        >
        <label class="flex items-center gap-2"
          ><input type="checkbox" class="toggle toggle-primary" />{{ t('theme.sample') }}</label
        >
        <label class="input">
          <Search aria-hidden="true" :size="16" />
          <input
            type="search"
            :aria-label="t('grid.filterLabel')"
            :placeholder="t('grid.filterPlaceholder')"
          />
        </label>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-navigation">
      <h2 id="theme-navigation" class="text-xl font-semibold">{{ t('theme.navigation') }}</h2>
      <div class="breadcrumbs text-sm">
        <ul>
          <li>
            <a href="#">{{ t('nav.home') }}</a>
          </li>
          <li>
            <a href="#">{{ t('nav.grid') }}</a>
          </li>
          <li>{{ t('theme.title') }}</li>
        </ul>
      </div>
      <div role="tablist" class="tabs tabs-border">
        <button
          v-for="(label, index) in [t('theme.tab1'), t('theme.tab2'), t('theme.tab3')]"
          :key="index"
          type="button"
          role="tab"
          :class="['tab', tab === index && 'tab-active']"
          :aria-selected="tab === index"
          @click="tab = index"
        >
          {{ label }}
        </button>
      </div>
      <div role="tabpanel" class="rounded-box border border-base-300 p-4">
        {{ t('theme.sample') }} {{ tab + 1 }}
      </div>
      <ul class="steps">
        <li
          v-for="(label, index) in [t('theme.step1'), t('theme.step2'), t('theme.step3')]"
          :key="index"
          :class="['step', step >= index && 'step-accent']"
        >
          {{ label }}
        </li>
      </ul>
      <div class="flex gap-2">
        <button type="button" class="btn btn-sm" :disabled="step === 0" @click="step--">
          {{ t('theme.back') }}
        </button>
        <button type="button" class="btn btn-sm btn-primary" :disabled="step === 2" @click="step++">
          {{ t('theme.next') }}
        </button>
      </div>
      <ul class="menu bg-base-200 rounded-box w-56">
        <li>
          <a class="menu-active" href="#">{{ t('nav.home') }}</a>
        </li>
        <li>
          <a href="#">{{ t('nav.grid') }}</a>
        </li>
        <li>
          <a href="#">{{ t('nav.api') }}</a>
        </li>
      </ul>
      <div class="join">
        <button
          v-for="page in pages"
          :key="page"
          type="button"
          :class="['join-item btn btn-sm', page === 2 && 'btn-active']"
        >
          {{ page }}
        </button>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-data">
      <h2 id="theme-data" class="text-xl font-semibold">{{ t('theme.data') }}</h2>
      <p class="text-sm">{{ t('theme.dataGrid') }}</p>
      <DataGrid
        :columns="gridColumns"
        :rows="sampleCases.slice(0, 4)"
        :direction="direction"
        :empty-text="t('grid.empty')"
        :label="t('theme.data')"
      />
      <div class="grid gap-4 sm:grid-cols-3">
        <div class="stats border border-base-300">
          <div class="stat">
            <div class="stat-title">{{ t('theme.statTitle') }}</div>
            <div class="stat-value font-mono">{{ statValue }}</div>
            <div class="stat-desc">{{ t('theme.statDesc') }}</div>
          </div>
        </div>
        <div class="card bg-base-100 border border-base-300">
          <div class="card-body">
            <h3 class="card-title">{{ t('theme.cardTitle') }}</h3>
            <p>{{ t('theme.sample') }}</p>
            <div class="card-actions justify-end">
              <button type="button" class="btn btn-primary btn-sm">{{ t('theme.next') }}</button>
            </div>
          </div>
        </div>
        <div class="flex flex-col gap-3 justify-center">
          <progress
            class="progress progress-primary"
            value="60"
            max="100"
            :aria-label="t('api.loading')"
          ></progress>
          <progress
            class="progress progress-error"
            value="30"
            max="100"
            :aria-label="t('api.loading')"
          ></progress>
          <p>
            <kbd v-for="key in shortcut" :key="key" class="kbd kbd-sm">{{ key }}</kbd>
          </p>
        </div>
      </div>
      <div class="collapse collapse-arrow bg-base-200 border border-base-300">
        <input type="checkbox" :aria-label="t('theme.collapse')" />
        <div class="collapse-title font-semibold">{{ t('theme.collapse') }}</div>
        <div class="collapse-content">{{ t('theme.sample') }}</div>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-nested">
      <h2 id="theme-nested" class="text-xl font-semibold">{{ t('theme.nested') }}</h2>
      <p class="text-sm">{{ t('rule.nesting') }}</p>
      <form class="flex max-w-3xl flex-col gap-4" @submit.prevent>
        <FeatureCard :title="t('rule.identity')" :hint="t('rule.identityHint')" always-on>
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-name">{{ t('rule.name') }}</label>
            <input id="rule-name" type="text" class="input input-sm w-full" />
          </div>
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-priority">{{ t('rule.priority') }}</label>
            <input
              id="rule-priority"
              type="number"
              class="input input-sm w-full tabular-nums"
              value="0"
            />
          </div>

          <label class="label cursor-pointer justify-start gap-3 sm:col-span-2">
            <input type="checkbox" class="toggle toggle-primary toggle-sm" checked />
            <span>{{ t('rule.active') }}</span>
          </label>
        </FeatureCard>

        <FeatureCard
          v-model="nested.override"
          :title="t('rule.enableOverride')"
          :hint="t('rule.overrideHint')"
        >
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-override-key">{{ t('rule.overrideKey') }}</label>
            <select id="rule-override-key" class="select select-sm w-full">
              <option>{{ t('rule.choose') }}</option>
            </select>
          </div>

          <label class="label cursor-pointer justify-start gap-3">
            <input type="checkbox" class="toggle toggle-primary toggle-sm" />
            <span>{{ t('rule.enableForce') }}</span>
          </label>
        </FeatureCard>

        <FeatureCard v-model="nested.cases" :title="t('rule.cases')" :hint="t('rule.casesHint')">
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-workflow">{{ t('rule.caseWorkflow') }}</label>
            <select id="rule-workflow" class="select select-sm w-full">
              <option>{{ t('rule.choose') }}</option>
            </select>
          </div>
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-status">{{ t('rule.caseStatus') }}</label>
            <select id="rule-status" class="select select-sm w-full">
              <option>{{ t('rule.choose') }}</option>
            </select>
          </div>
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-case-key">{{ t('rule.caseKey') }}</label>
            <select id="rule-case-key" class="select select-sm w-full">
              <option>{{ t('rule.choose') }}</option>
            </select>
          </div>

          <NestedGroup v-model="nested.bypass" :label="t('rule.bypass')">
            <fieldset class="fieldset">
              <legend class="fieldset-legend">{{ t('rule.bypassInterval') }}</legend>
              <div class="flex flex-wrap gap-4">
                <label class="label cursor-pointer gap-2"
                  ><input
                    type="radio"
                    name="rule-interval"
                    class="radio radio-sm radio-primary"
                  /><span>{{ t('rule.minutes') }}</span></label
                >
                <label class="label cursor-pointer gap-2"
                  ><input
                    type="radio"
                    name="rule-interval"
                    class="radio radio-sm radio-primary"
                    checked
                  /><span>{{ t('rule.hours') }}</span></label
                >
                <label class="label cursor-pointer gap-2"
                  ><input
                    type="radio"
                    name="rule-interval"
                    class="radio radio-sm radio-primary"
                  /><span>{{ t('rule.days') }}</span></label
                >
                <label class="label cursor-pointer gap-2"
                  ><input
                    type="radio"
                    name="rule-interval"
                    class="radio radio-sm radio-primary"
                  /><span>{{ t('rule.months') }}</span></label
                >
              </div>
            </fieldset>
            <div class="flex flex-col gap-1">
              <label class="label" for="rule-bypass-value">{{ t('rule.bypassValue') }}</label>
              <input
                id="rule-bypass-value"
                type="number"
                class="input input-sm w-full tabular-nums"
                value="0"
              />
            </div>
          </NestedGroup>
        </FeatureCard>

        <FeatureCard
          v-model="nested.elevation"
          :title="t('rule.responseElevation')"
          :hint="t('rule.elevationHint')"
        >
          <div class="flex flex-col gap-1 sm:col-span-2">
            <label class="label" for="rule-elevation-content">{{
              t('rule.elevationContent')
            }}</label>
            <textarea
              id="rule-elevation-content"
              class="textarea textarea-sm h-20 w-full"
            ></textarea>
          </div>
          <NestedGroup v-model="nested.watcher" :label="t('rule.sendToWatcher')">
            <div class="flex flex-col gap-1">
              <label class="label" for="rule-watcher-key">{{ t('rule.watcherKey') }}</label>
              <select id="rule-watcher-key" class="select select-sm w-full">
                <option>{{ t('rule.choose') }}</option>
              </select>
            </div>
            <div class="flex flex-col gap-1">
              <label class="label" for="rule-fore">{{ t('rule.foreColour') }}</label>
              <input id="rule-fore" type="text" class="input input-sm w-full font-mono" />
            </div>
            <div class="flex flex-col gap-1">
              <label class="label" for="rule-back">{{ t('rule.backColour') }}</label>
              <input id="rule-back" type="text" class="input input-sm w-full font-mono" />
            </div>
          </NestedGroup>
        </FeatureCard>

        <FeatureCard
          v-model="nested.notification"
          :title="t('rule.notification')"
          :hint="t('rule.notificationHint')"
        >
          <div class="flex flex-wrap gap-4 sm:col-span-2">
            <label class="label cursor-pointer gap-2"
              ><input
                type="radio"
                name="rule-channel"
                class="radio radio-sm radio-primary"
                checked
              /><span>{{ t('rule.email') }}</span></label
            >
            <label class="label cursor-pointer gap-2"
              ><input
                type="radio"
                name="rule-channel"
                class="radio radio-sm radio-primary"
              /><span>{{ t('rule.sms') }}</span></label
            >
          </div>
          <div class="flex flex-col gap-1">
            <label class="label" for="rule-destination">{{ t('rule.destination') }}</label>
            <input id="rule-destination" type="text" class="input input-sm w-full" />
          </div>
        </FeatureCard>

        <div
          class="sticky bottom-0 -mx-4 flex items-center justify-end gap-2 border-t border-base-300 bg-base-100/95 px-4 py-3"
        >
          <button type="reset" class="btn btn-ghost btn-sm">{{ t('rule.discard') }}</button>
          <button type="submit" class="btn btn-primary btn-sm">{{ t('rule.save') }}</button>
        </div>
      </form>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-feedback">
      <h2 id="theme-feedback" class="text-xl font-semibold">{{ t('theme.feedback') }}</h2>
      <div class="flex flex-wrap items-center gap-6">
        <span class="loading loading-spinner" role="status" :aria-label="t('api.loading')"></span>
        <span class="loading loading-dots" aria-hidden="true"></span>
        <div class="skeleton h-6 w-48" aria-hidden="true"></div>
        <div class="tooltip" :data-tip="t('theme.sample')">
          <button type="button" class="btn btn-sm">{{ t('theme.hover') }}</button>
        </div>
      </div>
      <div class="toast static flex flex-col gap-2 p-0">
        <div role="status" class="alert alert-success">
          <span>{{ t('theme.sample') }}</span>
        </div>
      </div>
    </section>

    <section class="flex flex-col gap-3" aria-labelledby="theme-overlays">
      <h2 id="theme-overlays" class="text-xl font-semibold">{{ t('theme.overlays') }}</h2>
      <div class="flex flex-wrap items-center gap-4">
        <button type="button" class="btn btn-primary" @click="dialog?.showModal()">
          {{ t('theme.openDialog') }}
        </button>
        <button
          type="button"
          class="btn btn-sm"
          popovertarget="theme-dropdown"
          style="anchor-name: --theme-dropdown"
        >
          {{ t('theme.dropdown') }}
        </button>
        <ul
          id="theme-dropdown"
          class="dropdown menu bg-base-100 rounded-box z-1 w-52 p-2 shadow-sm"
          popover
          style="position-anchor: --theme-dropdown"
        >
          <li>
            <a href="#">{{ t('nav.home') }}</a>
          </li>
          <li>
            <a href="#">{{ t('nav.grid') }}</a>
          </li>
        </ul>
      </div>
      <dialog ref="dialog" class="modal" :aria-label="t('theme.openDialog')">
        <div class="modal-box">
          <h3 class="text-lg font-semibold">{{ t('theme.openDialog') }}</h3>
          <p class="py-4">{{ t('theme.sample') }}</p>
          <div class="modal-action">
            <form method="dialog">
              <button class="btn">{{ t('theme.close') }}</button>
            </form>
          </div>
        </div>
        <form method="dialog" class="modal-backdrop">
          <button :aria-label="t('theme.close')"></button>
        </form>
      </dialog>
    </section>
  </section>
</template>
