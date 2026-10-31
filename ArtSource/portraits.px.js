// Musteri portreleri: 64x72, ReTamir'in duz renk + koyu cizgi tarzinda.
// Cikti: out/scenes/<id>.png ve <id>_happy.png -> Resources/Sprites/Customers
const INK = "#2b2b30"
const inBody = (x, y) => ((x - 30.5) * (x - 30.5)) / 800 + ((y - 75) * (y - 75)) / 300 < 1
const mix = (a, b, k) => "#" + [1, 3, 5].map((i) => Math.round(parseInt(a.slice(i, i + 2), 16) * (1 - k) + parseInt(b.slice(i, i + 2), 16) * k).toString(16).padStart(2, "0")).join("")

function eye(x, y) {
  return grid(x, y, `.kkkkk.
kwiinwk
.wiinw.
..www..`)
}

function happyEye(x, y) {
  return grid(x, y, `..kkk..
.kk.kk.
kk...kk`)
}

function brow(x, y, c, mirrored, w) {
  return mirrored ? [rect(x, y, w, 2, c), px(x + w - 1, y + 2, ".")] : [rect(x, y, w, 2, c)]
}

function glasses() {
  return [box(18, 31, 11, 9, "g"), box(33, 31, 11, 9, "g"), hline(29, 33, 4, "g"), hline(15, 33, 3, "g"), hline(44, 33, 3, "g")]
}

function portrait(id, c, happy) {
  pal({ k: INK, w: "#fff8f0", n: "#1d1a22", i: c.eye, r: "#7a2030", p: "#d9606a", b: mix(c.skin[0], "#e0604f", 0.4), g: c.frame || "#34343c",
    s: c.skin[0], S: c.skin[1], D: c.skin[2], h: c.hair[0], H: c.hair[1], J: c.hair[2],
    t: c.top[0], T: c.top[1], U: c.top[2], a: c.accent[0], A: c.accent[1] })
  ramps("DSs", "JHh", "UTt")

  const browY = happy ? 26 : 28
  const items = [
    ...(c.behind ? c.behind() : []),
    // Omuz ve boyun
    ellipse(30, 75, 27, 17, "t"), ellipse(31, 75, 27, 17, "t"),
    rect(25, 48, 12, 12, "s"), rect(25, 48, 12, 4, "D"), rect(35, 52, 2, 8, "S"),
    ...(c.clothes ? c.clothes() : []),
    // Bas
    ...(c.noEars ? [] : [ellipse(14, 35, 2, 4, "S"), ellipse(47, 35, 2, 4, "S")]),
    ellipse(30, 32, 15, 18, "s"), ellipse(31, 32, 15, 18, "s"),
    clip((x, y) => y > 8 && y < 53, shade((x, y) => (x - 26) * (x - 26) / 324 + (y - 30) * (y - 30) / 441 > 1)),
    // Burun
    vline(32, 34, 5, "S"), hline(29, 39, 4, "S"), px(28, 38, "S"), px(33, 38, "S"),
  ]

  if (c.age) items.push(hline(23, 21, 16, "S"), hline(25, 24, 12, "S"), px(16, 34, "S"), px(16, 36, "S"), px(45, 34, "S"), px(45, 36, "S"), line(24, 40, 22, 45, "S"), line(37, 40, 39, 45, "S"))
  if (c.freckles) items.push(px(21, 38, "S"), px(24, 40, "S"), px(19, 40, "S"), px(40, 38, "S"), px(37, 40, "S"), px(42, 40, "S"))

  if (happy) {
    // Yanak yukselir, gozler yukari kavis alir, agiz acilir.
    items.push(ellipse(20, 41, 3, 1, "b"), ellipse(41, 41, 3, 1, "b"))
    items.push(happyEye(19, 33), happyEye(36, 33))
    items.push(grid(22, 42, `k................k
kk..............kk
.kkkkkkkkkkkkkkkk.
.kwwwwwwwwwwwwwwk.
..kwwwwwwwwwwwwk..
..krrrrrrrrrrrrk..
...krrppppppppk...
....kkkkkkkkkk....`))
  } else {
    items.push(eye(19, 32), eye(36, 32))
    items.push(px(24, 45, "D"), hline(25, 46, 12, "D"), px(37, 45, "D"), hline(27, 47, 8, "S"))
  }

  items.push(rect(19, browY, 8, 2, c.browColor || "J"), rect(35, browY, 8, 2, c.browColor || "J"))
  if (c.brow) items.push(rect(18, browY - 1, 10, 3, c.brow), rect(34, browY - 1, 10, 3, c.brow))
  if (c.glasses) items.push(...glasses())
  if (c.front) items.push(...c.front(happy))

  const name = id + (happy ? "_happy" : "")
  sprite("_" + name, { w: 62, h: 72, outline: "k" }, ...items)
  scene(name, { w: 64, h: 72 }, stamp("_" + name, 0, 0))
}

const people = {
  zeynep: {
    skin: ["#f2c9a1", "#dba67c", "#b57f5e"], hair: ["#6b4226", "#4a2f1d", "#33201a"], top: ["#2a8a84", "#1f6f6b", "#155250"],
    accent: ["#e7dcc3", "#c9bda0"], eye: "#5a3a22", glasses: true,
    behind: () => [ellipse(31, 30, 20, 21, "H"), rect(11, 30, 41, 34, "H"), rect(13, 40, 3, 24, "J"), rect(46, 40, 3, 24, "J")],
    clothes: () => [poly([24, 56, 31, 64, 38, 56, 40, 58, 31, 69, 22, 58], "a"), clip(inBody, rect(0, 66, 62, 2, "T"))],
    front: () => [poly([14, 34, 15, 20, 22, 12, 40, 12, 47, 20, 48, 34, 44, 24, 34, 18, 26, 25, 19, 25], "h"), line(22, 15, 18, 28, "H"), line(38, 15, 44, 27, "H"), line(30, 13, 28, 21, "H")],
  },
  hatice_teyze: {
    skin: ["#edbe96", "#d39c74", "#ad7757"], hair: ["#7fb069", "#5d8f52", "#41703f"], top: ["#d2644a", "#b84c36", "#8e3626"],
    accent: ["#f0d9a0", "#d9b866"], eye: "#4a3220", age: true, noEars: true, browColor: "#6b4a2e",
    behind: () => [ellipse(31, 32, 21, 24, "H"), ellipse(30, 32, 21, 24, "H")],
    clothes: () => [clip(inBody, hline(0, 64, 62, "T")), vline(31, 58, 14, "U"), px(29, 63, "a"), px(29, 68, "a")],
    front: () => [
      poly([11, 34, 13, 18, 22, 8, 40, 8, 49, 18, 51, 34, 46, 34, 45, 24, 38, 18, 24, 18, 17, 24, 16, 34], "h"),
      poly([12, 36, 16, 36, 20, 50, 31, 54, 42, 50, 46, 36, 50, 36, 52, 52, 42, 62, 20, 62, 10, 52], "h"),
      line(18, 12, 14, 30, "H"), line(44, 12, 48, 30, "H"), line(28, 9, 26, 17, "H"), line(36, 9, 37, 17, "H"),
      poly([31, 53, 38, 58, 33, 66, 29, 66, 24, 58], "H"), line(31, 54, 31, 65, "J"),
      px(22, 13, "a"), px(33, 12, "a"), px(41, 15, "a"), px(16, 22, "a"), px(46, 23, "a"), px(14, 45, "a"), px(48, 46, "a"), px(21, 57, "a"), px(42, 57, "a"),
    ],
  },
  recep_amca: {
    skin: ["#d9a77c", "#bd8761", "#966546"], hair: ["#cfd0d2", "#a9abb0", "#85878c"], top: ["#8a6a48", "#6e5236", "#523c26"],
    accent: ["#e7dcc3", "#c9bda0"], eye: "#3e2a1b", age: true, brow: "h",
    clothes: () => [poly([24, 56, 38, 56, 31, 70], "a"), line(31, 68, 31, 72, "U"), line(24, 57, 31, 69, "U"), line(38, 57, 31, 69, "U")],
    front: (happy) => [
      ellipse(15, 28, 3, 7, "H"), ellipse(47, 28, 3, 7, "H"), ellipse(15, 26, 2, 4, "h"), ellipse(47, 26, 2, 4, "h"),
      poly(happy ? [21, 41, 31, 39, 41, 41, 42, 44, 36, 43, 31, 41, 26, 43, 20, 44] : [20, 42, 31, 40, 42, 42, 43, 46, 36, 44, 31, 43, 26, 44, 19, 46], "h"), line(26, 42, 31, 41, "H"),
    ],
  },
  deniz: {
    skin: ["#f2c9a1", "#dba67c", "#b57f5e"], hair: ["#3a3a42", "#26262c", "#17171b"], top: ["#f0bb55", "#d99a32", "#b37a22"],
    accent: ["#2a8a84", "#1f6f6b"], eye: "#3e2a1b", freckles: true,
    clothes: () => [clip(inBody, rect(0, 64, 62, 3, "a")), poly([24, 56, 31, 61, 38, 56, 40, 58, 31, 64, 22, 58], "T")],
    front: () => [
      ellipse(30, 19, 17, 12, "h"), ellipse(32, 19, 17, 12, "h"), rect(13, 18, 36, 6, "h"),
      poly([13, 30, 14, 18, 49, 18, 49, 30, 45, 22, 40, 26, 36, 21, 30, 26, 25, 21, 20, 26, 17, 22], "h"),
      poly([22, 9, 26, 3, 29, 9, 33, 2, 36, 9, 41, 5, 42, 12, 20, 12], "h"), line(20, 12, 24, 19, "H"), line(38, 11, 42, 19, "H"), line(30, 8, 30, 18, "H"),
    ],
  },
  kemal_usta: {
    skin: ["#c99468", "#ac774f", "#855838"], hair: ["#3f77b4", "#2f5f96", "#234874"], top: ["#4a7fae", "#3f6e99", "#2e5273"],
    accent: ["#e8b04a", "#c4902f"], eye: "#2b1d14", brow: "#3a2a1e", frame: "#3a2a1e",
    clothes: () => [rect(16, 58, 6, 14, "a"), rect(40, 58, 6, 14, "a"), poly([24, 56, 31, 63, 38, 56, 40, 59, 31, 67, 22, 59], "U"), box(25, 66, 12, 7, "U")],
    front: () => [
      poly([13, 26, 15, 13, 24, 7, 38, 7, 47, 13, 49, 26], "h"), rect(13, 22, 36, 4, "H"),
      poly([10, 25, 52, 25, 56, 30, 42, 28, 20, 28, 6, 30], "J"), rect(27, 12, 8, 7, "a"), box(27, 12, 8, 7, "A"),
      line(22, 10, 19, 21, "H"), line(40, 10, 43, 21, "H"),
      rect(14, 28, 3, 6, "#3a2a1e"), rect(45, 28, 3, 6, "#3a2a1e"),
    ],
  },
  muhtar_selma: {
    skin: ["#e6b48c", "#cb9570", "#a4714f"], hair: ["#b4b4b8", "#8c8c92", "#6d6d74"], top: ["#8a6aa8", "#70528e", "#553d70"],
    accent: ["#f4ecd8", "#d6cbb0"], eye: "#4a3220", glasses: true, age: true,
    behind: () => [ellipse(31, 8, 9, 7, "h"), ellipse(31, 8, 5, 3, "H"), ellipse(30, 26, 19, 17, "H"), ellipse(32, 26, 19, 17, "H")],
    clothes: () => [poly([24, 56, 38, 56, 34, 72, 28, 72], "a"), poly([24, 55, 31, 66, 29, 72, 18, 72, 14, 60], "T"), poly([38, 55, 31, 66, 33, 72, 44, 72, 48, 60], "T"), rect(39, 62, 3, 3, "#e8b04a"), px(40, 63, "#c4902f")],
    front: () => [poly([13, 34, 13, 20, 22, 12, 40, 12, 49, 20, 49, 34, 45, 25, 36, 17, 27, 19, 18, 25], "h"), line(24, 14, 17, 26, "H"), line(38, 14, 45, 26, "H"), line(31, 13, 27, 19, "H")],
  },
}

for (const id of Object.keys(people)) {
  portrait(id, people[id], false)
  portrait(id, people[id], true)
}
sheet("all", ["zeynep", "zeynep_happy", "hatice_teyze", "hatice_teyze_happy", "recep_amca", "recep_amca_happy", "deniz", "deniz_happy", "kemal_usta", "kemal_usta_happy", "muhtar_selma", "muhtar_selma_happy"], { cols: 4, pad: 4 })
