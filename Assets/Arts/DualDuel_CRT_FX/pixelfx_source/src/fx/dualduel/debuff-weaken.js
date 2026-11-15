import { rng, seg, ease, fly, clamp, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, chevron, tears, brackets, frame, blink } from "./_lib.js";

export const meta = {
  name: "DD Debuff weaken",
  desc: "Weakened: a sword glyph appears, a white crack runs across it and it snaps, the tip spins off and both halves drop away, while down-chevrons sink and static sweeps down.",
  tags: ["dualduel", "crt", "debuff", "weaken", "negative", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 101 },
};

const SNAP = 0.16;
// the break runs perpendicular to the blade, through the icon's middle: icon cells with q - r > K are the tip
const K = 1;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // the whole sword, a beat before it breaks
  if (t < SNAP) {
    const k = t / SNAP;
    icon(p, "Attack", 0, 0, { tones: [2, 4, 6, 7], a: f === 0 ? 0.6 : 1 });
    if (k > 0.5) { // the crack runs across
      const n = Math.round((k - 0.5) * 2 * 16);
      for (let i = 0; i < n; i++) { const x = -7 + i * 0.9, y = -7 + i * 0.9; p.px(Math.round(x + (i % 2)), Math.round(-y + 0), WHITE); }
    }
  } else {
    const age = t - SNAP, k = seg(t, SNAP, 0.7);
    const flash = f === Math.round(SNAP * FPS);
    // the tip flies up and right, then falls; the hilt sags and drops
    const [tx, ty] = fly(0, 0, 40, -70, age, 380, 1.5), [hx, hy] = fly(0, 0, -12, -10, age, 300, 1);
    const a = 1 - seg(t, 0.5, 0.72);
    if (a > 0) {
      const dither = (yy) => (k > 0.5 ? ((yy + f) % 2 ? { a } : false) : { a });
      icon(p, "Attack", Math.round(tx), Math.round(ty), { white: flash, tones: [1, 3, 5, 6], cell: (q, r) => q - r > K, row: dither });
      icon(p, "Attack", Math.round(hx), Math.round(hy), { white: flash, tones: [1, 3, 5, 6], cell: (q, r) => q - r <= K, row: dither });
    }
    if (age < 0.12) {
      p.glow(0, 0, 18, WHITE, 0.6 * (1 - age / 0.12));
      // shards from the break
      for (let i = 0; i < 10; i++) {
        const r = rng(S, 2, i), an = r.range(0, Math.PI * 2), sp = r.range(60, 150), [x, y] = fly(0, 0, Math.cos(an) * sp, Math.sin(an) * sp, age, 400, 3);
        p.px(Math.round(x), Math.round(y), age < 0.06 ? WHITE : C[5]);
      }
    }
  }

  // down-chevrons sink on both sides: losing power
  for (let side = -1; side <= 1; side += 2)
    for (let i = 0; i < 3; i++) {
      const t0 = 0.12 + i * 0.08, u = seg(t, t0, t0 + 0.34);
      if (u <= 0 || u >= 1) continue;
      const y = -20 + ease.inQuad(u) * 40;
      if (u > 0.75 && blink(t, 2, 1)) continue;
      chevron(p, side * 26, y, 4, 1, C[u < 0.3 ? 6 : u < 0.7 ? 5 : 3], 1, C[u < 0.5 ? 4 : 2]);
    }

  // static sweeps down the portrait
  const sy = -36 + seg(t, 0.1, 0.6) * 72;
  if (t > 0.1 && t < 0.6) for (let i = 0; i < 70; i++) {
    const r = rng(S, f, i), y = Math.round(sy + r.range(-3, 3)), x = Math.round(r.range(-36, 36));
    p.px(x, y, C[r.int(2, 5)]);
  }
  if (t > 0.18 && t < 0.6) tears(p, t, S + 1, 4, -40, 40, -32, 32, 0.7, 4);

  if (t > SNAP && t < 0.56 && blink(t, 2, 1)) brackets(p, 0, 0, 32, 32, 5, C[5]);
}
