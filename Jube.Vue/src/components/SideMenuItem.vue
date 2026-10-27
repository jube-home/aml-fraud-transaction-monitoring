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
import { RouterLink, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { containsRoute } from '../layout/menu'
import type { MenuNode } from '../layout/menuTree'
import { legacyUrl } from '../layout/legacyUrl'

const props = defineProps<{ node: MenuNode }>()
const { t } = useI18n()
const route = useRoute()

const text = (node: MenuNode): string => (node.labelKey ? t(node.labelKey) : (node.label ?? ''))
</script>

<template>
  <li>
    <details v-if="props.node.children" :open="containsRoute(props.node, String(route.name ?? ''))">
      <summary>
        <component :is="props.node.icon" v-if="props.node.icon" aria-hidden="true" class="size-4" />
        {{ text(props.node) }}
      </summary>
      <ul>
        <SideMenuItem v-for="child in props.node.children" :key="child.key" :node="child" />
      </ul>
    </details>
    <RouterLink
      v-else-if="props.node.routeName"
      :to="{ name: props.node.routeName }"
      class="aria-[current=page]:bg-base-200! aria-[current=page]:font-semibold aria-[current=page]:text-base-content!"
      :class="{
        'bg-base-200! font-semibold text-base-content!':
          route.meta.menuRoute === props.node.routeName,
      }"
    >
      <component :is="props.node.icon" v-if="props.node.icon" aria-hidden="true" class="size-4" />
      {{ text(props.node) }}
    </RouterLink>
    <a v-else :href="legacyUrl(props.node.legacyPath ?? '/')">
      <component :is="props.node.icon" v-if="props.node.icon" aria-hidden="true" class="size-4" />
      {{ text(props.node) }}
    </a>
  </li>
</template>
