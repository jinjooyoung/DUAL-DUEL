// DUAL DUEL: shared look for the CRT card-battle effects.
// The game's post-process turns everything into green luminance, so these effects are authored in a
// phosphor-green ramp and read by brightness and motion, not by hue.
//   positive: clean scan reveals, upward motion, expanding outlines, steady glow
//   negative: torn rows, jitter, flicker, downward motion, "error" brackets

import { ramp, rng, hash, clamp, WHITE } from "../../core/index.js";
import { ICONS } from "./_icons.js";

// dark -> bright, sampled from the game's screen (brightest text is #8affa6)
export const PHOS = ["#020a04", "#06200e", "#0c3c1c", "#16642e", "#24964a", "#4ccc70", "#8affa6", "#e2ffea"];
export const C = ramp(PHOS);
export const HOT = WHITE;

// every effect is drawn on a 96 x 96 canvas around the portrait's center (a portrait is ~78 px)
export const SIZE = [96, 96];
export const ORIGIN = [48, 48];
export const FPS = 20;

const glyphs = {};
function glyph(name) {
  if (glyphs[name]) return glyphs[name];
  const rows = ICONS[name].map((r) => [...r].map(Number));
  // the icon frame leaves a pixel in each corner: drop it
  const n = rows.length - 1;
  rows[0][0] = rows[0][n] = rows[n][0] = rows[n][n] = 0;
  return (glyphs[name] = rows);
}

/**
 * Draw a card icon centered on (cx, cy).
 * o.s       pixel scale (2 reads as a chunky CRT readout over a portrait)
 * o.tones   ramp index for icon levels 1..4 (default [2, 4, 6, 7])
 * o.white   all lit pixels white (hero frame)
 * o.a       alpha (dithered)
 * o.row(r)  per icon row: return false to hide it, or { dx, dy, a, tone } to tear/shift/fade it
 * o.cell(q, r) per icon pixel: return false to hide it
 * o.scan    on scale >= 2, the lower sub-row of each icon pixel is one step darker (CRT line look)
 */
export function icon(p, name, cx, cy, o = {}) {
  const g = glyph(name), s = o.s ?? 2, n = g.length, tones = o.tones ?? [2, 4, 6, 7];
  const x0 = Math.round(cx - (n * s) / 2), y0 = Math.round(cy - (n * s) / 2);
  for (let r = 0; r < n; r++) {
    const ro = o.row ? o.row(r) : null;
    if (ro === false) continue;
    const dx = ro?.dx ?? 0, dy = ro?.dy ?? 0, a = (o.a ?? 1) * (ro?.a ?? 1), shift = ro?.tone ?? 0;
    if (a <= 0) continue;
    for (let q = 0; q < n; q++) {
      const v = g[r][q];
      if (!v) continue;
      if (o.cell && o.cell(q, r) === false) continue;
      const ti = clamp(tones[v - 1] + shift, 0, 7), c = o.white ? WHITE : C[ti];
      const X = x0 + q * s + dx, Y = y0 + r * s + dy;
      if (s === 1) { p.dot(X, Y, c, a); continue; }
      const scan = o.scan !== false && !o.white ? C[clamp(ti - 1, 0, 7)] : c;
      for (let yy = 0; yy < s; yy++) for (let xx = 0; xx < s; xx++) p.dot(X + xx, Y + yy, yy === s - 1 ? scan : c, a);
    }
  }
}

/** The icon's silhouette as a 1 px outline ring around its lit pixels (for pop and pulse frames). */
export function iconOutline(p, name, cx, cy, s, c, a = 1, grow = 1) {
  const g = glyph(name), n = g.length, x0 = Math.round(cx - (n * s) / 2), y0 = Math.round(cy - (n * s) / 2);
  const on = (X, Y) => {
    const q = Math.floor((X - x0) / s), r = Math.floor((Y - y0) / s);
    return q >= 0 && r >= 0 && q < n && r < n && g[r][q] > 0;
  };
  const W = n * s;
  for (let Y = y0 - grow - 1; Y <= y0 + W + grow; Y++)
    for (let X = x0 - grow - 1; X <= x0 + W + grow; X++) {
      if (on(X, Y)) continue;
      // the ring at exactly `grow` px (manhattan) from the glyph
      let d = 99;
      for (let dy = -grow; dy <= grow; dy++) for (let dx = -grow; dx <= grow; dx++) {
        const m = Math.abs(dx) + Math.abs(dy);
        if (m <= grow && m < d && on(X + dx, Y + dy)) d = m;
      }
      if (d === grow) p.dot(X, Y, c, a);
    }
}

/** Four corner brackets around a box: the CRT "target lock" frame. */
export function brackets(p, cx, cy, hw, hh, len, c, a = 1) {
  hw = Math.round(hw); hh = Math.round(hh);
  for (const sx of [-1, 1]) for (const sy of [-1, 1]) {
    const x = cx + sx * hw, y = cy + sy * hh;
    for (let i = 0; i < len; i++) { p.dot(x - sx * i, y, c, a); p.dot(x, y - sy * i, c, a); }
  }
}

/** A 1 px rectangle outline. */
export function box(p, cx, cy, hw, hh, c, a = 1, cut = 0) {
  hw = Math.round(hw); hh = Math.round(hh);
  for (let x = -hw + cut; x <= hw - cut; x++) { p.dot(cx + x, cy - hh, c, a); p.dot(cx + x, cy + hh, c, a); }
  for (let y = -hh + cut; y <= hh - cut; y++) { p.dot(cx - hw, cy + y, c, a); p.dot(cx + hw, cy + y, c, a); }
}

/** A plus sign, arm length k (1 = 3x3, 2 = 5x5), with a darker rim on the bigger ones. */
export function plus(p, x, y, k, c, a = 1, rim = null) {
  x = Math.round(x); y = Math.round(y);
  if (rim !== null && k > 1) for (let i = -k - 1; i <= k + 1; i++) { p.dot(x + i, y - 1, rim, a); p.dot(x + i, y + 1, rim, a); p.dot(x - 1, y + i, rim, a); p.dot(x + 1, y + i, rim, a); }
  for (let i = -k; i <= k; i++) { p.dot(x + i, y, c, a); p.dot(x, y + i, c, a); }
}

/** A chevron of half-width w pointing up (dir -1) or down (dir 1), 2 px thick. */
export function chevron(p, x, y, w, dir, c, a = 1, c2 = c) {
  x = Math.round(x); y = Math.round(y);
  for (let i = -w; i <= w; i++) {
    const yy = y - dir * Math.abs(i);
    p.dot(x + i, yy, c, a);
    p.dot(x + i, yy - dir, c2, a);
  }
}

/**
 * Glitch tears: short horizontal bars that flicker frame to frame (frame = Math.floor(t * FPS)).
 * Inside a band [y0, y1] x [x0, x1], n bars, density k (0..1).
 */
export function tears(p, t, seed, n, x0, x1, y0, y1, k = 1, tone = 5) {
  const f = Math.floor(t * FPS + 1e-6);
  for (let i = 0; i < n; i++) {
    const r = rng(seed, f, i);
    if (r.next() > k) continue;
    const y = Math.round(r.range(y0, y1)), w = Math.round(r.range(4, 22)), x = Math.round(r.range(x0, x1 - w));
    const c = C[clamp(tone + r.int(-2, 1), 1, 7)];
    for (let xx = 0; xx < w; xx++) if ((xx + i) % 7 !== 3) p.px(x + xx, y, c);
    if (r.chance(0.35)) p.px(x + w + 2, y, C[7]);
  }
}

/** Row offsets for a torn image: rows grouped in bands that slide sideways, new every frame. */
export function tearRow(t, seed, r, amp, band = 2) {
  const f = Math.floor(t * FPS + 1e-6), h = hash(seed, f, Math.floor(r / band));
  const u = (h % 1000) / 1000;
  return u < 0.45 ? 0 : Math.round((u - 0.72) * 2 * amp);
}

/** A ring drawn with whole pixels (no anti-aliasing), optionally dashed. */
export function pixRing(p, cx, cy, r, c, a = 1, dash = 0, phase = 0) {
  const n = Math.max(8, Math.ceil(r * 7));
  let last = "";
  for (let i = 0; i < n; i++) {
    const g = (i / n) * Math.PI * 2;
    if (dash && Math.floor((g / (Math.PI * 2)) * dash * 2 + phase) % 2) continue;
    const x = Math.round(cx + Math.cos(g) * r), y = Math.round(cy + Math.sin(g) * r), key = x + "," + y;
    if (key === last) continue;
    last = key;
    p.dot(x, y, c, a);
  }
}

/** A hexagon outline with whole pixels. */
export function hexRing(p, cx, cy, r, c, a = 1, rot = Math.PI / 6) {
  for (let i = 0; i < 6; i++) {
    const a0 = rot + (i / 6) * Math.PI * 2, a1 = rot + ((i + 1) / 6) * Math.PI * 2;
    p.line(Math.round(cx + Math.cos(a0) * r), Math.round(cy + Math.sin(a0) * r), Math.round(cx + Math.cos(a1) * r), Math.round(cy + Math.sin(a1) * r), c, a);
  }
}

/** frame index at time t */
export const frame = (t) => Math.floor(t * FPS + 1e-6);

/** blink on/off by frame: on for `on` frames out of `per` */
export const blink = (t, per = 2, on = 1, ofs = 0) => (frame(t) + ofs) % per < on;

/**
 * CRT power-off exit for an icon: u 0..0.55 squashes it vertically into a bright line, 0.55..1 the line
 * shrinks to a dot and is gone. The positive effects all leave this way.
 */
export function collapse(p, name, cx, cy, u, o = {}) {
  if (u >= 1) return;
  const g = glyph(name), s = o.s ?? 2, n = g.length, W = n * s;
  if (u < 0.55) {
    const k = 1 - u / 0.55, x0 = Math.round(cx - W / 2), y0 = cy - W / 2;
    const tones = o.tones ?? [2, 4, 6, 7], lift = Math.round(u * 3);
    for (let r = 0; r < n; r++)
      for (let yy = 0; yy < s; yy++) {
        const Y = Math.round(cy + (y0 + r * s + yy - cy) * k);
        for (let q = 0; q < n; q++) {
          const v = g[r][q];
          if (!v) continue;
          const ti = Math.min(7, tones[v - 1] + lift);
          for (let xx = 0; xx < s; xx++) p.px(x0 + q * s + xx, Y, C[ti]);
        }
      }
    if (k < 0.5) for (let x = -W / 2 - 2; x <= W / 2 + 2; x++) p.px(cx + x, cy, C[7]);
    p.glowLine(cx - W / 2, cy, cx + W / 2, cy, 3, C[6], 0.5 * (1 - k));
  } else {
    const k = 1 - (u - 0.55) / 0.45, hw = Math.round((W / 2 + 4) * k * k);
    for (let x = -hw; x <= hw; x++) p.px(cx + x, cy, Math.abs(x) < hw * 0.6 ? WHITE : C[6]);
    if (k < 0.5) { p.px(cx, cy - 1, C[6]); p.px(cx, cy + 1, C[6]); }
    p.glow(cx, cy, 6 + hw * 0.5, C[7], 0.5 * k);
  }
}

// for glyph() access in other modules
export function iconRows(name) { return glyph(name); }

/**
 * The negative counterpart to collapse(): the icon tears apart and drops out, rows sliding sideways
 * further every frame and falling away. u 0..1. Debuffs all leave this way.
 */
export function shred(p, name, cx, cy, t, u, seed, o = {}) {
  if (u >= 1) return;
  icon(p, name, cx, cy, {
    ...o,
    row: (r) => {
      const h = hash(seed, frame(t), r) % 1000 / 1000;
      if (h < u * 1.1) return false; // rows drop out
      return { dx: tearRow(t, seed + r, r, 4 + u * 14, 1), dy: Math.round(u * u * 6), tone: -Math.round(u * 3) };
    },
  });
  if (u < 0.8) tears(p, t, seed + 9, 5, cx - 22, cx + 22, cy - 16, cy + 16, 1 - u, 4);
}

/** A ghost copy of the icon offset sideways: the signal doubling on a bad line. */
export function ghost(p, name, cx, cy, dx, a = 0.6, o = {}) {
  icon(p, name, cx + dx, cy, { ...o, tones: [1, 2, 3, 4], a, scan: false });
}
