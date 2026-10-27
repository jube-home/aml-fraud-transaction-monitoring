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

import './assets/main.css'

import { createVaporApp, vaporInteropPlugin } from 'vue'
import { createPinia } from 'pinia'
import { VueQueryPlugin } from '@tanstack/vue-query'

import App from './App.vue'
import router from './router'
import { createI18nInstance, loadLocale } from './i18n'
import { queryClient } from './api/queryClient'
import { setApiHandlers } from './api/useApi'
import { useUiStore } from './stores/ui'

const app = createVaporApp(App)

app.use(vaporInteropPlugin)
app.use(createPinia())
app.use(router)
app.use(VueQueryPlugin, { queryClient })

const ui = useUiStore()
const i18n = createI18nInstance()
ui.applyTheme()
await loadLocale(i18n.global, ui.locale)
app.use(i18n)

setApiHandlers({
  onUnauthorized: () => {
    void router.push({
      name: 'login',
      query: { redirect: router.currentRoute.value.fullPath },
    })
  },
})

app.mount('#app')
