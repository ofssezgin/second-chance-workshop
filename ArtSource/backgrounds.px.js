// Kamyon sahnesinin mahalle arka plani: 320x180. Kaldirim y=140, yol y=144.
const BASE = {
  k: "#3a3438", w: "#f6efdc", c: "#d8cfb4", g: "#8fb9c9", G: "#6f9fb5", l: "#f4d27a",
  a: "#9aa0a4", A: "#7d8387", r: "#5b5f63", R: "#e8e0cc", T: "#8a6a48", N: "#6e5236",
  v: "#7fb069", V: "#5d8f52", W: "#41703f", F: "#a7cf86", q: "#c8553d", Q: "#9a3c2a",
  y: "#e8b04a", Y: "#c4902f", b: "#3f77b4", p: "#e7a0bb", o: "#d88a68", O: "#b86e50", u: "#a9b8a6",
}

function street() {
  const dashes = []
  for (let x = 6; x < 320; x += 30) dashes.push(rect(x, 164, 16, 2, "R"))
  return [rect(0, 140, 320, 4, "a"), rect(0, 143, 320, 1, "A"), rect(0, 144, 320, 36, "r"), ...dashes]
}

function sky(top, low, clouds) {
  const list = [grad(0, 0, 320, 140, top + low, "v", 0.5)]
  for (const [x, y, w] of clouds) list.push(ellipse(x, y, w, 5, "w"), ellipse(x + w * 0.5, y - 3, w * 0.55, 5, "w"), ellipse(x - w * 0.4, y + 1, w * 0.5, 4, "w"))
  return list
}

function tree(x, base, size) {
  return [rect(x - 2, base - size - 2, 4, size + 2, "T"), rect(x, base - size - 2, 2, size + 2, "N"),
    ellipse(x, base - size - 8, size, size - 2, "V"), ellipse(x - size / 2, base - size - 4, size / 1.6, size / 2, "v"),
    ellipse(x + size / 2, base - size - 5, size / 1.8, size / 2, "W"), ellipse(x - 2, base - size - 12, size / 2, size / 3, "F")]
}

function window(x, y, lit) {
  return [rect(x, y, 10, 13, "w"), rect(x + 1, y + 1, 8, 11, lit ? "l" : "g"), rect(x + 1, y + 1, 8, 3, lit ? "y" : "G"),
    vline(x + 5, y + 1, 11, "w"), rect(x - 1, y + 13, 12, 2, "c")]
}

function flowers(x, y) {
  return [rect(x, y, 12, 2, "T"), px(x + 2, y - 1, "q"), px(x + 5, y - 1, "p"), px(x + 8, y - 1, "y"), px(x + 3, y - 2, "V"), px(x + 7, y - 2, "V")]
}

// Korkulugun ustunde duran saksi; her biri farkli cicek acar.
function pot(x, y, kind) {
  const bloom = ["q", "p", "y"][kind % 3]
  return [rect(x, y - 3, 5, 3, "O"), hline(x - 1, y - 3, 7, "o"), rect(x + 1, y - 6, 3, 3, "V"), px(x, y - 5, "v"), px(x + 4, y - 5, "v"), px(x + 2, y - 7, bloom), px(x + 1, y - 6, bloom), px(x + 3, y - 6, bloom)]
}

// Bahce citi; kapi hizasinda bos birakilir.
function fence(x0, x1, y = 128) {
  const list = []
  for (let x = x0; x < x1; x += 5) list.push(rect(x, y + 1, 3, 11, "w"), px(x + 1, y, "w"), rect(x + 2, y + 1, 1, 11, "c"))
  list.push(rect(x0, y + 3, x1 - x0 - 2, 1, "c"), rect(x0, y + 8, x1 - x0 - 2, 1, "c"))
  return list
}

function roof(x, w, top, color, dark) {
  const list = [poly([x - 4, top, x + w + 4, top, x + w - 7, top - 13, x + 7, top - 13], color)]
  for (let y = top - 11; y < top; y += 3) list.push(clip([x - 4, top, x + w + 4, top, x + w - 7, top - 13, x + 7, top - 13], hline(x - 4, y, w + 8, dark)))
  list.push(rect(x - 5, top, w + 10, 2, dark))
  return list
}

// Kat sayisi kadar pencere dizer; zemin kat kapi, dukkan ya da pencere olur.
function house(x, w, floors, wall, shade, roofColor, roofDark, o = {}) {
  const base = o.base || 140, height = floors * 22 + 4, top = base - height
  const list = [rect(x, top, w, height, wall), rect(x + w - 4, top, 4, height, shade), rect(x, base - 3, w, 3, shade), ...roof(x, w, top, roofColor, roofDark)]
  if (o.chimney) list.push(rect(x + w - 16, top - 20, 6, 12, "O"), rect(x + w - 17, top - 21, 8, 2, "N"))
  const count = Math.floor((w - 6) / 16)
  const gap = (w - count * 10) / (count + 1)
  for (let floor = 0; floor < floors; floor++) {
    const y = top + 6 + floor * 22
    const ground = floor === floors - 1
    if (ground && (o.shop || o.door)) continue
    for (let i = 0; i < count; i++) {
      const wx = Math.round(x + gap + i * (10 + gap))
      list.push(...window(wx, y, o.lit && (i + floor) % 2 === 0))
      if (o.flowers && (i + floor) % 2 === 0) list.push(...flowers(wx - 1, y + 13))
    }
    if (o.balcony === floor) {
      list.push(rect(x + 3, y + 15, w - 6, 2, "c"), rect(x + 3, y + 9, w - 6, 1, "k"), ...Array.from({ length: Math.floor((w - 6) / 4) + 1 }, (_, i) => vline(x + 3 + i * 4, y + 9, 6, "k")))
      if (o.pots) for (let i = 0; i < o.pots; i++) list.push(...pot(x + 7 + i * Math.floor((w - 18) / Math.max(1, o.pots - 1 || 1)), y + 9, i))
    }
  }
  if (o.door) list.push(rect(x + Math.floor(w / 2) - 7, base - 20, 14, 20, "w"), rect(x + Math.floor(w / 2) - 6, base - 19, 12, 19, "N"), rect(x + Math.floor(w / 2) - 5, base - 18, 10, 6, "g"), px(x + Math.floor(w / 2) + 3, base - 9, "y"))
  if (o.shop) {
    const s = o.shop
    list.push(rect(x + 4, base - 24, w - 8, 24, "w"), rect(x + 6, base - 18, w - 24, 18, o.lit ? "l" : "g"), rect(x + w - 16, base - 19, 10, 19, "N"), rect(x + w - 14, base - 17, 6, 8, "g"))
    for (let i = 0; i < w - 4; i += 6) list.push(rect(x + 2 + i, base - 30, 6, 7, i % 12 === 0 ? s : "w"))
    list.push(rect(x + 2, base - 23, w - 4, 1, "k"))
    list.push(rect(x + 8, base - 8, 8, 8, "T"), px(x + 10, base - 9, "q"), px(x + 12, base - 9, "y"), px(x + 14, base - 9, "v"))
  }
  return list
}

function laundry(x0, y0, x1, y1) {
  const list = [line(x0, y0, x1, y1, "k")]
  const colors = ["w", "q", "b", "p", "y"]
  for (let i = 0; i < 5; i++) {
    const t = (i + 0.7) / 5.4, x = Math.round(x0 + (x1 - x0) * t), y = Math.round(y0 + (y1 - y0) * t)
    list.push(rect(x, y + 1, 6, 7 + i % 2 * 2, colors[i]))
  }
  return list
}

function lamp(x) {
  return [rect(x, 110, 2, 30, "k"), rect(x, 110, 8, 2, "k"), rect(x + 6, 112, 5, 3, "l")]
}

function cat(x, y) {
  return grid(x, y, `.k...k
.kkkkk
.kwkwk
kkkkk.
kkkkk.
k.k.k.`)
}

pal({ ...BASE, s: "#bfe3ec", S: "#d9eff2", h: "#a7cf86", H: "#8fbd74" })
scene("background", { w: 320, h: 180 }, ...sky("s", "S", [[60, 24, 22], [254, 34, 18]]),
  ellipse(250, 142, 170, 88, "H"), ellipse(60, 142, 160, 82, "h"), ...tree(306, 140, 15),
  ...house(-6, 62, 2, "#d88a68", "#b86e50", "q", "Q", { shop: "q" }),
  ...house(60, 54, 3, "#ecd9a8", "#d4bd86", "q", "Q", { door: true, balcony: 1, pots: 3, flowers: true }),
  ...house(118, 70, 2, "#a9c4b8", "#8aa89b", "o", "O", { shop: "b", chimney: true }),
  ...house(192, 52, 3, "#e7b98c", "#cc9c70", "q", "Q", { door: true, balcony: 0, pots: 2 }),
  ...house(248, 58, 2, "#f0ead8", "#d8d0ba", "o", "O", { door: true, balcony: 0, pots: 3, chimney: true }),
  ...laundry(114, 70, 192, 74), ...laundry(244, 96, 192, 100),
  ...fence(60, 78), ...fence(100, 114), ...fence(192, 210), ...fence(232, 266), ...fence(290, 320), ...lamp(241), cat(106, 123), ...street())
