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
import type { ErrorSpan } from './types'

export interface Problem {
  source: 'compiler' | 'builder'
  line: number | null
  column: number | null
  endColumn: number | null
  text: string
}

function sentence(text: string): string {
  const trimmed = text.trim()

  return trimmed.length === 0 ? trimmed : trimmed[0]!.toUpperCase() + trimmed.slice(1)
}

export function compilerProblem(span: ErrorSpan): Problem {
  const prefixed = /^Line\s+(\d+):\s*(.*)$/s.exec(span.message)
  const line = span.line >= 0 ? span.line + 1 : prefixed ? Number(prefixed[1]) : null

  return {
    source: 'compiler',
    line,
    column: typeof span.column === 'number' ? span.column : null,
    endColumn: typeof span.endColumn === 'number' ? span.endColumn : null,
    text: sentence(prefixed ? prefixed[2]! : span.message),
  }
}

export function builderProblem(message: string): Problem {
  const split = /^([^:]{1,60}):\s+(.+)$/s.exec(message)
  const text = split ? `${sentence(split[1]!)}: ${split[2]!.replace(/\.$/, '')}` : sentence(message)

  return { source: 'builder', line: null, column: null, endColumn: null, text }
}

export function toProblems(spans: ErrorSpan[], builderMessages: string[]): Problem[] {
  const problems = spans.map(compilerProblem)

  for (const message of builderMessages) {
    const problem = builderProblem(message)

    if (
      !problems.some((existing) => existing.source === 'builder' && existing.text === problem.text)
    ) {
      problems.push(problem)
    }
  }

  return problems
}
