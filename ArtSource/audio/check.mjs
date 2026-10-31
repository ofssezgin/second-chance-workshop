// Uretilen seslerin seviyesini olcer: sessizlik, kesilme, dongu gecisi.
import * as fs from "node:fs/promises";
const dir = new URL("../../Assets/_Game/Resources/Audio/", import.meta.url);
for (const file of (await fs.readdir(dir)).filter((f) => f.endsWith(".wav"))) {
  const b = await fs.readFile(new URL(file, dir));
  let p = 12, fmt = null, data = null;
  while (p < b.length - 8) {
    const id = b.toString("latin1", p, p + 4), size = b.readUInt32LE(p + 4);
    if (id === "fmt ") fmt = { ch: b.readUInt16LE(p + 10), rate: b.readUInt32LE(p + 12), bits: b.readUInt16LE(p + 22) };
    if (id === "data") data = b.subarray(p + 8, p + 8 + size);
    p += 8 + size + (size & 1);
  }
  const step = fmt.bits / 8, frames = Math.floor(data.length / step / fmt.ch);
  const read = (i, c = 0) => fmt.bits === 16 ? data.readInt16LE((i * fmt.ch + c) * step) / 32768 : data.readFloatLE((i * fmt.ch + c) * step);
  let peak = 0, sum = 0, clipped = 0;
  for (let i = 0; i < frames; i++) { const v = read(i); peak = Math.max(peak, Math.abs(v)); sum += v * v; if (Math.abs(v) > 0.99) clipped++ }
  const seam = Math.abs(read(frames - 1) - read(0));
  const rmsDb = (10 * Math.log10(sum / frames)).toFixed(1);
  let line = `${file.padEnd(16)} ${(frames / fmt.rate).toFixed(2)}s ${fmt.ch}k ${fmt.bits}bit tepe ${peak.toFixed(2)} ort ${rmsDb}dB kesilen ${clipped} gecis ${seam.toFixed(3)}`;
  if (file.startsWith("music")) {
    const parts = [];
    const window = Math.floor(frames / 8);
    for (let w = 0; w < 8; w++) { let s = 0; for (let i = 0; i < window; i++) { const v = read(w * window + i); s += v * v } parts.push((10 * Math.log10(s / window)).toFixed(0)) }
    line += ` bolumler ${parts.join(" ")}`;
    // Enerjinin yuzde kaci yuksek frekansta: kulagi rahatsiz eden parlaklik olcusu.
    const share = (cutoff) => {
      const a = 1 - Math.exp(-2 * Math.PI * cutoff / fmt.rate);
      let l1 = 0, l2 = 0, high = 0, all = 0;
      for (let i = 0; i < frames; i++) { const v = read(i); l1 += a * (v - l1); l2 += a * (l1 - l2); const h = v - l2; high += h * h; all += v * v }
      return (100 * high / all).toFixed(1);
    };
    line += ` | 2kHz+ %${share(2000)} 5kHz+ %${share(5000)}`;
  }
  console.log(line);
}
