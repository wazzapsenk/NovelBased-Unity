using System.Collections;
using UnityEngine;

namespace Nullframes.Intrigues.Utils
{
    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager instance;

        private static CoroutineManager Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("Coroutine")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                instance = go.AddComponent<CoroutineManager>();
                if (Application.isPlaying)
                    DontDestroyOnLoad(go);
                return instance;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public static Coroutine StartRoutine(IEnumerator coroutine)
        {
            return coroutine == null ? null : Instance.StartCoroutine(coroutine);
        }


        public static void StopRoutine(Coroutine coroutine)
        {
            if (coroutine == null) return;
            Instance.StopCoroutine(coroutine);
        }
    }
}