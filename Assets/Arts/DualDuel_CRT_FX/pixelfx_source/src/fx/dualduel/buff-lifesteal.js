import { rng, seg, ease, clamp, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, pixRing, frame, collapse } from "./_lib.js";

export const meta = {
  name: "DD Buff lifesteal",
  desc: "Lifesteal gained: droplets spiral in from outside and are drunk by the drop glyph, which fills from the bottom up, flashes full and beats twice like a heart, then a CRT power-off.",
  tags: ["dualduel", "crt", "buff", "lifesteal", "drain", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 71 },
};

const FULL = 0.42;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // droplets spiral inward, each with a short tail
  for (let i = 0; i < 14; i++) {
    const r = rng(S, 6, i), t0 = r.range(0, 0.18), dur = r.range(0.22, 0.3), a0 = (i / 14) * Math.PI * 2 + r.range(-0.2, 0.2);
    for (let k = 0; k < 4; k++) {
      const u = seg(t - k * 0.018, t0, t0 + dur);
      if (u <= 0 || u >= 1) continue;
      const e = ease.inQuad(u), d = 42 * (1 - e) + 3, a = a0 + e * 2.2;
      const x = Math.round(Math.cos(a) * d), y = Math.round(Math.sin(a) * d);
      if (k === 0) { p.rect(x, y, 2, 2, u > 0.7 ? WHITE : C[6]); p.px(x, y + 2, C[4]); p.light(x, y, C[6], 0.5); }
      else p.px(x, y, C[clamp(5 - k, 2, 5)]);
    }
  }

  // the glyph: an empty outline that fills from the bottom up as the drops arrive
  const gone = seg(t, 0.6, 0.8);
  if (gone > 0) collapse(p, "Buff_Lifesteal", 0, 0, gone);
  else {
    const fill = ease.inOutSine(seg(t, 0.08, FULL)), level = 15 - fill * 15;
    icon(p, "Buff_Lifesteal", 0, 0, {
      white: f === Math.round(FULL * FPS),
      row: (r) => (r < level ? { tone: -3, a: 0.7 } : r < level + 1.5 ? { tone: 2 } : null),
    });
  }

  // two heartbeats: rings thump out from the glyph
  for (const [t0, tone] of [[FULL, 7], [FULL + 0.12, 5]]) {
    const u = seg(t, t0, t0 + 0.24);
    if (u <= 0 || u >= 1) continue;
    pixRing(p, 0, 0, 18 + ease.outCubic(u) * 20, C[u < 0.4 ? tone : tone - 2], 1 - u * 0.6);
    if (u < 0.3) iconOutline(p, "Buff_Lifesteal", 0, 0, 2, C[6], 1, 1);
    p.glow(0, 0, 26, C[6], 0.45 * (1 - u));
  }
}
