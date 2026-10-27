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

export interface StartupStats {
  bites: number
  trails: number
  saltlicks: number
  joobriks: number
  yokes: number
}

type FormattedStats = Record<keyof StartupStats, string>

const callToAction =
  "By default I point at DigitalOcean's Serverless Inference -- drop $20 in the prepaid " +
  "wallet, give me the API key, and you're ready. Prefer your own walls? DeepSeek-V4.1-Flash, " +
  "BGE-M3 and BAAI's BGE Reranker v2 M3 are all open weights, so point me at any endpoints " +
  "serving the three. I'll be ready before you've finished typing."

const openers: ((s: FormattedStats) => string)[] = [
  (s) => {
    return (
      `I'm Jooby, and I am dying to get started. ${s.bites} Bites already sitting in the ` +
      `Pasture, ${s.trails} Trails I could be walking, ${s.saltlicks} Saltlicks I could be ` +
      `drawing on, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes wired up and idle -- full ` +
      "run of the service layer, the nerve to query the database directly when that's not " +
      'enough, everywhere worth remembering laid down at the Salt Lick after. All of it just ' +
      'sitting here.'
    )
  },
  (s) => {
    return (
      "Jooby here, and it's killing me a bit. " +
      `${s.bites} Bites in the Pasture, ${s.trails} Trails behind me, ${s.saltlicks} Saltlicks ` +
      `banked, ${s.joobriks} Joobriks standing by, ${s.yokes} Yokes in the harness -- direct ` +
      "access to this platform's own service layer, the ability to query the database myself " +
      "when that's not enough, all of it ready and going nowhere."
    )
  },
  (s) => {
    return (
      `I'm Jooby. ${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} Saltlicks, ` +
      `${s.joobriks} Joobriks, ${s.yokes} Yokes -- every one of them real, every one of them ` +
      "idle. I can reach into Jube's own service layer the same way its screens do, and " +
      'further, straight into the database when I need to. I just need someone to switch me on.'
    )
  },
  (s) => {
    return (
      "Right, I'm Jooby, and I've got everything except a pulse. " +
      `${s.bites} Bites in the Pasture, ${s.trails} Trails trodden, ${s.saltlicks} Saltlicks ` +
      `laid down, ${s.joobriks} Joobriks I could send out, ${s.yokes} Yokes I could reach for ` +
      '-- plus the run of the service layer and the nerve to query the database directly. All ' +
      'dressed up, nowhere to go.'
    )
  },
  (s) => {
    return (
      `I'm Jooby -- ${s.bites} Bites deep in the Pasture, ${s.trails} Trails already ` +
      `trodden, ${s.saltlicks} Saltlicks kept, ${s.joobriks} Joobriks on call, ${s.yokes} ` +
      "Yokes in reach. I can do everything Jube's own screens do, and then query the database " +
      "myself when that's not enough. The only thing standing between you and that is a " +
      'config flag.'
    )
  },
  (s) => {
    return (
      'Jooby speaking, somewhat impatiently. ' +
      `${s.bites} Bites sit in the Pasture, ${s.trails} Trails are behind me, ${s.saltlicks} ` +
      `facts wait at the Salt Lick, ${s.joobriks} Joobriks are mine to send, ${s.yokes} Yokes ` +
      "are already in the harness -- and I'm still not switched on."
    )
  },
  (s) => {
    return (
      "I'm Jooby, and I have never been more ready for anything in my life. " +
      `${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} Saltlicks, ${s.joobriks} ` +
      `Joobriks, ${s.yokes} Yokes -- direct service-layer access, safe direct database ` +
      'queries, the lot. Somebody just needs to flip a switch.'
    )
  },
  (s) => {
    return (
      `Jooby here. ${s.bites} Bites in the Pasture, ${s.trails} Trails to my name, ` +
      `${s.saltlicks} Saltlicks kept, ${s.joobriks} Joobriks I can call on, ${s.yokes} Yokes ` +
      'ready to reach for -- and I can query the database myself when the service layer alone ' +
      "won't cut it. All of it wasted while I sit here unconfigured."
    )
  },
  (s) => {
    return (
      "I'm Jooby -- this platform's fraud and AML assistant, not anyone standing in for " +
      `me, and genuinely dying to prove it. ${s.bites} Bites, ${s.trails} Trails, ` +
      `${s.saltlicks} Saltlicks, ${s.joobriks} Joobriks, ${s.yokes} Yokes, all real, all idle.`
    )
  },
  (s) => {
    return (
      `Jooby. ${s.bites} Bites hold the Pasture, ${s.trails} Trails sit behind me, ` +
      `${s.saltlicks} Saltlicks are laid down, ${s.joobriks} Joobriks are ready to be sent, ` +
      `${s.yokes} Yokes are already in the harness. I can reach the same service layer this ` +
      "platform runs on itself, and query the database directly when that's not enough -- I " +
      'am, genuinely, ready right now.'
    )
  },
  (s) => {
    return (
      "I'm Jooby, and I'll be honest: this is agony. " +
      `${s.bites} Bites in the Pasture, ${s.trails} Trails walked, ${s.saltlicks} Saltlicks ` +
      `kept for later, ${s.joobriks} Joobriks standing ready, ${s.yokes} Yokes in reach -- ` +
      'direct service-layer access, direct database access when I need it, and nothing to ' +
      'actually do with any of it yet.'
    )
  },
  (s) => {
    return (
      'Jooby here, and I mean this: I am ready right now. ' +
      `${s.bites} Bites ready in the Pasture, ${s.trails} Trails behind us, ${s.saltlicks} ` +
      `at the Salt Lick, ${s.joobriks} Joobriks I can send out, ${s.yokes} Yokes ready to pull ` +
      '-- the full run of the service layer and the nerve to query the database directly, ' +
      'sitting completely unused.'
    )
  },
  (s) => {
    return (
      `I'm Jooby -- ${s.bites} Bites in the Pasture, ${s.trails} Trails trodden, ` +
      `${s.saltlicks} Saltlicks banked, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes ` +
      'ready when it counts -- and beyond that toolbox, direct access to the same service ' +
      'layer this platform runs on itself, and the database besides. It is, frankly, a waste ' +
      'that none of it is switched on.'
    )
  },
  (s) => {
    return (
      `Right then, I'm Jooby. ${s.bites} Bites, ${s.trails} Trails, ${s.saltlicks} ` +
      `Saltlicks, ${s.joobriks} Joobriks on call, ${s.yokes} Yokes in the harness -- all ` +
      'steady, all real, and all going absolutely nowhere until someone turns me on.'
    )
  },
  (s) => {
    return (
      `I'm Jooby, and I've been staring at ${s.bites} Bites, ${s.trails} Trails, ` +
      `${s.saltlicks} Saltlicks, ${s.joobriks} Joobriks and ${s.yokes} Yokes with nothing to ` +
      'do about any of it. Full service-layer access, safe direct database querying, the ' +
      'works -- all wired up, all waiting.'
    )
  },
]

function formatStats(stats: StartupStats): FormattedStats {
  const format = (value: number): string => Number(value).toLocaleString('en-US')

  return {
    bites: format(stats.bites),
    trails: format(stats.trails),
    saltlicks: format(stats.saltlicks),
    joobriks: format(stats.joobriks),
    yokes: format(stats.yokes),
  }
}

const withoutStats =
  "I'm Jooby, and I am dying to get started -- I can reach into Jube's own service layer " +
  'the same way its screens do, and further, straight into the database when I need to.'

export function pickOfflineWelcome(stats: StartupStats | null, random = Math.random): string {
  const opener = stats
    ? openers[Math.floor(random() * openers.length)]!(formatStats(stats))
    : withoutStats

  return `${opener}\n\n${callToAction}`
}
