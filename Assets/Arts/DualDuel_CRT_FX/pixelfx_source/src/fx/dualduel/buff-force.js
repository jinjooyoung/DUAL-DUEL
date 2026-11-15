import { rng, seg, ease, clamp, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, chevron, box, frame, collapse } from "./_lib.js";

export const meta = {
  name: "DD Buff force",
  desc: "Force gained: streaks rush in and compress, the fist glyph bursts out white with a square shockwave, rising chevrons stack up beside it, sparks lift, then a CRT power-off.",
  tags: ["dualduel", "crt", "buff", "force", "strength", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 61 },
};

const BURST = 0.2;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // charge: streaks rush in from all round, faster and faster
  if (t < BURST) {
    for (let i = 0; i < 16; i++) {
      const r = rng(S, 2, i), a = (i / 16) * Math.PI * 2 + r.range(-0.15, 0.15), d0 = r.range(34, 44);
      const u = ease.inCubic(seg(t, r.range(0, 0.05), BURST)), d = d0 * (1 - u) + 4;
      const len = 3 + u * 9;
      p.line(Math.cos(a) * d, Math.sin(a) * d, Math.cos(a) * (d + len), Math.sin(a) * (d + len), (q) => (q < 0.3 ? WHITE : q < 0.7 ? C[6] : C[4]));
      p.rect(Math.round(Math.cos(a) * d), Math.round(Math.sin(a) * d), 2, 2, u > 0.4 ? WHITE : C[6]);
    }
    const k = seg(t, 0, BURST);
    p.rect(-1, -1, 2, 2, k > 0.5 ? WHITE : C[6]);
    p.glow(0, 0, 8 + k * 10, C[6], 0.3 + k * 0.4);
  }

  // burst: a square shockwave, two of them
  for (const [t0, tone] of [[BURST, 7], [BURST + 0.08, 5]]) {
    const u = seg(t, t0, t0 + 0.3);
    if (u <= 0 || u >= 1) continue;
    const hw = 10 + 27 * ease.outQuart(u);
    box(p, 0, 0, hw, hw, C[u < 0.4 ? tone : tone - 2], 1 - u * 0.7, 2);
    if (u < 0.2) box(p, 0, 0, hw - 1, hw - 1, C[5], 1, 2);
  }

  // chevrons stack up on both sides, rising: more power
  for (let side = -1; side <= 1; side += 2)
    for (let i = 0; i < 3; i++) {
      const t0 = BURST + 0.04 + i * 0.07, u = seg(t, t0, t0 + 0.32);
      if (u <= 0 || u >= 1) continue;
      const y = 18 - ease.outCubic(u) * 34, x = side * 26;
      chevron(p, x, y, 4, -1, C[u < 0.3 ? 7 : u < 0.7 ? 6 : 4], u > 0.8 ? 0.5 : 1, C[u < 0.5 ? 5 : 3]);
    }

  // the glyph: bursts in white, holds bright
  const gone = seg(t, 0.6, 0.8);
  if (gone > 0) collapse(p, "Buff_Force", 0, 0, gone);
  else if (t >= BURST) {
    const k = seg(t, BURST, BURST + 0.1);
    icon(p, "Buff_Force", 0, 0, { white: f === Math.round(BURST * FPS), tones: k < 1 ? [4, 6, 7, 7] : [2, 4, 6, 7] });
    if (k < 1) iconOutline(p, "Buff_Force", 0, 0, 2, C[6], 1 - k, 1 + Math.round(k * 3));
    p.glow(0, 0, 30, C[6], 0.55 * (1 - seg(t, BURST, 0.6)));
  }

  // sparks lift off the glyph
  for (let i = 0; i < 12; i++) {
    const r = rng(S, 8, i), t0 = BURST + r.range(0, 0.25), life = r.range(0.2, 0.32), age = t - t0;
    if (age < 0 || age > life) continue;
    const k = age / life, x = Math.round(r.range(-16, 16)), y = Math.round(r.range(-6, 8) - ease.outQuad(k) * r.range(16, 26));
    p.px(x, y, C[clamp(7 - Math.floor(k * 4), 3, 7)]);
    if (k < 0.5) p.px(x, y + 1, C[4]), p.px(x, y + 2, C[2]);
  }
}
