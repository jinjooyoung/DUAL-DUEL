import { rng, seg, ease, fly, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, shred, tears, brackets, frame, blink } from "./_lib.js";

export const meta = {
  name: "DD Debuff bleed",
  desc: "Bleeding: three claw rakes tear down the portrait one after another, each leaves a dashed wound that weeps drops falling and stretching downward, then the claw glyph shreds away.",
  tags: ["dualduel", "crt", "debuff", "bleed", "negative", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 91 },
};

// three parallel rakes, top-left to bottom-right like the icon, 0.05 s apart
const RAKES = [-10, 0, 10].map((o, i) => ({ o, t0: -0.04 + i * 0.05 }));
const L = 26, DX = 0.6, DY = 0.8;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  for (const [i, k] of RAKES.entries()) {
    const ox = k.o * DY, oy = -k.o * DX; // offset sideways from the center line
    const grow = ease.outQuart(seg(t, k.t0, k.t0 + 0.06)), fade = seg(t, k.t0 + 0.1, 0.36);
    if (grow <= 0 || fade >= 1) continue;
    const s0 = -L, s1 = -L + 2 * L * grow, hot = t - k.t0 < 0.1;
    for (let s = s0; s <= s1; s += 0.5) {
      const u = (s + L) / (2 * L), w = Math.sin(Math.PI * u);
      const x = ox + s * DX, y = oy + s * DY;
      if (!hot && ((Math.floor(s) + f) % 5 === 0 || fade > u + 0.2)) continue;
      p.px(x, y, hot ? (w > 0.4 ? WHITE : C[6]) : C[w > 0.5 ? 5 : 3]);
      if (w > 0.3 && hot) { p.px(x + 1, y, w > 0.6 ? WHITE : C[6]); p.px(x - 1, y, C[5]); }
      else if (w > 0.6) p.px(x + 1, y, C[3]);
    }
    if (hot) p.glowLine(ox + s0 * DX, oy + s0 * DY, ox + s1 * DX, oy + s1 * DY, 5, C[6], 0.55);
  }

  // the first frame of each rake: the portrait jolts, rows torn
  if (f <= 3) tears(p, t, S, 8, -40, 40, -32, 32, 1, 6);

  // drops weep from the wounds and fall, stretching as they speed up
  for (let i = 0; i < 16; i++) {
    const r = rng(S, 4, i), k = RAKES[i % 3], s = r.range(-20, 20);
    const t0 = k.t0 + 0.1 + r.range(0, 0.3), age = t - t0;
    if (age < 0 || age > 0.4) continue;
    const x = Math.round(k.o * DY + s * DX), y0 = -k.o * DX + s * DY;
    const [, y] = fly(0, y0, 0, 0, Math.max(0, age - 0.06), 420);
    const L2 = 1 + Math.min(5, Math.round(Math.max(0, age - 0.06) * 18));
    if (age > 0.3 && (f + i) % 2) continue;
    const Y = Math.round(y);
    p.rect(x, Y - 1, 2, 2, C[7]);
    for (let q = 2; q < L2 + 2; q++) p.px(x + (q > 3 ? 1 : 0), Y - q, q < 4 ? C[5] : C[3]);
  }

  // the claw glyph surfaces dimly under the rakes, then shreds
  const out = seg(t, 0.5, 0.74);
  if (out > 0) shred(p, "Debuff_Bleed", 0, 0, t, out, S, { tones: [1, 3, 5, 6] });
  else if (t > 0.18) icon(p, "Debuff_Bleed", 0, 0, { tones: [1, 3, 5, 6], white: f === 4, a: t < 0.24 ? 0.5 : 1 });

  if (t > 0.1 && t < 0.5 && blink(t, 2, 1)) brackets(p, 0, 0, 32, 32, 5, C[5]);
}
