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
import {useId} from 'vue'
import FieldHelp from '../../components/FieldHelp.vue'

defineProps<{ title: string; help?: string; testid: string }>()

const headingId = useId()
</script>

<template>
  <section class="card card-sm min-w-0 bg-base-200" :aria-labelledby="headingId" :data-testid="testid">
    <div class="card-body min-w-0 gap-3 p-3">
      <div class="flex min-w-0 items-center gap-3">
        <span
          class="grid size-9 shrink-0 place-items-center rounded-field bg-base-100"
          aria-hidden="true"
        >
          <slot name="icon"/>
        </span>
        <h4 :id="headingId" class="min-w-0 grow text-sm font-medium">{{ title }}</h4>
        <slot name="status"/>
        <FieldHelp v-if="help" key="help" :label="title" :text="help"/>
      </div>
      <slot/>
    </div>
  </section>
</template>
