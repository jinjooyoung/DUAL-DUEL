import { rng, seg, ease, clamp, fly, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, ghost, shred, brackets, tearRow, frame, blink } from "./_lib.js";

export const meta = {
  name: "DD Debuff poison",
  desc: "Poisoned: the skull glyph glitches in doubled and jittering, toxic fumes swell around it, bubbles rise and pop, drops ooze off its jaw, then the signal shreds away.",
  tags: ["dualduel", "crt", "debuff", "poison", "negative", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 81 },
};

const IN = 0.12, OUT = 0.58;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // fumes: soft translucent puffs swelling and thinning, behind everything
  for (let i = 0; i < 9; i++) {
    const r = rng(S, 1, i), t0 = r.range(0, 0.25), life = r.range(0.4, 0.55), age = t - t0;
    if (age < 0 || age > life) continue;
    const k = age / life, a = (i / 9) * Math.PI * 2, d = 12 + k * 14;
    const x = Math.cos(a) * d, y = Math.sin(a) * d * 0.8 - k * 6;
    p.disc(x, y, 5 + k * 7, C[3], 0.32 * Math.sin(Math.PI * k));
    p.disc(x, y - 2, 3 + k * 4, C[4], 0.18 * Math.sin(Math.PI * k));
  }

  // the skull: torn in for a few frames, then held with a jitter and a ghost; shreds out at the end
  const out = seg(t, OUT, 0.78);
  if (out > 0) shred(p, "Debuff_Poison", 0, 0, t, out, S);
  else if (t < IN) {
    const k = t / IN;
    icon(p, "Debuff_Poison", 0, 0, { row: (r) => ((r + f) % 3 === 0 ? false : { dx: tearRow(t, S, r, 14 * (1 - k), 1), tone: f === 0 ? 1 : 0 }) });
    ghost(p, "Debuff_Poison", 0, 0, f % 2 ? 4 : -4, 0.7);
  } else {
    const jit = blink(t, 4, 1, 1) ? (f % 8 < 4 ? 1 : -1) : 0;
    ghost(p, "Debuff_Poison", jit, 0, f % 2 ? 2 : -2, 0.45);
    icon(p, "Debuff_Poison", jit, 0, { white: f === Math.round(IN * FPS), row: (r) => (blink(t, 5, 1) && r % 5 === f % 5 ? { dx: 3 } : null) });
  }
  if (t >= IN && t < 0.5) p.glow(0, 0, 24, C[5], 0.35);

  // drops ooze from the jaw and fall
  for (let i = 0; i < 5; i++) {
    const r = rng(S, 3, i), t0 = IN + 0.04 + i * 0.06, age = t - t0;
    if (age < 0 || age > 0.3) continue;
    const x = Math.round(-6 + i * 3 + r.range(-1, 1)), hang = Math.min(age, 0.1), [, y] = fly(0, 12 + hang * 30, 0, 0, Math.max(0, age - 0.1), 500);
    const L = age < 0.1 ? 1 + Math.round(age * 30) : 3;
    for (let k = 0; k < L; k++) p.px(x, Math.round(y) - k, k === 0 ? C[6] : C[4]);
  }

  // bubbles rise from below, wobble and pop into four dots
  for (let i = 0; i < 11; i++) {
    const r = rng(S, 5, i), t0 = r.range(0.02, 0.36), life = r.range(0.2, 0.3), age = t - t0;
    if (age < 0) continue;
    const x0 = r.range(-30, 30), y0 = r.range(14, 30), rad = r.int(1, 3);
    const x = Math.round(x0 + Math.sin(age * 18 + i) * 1.5), y = Math.round(y0 - age * r.range(50, 80));
    if (age < life) {
      if (rad === 1) { p.px(x, y, C[6]); p.px(x - 1, y, C[4]); p.px(x + 1, y, C[4]); p.px(x, y - 1, C[4]); p.px(x, y + 1, C[4]); }
      else {
        for (let a = 0; a < 16; a++) p.px(Math.round(x + Math.cos((a / 16) * Math.PI * 2) * rad), Math.round(y + Math.sin((a / 16) * Math.PI * 2) * rad), C[5]);
        p.px(x - 1, y - 1, C[7]);
      }
    } else if (age < life + 0.1) {
      const s = rad + 2 + Math.round((age - life) * 30);
      for (const [dx, dy] of [[-1, -1], [1, -1], [-1, 1], [1, 1]]) p.px(x + dx * s, y + dy * s, C[age - life < 0.05 ? 7 : 4]);
    }
  }

  // error brackets blink round the portrait
  if (t < 0.55 && blink(t, 2, 1)) brackets(p, 0, 0, 32, 32, 5, C[5]);
}
