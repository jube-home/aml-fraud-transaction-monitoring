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
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useJoobyWidgetStore } from '../../askjooby/widgetState'

const { t } = useI18n()
const store = useJoobyWidgetStore()
const { open } = storeToRefs(store)
</script>

<template>
  <button
    id="jooby-launcher"
    type="button"
    class="relative size-8 shrink-0 overflow-hidden rounded-full p-0 focus-visible:outline-3 focus-visible:outline-offset-2 focus-visible:outline-primary"
    :aria-expanded="open"
    aria-controls="jooby-widget-panel"
    :aria-label="open ? t('askJooby.closeChat') : t('askJooby.openChat')"
    data-testid="widget-launcher"
    @click="store.toggle()"
  >
    <img
      src="/images/askjooby-launcher-closed.png"
      alt=""
      width="32"
      height="32"
      class="absolute inset-0 size-full rounded-full object-cover transition duration-200 motion-reduce:transition-none"
      :class="open ? 'scale-90 rotate-90 opacity-0' : 'opacity-100'"
    />
    <img
      src="/images/askjooby-launcher-open.png"
      alt=""
      width="32"
      height="32"
      class="absolute inset-0 size-full rounded-full object-cover transition duration-200 motion-reduce:transition-none"
      :class="open ? 'opacity-100' : 'scale-90 -rotate-90 opacity-0'"
    />
  </button>
</template>
