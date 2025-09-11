using UnityEngine;

namespace Nullframes.Intrigues
{
    [DefaultExecutionOrder(-370)]
    public abstract class IRuntime : MonoBehaviour
    {
        private void Awake()
        {
            IM.onRuntimeActorCreated += OnRuntimeActorCreated;
        }

        private void OnDestroy()
        {
            IM.onRuntimeActorCreated -= OnRuntimeActorCreated;
        }

        protected abstract void OnRuntimeActorCreated(Actor runtimeActor, GameObject actorGameObject);
    }
}