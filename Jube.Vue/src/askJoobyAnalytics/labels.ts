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
import { useI18n } from 'vue-i18n'
import { kindKey, sourceKey } from './model'

export function useAnalyticsLabels() {
  const { t } = useI18n()

  function kindLabel(kind: number): string {
    switch (kindKey(kind)) {
      case 'classifier':
        return t('analytics.kind.classifier')
      case 'regressor':
        return t('analytics.kind.regressor')
      case 'anomalyDetector':
        return t('analytics.kind.anomalyDetector')
      case 'clusterer':
        return t('analytics.kind.clusterer')
      case 'forecaster':
        return t('analytics.kind.forecaster')
      case 'sequenceModel':
        return t('analytics.kind.sequenceModel')
      case 'dimensionReducer':
        return t('analytics.kind.dimensionReducer')
      case 'densityEstimator':
        return t('analytics.kind.densityEstimator')
      default:
        return String(kind)
    }
  }

  function sourceLabel(source: number): string {
    switch (sourceKey(source)) {
      case 'agent':
        return t('analytics.source.agent')
      case 'upload':
        return t('analytics.source.upload')
      default:
        return String(source)
    }
  }

  return { kindLabel, sourceLabel }
}
