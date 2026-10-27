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

export type Translate = (key: string) => string

export interface SoonItem {
  id: string
  title: string
  body: string
}

export type SoonArea = 'summary' | 'activations' | 'network' | 'notes' | 'decide'

export function soonFor(area: SoonArea, t: Translate): SoonItem[] {
  switch (area) {
    case 'summary':
      return [
        {
          id: 'similar',
          title: t('caseDetail.soon.similar.title'),
          body: t('caseDetail.soon.similar.body'),
        },
        {
          id: 'risk',
          title: t('caseDetail.soon.risk.title'),
          body: t('caseDetail.soon.risk.body'),
        },
      ]
    case 'activations':
      return [
        {
          id: 'cards',
          title: t('caseDetail.soon.cards.title'),
          body: t('caseDetail.soon.cards.body'),
        },
        {
          id: 'suppress',
          title: t('caseDetail.soon.suppress.title'),
          body: t('caseDetail.soon.suppress.body'),
        },
        {
          id: 'facts',
          title: t('caseDetail.soon.facts.title'),
          body: t('caseDetail.soon.facts.body'),
        },
      ]
    case 'network':
      return [
        {
          id: 'explode',
          title: t('caseDetail.soon.explode.title'),
          body: t('caseDetail.soon.explode.body'),
        },
      ]
    case 'notes':
      return [
        {
          id: 'compose',
          title: t('caseDetail.soon.compose.title'),
          body: t('caseDetail.soon.compose.body'),
        },
        {
          id: 'research',
          title: t('caseDetail.soon.research.title'),
          body: t('caseDetail.soon.research.body'),
        },
      ]
    case 'decide':
      return [
        {
          id: 'suggest',
          title: t('caseDetail.soon.suggest.title'),
          body: t('caseDetail.soon.suggest.body'),
        },
        {
          id: 'file',
          title: t('caseDetail.soon.file.title'),
          body: t('caseDetail.soon.file.body'),
        },
        { id: 'rfi', title: t('caseDetail.soon.rfi.title'), body: t('caseDetail.soon.rfi.body') },
      ]
  }
}
