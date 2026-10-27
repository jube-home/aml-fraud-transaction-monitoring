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

import type { MessageStyles } from 'deep-chat/dist/types/messages'
import type { CustomStyle } from 'deep-chat/dist/types/styles'

const surface = 'var(--color-base-100)'
const text = 'var(--color-base-content)'
const line = 'var(--color-base-300)'
const muted = 'var(--color-base-200)'

const bubble = (style: CustomStyle): { bubble: CustomStyle } => ({
  bubble: {
    borderRadius: 'var(--radius-box)',
    fontFamily: 'inherit',
    fontSize: '0.875rem',
    maxWidth: '85%',
    ...style,
  },
})

const flat = (style: CustomStyle): { outerContainer: CustomStyle; bubble: CustomStyle } => ({
  outerContainer: { marginInlineStart: '2.25rem' },
  bubble: {
    backgroundColor: 'transparent',
    border: 'none',
    boxShadow: 'none',
    padding: '0',
    maxWidth: '100%',
    fontFamily: 'inherit',
    fontSize: '0.8125rem',
    color: text,
    ...style,
  },
})

const roles = {
  activity: flat({}),
  banner: flat({ width: '100%' }),
  guard: flat({ width: '100%' }),
  failure: flat({}),
  meta: flat({ fontSize: '0.75rem' }),
  files: flat({}),
  who: flat({ fontSize: '0.75rem' }),
  thinking: bubble({
    backgroundColor: muted,
    color: text,
    fontStyle: 'italic',
    border: `1px dashed ${line}`,
  }),
  thought: bubble({ backgroundColor: muted, color: text, border: `1px dashed ${line}` }),
}

export const messageStyles: MessageStyles = {
  default: {
    shared: bubble({}),
    user: bubble({
      backgroundColor: 'var(--color-primary)',
      color: 'var(--color-primary-content)',
    }),
    ai: bubble({ backgroundColor: surface, color: text, border: `1px solid ${line}` }),
    ...roles,
  },
  html: {
    shared: flat({}),
    ...roles,
  },
  loading: undefined,
}

export const chatStyle: CustomStyle = {
  width: '100%',
  height: '100%',
  border: `1px solid ${line}`,
  borderRadius: 'var(--radius-box)',
  backgroundColor: 'var(--color-base-200)',
  fontFamily: 'inherit',
  color: text,
}

export const inputAreaStyle: CustomStyle = {
  backgroundColor: surface,
  borderTop: `1px solid ${line}`,
}

export const textInputStyles = {
  container: {
    backgroundColor: surface,
    border: '1px solid var(--color-base-content)',
    borderRadius: 'var(--radius-field)',
    boxShadow: 'none',
  },
  text: { color: text, fontFamily: 'inherit', fontSize: '0.875rem' },
}

export const auxiliaryStyle = `
  .jooby-activity { display: flex; align-items: center; gap: 0.5rem; color: var(--color-base-content); }
  details.jooby-activity { display: block; }
  details.jooby-activity > summary { display: flex; align-items: center; gap: 0.5rem; cursor: pointer; list-style: none; }
  details.jooby-activity > summary::-webkit-details-marker { display: none; }
  details.jooby-activity > summary::after { content: '\\203A'; margin-inline-start: 0.25rem; }
  details.jooby-activity[open] > summary::after { content: '\\2304'; }
  .jooby-detail { margin: 0.25rem 0 0; padding: 0.5rem; white-space: pre-wrap; word-break: break-word; font-family: var(--font-mono, monospace); font-size: 0.75rem; color: var(--color-base-content); background: var(--color-base-100); border: 1px solid var(--color-base-300); border-radius: var(--radius-field); }
  div.jooby-activity-step, details.jooby-activity-step > summary { display: inline-flex; max-width: 100%; align-items: center; gap: 0.5rem; padding: 0.25rem 0.75rem; border: 1px solid var(--color-base-300); border-radius: var(--radius-selector); background: var(--color-base-100); color: var(--color-base-content); }
  details.jooby-activity-step { display: block; }
  details.jooby-activity-step[open] > summary { border-end-start-radius: var(--radius-field); border-end-end-radius: var(--radius-field); }
  .jooby-tool-icon { width: 1rem; height: 1rem; flex: none; }
  .jooby-pill { display: inline-flex; align-items: center; padding: 0.0625rem 0.5rem; border: 1px solid var(--color-base-300); border-radius: var(--radius-selector); background: var(--color-base-100); color: var(--color-base-content); font-family: var(--font-mono, ui-monospace, monospace); font-size: 0.6875rem; font-variant-numeric: tabular-nums; white-space: nowrap; }
  .jooby-stamp { margin-inline-start: auto; }
  .jooby-activity-failure, .jooby-activity-error { color: var(--color-error); font-weight: 600; }
  div.jooby-activity-delegation, details.jooby-activity-delegation > summary, div.jooby-activity-handoff, details.jooby-activity-handoff > summary, div.jooby-activity-validation, details.jooby-activity-validation > summary, div.jooby-activity-revision, details.jooby-activity-revision > summary, div.jooby-activity-guard, details.jooby-activity-guard > summary { display: flex; align-items: center; gap: 0.5rem; box-sizing: border-box; width: 100%; height: 1.5rem; padding: 0 0.75rem; border-radius: 2rem; color: #fff; font-size: 0.75rem; font-weight: 600; }
  details.jooby-activity-delegation, details.jooby-activity-handoff, details.jooby-activity-validation, details.jooby-activity-revision, details.jooby-activity-guard { display: block; }
  div.jooby-activity-delegation, details.jooby-activity-delegation > summary { background: linear-gradient(90deg, #5b3fa0, #7a52c4); box-shadow: 0 2px 6px rgb(91 63 160 / 0.25); }
  div.jooby-activity-handoff, details.jooby-activity-handoff > summary { background: linear-gradient(90deg, #11555f, #177a7e); box-shadow: 0 2px 6px rgb(17 85 95 / 0.25); }
  div.jooby-activity-validation, details.jooby-activity-validation > summary, div.jooby-activity-revision, details.jooby-activity-revision > summary { background: linear-gradient(90deg, #c9a000, #e0bb00); box-shadow: 0 2px 6px rgb(169 134 0 / 0.3); color: #241c00; }
  div.jooby-activity-guard, details.jooby-activity-guard > summary { background: linear-gradient(90deg, #7a1212, #b73333); box-shadow: 0 2px 6px rgb(122 18 18 / 0.3); }
  .jooby-activity-delegation .jooby-pill, .jooby-activity-handoff .jooby-pill, .jooby-activity-validation .jooby-pill, .jooby-activity-revision .jooby-pill, .jooby-activity-guard .jooby-pill { background: transparent; border-color: currentColor; color: inherit; }
  .jooby-banner-glyph { width: 1rem; height: 1rem; flex: none; }
  .jooby-banner-avatar { border-radius: 50%; object-fit: cover; }
  .jooby-activity-text { min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .jooby-meta { display: flex; flex-wrap: wrap; align-items: center; gap: 0.375rem; }
  .jooby-feedback { display: inline-flex; padding: 0.25rem; border-radius: var(--radius-field); border: 1px solid transparent; background: transparent; color: inherit; cursor: pointer; }
  .jooby-feedback:hover:not([disabled]) { border-color: var(--color-base-300); background: var(--color-base-100); }
  .jooby-feedback-chosen { background: var(--color-base-100); border-color: var(--color-base-content); }
  .jooby-chip { display: inline-flex; align-items: center; gap: 0.25rem; margin-inline-end: 0.5rem; padding: 0.125rem 0.5rem; border: 1px solid var(--color-base-300); border-radius: 2rem; background: var(--color-base-100); color: var(--color-base-content); font-size: 0.75rem; text-decoration: none; }
  .jooby-diagram { max-width: 100%; background: white; border: 1px solid var(--color-base-300); border-radius: var(--radius-field); }
`

const avatar = {
  src: '/images/askjooby-avatar.png',
  styles: {
    avatar: { width: '28px', height: '28px', borderRadius: '50%', objectFit: 'cover' },
    container: { marginInlineEnd: '0.5rem' },
  },
}

const noAvatar = { styles: { container: { display: 'none' }, avatar: { display: 'none' } } }

export const avatars = {
  ai: avatar,
  thinking: avatar,
  thought: avatar,
  user: noAvatar,
  activity: noAvatar,
  banner: noAvatar,
  guard: noAvatar,
  failure: noAvatar,
  meta: noAvatar,
  files: noAvatar,
  who: noAvatar,
}
