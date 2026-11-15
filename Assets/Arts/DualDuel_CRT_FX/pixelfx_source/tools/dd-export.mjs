// Export DUAL DUEL effects as Unity flipbooks: <Name>_Color.png (alpha) + <Name>_Glow.png (additive), 2x nearest.
import { join } from "node:path";
import { loadEffect, renderFrames, canvas, upscale, save } from "./lib/fxlib.mjs";

const OUT = process.argv[2] ?? "/home/claude/export";
const K = 2; // card art is exported at 200%: one FX pixel = 2x2 texture pixels, PPU 100
const LIST = [
  ["attack-hit", "Attack"], ["defense-gain", "Defense_Gain"], ["defense-block", "Defense_Block"], ["heal", "Heal"],
  ["buff-regeneration", "Buff_Regeneration"], ["buff-force", "Buff_Force"], ["buff-lifesteal", "Buff_Lifesteal"],
  ["debuff-poison", "Debuff_Poison"], ["debuff-bleed", "Debuff_Bleed"], ["debuff-weaken", "Debuff_Weaken"],
];
const manifest = [];
for (const [id, name] of LIST) {
  const fx = await loadEffect("dualduel/" + id);
  const r = renderFrames(fx, { bg: "none" });
  const frames = r.frames.slice(0, r.n - 1); // the last frame is the empty one at t = duration
  const n = frames.length, cols = 4, rows = Math.ceil(n / cols);
  if (cols * rows !== n) throw new Error(`${id}: ${n} frames don't fill a ${cols}-wide grid`);
  const W = r.w * K, H = r.h * K;
  const color = canvas(cols * W, rows * H, [0, 0, 0, 0]), glow = canvas(cols * W, rows * H, [0, 0, 0, 0]);
  frames.forEach((f, i) => {
    const px = r.w * r.h, c = new Uint8ClampedArray(px * 4), l = new Uint8ClampedArray(px * 4);
    for (let j = 0; j < px; j++) {
      const v = f.raw[j], a = v >>> 24;
      c[j * 4] = v & 255; c[j * 4 + 1] = (v >>> 8) & 255; c[j * 4 + 2] = (v >>> 16) & 255; c[j * 4 + 3] = a;
      const L = f.light[j] & 0xffffff;
      if (L) { l[j * 4] = L & 255; l[j * 4 + 1] = (L >>> 8) & 255; l[j * 4 + 2] = (L >>> 16) & 255; l[j * 4 + 3] = 255; }
    }
    const x = (i % cols) * W, y = Math.floor(i / cols) * H;
    color.paste(upscale(c, r.w, r.h, K), W, H, x, y);
    glow.paste(upscale(l, r.w, r.h, K), W, H, x, y);
  });
  save(join(OUT, "Textures", `VFX_CRT_${name}_Color.png`), color.png());
  save(join(OUT, "Textures", `VFX_CRT_${name}_Glow.png`), glow.png());
  manifest.push({ id, name, frames: n, cols, rows, fps: r.fps, duration: n / r.fps, frameNative: [r.w, r.h], framePx: [W, H], sheetPx: [cols * W, rows * H] });
  console.log(`${name}: ${n} frames ${cols}x${rows}, ${(n / r.fps).toFixed(2)} s, sheet ${cols * W}x${rows * H}`);
}
save(join(OUT, "manifest.json"), JSON.stringify(manifest, null, 1));
