// Oyunun muzik ve efektlerini uretir: node gen.mjs -> Assets/_Game/Resources/Audio/*.wav
// Sesler yalniz yumusak sinus dalgalaridir: keskin enstruman ve gurultu kullanilmaz.
import * as fs from "node:fs/promises";

const OUT = new URL("../../Assets/_Game/Resources/Audio/", import.meta.url);
const RATE = 44100;

function save(name, data) {
  const pcm = Buffer.alloc(44 + data.length * 2);
  pcm.write("RIFF", 0); pcm.writeUInt32LE(36 + data.length * 2, 4); pcm.write("WAVEfmt ", 8);
  pcm.writeUInt32LE(16, 16); pcm.writeUInt16LE(1, 20); pcm.writeUInt16LE(1, 22);
  pcm.writeUInt32LE(RATE, 24); pcm.writeUInt32LE(RATE * 2, 28); pcm.writeUInt16LE(2, 32); pcm.writeUInt16LE(16, 34);
  pcm.write("data", 36); pcm.writeUInt32LE(data.length * 2, 40);
  data.forEach((value, i) => pcm.writeInt16LE(Math.round(Math.max(-1, Math.min(1, value)) * 32767), 44 + i * 2));
  return fs.writeFile(new URL(name + ".wav", OUT), pcm);
}

// Eski AudioKit formulu: yumusak sonumlu notalar art arda calar.
function notes(noteSeconds, ...frequencies) {
  const count = Math.floor(noteSeconds * RATE);
  const data = new Float32Array(count * frequencies.length);
  frequencies.forEach((frequency, n) => {
    for (let i = 0; i < count; i++) {
      const t = i / RATE;
      data[n * count + i] = Math.sin(2 * Math.PI * frequency * t) * Math.exp(-t * 5 / noteSeconds) * Math.min(1, i / 80);
    }
  });
  return data;
}

const hz = (midi) => 440 * 2 ** ((midi - 69) / 12);

// Eski arpej sesi; notalar kesilmeden sonumlenip birbirine karisir. Dongu basa sarar.
function pluck(target, start, frequency, gain) {
  const length = Math.floor(1.6 * RATE);
  for (let i = 0; i < length; i++) {
    const t = i / RATE;
    const value = (Math.sin(2 * Math.PI * frequency * t) + 0.3 * Math.sin(4 * Math.PI * frequency * t)) * Math.exp(-t * 3) * Math.min(1, i / 200) * gain;
    target[(start + i) % target.length] += value;
  }
}

// Melodi icin yavas girip yavas sonen tek sinus.
function hum(target, start, frequency, seconds, gain) {
  const length = Math.floor(seconds * RATE);
  for (let i = 0; i < length; i++) {
    const t = i / length;
    target[(start + i) % target.length] += Math.sin(2 * Math.PI * frequency * i / RATE) * Math.sin(Math.PI * t) ** 2 * gain;
  }
}

const CHORDS = { C: [60, 64, 67, 72], Am: [57, 60, 64, 69], F: [53, 57, 60, 65], G: [55, 59, 62, 67], Em: [52, 55, 59, 64], Dm: [50, 53, 57, 62] };

function song(noteSeconds, form, tune, shift = 0) {
  const noteSamples = Math.floor(noteSeconds * RATE);
  const data = new Float32Array(noteSamples * 4 * form.length);
  form.forEach((chord, bar) => {
    [0, 2, 1, 3].forEach((index, n) => pluck(data, (bar * 4 + n) * noteSamples, hz(CHORDS[chord][index] + shift), 0.3));
  });
  for (const [bar, midi, bars] of tune) hum(data, bar * 4 * noteSamples, hz(midi), bars * 4 * noteSeconds, 0.1);
  return data;
}

const a = ["C", "Am", "F", "G"];
const b = ["Am", "F", "C", "G"];
const c = ["F", "G", "Em", "Am"];
const main = song(0.42, [...a, ...a, ...a, ...a, ...b, ...b, ...a, ...a, ...c, ...c], [
  [8, 76, 1], [9, 72, 1], [10, 69, 1], [11, 74, 1], [12, 76, 1], [13, 72, 1], [14, 72, 1], [15, 71, 1],
  [16, 72, 2], [18, 69, 2], [20, 76, 2], [22, 74, 2],
  [24, 76, 1], [25, 72, 1], [26, 69, 1], [27, 74, 1], [28, 72, 2], [30, 71, 2],
]);
const good = song(0.36, [...a, "C", "G", "Am", "F", "C", "F", "G", "C"], [[0, 76, 2], [2, 79, 2], [4, 76, 2], [6, 72, 2], [8, 74, 2], [10, 72, 2]]);
const dry = song(0.6, ["Am", "F", "Dm", "Am", "Am", "F", "Em", "Am"], [[2, 69, 2], [4, 72, 2], [6, 69, 2]], -12);

await fs.mkdir(OUT, { recursive: true });
await save("music_main", main);
await save("music_good", good);
await save("music_dry", dry);
await save("click", notes(0.06, 660));
await save("drop", notes(0.1, 330, 247));
await save("ding", notes(0.3, 880, 1175));
await save("bell", notes(0.35, 1047, 784));
await save("success", notes(0.16, 523, 659, 784, 1047));
await save("error", notes(0.16, 196, 165));
await save("star", notes(0.12, 1319));
console.log("tamam");
