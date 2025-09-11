using UnityEngine;

namespace Nullframes.Intrigues.UI {
    public abstract class IUISchemeBehaviour : MonoBehaviour {
        public Actor Actor { get; private set; }

        public virtual void OnMenuOpened(Actor actor) { }
        public virtual void OnPageUpdated() { }
        public virtual void OnActiveSchemeLoaded(Scheme scheme, Transform Transform) { }

        public void Init(Actor actor) {
            Actor = actor;
        }
    }
}