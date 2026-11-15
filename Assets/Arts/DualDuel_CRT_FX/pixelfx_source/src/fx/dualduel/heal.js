import { rng, seg, ease, clamp, bump, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, plus, frame, collapse } from "./_lib.js";

export const meta = {
  name: "DD Heal",
  desc: "Healed: light columns stream up, the cross pops in white and pulses two cross halos, plus signs float up and wink out, then a CRT power-off.",
  tags: ["dualduel", "crt", "heal", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 41 },
};

const POP = 0.1;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // light columns stream upward: dashed segments that run up and thin out
  for (let i = 0; i < 7; i++) {
    const r = rng(S, 1, i), x = Math.round(-30 + i * 10 + r.range(-2, 2)), t0 = i % 3 === 0 ? 0 : r.range(0, 0.2), life = r.range(0.35, 0.5);
    const u = seg(t, t0, t0 + life);
    if (u <= 0 || u >= 1) continue;
    const head = 34 - ease.outQuad(u) * 70, len = 14 + 10 * (1 - u);
    for (let y = Math.round(head); y < head + len; y++) {
      if (y > 36 || y < -40) continue;
      const k = (y - head) / len;
      if ((y + f) % 3 === 0 && k > 0.3) continue;
      p.px(x, y, k < 0.15 ? WHITE : C[clamp(6 - Math.floor(k * 4), 2, 6)]);
    }
    p.glowLine(x, head, x, head + len, 3, C[5], 0.35 * (1 - u));
  }

  // the cross glyph: in from nothing, a white frame, then steady; leaves upward in scanlines
  // the cast: a small white plus at the center, before the glyph
  if (f === 0) { plus(p, 0, 0, 3, WHITE, 1, C[5]); p.glow(0, 0, 14, C[7], 0.6); }
  const gone = seg(t, 0.56, 0.78);
  if (gone > 0) collapse(p, "Heal", 0, 0, gone);
  else if (t >= POP - 0.05) icon(p, "Heal", 0, 0, { white: f === Math.round(POP * FPS), a: t < POP ? 0.5 : 1 });

  // two halos: the cross's outline pushed outward
  for (const [t0, tone] of [[POP, 7], [POP + 0.14, 5]]) {
    const u = seg(t, t0, t0 + 0.3);
    if (u <= 0 || u >= 1) continue;
    iconOutline(p, "Heal", 0, 0, 2, C[u < 0.4 ? tone : tone - 2], 1 - u * 0.7, 1 + Math.round(ease.outCubic(u) * 6));
  }
  p.glow(0, 0, 32, C[6], 0.5 * bump(t, POP - 0.05, 0.6));

  // plus signs float up and wink out
  for (let i = 0; i < 12; i++) {
    const r = rng(S, 9, i), t0 = r.range(0.05, 0.45), life = r.range(0.25, 0.38), age = t - t0;
    if (age < 0 || age > life) continue;
    const k = age / life, side = i % 2 ? 1 : -1;
    const x = side * r.range(14, 34) + Math.sin(age * 14 + i) * 1.2, y = r.range(4, 28) - ease.outCubic(k) * r.range(20, 30);
    const big = r.chance(0.4) && k < 0.6 ? 2 : 1;
    if (k > 0.75 && (f + i) % 2) continue;
    plus(p, x, y, big, C[k < 0.3 ? 7 : k < 0.6 ? 6 : 4], 1, big > 1 ? C[3] : null);
    p.light(x, y, C[5], 0.4 * (1 - k));
  }
}
