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

import { describe, expect, it } from 'vitest'
import { activityHtml, attachmentsHtml, metaHtml, svgDiagramHtml } from '../render'

describe('activityHtml', () => {
  it('escapes the text and the detail', () => {
    const html = activityHtml('Step', '<b>x</b>', '<script>alert(1)</script>', null, null)

    expect(html).not.toContain('<script>')
    expect(html).not.toContain('<b>')
    expect(html).toContain('&lt;script&gt;')
  })

  it('is a disclosure only when there is detail', () => {
    expect(activityHtml('Step', 'a', 'b', null, null)).toContain('<details')
    expect(activityHtml('Step', 'a', null, null, null)).not.toContain('<details')
  })

  it('draws the tool icon when one is known', () => {
    expect(activityHtml('Step', 'a', null, 3, null)).toContain('jooby-tool-icon')
    expect(activityHtml('Step', 'a', null, 99999, null)).not.toContain('jooby-tool-icon')
  })
})

describe('activityHtml agent banners', () => {
  it('gives each boundary kind its own class and glyph', () => {
    expect(activityHtml('Delegation', 'Launching Zubr', null, null, null)).toContain(
      'jooby-banner-avatar',
    )

    for (const kind of ['Handoff', 'Validation', 'Revision', 'Guard'] as const) {
      const html = activityHtml(kind, 'x', null, null, null)

      expect(html).toContain(`jooby-activity-${kind.toLowerCase()}`)
      expect(html).toContain('jooby-banner-glyph')
    }
  })

  it('shows Jadek the same way whether it is checking or asking for a revision', () => {
    const glyphOf = (html: string) => html.match(/<svg class="jooby-banner-glyph".*?<\/svg>/)?.[0]

    expect(glyphOf(activityHtml('Validation', 'x', null, null, null))).toBe(
      glyphOf(activityHtml('Revision', 'x', null, null, null)),
    )
  })

  it('leaves an ordinary step with its tool icon and no banner glyph', () => {
    expect(activityHtml('Step', 'x', null, 3, null)).not.toContain('jooby-banner-glyph')
  })
})

describe('metaHtml pills', () => {
  it('puts the time and each statistic in its own pill', () => {
    const html = metaHtml({
      timestamp: '2026-10-20T10:00:00Z',
      stats: { inputTokens: 5, outputTokens: 7, durationMs: 1500 },
    })

    expect(html.match(/class="jooby-pill"/g)).toHaveLength(4)
    expect(html).toContain('>↑5<')
    expect(html).toContain('>1.5s<')
  })
})

describe('metaHtml', () => {
  it('offers feedback only for a message that can have it', () => {
    expect(metaHtml({ messageId: 7 })).toContain('data-message-id="7"')
    expect(metaHtml({ messageId: null })).not.toContain('jooby-feedback')
  })

  it('locks both buttons once an answer is chosen', () => {
    const html = metaHtml({ messageId: 7, chosen: true })

    expect(html.match(/ disabled/g)).toHaveLength(2)
    expect(html).toContain('jooby-feedback-chosen')
  })
})

describe('attachmentsHtml', () => {
  it('escapes names and links to the file endpoint', () => {
    const html = attachmentsHtml([{ id: 4, name: '"><x>.csv' }], (id) => `/files/${id}`)

    expect(html).toContain('href="/files/4"')
    expect(html).not.toContain('"><x>')
  })
})

describe('svgDiagramHtml', () => {
  it('embeds only a whole svg document as an image', () => {
    expect(svgDiagramHtml('d', '<svg viewBox="0 0 1 1"></svg>')).toContain('data:image/svg+xml')
    expect(svgDiagramHtml('d', '<div><svg></svg></div>')).toBeUndefined()
    expect(svgDiagramHtml('d', '<svg>')).toBeUndefined()
  })
})
