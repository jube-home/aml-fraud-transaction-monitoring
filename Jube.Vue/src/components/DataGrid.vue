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
import { computed, onBeforeUnmount, onMounted, ref, useTemplateRef, watch } from 'vue'
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from 'lucide-vue-next'
import {
  TabulatorFull as Tabulator,
  type ColumnDefinition,
  type Options,
  type RowComponent,
} from 'tabulator-tables'
import { toTabulatorColumns } from './dataGridColumns'
import { buildWorkbook, exportableColumns, saveBuffer } from '../export/xlsx'
import type {
  GridActionEvent,
  GridColumn,
  GridDirection,
  GridExpandEvent,
  GridExpandLabels,
  GridInputEvent,
  GridPagerLabels,
  GridRowPaint,
  GridSort,
} from './dataGridTypes'

const themeClasses = [
  'w-full bg-base-100 text-base-content border-base-300',
  '[&_.tabulator-header]:bg-base-200 [&_.tabulator-header]:border-base-300 [&_.tabulator-header]:text-base-content',
  '[&_.tabulator-col]:bg-base-200 [&_.tabulator-col]:border-base-300 [&_.tabulator-col]:text-base-content',
  '[&_.tabulator-arrow]:border-t-base-content [&_.tabulator-arrow]:border-b-base-content',
  '[&_.tabulator-table]:bg-base-100 [&_.tabulator-table]:text-base-content',
  '[&_.tabulator-row]:bg-base-100 [&_.tabulator-row]:border-base-300 [&_.tabulator-row]:text-base-content',
  '[&_.tabulator-row.tabulator-row-even]:bg-base-200',
  '[&_.tabulator-row.tabulator-selectable:hover]:bg-base-300',
  '[&_.tabulator-cell]:border-base-300',
  '[&_.tabulator-placeholder_span]:text-base-content',
]

const pageSizes = [10, 25, 50, 100]

const autoHeightClass = '[&_.tabulator-tableholder]:h-auto!'

const props = defineProps<{
  columns: GridColumn[]
  rows: object[]
  filterField?: string
  filterText?: string
  sort?: GridSort
  direction?: GridDirection
  emptyText: string
  label: string
  pageSize?: number
  pagerLabels?: GridPagerLabels
  tree?: boolean
  expandable?: boolean
  autoHeight?: boolean
  fill?: boolean
  expandLabels?: GridExpandLabels
  expandedKeys?: unknown[]
  rowKey?: string
  movable?: boolean
  rowPaint?: (row: object) => GridRowPaint | null
  mark?: { field: string; values: readonly unknown[] }
}>()

const emit = defineEmits<{
  ready: []
  sort: [sort: GridSort | undefined]
  visibleRows: [count: number]
  action: [event: GridActionEvent]
  input: [event: GridInputEvent]
  expand: [event: GridExpandEvent]
  columnMoved: [fields: string[]]
  columnResized: [event: { field: string; width: number }]
}>()

const handlers = {
  onAction: (event: GridActionEvent) => emit('action', event),
  onInput: (event: GridInputEvent) => emit('input', event),
}

const classes = computed(() =>
  props.autoHeight ? [...themeClasses, autoHeightClass] : themeClasses,
)

const host = useTemplateRef<HTMLDivElement>('host')
const parking = useTemplateRef<HTMLDivElement>('parking')
const page = ref(1)
const pages = ref(1)
const size = ref(props.pageSize ?? 25)
const total = ref(0)

let table: Tabulator | undefined
let built = false
let seeded: object[] = []
let seededColumns: GridColumn[] = []

const expanded = new Set<unknown>(props.expandedKeys ?? [])
const detailObservers = new Map<HTMLElement, ResizeObserver>()

function keyOf(row: RowComponent): unknown {
  return (row.getData() as Record<string, unknown>)[props.rowKey ?? 'id']
}

function hostOf(row: RowComponent): HTMLElement | undefined {
  const found = row.getElement().querySelector<HTMLElement>(':scope > [data-detail]')

  if (found) return found

  const created = document.createElement('div')

  created.dataset.detail = ''
  created.setAttribute('role', 'cell')
  created.className =
    'sticky start-0 box-border w-(--grid-view,100%) max-w-full shrink-0 basis-auto border-t border-base-300 bg-base-100 p-3 whitespace-normal'
  syncView()
  row.getElement().append(created)

  if (typeof ResizeObserver !== 'undefined') {
    const observer = new ResizeObserver(() => fit(row, true))

    observer.observe(created)
    detailObservers.set(created, observer)
  }

  return created
}

function syncView(): void {
  const holder = host.value?.querySelector<HTMLElement>('.tabulator-tableholder')

  if (host.value && holder && holder.clientWidth > 0) {
    host.value.style.setProperty('--grid-view', `${holder.clientWidth}px`)
  }
}

function fit(row: RowComponent, open: boolean): void {
  void row.normalizeHeight()

  const element = row.getElement()

  element.style.height = open ? 'auto' : ''
  element.style.overflow = open ? 'visible' : ''
  scheduleRegrow()
}

let regrowFrame = 0

function scheduleRegrow(): void {
  if (!built || regrowFrame) return

  regrowFrame = requestAnimationFrame(() => {
    regrowFrame = 0
    void table?.redraw()
  })
}

function showDetail(row: RowComponent): void {
  const host = hostOf(row)

  emit('expand', { id: keyOf(row), row: row.getData(), host })
  fit(row, true)
}

function hideDetail(row: RowComponent): void {
  const detail = row.getElement().querySelector<HTMLElement>(':scope > [data-detail]')

  if (detail) {
    detailObservers.get(detail)?.disconnect()
    detailObservers.delete(detail)
    detail.remove()
  }

  emit('expand', { id: keyOf(row), row: row.getData(), host: undefined })
  fit(row, false)
}

function expanderColumn(): ColumnDefinition {
  return {
    title: props.expandLabels?.expand ?? '',
    field: '',
    width: 48,
    hozAlign: 'center',
    titleFormatter: (cell) => {
      const hidden = document.createElement('span')

      hidden.className = 'sr-only'
      hidden.textContent = cell.getValue() as string

      return hidden
    },
    headerSort: false,
    formatter: (cell) => {
      const row = cell.getRow()
      const open = expanded.has(keyOf(row))
      const button = document.createElement('button')

      button.type = 'button'
      button.className = 'btn btn-ghost btn-xs btn-circle'
      button.dataset.testid = 'grid-expand'
      button.setAttribute('aria-expanded', String(open))
      button.setAttribute(
        'aria-label',
        open ? (props.expandLabels?.collapse ?? '') : (props.expandLabels?.expand ?? ''),
      )

      const mark = document.createElement('span')

      mark.setAttribute('aria-hidden', 'true')
      mark.className = open ? 'inline-block rotate-90' : 'inline-block'
      mark.textContent = '›'
      button.append(mark)
      button.addEventListener('click', () => {
        const key = keyOf(row)
        const now = !expanded.has(key)

        if (now) expanded.add(key)
        else expanded.delete(key)

        button.setAttribute('aria-expanded', String(now))
        button.setAttribute(
          'aria-label',
          now ? (props.expandLabels?.collapse ?? '') : (props.expandLabels?.expand ?? ''),
        )
        mark.className = now ? 'inline-block rotate-90' : 'inline-block'

        if (now) showDetail(row)
        else hideDetail(row)
      })

      return button
    },
  }
}

function withExpander(columns: ColumnDefinition[]): ColumnDefinition[] {
  return props.expandable ? [expanderColumn(), ...columns] : columns
}

function applyFilter(): void {
  if (!table || !built) return

  if (props.filterField && props.filterText) {
    table.setFilter(props.filterField, 'like', props.filterText)
  } else {
    table.clearFilter(true)
  }

  emit('visibleRows', table.getDataCount('active'))
}

function applySort(): void {
  if (!table || !built) return

  if (props.sort) {
    table.setSort(props.sort.field, props.sort.dir)
  } else {
    table.clearSort()
  }
}

function repairAccessibilityTree(): void {
  host.value?.querySelector('.tabulator-tableholder')?.setAttribute('role', 'rowgroup')
  host.value?.querySelector('.tabulator-table')?.setAttribute('role', 'presentation')
  host.value?.removeAttribute('aria-owns')
}

function setExpanded(expanded: boolean): void {
  if (!table || !built) return

  const walk = (rows: RowComponent[]): void => {
    for (const row of rows) {
      if (expanded) row.treeExpand()
      else row.treeCollapse()

      walk(row.getTreeChildren())
    }
  }

  walk(table.getRows())
}

function downloadCsv(filename: string): void {
  table?.download('csv', filename, { bom: true })
}

function pagination(): Partial<Options> {
  if (!props.pageSize) return {}

  return {
    pagination: true,
    paginationMode: 'local',
    paginationSize: props.pageSize,
    paginationElement: parking.value ?? undefined,
    paginationButtonCount: 0,
    paginationCounter: undefined,
  }
}

function syncPager(): void {
  if (!table || !built || !props.pageSize) return

  page.value = table.getPage() || 1
  pages.value = table.getPageMax() || 1
  size.value = table.getPageSize() as number
  total.value = table.getDataCount('active')
}

function goTo(next: number): void {
  table?.setPage(Math.min(Math.max(next, 1), pages.value))
}

function setSize(next: number): void {
  table?.setPageSize(next)
}

function paintRow(row: RowComponent): void {
  const element = row.getElement()
  const paint = props.rowPaint?.(row.getData()) ?? null

  element.style.color = paint?.fore ?? ''
  element.style.backgroundColor = paint?.back ?? ''
  element.style.fontWeight = paint?.bold ? '700' : ''

  if (paint?.title) element.title = paint.title
  else element.removeAttribute('title')

  const marked = isMarked(row.getData())

  element.style.boxShadow = marked ? 'inset 0.25rem 0 0 0 var(--color-primary)' : ''
  element.style.outline = marked ? '2px solid var(--color-primary)' : ''
  element.style.outlineOffset = marked ? '-2px' : ''

  if (marked) element.setAttribute('aria-current', 'true')
  else element.removeAttribute('aria-current')
}

function isMarked(data: object): boolean {
  const mark = props.mark

  return (
    !!mark &&
    mark.values.length > 0 &&
    mark.values.includes((data as Record<string, unknown>)[mark.field])
  )
}

async function showMarked(): Promise<void> {
  if (!table || !built) return

  const rows = table.getRows('active')

  rows.forEach((row) => row.reformat())

  const first = rows.find((row) => isMarked(row.getData()))

  if (!first) return

  if (props.pageSize) await table.setPageToRow(first)

  first.getElement().scrollIntoView?.({ block: 'nearest' })
}

function build(): void {
  if (!host.value) return

  host.value.classList.remove('tabulator-rtl', 'tabulator-ltr')

  seeded = props.rows
  seededColumns = props.columns
  table = new Tabulator(host.value, {
    data: props.rows,
    columns: withExpander(toTabulatorColumns(props.columns, props.direction ?? 'ltr', handlers)),
    layout: 'fitColumns',
    ...(props.movable ? { movableColumns: true } : {}),
    ...(props.fill ? { height: '100%' } : {}),
    rowFormatter: (row) => {
      paintRow(row)
      if (props.expandable && expanded.has(keyOf(row))) showDetail(row)
    },
    textDirection: props.direction ?? 'auto',
    placeholder: props.emptyText,
    ...pagination(),
    ...(props.tree
      ? {
          dataTree: true,
          dataTreeStartExpanded: true,
          dataTreeChildIndent: 18,
          dataTreeBranchElement: false,
        }
      : {}),
    initialSort: props.sort ? [{ column: props.sort.field, dir: props.sort.dir }] : undefined,
    initialFilter:
      props.filterField && props.filterText
        ? [{ field: props.filterField, type: 'like', value: props.filterText }]
        : undefined,
  })

  table.on('tableBuilt', () => {
    built = true

    if (props.columns !== seededColumns) {
      void table?.setColumns(
        withExpander(toTabulatorColumns(props.columns, props.direction ?? 'ltr', handlers)),
      )
    }

    if (props.rows !== seeded) void table?.replaceData(props.rows)?.then(syncPager)
    syncPager()
    repairAccessibilityTree()
    emit('ready')
    emit('visibleRows', table?.getDataCount('active') ?? 0)
  })

  if (props.movable) {
    table.on('columnMoved', (_column, columns) => {
      emit(
        'columnMoved',
        columns.map((column) => column.getField()).filter((field) => !!field),
      )
    })

    table.on('columnResized', (column) => {
      const field = column.getField()

      if (field) emit('columnResized', { field, width: column.getWidth() })
    })
  }

  table.on('pageLoaded', syncPager)
  table.on('dataFiltered', syncPager)
  table.on('dataProcessed', syncPager)

  table.on('dataSorted', (sorters) => {
    const first = sorters[0]

    emit(
      'sort',
      first ? { field: first.field, dir: first.dir === 'desc' ? 'desc' : 'asc' } : undefined,
    )
  })
}

let resizer: ResizeObserver | undefined
let lastWidth = 0

onMounted(() => {
  build()

  if (host.value && typeof ResizeObserver !== 'undefined') {
    resizer = new ResizeObserver((entries) => {
      const width = entries[0]?.contentRect.width ?? 0

      if (width > 0 && width !== lastWidth) {
        lastWidth = width
        syncView()

        if (built) void table?.redraw(true)
      }
    })
    resizer.observe(host.value)
  }
})

onBeforeUnmount(() => {
  cancelAnimationFrame(regrowFrame)
  regrowFrame = 0
  resizer?.disconnect()
  for (const observer of detailObservers.values()) observer.disconnect()
  detailObservers.clear()
  table?.destroy()
  table = undefined
  built = false
})

watch(
  () => props.columns,
  (columns) => {
    if (built)
      void table?.setColumns(
        withExpander(toTabulatorColumns(columns, props.direction ?? 'ltr', handlers)),
      )
  },
)

watch(
  () => props.rows,
  (rows) => {
    if (built) void table?.replaceData(rows)?.then(syncPager)
  },
)

watch(
  () => props.direction,
  () => {
    table?.destroy()
    table = undefined
    built = false
    build()
  },
)

watch(
  () => props.mark,
  () => {
    void showMarked()
  },
  { deep: true },
)

watch(() => [props.filterField, props.filterText], applyFilter)
watch(() => props.sort, applySort)

async function downloadXlsx(filename: string, sheetName = 'Data'): Promise<void> {
  if (!table || !built) return

  const buffer = await buildWorkbook(
    exportableColumns(props.columns),
    table.getData('active') as Record<string, unknown>[],
    sheetName,
  )

  saveBuffer(buffer, filename)
}

defineExpose({
  downloadCsv,
  downloadXlsx,
  expandAll: () => setExpanded(true),
  collapseAll: () => setExpanded(false),
})
</script>

<template>
  <div :class="fill ? 'flex h-full min-h-0 flex-col' : ''">
    <div ref="host" :aria-label="label" :class="[classes, fill ? 'min-h-0 flex-1' : '']"></div>
    <div v-if="pageSize" ref="parking" hidden></div>
    <nav
      v-if="pageSize && pagerLabels"
      :aria-label="`${pagerLabels.navigation}: ${label}`"
      class="flex flex-wrap items-center justify-between gap-3 rounded-b-box border border-t-0 border-base-300 bg-base-200 px-3 py-2 text-sm"
      data-testid="grid-pager"
    >
      <p class="font-mono tabular-nums" role="status" data-testid="grid-pager-summary">
        {{
          pagerLabels.summary(
            total === 0 ? 0 : (page - 1) * size + 1,
            Math.min(page * size, total),
            total,
          )
        }}
      </p>
      <div class="flex flex-wrap items-center gap-3">
        <label class="flex items-center gap-2">
          <span class="label">{{ pagerLabels.pageSize }}</span>
          <select
            class="select select-xs w-auto"
            :value="size"
            data-testid="grid-pager-size"
            @change="setSize(Number(($event.target as HTMLSelectElement).value))"
          >
            <option v-for="option in pageSizes" :key="option" :value="option">{{ option }}</option>
          </select>
        </label>
        <div class="join">
          <button
            type="button"
            class="btn btn-xs btn-square join-item border-base-300 bg-base-100"
            :aria-label="pagerLabels.first"
            :disabled="page <= 1"
            data-testid="grid-pager-first"
            @click="goTo(1)"
          >
            <ChevronsLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
          </button>
          <button
            type="button"
            class="btn btn-xs btn-square join-item border-base-300 bg-base-100"
            :aria-label="pagerLabels.previous"
            :disabled="page <= 1"
            data-testid="grid-pager-previous"
            @click="goTo(page - 1)"
          >
            <ChevronLeft aria-hidden="true" class="size-4 rtl:rotate-180" />
          </button>
          <span
            class="join-item flex items-center border border-base-300 bg-base-100 px-3 font-mono text-xs tabular-nums"
            data-testid="grid-pager-page"
          >
            {{ pagerLabels.page(page, pages) }}
          </span>
          <button
            type="button"
            class="btn btn-xs btn-square join-item border-base-300 bg-base-100"
            :aria-label="pagerLabels.next"
            :disabled="page >= pages"
            data-testid="grid-pager-next"
            @click="goTo(page + 1)"
          >
            <ChevronRight aria-hidden="true" class="size-4 rtl:rotate-180" />
          </button>
          <button
            type="button"
            class="btn btn-xs btn-square join-item border-base-300 bg-base-100"
            :aria-label="pagerLabels.last"
            :disabled="page >= pages"
            data-testid="grid-pager-last"
            @click="goTo(pages)"
          >
            <ChevronsRight aria-hidden="true" class="size-4 rtl:rotate-180" />
          </button>
        </div>
      </div>
    </nav>
  </div>
</template>
