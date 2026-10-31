using System.Collections.Generic;
using CopAtolyesi.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CopAtolyesi.UI
{
    public sealed class CareMark
    {
        public RectTransform Rect;
        public CanvasGroup Group;
        public TreatmentDef Treatment;
        public float Remaining = 1f;
        public int Progress;
        public readonly List<Image> Segments = new List<Image>();
        public readonly List<float> SegmentPositions = new List<float>();
        public readonly List<Image> Dots = new List<Image>();

        public Vector2 SegmentCenter(int index)
        {
            var area = Rect.rect;
            return Rect.TransformPoint(new Vector3(area.xMin + SegmentPositions[index] * area.width, area.center.y, 0));
        }

        public void Finish()
        {
            Remaining = 0;
            Group.alpha = 0;
        }
    }

    // Leke, cizik gibi izleri esyanin ustune cizer. Yer secimi Seed ile tekrarlanabilir; izler yalnizca
    // cizimin dolu pikselleri uzerinde baslar ve ItemView'in maskesi tasan kismi keser.
    public static class CareMarks
    {
        const float RegionOffset = 0.3f;
        const float MinSpread = 0.12f;
        const float MaxSpread = 0.36f;
        const int PlacementTries = 8;
        const int OpaqueSearchTries = 200;
        const float ScratchHeightRatio = 0.3f;
        const int ScratchSegments = 4;
        const float ScratchThickness = 0.028f;
        const float ScratchZigzag = 20f;
        const float DotSize = 15f;
        const int ScratchPlacementTries = 60;
        const float MinPointGap = 0.1f;

        public static List<CareMark> Build(RectTransform parent, ItemInstance item)
        {
            var marks = new List<CareMark>();
            var random = new System.Random(item.Seed);
            var art = Resources.Load<Sprite>(item.Def.sprite);
            var scratchPoints = new List<Vector2>();
            foreach (var treatment in item.Treatments)
            {
                var blob = treatment.shape != "scratch";
                var edge = treatment.sizeRatio / 2 + (blob ? 0.01f : -0.12f);

                // Her bakim isi urunun rastgele bir bolgesinde toplanir: kimi koseye yigilir, kimi yayilir.
                var region = new Vector2(0.5f, 0.5f) + RandomInDisc(random) * RegionOffset;
                var spread = MinSpread + (float)random.NextDouble() * (MaxSpread - MinSpread);
                var placed = new List<Vector2>();
                for (var i = 0; i < item.MarkCount(treatment); i++)
                {
                    if (!blob)
                    {
                        var (scratchCenter, scratchTilt) = PlaceScratch(random, region, spread, edge, treatment.sizeRatio, scratchPoints, art);
                        marks.Add(Scratch(parent, treatment, scratchCenter, scratchTilt, art, scratchPoints));
                        continue;
                    }
                    var center = PickPosition(random, region, spread, edge, placed, art);
                    placed.Add(center);
                    marks.Add(Blob(parent, treatment, center, random.Next(MarkArt.Variants)));
                }
            }
            return marks;
        }

        // Cizginin parca merkezleri esyanin dolu pikselleri uzerinde ve baska cizgilerin
        // noktalarindan uzak olmali: gorunmeyen veya ust uste binen dikis noktasi kalmaz.
        static (Vector2 center, float tilt) PlaceScratch(System.Random random, Vector2 region, float spread, float edge,
            float length, List<Vector2> taken, Sprite art)
        {
            var best = (center: region, tilt: 0f);
            var bestFit = -1;
            for (var i = 0; i < ScratchPlacementTries && bestFit < ScratchSegments; i++)
            {
                var center = (i < ScratchPlacementTries / 2 ? region : new Vector2(0.5f, 0.5f)) + RandomInDisc(random) * (spread + i * 0.005f);
                center.x = Mathf.Clamp(center.x, edge, 1 - edge);
                center.y = Mathf.Clamp(center.y, edge, 1 - edge);
                var tilt = (float)random.NextDouble() * 180f;

                var fit = 0;
                for (var s = 0; s < ScratchSegments; s++)
                {
                    var point = SegmentPoint(center, tilt, length, s);
                    if (IsOpaque(art, point) && !taken.Exists(p => (p - point).magnitude < MinPointGap)) fit++;
                }
                if (fit <= bestFit) continue;
                bestFit = fit;
                best = (center, tilt);
            }
            return best;
        }

        static Vector2 SegmentPoint(Vector2 center, float tilt, float length, int segment)
        {
            var along = ((segment + 0.5f) / ScratchSegments - 0.5f) * length;
            var radians = tilt * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * along;
        }

        // Bolge icinden birkac aday dener, mevcut izlere en uzak olani secer; dolu piksellerin disina cikmaz.
        static Vector2 PickPosition(System.Random random, Vector2 region, float spread, float edge, List<Vector2> placed, Sprite art)
        {
            var best = region;
            var bestDistance = -1f;
            for (var i = 0; i < PlacementTries; i++)
            {
                var candidate = region + RandomInDisc(random) * spread;
                candidate.x = Mathf.Clamp(candidate.x, edge, 1 - edge);
                candidate.y = Mathf.Clamp(candidate.y, edge, 1 - edge);
                if (!IsOpaque(art, candidate)) continue;
                var distance = float.MaxValue;
                foreach (var other in placed) distance = Mathf.Min(distance, (other - candidate).sqrMagnitude);
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                best = candidate;
            }
            if (bestDistance >= 0) return best;

            for (var i = 0; i < OpaqueSearchTries; i++)
            {
                var candidate = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
                if (IsOpaque(art, candidate)) return candidate;
            }
            return new Vector2(0.5f, 0.5f);
        }

        static bool IsOpaque(Sprite art, Vector2 point)
        {
            if (art == null || !art.texture.isReadable) return true;
            var area = art.rect;
            return art.texture.GetPixel((int)(area.x + point.x * area.width), (int)(area.y + point.y * area.height)).a > 0.5f;
        }

        static Vector2 RandomInDisc(System.Random random)
        {
            var angle = (float)random.NextDouble() * Mathf.PI * 2;
            var length = Mathf.Sqrt((float)random.NextDouble());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
        }

        // Dikis noktalari yalnizca lavabodaki buyuk gorunumde cizilir.
        public static void AddStitchDots(CareMark mark)
        {
            for (var i = 0; i < mark.Segments.Count; i++)
            {
                var anchor = new Vector2(mark.SegmentPositions[i], 0.5f);
                var dot = UiKit.Panel(mark.Rect, "Dot", anchor, anchor, Color.white);
                dot.sprite = UiKit.Circle;
                dot.raycastTarget = false;
                dot.rectTransform.sizeDelta = Vector2.one * DotSize;
                mark.Dots.Add(dot);
            }
        }

        static CareMark Blob(RectTransform parent, TreatmentDef treatment, Vector2 center, int variant)
        {
            var half = Vector2.one * treatment.sizeRatio / 2;
            var image = UiKit.Panel(parent, "Mark", center - half, center + half, GameData.ParseColor(treatment.color));
            image.sprite = MarkArt.Get(treatment.shape, variant);
            image.raycastTarget = false;
            return Wrap(image.rectTransform, treatment);
        }

        // Ince, kirik bir cizgi: yan yana egik parcalardan olusur. Esyanin disina dusen parca hic olusmaz,
        // yoksa maske onu gizler ve oyuncu gormedigi bir isi bitirmek zorunda kalir.
        static CareMark Scratch(RectTransform parent, TreatmentDef treatment, Vector2 center, float tilt, Sprite art, List<Vector2> taken)
        {
            var half = new Vector2(treatment.sizeRatio, treatment.sizeRatio * ScratchHeightRatio) / 2;
            var rect = UiKit.Area(parent, "Mark", center - half, center + half);
            rect.localEulerAngles = new Vector3(0, 0, tilt);
            var mark = Wrap(rect, treatment);

            var color = GameData.ParseColor(treatment.color);
            for (var i = 0; i < ScratchSegments; i++)
            {
                var point = SegmentPoint(center, tilt, treatment.sizeRatio, i);
                if (!IsOpaque(art, point) && (i < ScratchSegments - 1 || mark.Segments.Count > 0)) continue;
                taken.Add(point);

                var segment = UiKit.Panel(rect, "Segment",
                    new Vector2(i / (float)ScratchSegments - 0.02f, 0.5f - ScratchThickness),
                    new Vector2((i + 1f) / ScratchSegments + 0.02f, 0.5f + ScratchThickness), color);
                segment.raycastTarget = false;
                segment.rectTransform.localEulerAngles = new Vector3(0, 0, i % 2 == 0 ? ScratchZigzag : -ScratchZigzag);
                mark.Segments.Add(segment);
                mark.SegmentPositions.Add((i + 0.5f) / ScratchSegments);
            }
            return mark;
        }

        static CareMark Wrap(RectTransform rect, TreatmentDef treatment)
        {
            var group = rect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            return new CareMark { Rect = rect, Group = group, Treatment = treatment };
        }
    }
}
