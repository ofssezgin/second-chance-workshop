// Son sahnedeki Mahalle Aniti: atiktan agac ve meyve koparan robot.
pal({ k: "#2b2b30", t: "#3a3a42", T: "#55555e", p: "#b8875a", P: "#8a6a48", N: "#6e5236",
  r: "#c8553d", R: "#9a3c2a", s: "#c9d1d8", S: "#8f99a3", w: "#f4f2ee",
  b: "#a8d8f0", B: "#6fb2dc", d: "#4a7fb8", D: "#2f5f96",
  g: "#3d9a78", G: "#2a7558", l: "#7fb069", L: "#a7cf86", y: "#e8b04a", Y: "#c4902f", c: "#d9b878" })

function tireSide(x, y, w, h) {
  const list = [rect(x + 1, y, w - 2, h, "t"), rect(x, y + 1, w, h - 2, "t"), hline(x + 2, y + 1, w - 4, "T")]
  for (let i = x + 3; i < x + w - 2; i += 4) list.push(vline(i, y + 2, h - 3, "k"))
  return list
}

function tireRing(cx, cy, r) {
  return [circ(cx, cy, r, "t"), ring(cx, cy, r - 1, "T"), circ(cx, cy, Math.max(1, r - 3), "_")]
}

function can(x, y, h) {
  return [rect(x, y, 11, h, "s"), rect(x, y + 3, 11, h - 6, "r"), hline(x, y + 3, 11, "y"), hline(x, y + h - 4, 11, "y"), vline(x + 9, y, h, "S"), hline(x + 1, y, 9, "w")]
}

function bottle(x, y, body, dark, cap) {
  return [rect(x + 1, y, 2, 2, cap), rect(x + 1, y + 2, 2, 2, body), rect(x, y + 4, 4, 9, body), vline(x + 3, y + 4, 9, dark), vline(x + 1, y + 5, 5, "w")]
}

sprite("_monument", { w: 90, h: 73, outline: "k" },
  // Agac: lastik govde, palet dallar, sise kapagi meyveler
  ...tireSide(14, 63, 26, 10), ...tireSide(16, 54, 22, 9), ...tireSide(18, 46, 18, 8),
  line(27, 47, 9, 31, "p", 3), line(27, 47, 46, 31, "p", 3), line(27, 47, 27, 23, "P", 4), line(19, 39, 16, 27, "p", 2), line(35, 39, 39, 26, "p", 2),
  ellipse(28, 20, 28, 19, "G"), ellipse(14, 26, 13, 11, "g"), ellipse(42, 26, 14, 11, "g"), ellipse(28, 14, 19, 12, "l"),
  (c) => { const dots = [[7, 23, "r"], [16, 14, "y"], [27, 8, "b"], [38, 13, "r"], [49, 22, "y"], [13, 31, "b"], [23, 24, "L"], [35, 24, "r"], [28, 16, "y"], [19, 8, "L"], [37, 6, "L"]]
    for (const [x, y, k] of dots) { c.circ(x, y, 2, k); c.px(x, y, "w") } },
  ...bottle(3, 33, "b", "B", "D"),
  // Robot: tiptoe uzanip en uctaki meyveyi koparir
  ...tireSide(56, 66, 12, 7), ...tireSide(72, 66, 12, 7), rect(60, 59, 5, 7, "d"), rect(75, 59, 5, 7, "d"), vline(62, 59, 7, "D"), vline(77, 59, 7, "D"),
  rect(55, 41, 29, 18, "p"), rect(55, 41, 29, 2, "P"), vline(70, 43, 16, "P"), rect(76, 52, 6, 5, "P"),
  circ(62, 50, 5, "s"), ring(62, 50, 5, "S"), circ(62, 50, 1, "w"), px(59, 47, "r"), px(65, 53, "b"),
  rect(65, 37, 9, 4, "s"), hline(65, 38, 9, "y"),
  rect(58, 24, 23, 13, "s"), rect(58, 27, 23, 7, "r"), hline(58, 27, 23, "y"), hline(58, 33, 23, "y"), vline(78, 24, 13, "S"), hline(59, 24, 18, "w"),
  circ(65, 30, 2, "w"), circ(74, 30, 2, "w"), px(64, 29, "k"), px(73, 29, "k"), px(67, 34, "k"), hline(68, 35, 3, "k"), px(71, 34, "k"),
  line(69, 24, 69, 20, "S"), px(69, 19, "y"),
  line(56, 44, 51, 34, "c", 2), line(51, 34, 53, 27, "c", 2), circ(53, 25, 2, "s"), circ(50, 22, 3, "r"), px(50, 21, "w"), px(52, 19, "G"), px(53, 18, "l"),
  line(84, 45, 87, 54, "c", 2), rect(82, 55, 8, 9, "s"), rect(82, 58, 8, 3, "r"), hline(82, 55, 8, "w"), circ(84, 53, 2, "r"), circ(88, 53, 2, "y"), px(86, 51, "b"))

scene("monument", { w: 92, h: 75 }, stamp("_monument", 0, 0))

pal({ k: "#2b2b30", a: "#c8c3b4", A: "#a8a396", B: "#928d80" })
sprite("pedestal", { w: 94, h: 18, outline: "k" }, rect(4, 0, 86, 8, "a"), rect(0, 8, 94, 10, "A"), hline(0, 8, 94, "B"), hline(4, 7, 86, "A"))

// Basarisiz son: ayni heykel, agac kurumus ve robot uzgun.
pal({ k: "#2b2b30", t: "#3a3a42", T: "#55555e", p: "#8f7350", P: "#6e5236", N: "#54402c", s: "#b4bac0", S: "#858d95", w: "#e8e4da",
  r: "#b8704f", R: "#8a5238", y: "#c9a35a", d: "#4a6f96", D: "#2f4f76", b: "#a8d8f0", c: "#c9ad78", o: "#a8843f", O: "#7d6230" })
sprite("_monument_dry", { w: 90, h: 73, outline: "k" },
  ...tireSide(14, 63, 26, 10), ...tireSide(16, 54, 22, 9), ...tireSide(18, 46, 18, 8),
  line(27, 47, 27, 14, "N", 4), line(26, 46, 26, 22, "P", 2),
  line(27, 41, 9, 31, "P", 2), line(9, 31, 3, 40, "N", 2), line(16, 36, 12, 42, "N"),
  line(27, 36, 46, 27, "P", 2), line(46, 27, 53, 36, "N", 2), line(39, 30, 42, 37, "N"),
  line(27, 28, 13, 15, "P", 2), line(13, 15, 6, 22, "N", 2), line(19, 21, 17, 28, "N"),
  line(28, 23, 41, 10, "P", 2), line(41, 10, 48, 17, "N", 2), line(35, 16, 37, 23, "N"),
  line(27, 14, 26, 5, "N", 2), line(26, 8, 21, 5, "N"), line(26, 9, 31, 4, "N"),
  px(4, 42, "o"), px(53, 38, "O"), px(12, 44, "o"), px(48, 18, "o"), px(7, 23, "O"),
  ...tireSide(56, 66, 12, 7), ...tireSide(72, 66, 12, 7), rect(60, 59, 5, 7, "d"), rect(75, 59, 5, 7, "d"), vline(62, 59, 7, "D"), vline(77, 59, 7, "D"),
  rect(55, 41, 29, 18, "p"), rect(55, 41, 29, 2, "P"), vline(70, 43, 16, "P"), rect(76, 52, 6, 5, "P"),
  circ(62, 50, 5, "s"), ring(62, 50, 5, "S"), circ(62, 50, 1, "w"),
  rect(65, 38, 9, 3, "s"), rect(58, 26, 23, 13, "s"), rect(58, 29, 23, 7, "r"), hline(58, 29, 23, "y"), hline(58, 35, 23, "y"), vline(78, 26, 13, "S"), hline(59, 26, 18, "w"),
  circ(65, 32, 2, "w"), circ(74, 32, 2, "w"), px(65, 33, "k"), px(74, 33, "k"),
  line(62, 29, 67, 27, "k"), line(77, 29, 72, 27, "k"), hline(67, 36, 5, "k"), px(66, 37, "k"), px(72, 37, "k"),
  px(62, 34, "b"), px(62, 35, "b"), px(62, 37, "b"),
  line(69, 26, 69, 22, "S"), line(69, 22, 73, 20, "S"), px(74, 21, "y"),
  line(56, 44, 51, 56, "c", 2), circ(50, 58, 2, "s"), line(84, 45, 87, 56, "c", 2), circ(88, 58, 2, "s"),
  rect(42, 64, 13, 8, "s"), rect(42, 66, 13, 4, "r"), vline(42, 64, 8, "S"), circ(50, 71, 1, "r"), px(38, 71, "O"), px(46, 72, "o"))
scene("monument_dry", { w: 92, h: 75 }, stamp("_monument_dry", 0, 0))
