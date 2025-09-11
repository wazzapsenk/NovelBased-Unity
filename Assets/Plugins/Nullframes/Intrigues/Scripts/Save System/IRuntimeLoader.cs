using UnityEngine;

namespace Nullframes.Intrigues.SaveSystem
{
    [DefaultExecutionOrder(-350)]
    public abstract class IRuntimeLoader : MonoBehaviour
    {
        private static RuntimeActorLoader current;
        
        private void Awake()
        {
            if (current != null)
            {
                Destroy(gameObject);
                return;
            }

            current = (RuntimeActorLoader)this;
            Load();
        }

        public static void Load() {
            if (current == null || IntrigueSaveSystem.Instance == null) return;
            IntrigueSaveSystem.Instance.LoadRuntimeActors();
        }
    }
}