import {readFileSync, writeFileSync} from 'node:fs'
import {dirname, join, resolve} from 'node:path'
import {fileURLToPath} from 'node:url'

const here = dirname(fileURLToPath(import.meta.url))
const iconDir = join(here, '..', 'node_modules', 'lucide-vue-next', 'dist', 'esm', 'icons')
const target = resolve(here, '..', '..', '..', 'Jube.Charting', 'Diagrams', 'Icons', 'LucideIcons.g.cs')
const version = JSON.parse(readFileSync(join(here, '..', 'node_modules', 'lucide-vue-next', 'package.json'), 'utf8')).version
const names = [...new Set(JSON.parse(readFileSync(join(here, 'lucideIconNames.json'), 'utf8')))].sort()

const quote = (value) => '"' + String(value).replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"'

const entries = []

for (const name of names) {
  const source = readFileSync(join(iconDir, name + '.js'), 'utf8')
  const match = source.match(/createLucideIcon\("[^"]+",\s*(\[[\s\S]*?\])\s*\);/)

  if (!match) {
    throw new Error('Cannot read icon ' + name)
  }

  const nodes = new Function('return ' + match[1])()
  const elements = nodes.map(([tag, attributes]) => {
    const pairs = Object.entries(attributes)
      .filter(([key]) => key !== 'key')
      .map(([key, value]) => '(' + quote(key) + ', ' + quote(value) + ')')

    return 'new(' + quote(tag) + ', [' + pairs.join(', ') + '])'
  })

  entries.push('            [' + quote(name) + '] = [' + elements.join(', ') + ']')
}

const pascal = (name) => name.split('-').map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join('')
const typeDeclarations = readFileSync(join(here, '..', 'node_modules', 'lucide-vue-next', 'dist', 'lucide-vue-next.d.ts'), 'utf8')
const missing = names.filter((name) => !new RegExp('declare const ' + pascal(name) + ':').test(typeDeclarations))

if (missing.length > 0) {
  throw new Error('No lucide-vue-next export for ' + missing.join(', '))
}

const tsTarget = resolve(here, '..', 'src', 'graph', 'lucideIcons.generated.ts')
writeFileSync(tsTarget, header_ts())

function header_ts() {
  const imports = names.map((name) => '  ' + pascal(name) + ' as ' + pascal(name) + 'Icon,').join('\n')
  const entries = names.map((name) => "  '" + name + "': " + pascal(name) + 'Icon,').join('\n')

  return `import type {Component} from 'vue'
import {
${imports}
} from 'lucide-vue-next'

export const lucideIcons: Record<string, Component> = {
${entries}
}
`
}

const header = readFileSync(join(here, 'licenseHeader.txt'), 'utf8')

writeFileSync(target, header + `using System.Collections.Generic;

namespace Jube.Charting.Diagrams.Icons
{
    public static class LucideIcons
    {
        public const string Version = ${quote(version)};

        public static readonly IReadOnlyDictionary<string, IReadOnlyList<IconElement>> Elements =
            new Dictionary<string, IReadOnlyList<IconElement>>(System.StringComparer.Ordinal)
            {
${entries.join(',\n')}
            };
    }
}
`)
