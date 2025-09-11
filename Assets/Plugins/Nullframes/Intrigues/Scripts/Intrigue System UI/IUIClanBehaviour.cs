using UnityEngine;

namespace Nullframes.Intrigues.UI {
    public abstract class IUIClanBehaviour : MonoBehaviour {
        public Clan Clan { get; private set; }

        public virtual void OnMenuOpened(Clan clan) { }
        public virtual void OnPageUpdated() { }
        public virtual void OnClanMemberLoaded(Actor actor, Transform Transform) { }
        public virtual void OnPolicyLoaded(Policy policy, Transform Transform) { }

        public void Init(Clan clan) {
            Clan = clan;
        }
    }
}