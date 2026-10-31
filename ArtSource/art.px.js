// Oyun gorsellerinin elden gecirilen parcalari. Cikti: out/... -> Resources/Sprites
pal({ k: "#2b2b30", w: "#f6efdc", c: "#d8cfb4", y: "#f0bb55", Y: "#d99a32", o: "#b37a22", r: "#c8553d", R: "#9a3c2a", E: "#e47a5c",
  p: "#e7a0bb", P: "#c9799a", M: "#f6c2d4", q: "#efe3cc", s: "#c9d1d8", S: "#8f99a3", T: "#e8eef2", e: "#a33a2c",
  g: "#2f7a4f", G: "#7fb069", L: "#a7cf86", D: "#3f7a45", b: "#3f77b4", B: "#2f5f96", n: "#8a6a48", N: "#6e5236", f: "#f2d7a6", F: "#dcbc85" })
ramps("oYy", "RrE", "PpM", "STs")

// Evrensel geri donusum isareti: her ok ucgenin bir kosesinde kivrilir, ucu sonraki oka bakar.
function recycle(cx, cy, radius, band, color) {
  const ring = [0, 1, 2].map((i) => { const a = (-90 + i * 120) * Math.PI / 180; return [Math.cos(a), Math.sin(a)] })
  const rot = ([x, y], k) => { const a = k * 2 * Math.PI / 3; return [x * Math.cos(a) - y * Math.sin(a), x * Math.sin(a) + y * Math.cos(a)] }
  const lerp = (p, q, t) => [p[0] + (q[0] - p[0]) * t, p[1] + (q[1] - p[1]) * t]
  const add = (p, n, k) => [p[0] + n[0] * k, p[1] + n[1] * k]
  const scale = (p, k) => [p[0] * k, p[1] * k]
  const top = scale(ring[0], radius), right = scale(ring[1], radius), left = scale(ring[2], radius)
  const inward = (a, b) => { const d = Math.hypot(b[0] - a[0], b[1] - a[1]), n = [-(b[1] - a[1]) / d, (b[0] - a[0]) / d]; return n[0] * -a[0] + n[1] * -a[1] > 0 ? n : [-n[0], -n[1]] }
  const nl = inward(left, top), nr = inward(top, right)
  const start = lerp(left, top, 0.62), neck = lerp(top, right, 0.32), tip = add(lerp(top, right, 0.56), nr, band / 2)
  const shape = [start, lerp(top, left, 0.1), lerp(top, right, 0.1), neck, add(neck, nr, -band * 0.75), tip,
    add(neck, nr, band * 1.75), add(neck, nr, band), scale(ring[0], radius - band * 2), add(start, nl, band)]
  const polys = [0, 1, 2].map((k) => shape.map((p) => rot(p, k)))
  const inside = (poly, x, y) => { let hit = false; for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) { const [xi, yi] = poly[i], [xj, yj] = poly[j]; if ((yi > y) !== (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) hit = !hit } return hit }
  return (c) => { for (let y = 0; y < c.h; y++) for (let x = 0; x < c.w; x++) if (polys.some((poly) => inside(poly, x + 0.5 - cx, y + 0.5 - cy))) c.px(x, y, color) }
}

sprite("truck_logo", { w: 40, h: 40 }, ellipse(20, 20, 19, 19, "k"), ellipse(20, 20, 18, 18, "w"), recycle(20, 22, 18, 3, "D"))

sprite("star", { w: 22, h: 22, outline: "k" },
  poly([11, 0, 14, 7, 22, 8, 16, 13, 18, 21, 11, 17, 4, 21, 6, 13, 0, 8, 8, 7], "y"),
  clip((x, y) => x > 11, shade()), px(9, 7, "w"), px(8, 8, "w"))

sprite("bottle_cap", { w: 46, h: 46, outline: "k" },
  (c) => { for (let i = 0; i < 21; i++) { const a = i / 21 * Math.PI * 2; c.circ(Math.round(23 + Math.cos(a) * 19), Math.round(23 + Math.sin(a) * 19), 3, "R") } },
  ellipse(23, 23, 19, 19, "r"), ring(23, 23, 15, "R"), ellipse(23, 23, 13, 13, "E"), ellipse(21, 21, 8, 8, "r"),
  poly([15, 13, 21, 9, 19, 12, 14, 18], "w"), px(26, 12, "w"))

sprite("tshirt", { w: 46, h: 46, outline: "k" },
  poly([15, 5, 31, 5, 45, 15, 39, 24, 34, 21, 34, 45, 12, 45, 12, 21, 7, 24, 0, 15], "p"),
  ellipse(23, 4, 6, 3, "P"), ellipse(23, 3, 6, 3, "_"), rect(15, 0, 16, 2, "_"),
  rect(12, 41, 22, 1, "P"), line(12, 21, 12, 40, "M"), line(7, 22, 10, 18, "P"), line(39, 22, 36, 18, "P"), rect(18, 18, 10, 7, "q"))

sprite("needle", { w: 26, h: 26, outline: "k" },
  line(3, 22, 21, 4, "T", 2), line(4, 22, 21, 5, "s"), px(21, 3, "_"), px(20, 4, "_"), px(3, 23, "S"),
  line(21, 4, 24, 12, "e"), line(24, 12, 19, 18, "e"), line(19, 18, 22, 23, "e"))

// Parka baglanan yol, tas kenar ve cicekler; mevcut park resminin ustune yazilir.
scene("park_overlay", { w: 320, h: 180 },
  rect(138, 86, 44, 36, "#7FB069"), poly([152, 88, 168, 88, 188, 124, 132, 124], "#E3D3B0"),
  line(151, 88, 131, 124, "F"), line(169, 88, 189, 124, "F"),
  
  (c) => { const rng = c.rng(7); const colors = ["w", "y", "M", "E"]
    for (let i = 0; i < 46; i++) { const x = 6 + Math.floor(rng() * 308), y = 96 + Math.floor(rng() * 80)
      if (((x - 160) * (x - 160)) / 11000 + ((y - 140) * (y - 140)) / 900 < 1 || (x > 125 && x < 195 && y < 125)) continue
      const k = colors[i % 4]; c.px(x, y, k); c.px(x - 1, y, k); c.px(x + 1, y, k); c.px(x, y - 1, k); c.px(x, y + 1, "L"); c.px(x, y, "y") } })

sprite("gear", { w: 28, h: 28 },
  (c) => { for (let i = 0; i < 8; i++) { const a = i / 8 * Math.PI * 2; c.line(14, 14, Math.round(14 + Math.cos(a) * 12), Math.round(14 + Math.sin(a) * 12), "w", 4) } },
  circ(14, 14, 9, "w"), circ(14, 14, 4, "_"))
