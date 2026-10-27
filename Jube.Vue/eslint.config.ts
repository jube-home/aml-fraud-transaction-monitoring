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

import { globalIgnores } from 'eslint/config'
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import pluginVue from 'eslint-plugin-vue'
import pluginPlaywright from 'eslint-plugin-playwright'
import pluginVitest from '@vitest/eslint-plugin'
import pluginOxlint from 'eslint-plugin-oxlint'
import skipFormatting from 'eslint-config-prettier/flat'

// To allow more languages other than `ts` in `.vue` files, uncomment the following lines:
// import { configureVueProject } from '@vue/eslint-config-typescript'
// configureVueProject({ scriptLangs: ['ts', 'tsx'] })
// More info at https://github.com/vuejs/eslint-config-typescript/#advanced-setup

export default defineConfigWithVueTs(
  {
    name: 'app/files-to-lint',
    files: ['**/*.{vue,ts,mts,tsx}'],
  },

  globalIgnores(['public/vendor/**', '**/dist/**', '**/dist-ssr/**', '**/coverage/**']),

  ...pluginVue.configs['flat/essential'],
  vueTsConfigs.recommended,

  {
    ...pluginPlaywright.configs['flat/recommended'],
    files: ['e2e/**/*.{test,spec}.{js,ts,jsx,tsx}'],
  },

  {
    ...pluginVitest.configs.recommended,
    files: ['src/**/__tests__/*'],
  },

  {
    name: 'jube/house-rules',
    files: ['src/**/*.{vue,ts}'],
    rules: {
      'vue/no-v-html': 'error',
      'no-eval': 'error',
      'no-new-func': 'error',
      'no-implied-eval': 'error',
      'vue/no-bare-strings-in-template': [
        'error',
        { allowlist: ['.', ':', '(', ')', '-', '|', '/'] },
      ],
      'no-restricted-globals': [
        'error',
        { name: 'fetch', message: 'Call the API through useApi in src/api.' },
        { name: 'XMLHttpRequest', message: 'Call the API through useApi in src/api.' },
      ],
    },
  },

  {
    name: 'jube/jquery-only-in-the-rule-builder',
    files: ['src/**/*.{vue,ts}'],
    ignores: ['src/ruleBuilder/queryBuilderPlugin.ts', 'src/**/__tests__/**'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          paths: [
            {
              name: 'jquery',
              message: 'jQuery is confined to src/ruleBuilder/queryBuilderPlugin.ts.',
            },
          ],
        },
      ],
    },
  },

  {
    name: 'jube/api-may-fetch',
    files: ['src/api/**/*.ts', 'src/**/__tests__/**'],
    rules: { 'no-restricted-globals': 'off' },
  },

  {
    name: 'jube/tabulator-only-in-data-grid',
    files: ['src/**/*.{vue,ts}'],
    ignores: [
      'src/components/DataGrid.vue',
      'src/components/dataGridColumns.ts',
      'src/**/__tests__/**',
    ],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            { group: ['tabulator-tables'], message: 'Only DataGrid.vue imports Tabulator.' },
          ],
        },
      ],
    },
  },

  ...pluginOxlint.buildFromOxlintConfigFile('.oxlintrc.json'),

  skipFormatting,
)
