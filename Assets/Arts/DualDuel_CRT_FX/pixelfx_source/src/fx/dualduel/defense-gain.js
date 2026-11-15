import { rng, seg, ease, clamp, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, hexRing, brackets, plus, frame, blink, collapse } from "./_lib.js";

export const meta = {
  name: "DD Defense gain",
  desc: "Armor online: brackets open, a scan line prints the shield glyph row by row, it locks with a white frame and a hex ring, motes rise, then a CRT power-off.",
  tags: ["dualduel", "crt", "buff", "defense", "shield", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.8, loop: false, fps: FPS, bg: "dark",
  options: { seed: 31 },
};

const TOP = -16, BOT = 16, LOCK = 0.3;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // brackets open from the center and lock on the glyph's box
  if (t < 0.62) {
    const k = ease.outQuart(seg(t, 0, 0.15)), hw = 4 + 16 * k;
    if (t < 0.45 || blink(t, 2, 1)) brackets(p, 0, 0, hw, hw, 4, t < LOCK ? C[5] : C[6]);
  }

  // the scan: a line sweeping down prints the glyph; the freshest rows are white
  const scanY = TOP + (BOT - TOP) * ease.inOutSine(seg(t, 0.03, LOCK - 0.03));
  const gone = seg(t, 0.56, 0.78);
  if (gone > 0) collapse(p, "Defense", 0, 0, gone);
  else icon(p, "Defense", 0, 0, {
    white: f === Math.round(LOCK * FPS),
    row: (r) => {
      const y = TOP + r * 2 + 1;
      if (t < LOCK && y > scanY) return false;
      if (t < LOCK && scanY - y < 4) return { tone: 2 };
      return null;
    },
  });
  if (t < LOCK) {
    for (let x = -18; x <= 18; x++) p.px(x, Math.round(scanY), Math.abs(x) < 15 ? WHITE : C[5]);
    p.glowLine(-18, scanY, 18, scanY, 4, C[6], 0.6);
  }

  // the lock: outline flare and a hex ring that grows and thins
  const lk = seg(t, LOCK, LOCK + 0.35);
  if (lk > 0 && lk < 1) {
    if (lk < 0.3) iconOutline(p, "Defense", 0, 0, 2, lk < 0.15 ? C[7] : C[5], 1, lk < 0.15 ? 1 : 2);
    const r = 20 + 16 * ease.outCubic(lk);
    hexRing(p, 0, 0, r, C[lk < 0.4 ? 7 : 5], 1 - lk);
    p.glow(0, 0, 30, C[6], 0.45 * (1 - lk));
  }

  // motes rise around the glyph after the lock: small squares and plus signs
  for (let i = 0; i < 14; i++) {
    const r = rng(S, 3, i), t0 = LOCK - 0.05 + r.range(0, 0.22), life = r.range(0.25, 0.4), age = t - t0;
    if (age < 0 || age > life) continue;
    const k = age / life, x = Math.round(r.range(-26, 26)), y = Math.round(r.range(4, 22) - ease.outQuad(k) * r.range(18, 30));
    if (Math.abs(x) < 15 && Math.abs(y) < 15) continue;
    const c = C[clamp(7 - Math.floor(k * 4), 3, 7)];
    if (i % 3 === 0) plus(p, x, y, 1, c);
    else p.px(x, y, c);
    if (k < 0.5) p.px(x, y + 1, C[3]);
  }
}
