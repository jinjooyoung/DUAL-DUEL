import { rng, seg, ease, clamp, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, plus, frame, collapse } from "./_lib.js";

export const meta = {
  name: "DD Buff regeneration",
  desc: "Regeneration gained: a loading ring of dots boots up and spins one turn, its head sheds plus signs upward, the ring closes into the glyph with a flash, then a CRT power-off.",
  tags: ["dualduel", "crt", "buff", "regeneration", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 51 },
};

const N = 16, R = 24, CLOSE = 0.5;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;
  const boot = seg(t, 0, 0.12), spin = ease.inOutSine(seg(t, 0.04, CLOSE)), close = ease.inQuad(seg(t, CLOSE, CLOSE + 0.12));
  const head = -Math.PI / 2 + spin * Math.PI * 2;

  // the loading ring: dots pop on in order, a bright head with a fading tail chases round, then it closes in
  if (close < 1) {
    const rr = R * (1 - close);
    for (let i = 0; i < N; i++) {
      if (i / N > boot) continue;
      const a = -Math.PI / 2 + (i / N) * Math.PI * 2;
      let d = (head - a) % (Math.PI * 2);
      if (d < 0) d += Math.PI * 2;
      const tail = d / (Math.PI * 2), x = Math.round(Math.cos(a) * rr), y = Math.round(Math.sin(a) * rr);
      const c = tail < 0.07 ? WHITE : tail < 0.2 ? C[7] : tail < 0.4 ? C[6] : tail < 0.7 ? C[5] : C[4];
      p.rect(x - 1, y - 1, 2, 2, c);
      if (tail < 0.2 || close > 0) { p.rect(x - 1, y + 1, 2, 1, C[3]); p.light(x, y, C[6], 0.7); }
    }
    p.glowRing(0, 0, rr, 3, C[5], 0.3);
    p.glow(Math.cos(head) * rr, Math.sin(head) * rr, 8, C[7], 0.5);
  }

  // the glyph: dim while the ring loads, full when it closes
  const gone = seg(t, 0.6, 0.8);
  if (gone > 0) collapse(p, "Buff_Regeneration", 0, 0, gone);
  else if (t > 0.03) {
    const lit = t >= CLOSE + 0.12;
    icon(p, "Buff_Regeneration", 0, 0, { white: f === Math.round((CLOSE + 0.12) * FPS), tones: lit ? [3, 5, 7, 7] : [2, 3, 5, 6] });
  }

  // the close: a flash ring around the glyph
  const fl = seg(t, CLOSE + 0.1, CLOSE + 0.24);
  if (fl > 0 && fl < 1 && gone === 0) {
    iconOutline(p, "Buff_Regeneration", 0, 0, 2, C[fl < 0.4 ? 7 : 5], 1 - fl * 0.6, 1 + Math.round(fl * 3));
    p.glow(0, 0, 26, C[6], 0.5 * (1 - fl));
  }

  // plus signs shed from the head, drifting up
  for (let i = 0; i < 10; i++) {
    const r = rng(S, 4, i), t0 = 0.08 + i * 0.04, life = r.range(0.22, 0.32), age = t - t0;
    if (age < 0 || age > life || t0 > CLOSE) continue;
    const s0 = ease.inOutSine(seg(t0, 0.04, CLOSE)), a = -Math.PI / 2 + s0 * Math.PI * 2;
    const k = age / life, x = Math.cos(a) * R + r.range(-2, 2), y = Math.sin(a) * R - ease.outQuad(k) * 14;
    if (k > 0.7 && (f + i) % 2) continue;
    plus(p, x, y, k < 0.4 ? 2 : 1, C[clamp(7 - Math.floor(k * 4), 3, 7)], 1, k < 0.4 ? C[3] : null);
  }
}
