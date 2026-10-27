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
export const indentUnit = '  '
const wrapColumn = 90

type Atom =
  | { kind: 'space' }
  | { kind: 'open' }
  | { kind: 'close' }
  | { kind: 'comma' }
  | { kind: 'literal'; text: string }
  | { kind: 'comment'; text: string }
  | { kind: 'word'; text: string }

function atomise(line: string): Atom[] {
  const atoms: Atom[] = []
  let index = 0

  while (index < line.length) {
    const character = line[index]!

    if (character === ' ' || character === '\t') {
      while (line[index] === ' ' || line[index] === '\t') index++

      atoms.push({ kind: 'space' })
    } else if (character === '"') {
      let end = index + 1

      while (end < line.length) {
        if (line[end] === '"' && line[end + 1] === '"') end += 2
        else if (line[end] === '"') break
        else end++
      }

      atoms.push({ kind: 'literal', text: line.slice(index, end + 1) })
      index = end + 1
    } else if (character === "'") {
      atoms.push({ kind: 'comment', text: line.slice(index) })
      index = line.length
    } else if (character === '(') {
      atoms.push({ kind: 'open' })
      index++
    } else if (character === ')') {
      atoms.push({ kind: 'close' })
      index++
    } else if (character === ',') {
      atoms.push({ kind: 'comma' })
      index++
    } else {
      let end = index

      while (end < line.length && !' \t"\'(),'.includes(line[end]!)) end++

      atoms.push({ kind: 'word', text: line.slice(index, end) })
      index = end
    }
  }

  return atoms
}

function render(atoms: Atom[]): string {
  const parts: string[] = []

  atoms.forEach((atom, position) => {
    const previous = atoms[position - 1]
    const next = atoms[position + 1]

    switch (atom.kind) {
      case 'space':
        if (!previous || previous.kind === 'open') return
        if (!next || next.kind === 'close' || next.kind === 'comma') return

        parts.push(' ')
        return
      case 'open':
        parts.push('(')
        return
      case 'close':
        parts.push(')')
        return
      case 'comma':
        parts.push(',')
        return
      default:
        parts.push(atom.text)
    }
  })

  return parts.join('')
}

function tidy(line: string): string {
  return render(atomise(line)).trim()
}

function depthChange(text: string): number {
  return atomise(text).reduce(
    (depth, atom) => depth + (atom.kind === 'open' ? 1 : atom.kind === 'close' ? -1 : 0),
    0,
  )
}

function hasComment(text: string): boolean {
  return atomise(text).some((atom) => atom.kind === 'comment')
}

function endsWithThen(text: string): boolean {
  return /\sthen$/i.test(text)
}

function startsConditional(text: string): boolean {
  return /^(if|elseif)\b/i.test(text)
}

const logicalOperators = new Set(['and', 'or', 'andalso', 'orelse'])

interface Line {
  level: number
  text: string
}

function splitTopLevel(expression: string): { terms: string[]; operators: string[] } {
  const terms: string[] = []
  const operators: string[] = []
  let depth = 0
  let current: Atom[] = []

  for (const atom of atomise(expression)) {
    if (atom.kind === 'open') depth++
    if (atom.kind === 'close') depth--

    if (depth === 0 && atom.kind === 'word' && logicalOperators.has(atom.text.toLowerCase())) {
      terms.push(render(current).trim())
      operators.push(atom.text)
      current = []
    } else {
      current.push(atom)
    }
  }

  terms.push(render(current).trim())

  return { terms, operators }
}

function wholeGroup(term: string): { prefix: string; inner: string } | null {
  const match = /^(not\s+)?\(/i.exec(term)

  if (!match) return null

  let depth = 0
  const atoms = atomise(term)
  let consumed = 0

  for (const [position, atom] of atoms.entries()) {
    if (atom.kind === 'open') depth++
    if (atom.kind === 'close') depth--

    consumed = position

    if (depth === 0 && atom.kind === 'close') break
  }

  const rest = atoms.slice(consumed + 1)

  if (rest.some((atom) => atom.kind !== 'space')) return null

  const firstOpen = atoms.findIndex((atom) => atom.kind === 'open')
  const inner = render(atoms.slice(firstOpen + 1, consumed)).trim()

  return { prefix: match[1] ? 'NOT ' : '', inner }
}

function layoutTerms(expression: string, level: number): Line[] {
  const { terms, operators } = splitTopLevel(expression)
  const lines: Line[] = []

  terms.forEach((term, position) => {
    const operator = operators[position]
    const suffix = operator ? ` ${operator}` : ''
    const inline = term + suffix
    const group = wholeGroup(term)

    if (!group || level * indentUnit.length + inline.length <= wrapColumn) {
      lines.push({ level, text: inline })

      return
    }

    lines.push({ level, text: `${group.prefix}(` })
    lines.push(...layoutTerms(group.inner, level + 1))
    lines.push({ level, text: `)${suffix}` })
  })

  return lines
}

function layoutConditional(statement: string): Line[] {
  const keyword = /^(if|elseif)\s*/i.exec(statement)![0]
  const expression = statement.slice(keyword.length, statement.length - 4).trim()
  const head = keyword.trim()

  if (statement.length <= wrapColumn) return [{ level: 0, text: statement }]

  const group = wholeGroup(expression)

  if (group && group.prefix === '') {
    return [
      { level: 0, text: `${head} (` },
      ...layoutTerms(group.inner, 1),
      { level: 0, text: ') Then' },
    ]
  }

  const lines = layoutTerms(expression, 1)

  return [
    { level: 0, text: head },
    ...lines.slice(0, -1),
    { level: 1, text: `${lines[lines.length - 1]!.text} Then` },
  ]
}

function withoutComment(line: string): string {
  return render(atomise(line).filter((atom) => atom.kind !== 'comment')).trim()
}

function gatherStatement(lines: string[], start: number): { lines: Line[]; end: number } | null {
  const first = tidy(lines[start]!)

  if (!startsConditional(first)) return null

  const firstCode = withoutComment(first)

  if (endsWithThen(firstCode) && depthChange(firstCode) === 0) {
    return { lines: layoutConditional(first), end: start }
  }

  let joined = first
  let depth = depthChange(firstCode)
  let joinable = !hasComment(first) && !/\s_$/.test(first)
  const kept: Line[] = [{ level: 0, text: first }]

  for (let index = start + 1; index < lines.length && index < start + 60; index++) {
    const line = tidy(lines[index]!)
    const code = withoutComment(line)

    if (line === '') return null

    if (hasComment(line) || /\s_$/.test(line)) joinable = false

    kept.push({ level: 1, text: line })
    joined = tidy(`${joined} ${line}`)
    depth += depthChange(code)

    if (depth <= 0 && endsWithThen(code)) {
      return { lines: joinable ? layoutConditional(joined) : kept, end: index }
    }
  }

  return null
}

const closers = /^(end\s+(if|select|sub|function|try|with|while)\b|next\b|loop\b|wend\b)/i
const middles = /^(else\b|elseif\b|case\b|catch\b|finally\b)/i
const openers =
  /^((public\s+|private\s+)?(shared\s+)?(sub|function)\b|for\b|while\b|do\b|select\s+case\b|try$|with\b)/i

function layoutRuleText(text: string): { output: string[]; depth: number } {
  const physical = text.split(/\r\n|\r|\n/)
  const statements: Line[][] = []
  let index = 0

  while (index < physical.length) {
    const gathered = gatherStatement(physical, index)

    if (gathered) {
      statements.push(gathered.lines)
      index = gathered.end + 1
    } else {
      statements.push([{ level: 0, text: tidy(physical[index]!) }])
      index++
    }
  }

  const output: string[] = []
  let depth = 0

  for (const lines of statements) {
    const first = lines[0]!.text

    if (first === '') {
      if (output.length > 0 && output[output.length - 1] !== '') output.push('')

      continue
    }

    let printedAt = depth
    let opens = false

    if (closers.test(first)) {
      depth = Math.max(depth - 1, 0)
      printedAt = depth
    } else if (middles.test(first)) {
      printedAt = Math.max(depth - 1, 0)
    } else {
      opens =
        openers.test(first) ||
        (startsConditional(first) && endsWithThen(lines[lines.length - 1]!.text))
    }

    for (const line of lines) output.push(indentUnit.repeat(printedAt + line.level) + line.text)

    if (opens) depth += 1
  }

  while (output.length > 0 && output[output.length - 1] === '') output.pop()

  return { output, depth }
}

export function formatRuleText(text: string): string {
  return layoutRuleText(text).output.join('\n')
}

export function blockDepthAfter(text: string): number {
  return layoutRuleText(text).depth
}

export function indentLevelFor(textBefore: string, currentLine: string): number {
  const depth = blockDepthAfter(textBefore)
  const line = tidy(currentLine)

  return closers.test(line) || middles.test(line) ? Math.max(depth - 1, 0) : depth
}

export const dedentOnInput =
  /^\s*(end\s+(if|select|sub|function|try|with|while)|else|elseif|case|catch|finally|next|loop|wend)$/i
