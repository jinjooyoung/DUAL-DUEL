import { fly, rng, clamp, seg, ease, WHITE } from "../../core/index.js";
import { C, SIZE, ORIGIN, FPS, brackets, tears, tearRow, blink, frame } from "./_lib.js";

export const meta = {
  name: "DD Attack hit",
  desc: "Damage on a portrait: a white diagonal cut lands in one frame, tears into sliding scanlines, throws pixel shards off both sides while red-alert brackets slam in and blink out.",
  tags: ["dualduel", "crt", "hit", "attack", "negative", "one-shot"],
  size: SIZE, origin: ORIGIN, duration: 0.6, loop: false, fps: FPS, bg: "dark",
  options: { seed: 7 },
};

// the cut runs along the sword icon's blade: bottom-left to top-right
const L = 30, DX = Math.SQRT1_2, DY = -Math.SQRT1_2;

export function draw(p, t, o) {
  const f = frame(t), S = o.seed;

  // 0. the hero frame: a solid white square of light behind the cut, one frame only
  if (f === 0) {
    // the whole portrait flashes, in scanlines: bright rows and dim rows
    for (let y = -37; y <= 37; y++) for (let x = -37; x <= 37; x++) p.blend(x, y, C[6], y & 1 ? 0.18 : 0.42);
    p.glow(0, 0, 40, C[6], 0.55);
  } else if (f === 1) {
    for (let y = -37; y <= 37; y += 2) for (let x = -37; x <= 37; x++) p.blend(x, y, C[5], 0.16);
  }

  // 1. the cut: thick white, then thinning, its rows torn sideways
  const cutK = 1 - seg(t, 0.03, 0.28);
  if (cutK > 0) {
    const half = f === 0 ? 3.4 : f === 1 ? 2.8 : f === 2 ? 2 : 1.2 * cutK + 0.4;
    const amp = f === 0 ? 0 : f < 3 ? 9 : f < 5 ? 6 : 3;
    for (let y = -L; y <= L; y++) {
      const u = (y + L) / (2 * L);
      const cx = -y, taper = Math.sin(Math.PI * clamp(u)) ** 0.6;
      const hw = half * taper * 1.42;
      if (hw < 0.4) continue;
      const dx = tearRow(t, S, y + 40, amp, 2);
      for (let x = Math.floor(cx - hw); x <= Math.ceil(cx + hw); x++) {
        const d = Math.abs(x - cx) / hw;
        const c = f < 2 ? (d < 0.65 ? WHITE : C[7]) : d < 0.4 ? C[7] : d < 0.8 ? C[6] : C[4];
        p.px(x + dx, y, c);
      }
      // a ghost of the row a few px off: the signal doubling
      if (f >= 1 && f < 6 && dx !== 0) p.dot(Math.round(cx) - dx, y, C[4], 0.7);
    }
    p.glowLine(-L, L, L, -L, 6, C[6], 0.5 * cutK);
  }

  // 1b. the scar: a dashed afterimage of the cut that dithers away
  const scar = seg(t, 0.2, 0.25) * (1 - seg(t, 0.3, 0.55));
  if (scar > 0) for (let y = -L + 3; y <= L - 3; y++) if ((y + f) % 4 !== 0) p.dot(-y, y, y % 3 ? C[3] : C[4], scar);

  // 2. impact lines snap outward from the center for three frames
  if (f >= 1 && f <= 3) {
    for (let i = 0; i < 10; i++) {
      const r = rng(S, 40, i), a = (i / 10) * Math.PI * 2 + r.range(-0.2, 0.2);
      const r0 = 8 + f * 7, r1 = r0 + (i % 2 ? 6 : 11) - f * 2;
      p.line(Math.cos(a) * r0, Math.sin(a) * r0, Math.cos(a) * r1, Math.sin(a) * r1, (u) => (u < 0.5 ? C[7] : C[5]));
    }
  }

  // 3. shards: square pixels thrown off both sides of the cut, flickering as they die
  for (let i = 0; i < 26; i++) {
    const r = rng(S, 77, i), side = i % 2 ? 1 : -1;
    const at = r.range(-22, 22), sp = r.range(70, 210), spread = r.range(-0.5, 0.5);
    const nx = -DY * side, ny = DX * side; // normal to the cut
    const vx = (nx + DX * spread) * sp, vy = (ny + DY * spread) * sp;
    const t0 = r.range(0, 0.06), life = r.range(0.22, 0.48), age = t - t0;
    if (age < 0 || age > life) continue;
    const [x, y] = fly(at * DX, at * DY, vx, vy, age, 160, 6);
    const k = age / life;
    if (k > 0.6 && blink(t, 2, 1, i)) continue;
    const c = k < 0.25 ? WHITE : k < 0.55 ? C[6] : C[4], s = r.chance(0.35) && k < 0.5 ? 2 : 1;
    p.rect(Math.round(x), Math.round(y), s, s, c);
    if (k < 0.3) p.light(x, y, C[5], 0.6);
  }

  // 4. alert brackets slam in, then blink out
  if (t < 0.46) {
    const k = ease.outExpo(seg(t, 0, 0.08)), hw = 40 - 10 * k;
    if (t < 0.14 || blink(t, 2, 1)) brackets(p, 0, 0, hw, hw, f < 2 ? 9 : 6, f < 2 ? WHITE : C[6]);
  }

  // 5. signal tears across the portrait, dying down
  const tk = 1 - seg(t, 0.05, 0.5);
  if (f >= 1 && tk > 0) tears(p, t, S + 3, 9, -44, 44, -34, 34, tk, 5);

  // 6. a few dead pixels hold on after the hit
  if (t > 0.2 && t < 0.55) for (let i = 0; i < 6; i++) {
    const r = rng(S, 99, i);
    if (blink(t, 3, 2, i)) p.px(Math.round(r.range(-30, 30)), Math.round(r.range(-30, 30)), C[r.int(3, 6)]);
  }
}
