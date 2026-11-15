import { fly, rng, seg, ease, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, icon, iconOutline, hexRing, brackets, blink, frame, collapse } from "./_lib.js";

export const meta = {
  name: "DD Defense block",
  desc: "Blocked: the shield slams in white over a hex barrier, the blow flares on its face, sparks glance off, two hex rings ping out, then a CRT power-off.",
  tags: ["dualduel", "crt", "block", "defense", "shield", "positive", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.6, loop: false, fps: FPS, bg: "dark",
  options: { seed: 21 },
};

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // the barrier: a hexagon that flashes solid on contact, then holds as a faint line
  const bk = 1 - seg(t, 0.15, 0.45);
  if (f <= 1) {
    for (let r = 22; r >= 4; r -= 2) hexRing(p, 0, 0, r, f === 0 ? (r > 18 ? WHITE : C[6]) : C[r > 18 ? 6 : 3], f === 0 ? 1 : 0.6);
    p.glow(0, 0, 40, C[6], f === 0 ? 0.7 : 0.4);
  } else if (bk > 0) {
    hexRing(p, 0, 0, 22, C[5], bk);
    hexRing(p, 0, 0, 23, C[3], bk * 0.6);
  }

  // two hex rings ping outward
  for (const [t0, tone] of [[0.03, 7], [0.11, 5]]) {
    const u = seg(t, t0, t0 + 0.3);
    if (u <= 0 || u >= 1) continue;
    const r = 23 + 18 * ease.outQuart(u);
    hexRing(p, 0, 0, r, C[tone - (u > 0.6 ? 2 : 0)], 1 - u * 0.6);
    p.glowRing(0, 0, r, 3, C[5], 0.3 * (1 - u));
  }

  // the glyph: white on contact, pushed back 2 px, then home; it fades out row by row from the bottom
  const push = f === 1 ? 2 : f === 2 ? 1 : 0;
  const gone = seg(t, 0.36, 0.56);
  if (gone > 0) collapse(p, "Defense", 0, 0, gone);
  else icon(p, "Defense", 0, push, { white: f <= 1, tones: f <= 3 ? [4, 6, 7, 7] : [2, 4, 6, 7] });
  if (f <= 2) iconOutline(p, "Defense", 0, push, 2, f === 0 ? C[7] : C[5], 1, f + 1);

  // the impact on the shield's face: a hot star, top left where blows come in
  if (f <= 2) {
    const hx = -3, hy = -4, Lr = f === 0 ? 13 : f === 1 ? 9 : 5;
    for (let i = 0; i < 8; i++) {
      const a = (i / 8) * Math.PI * 2 + 0.39, l = i % 2 ? Lr * 0.5 : Lr;
      p.line(hx, hy, hx + Math.cos(a) * l, hy + Math.sin(a) * l, (u) => (u < 0.5 ? WHITE : C[6]));
    }
    p.glow(hx, hy, 14, WHITE, 0.6);
  }

  // sparks glance off the sides along the shield's face, then fall
  for (let i = 0; i < 18; i++) {
    const r = rng(S, 5, i), side = i % 2 ? 1 : -1;
    const a = (side > 0 ? 0 : Math.PI) + side * r.range(-0.85, 0.25), sp = r.range(170, 300);
    const t0 = r.range(0, 0.04), life = r.range(0.18, 0.4), age = t - t0;
    if (age < 0 || age > life) continue;
    const vx = Math.cos(a) * sp, vy = Math.sin(a) * sp;
    const [x, y] = fly(-3, -4, vx, vy, age, 380, 4), [x2, y2] = fly(-3, -4, vx, vy, Math.max(0, age - 0.045), 380, 4);
    const k = age / life;
    if (k > 0.7 && blink(t, 2, 1, i)) continue;
    if (k < 0.4) p.line(x2, y2 + 1, x, y + 1, C[4]);
    p.line(x2, y2, x, y, (u) => (u > 0.6 ? (k < 0.35 ? WHITE : C[7]) : C[k < 0.5 ? 6 : 4]));
    p.light(x, y, C[6], 0.5 * (1 - k));
  }

  // a confirm frame: brackets close on the glyph and blink twice
  if (t >= 0.1 && t < 0.42 && blink(t, 3, 2)) brackets(p, 0, 0, 19, 19, 5, C[6]);
}
