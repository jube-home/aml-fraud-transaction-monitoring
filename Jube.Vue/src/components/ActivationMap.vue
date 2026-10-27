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
import { onBeforeUnmount, onMounted, useTemplateRef, watch } from 'vue'
import type * as MapLibre from 'maplibre-gl'
import { pointsGeoJson, trailsGeoJson } from '../watcher/geojson'
import { styleFor } from '../watcher/mapStyle'
import type { Activation, TrailSegment } from '../watcher/types'

const props = defineProps<{
  points: Activation[]
  trails: TrailSegment[]
  showTrails: boolean
  showHeat: boolean
  dark: boolean
  accent: string
  selectedId: string | null
  fadeMs: number | null
  now: number
  label: string
  autoFit?: boolean
}>()

const emit = defineEmits<{ select: [id: string | null]; failed: [] }>()

const host = useTemplateRef<HTMLDivElement>('host')

const errorColour = '#b42318'
const reducedMotion =
  typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches

let lib: typeof MapLibre | undefined
let map: MapLibre.Map | undefined
let popup: MapLibre.Popup | undefined
let frame = 0
let installed = false
let fitted = 0
let fitTimer: ReturnType<typeof setTimeout> | undefined

function line(parent: HTMLElement, text: string, strong = false): void {
  const row = document.createElement('div')

  row.className = strong ? 'font-semibold' : 'text-xs'
  row.textContent = text
  parent.append(row)
}

function showPopup(at: [number, number], lines: { text: string; strong?: boolean }[]): void {
  if (!lib || !map) return

  const content = document.createElement('div')

  content.className = 'flex max-w-64 flex-col gap-0.5 text-black'

  for (const entry of lines) line(content, entry.text, entry.strong)

  popup?.remove()
  popup = new lib.Popup({ closeButton: false, offset: 12, maxWidth: '18rem' })
    .setLngLat(at)
    .setDOMContent(content)
    .addTo(map)
}

function pointLines(properties: Record<string, unknown>) {
  return [
    { text: String(properties.entity), strong: true },
    { text: String(properties.rule || '-') },
    { text: `Response elevation ${String(properties.elevation)}` },
  ]
}

function pushData(): void {
  if (!map || !installed) return

  const plain = pointsGeoJson(props.points, props.now, props.fadeMs, props.accent)
  const trails = trailsGeoJson(props.trails, props.now, props.fadeMs)

  ;(map.getSource('clustered') as MapLibre.GeoJSONSource | undefined)?.setData(plain as never)
  ;(map.getSource('plain') as MapLibre.GeoJSONSource | undefined)?.setData(plain as never)
  ;(map.getSource('trails') as MapLibre.GeoJSONSource | undefined)?.setData(trails as never)

  if (host.value) {
    host.value.dataset.points = String(plain.features.length)
    host.value.dataset.trails = String(trails.features.length)
    host.value.dataset.impossible = String(
      trails.features.filter((feature) => feature.properties.impossible).length,
    )
  }
}

function install(): void {
  if (!map) return

  const glyphs = Boolean(map.getStyle()?.glyphs)

  map.addSource('clustered', {
    type: 'geojson',
    data: { type: 'FeatureCollection', features: [] },
    cluster: true,
    clusterRadius: 45,
    clusterMaxZoom: 11,
    clusterProperties: { elevation: ['+', ['get', 'elevation']] },
  })
  map.addSource('plain', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } })
  map.addSource('trails', { type: 'geojson', data: { type: 'FeatureCollection', features: [] } })

  map.addLayer({
    id: 'trails',
    type: 'line',
    source: 'trails',
    layout: { visibility: props.showTrails ? 'visible' : 'none', 'line-cap': 'round' },
    paint: {
      'line-color': ['case', ['get', 'impossible'], errorColour, props.accent],
      'line-width': ['case', ['get', 'impossible'], 3, 1.5],
      'line-opacity': ['get', 'opacity'],
      'line-dasharray': [2, 2],
    },
  })
  map.addLayer({
    id: 'heat',
    type: 'heatmap',
    source: 'plain',
    layout: { visibility: props.showHeat ? 'visible' : 'none' },
    paint: {
      'heatmap-weight': ['interpolate', ['linear'], ['get', 'elevation'], 0, 0.3, 10, 1],
      'heatmap-radius': 28,
      'heatmap-opacity': 0.7,
    },
  })
  map.addLayer({
    id: 'clusters',
    type: 'circle',
    source: 'clustered',
    filter: ['has', 'point_count'],
    paint: {
      'circle-color': props.accent,
      'circle-opacity': 0.85,
      'circle-radius': ['step', ['get', 'point_count'], 14, 10, 18, 50, 24],
      'circle-stroke-color': '#ffffff',
      'circle-stroke-width': 2,
    },
  })

  if (glyphs) {
    map.addLayer({
      id: 'cluster-count',
      type: 'symbol',
      source: 'clustered',
      filter: ['has', 'point_count'],
      layout: {
        'text-field': ['get', 'point_count_abbreviated'],
        'text-font': ['Noto Sans Regular'],
        'text-size': 12,
      },
      paint: { 'text-color': '#ffffff' },
    })
  }

  map.addLayer({
    id: 'halo',
    type: 'circle',
    source: 'plain',
    filter: ['==', ['get', 'fresh'], true],
    paint: {
      'circle-color': ['get', 'color'],
      'circle-opacity': 0.35,
      'circle-radius': 14,
    },
  })
  map.addLayer({
    id: 'points',
    type: 'circle',
    source: 'clustered',
    filter: ['!', ['has', 'point_count']],
    paint: {
      'circle-color': ['get', 'color'],
      'circle-opacity': ['get', 'opacity'],
      'circle-radius': ['interpolate', ['linear'], ['get', 'elevation'], 0, 6, 10, 12],
      'circle-stroke-color': '#ffffff',
      'circle-stroke-width': 1.5,
    },
  })
  map.addLayer({
    id: 'selected',
    type: 'circle',
    source: 'plain',
    filter: ['==', ['get', 'id'], props.selectedId ?? ''],
    paint: {
      'circle-color': 'rgba(0,0,0,0)',
      'circle-radius': 16,
      'circle-stroke-color': props.accent,
      'circle-stroke-width': 3,
    },
  })

  installed = true
  pushData()
  fitSoon()
}

function pulse(): void {
  if (!map || reducedMotion) return

  const phase = (performance.now() % 1600) / 1600

  if (installed && map.getLayer('halo')) {
    map.setPaintProperty('halo', 'circle-radius', 10 + phase * 18)
    map.setPaintProperty('halo', 'circle-opacity', 0.45 * (1 - phase))
  }

  frame = requestAnimationFrame(pulse)
}

function wire(): void {
  if (!map || !lib) return

  const active = map

  active.on('click', (event) => {
    const hit = active.queryRenderedFeatures(event.point, {
      layers: ['points', 'clusters', 'trails'].filter((id) => active.getLayer(id)),
    })

    if (hit.length === 0) {
      popup?.remove()
      emit('select', null)
    }
  })
  active.on('click', 'points', (event) => {
    const feature = event.features?.[0]

    if (feature) emit('select', String(feature.properties.id))
  })
  active.on('click', 'clusters', async (event) => {
    const feature = event.features?.[0]
    const source = active.getSource('clustered') as MapLibre.GeoJSONSource | undefined

    if (!feature || !source) return

    const zoom = await source.getClusterExpansionZoom(Number(feature.properties.cluster_id))

    active.easeTo({
      center: (feature.geometry as unknown as { coordinates: [number, number] }).coordinates,
      zoom,
    })
  })
  active.on('mouseenter', 'points', (event) => {
    active.getCanvas().style.cursor = 'pointer'

    const feature = event.features?.[0]

    if (feature) {
      showPopup(
        (feature.geometry as unknown as { coordinates: [number, number] }).coordinates,
        pointLines(feature.properties),
      )
    }
  })
  active.on('mouseleave', 'points', () => {
    active.getCanvas().style.cursor = ''

    if (!props.selectedId) popup?.remove()
  })
  active.on('mouseenter', 'clusters', () => {
    active.getCanvas().style.cursor = 'pointer'
  })
  active.on('mouseleave', 'clusters', () => {
    active.getCanvas().style.cursor = ''
  })
  active.on('mousemove', 'trails', (event) => {
    const feature = event.features?.[0]

    if (!feature) return

    const speed = Number(feature.properties.speed)
    const verdict =
      feature.properties.impossible === true || feature.properties.impossible === 'true'

    showPopup(
      [event.lngLat.lng, event.lngLat.lat],
      [
        { text: String(feature.properties.entity), strong: true },
        {
          text: `${String(feature.properties.kilometres)} km at ${speed < 0 ? 'an instant' : `${speed} km/h`}`,
        },
        { text: verdict ? 'Impossible travel' : 'Plausible travel' },
      ],
    )
  })
  active.on('mouseleave', 'trails', () => popup?.remove())
}

async function start(): Promise<void> {
  try {
    lib = await import('maplibre-gl')

    const { default: workerUrl } =
      await import('maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url')

    lib.setWorkerUrl(workerUrl)
    await import('maplibre-gl/dist/maplibre-gl.css')

    if (!host.value) return

    map = new lib.Map({
      container: host.value,
      style: styleFor(props.dark),
      center: [10, 25],
      zoom: 1.5,
      attributionControl: { compact: true },
    })
    map.addControl(new lib.NavigationControl({ showCompass: false }), 'top-right')
    map.addControl(new lib.FullscreenControl(), 'top-right')
    map.addControl(new lib.ScaleControl({ unit: 'metric' }), 'bottom-left')
    map.on('style.load', () => {
      installed = false
      install()
    })
    map.on('error', (event) => {
      if (host.value)
        host.value.dataset.error = String(event.error?.message ?? event.error ?? 'error')
    })
    wire()
    frame = requestAnimationFrame(pulse)
  } catch {
    emit('failed')
  }
}

function fitSoon(): void {
  if (!props.autoFit || props.points.length === 0 || props.points.length === fitted) return

  clearTimeout(fitTimer)
  fitTimer = setTimeout(() => {
    if (!map || !installed) return

    fitted = props.points.length
    map?.resize()
    fit(0)

    if (host.value) host.value.dataset.fitted = String(fitted)
  }, 300)
}

function fit(duration = 700): void {
  if (!map || !lib || props.points.length === 0) return

  const bounds = new lib.LngLatBounds()

  for (const point of props.points) bounds.extend([point.longitude, point.latitude])

  if (props.points.length === 1) {
    map.flyTo({ center: bounds.getCenter(), zoom: 4 })

    return
  }

  map.fitBounds(bounds, { padding: 90, maxZoom: 5, duration })
}

function flyTo(id: string): void {
  const found = props.points.find((point) => point.id === id)

  if (found && map)
    map.flyTo({ center: [found.longitude, found.latitude], zoom: Math.max(map.getZoom(), 5) })
}

defineExpose({ fit, flyTo })

watch(() => [props.points, props.trails, props.now, props.fadeMs, props.accent], pushData)

watch(() => props.points, fitSoon)

watch(
  () => props.showTrails,
  (on) => {
    if (installed && map?.getLayer('trails')) {
      map.setLayoutProperty('trails', 'visibility', on ? 'visible' : 'none')
    }
  },
)

watch(
  () => props.showHeat,
  (on) => {
    if (installed && map?.getLayer('heat')) {
      map.setLayoutProperty('heat', 'visibility', on ? 'visible' : 'none')
    }
  },
)

watch(
  () => props.dark,
  (dark) => {
    map?.setStyle(styleFor(dark))
  },
)

watch(
  () => props.selectedId,
  (id) => {
    if (installed && map?.getLayer('selected')) {
      map.setFilter('selected', ['==', ['get', 'id'], id ?? ''])
    }

    const found = id ? props.points.find((point) => point.id === id) : undefined

    if (found) {
      showPopup(
        [found.longitude, found.latitude],
        [
          { text: found.entity, strong: true },
          { text: found.ruleSummary || '-' },
          { text: `Response elevation ${found.responseElevation}` },
        ],
      )
    } else {
      popup?.remove()
    }
  },
)

onMounted(() => {
  void start()
})

onBeforeUnmount(() => {
  clearTimeout(fitTimer)
  cancelAnimationFrame(frame)
  popup?.remove()
  map?.remove()
  map = undefined
})
</script>

<template>
  <div
    ref="host"
    role="application"
    :aria-label="label"
    class="size-full min-h-80 overflow-hidden rounded-box border border-base-300 bg-base-200"
    data-testid="activation-map"
  ></div>
</template>
