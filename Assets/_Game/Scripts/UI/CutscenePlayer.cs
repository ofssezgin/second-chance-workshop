using System.Collections;
using UnityEngine;

namespace CopAtolyesi.UI
{
    // Ara sahneye konan bilesen. Sahne ya duration kadar surer ya da animasyon Finished'i true yapar.
    public sealed class CutsceneClip : MonoBehaviour
    {
        public float duration = 3f;
        public bool Finished { get; set; }
        public UiContext Context { get; set; }
    }

    // Ara sahneler Resources/Cutscenes/<id> prefablaridir. Prefab yoksa koddaki sahne oynar;
    // o da yoksa sahne sessizce atlanir.
    public sealed class CutscenePlayer : MonoBehaviour
    {
        public bool Playing { get; private set; }

        public IEnumerator Play(string id, UiContext context)
        {
            var prefab = string.IsNullOrEmpty(id) ? null : Resources.Load<GameObject>("Cutscenes/" + id);
            var instance = prefab != null ? Instantiate(prefab, context.Overlay)
                : id == "truck_arrival" ? TruckCutscene.Create(context.Overlay)
                : null;
            if (instance == null) yield break;

            Playing = true;
            var clip = instance.GetComponent<CutsceneClip>();
            if (clip == null) clip = instance.AddComponent<CutsceneClip>();
            clip.Context = context;

            for (var t = 0f; t < clip.duration && !clip.Finished; t += Time.unscaledDeltaTime) yield return null;

            Destroy(instance);
            Playing = false;
        }
    }
}
