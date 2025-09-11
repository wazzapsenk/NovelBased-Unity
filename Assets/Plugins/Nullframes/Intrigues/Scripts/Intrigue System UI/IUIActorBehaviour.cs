using UnityEngine;

namespace Nullframes.Intrigues.UI {
    public abstract class IUIActorBehaviour : MonoBehaviour {
        public Actor Actor { get; private set; }
        public Clan Clan { get; private set; }
        public Scheme Scheme { get; private set; }
        public Actor Conspirator { get; private set; }
        public Actor Target { get; private set; }

        public virtual void OnMenuOpened(Actor actor) { }
        public virtual void OnPageUpdated() { }
        public virtual void OnFamilyMemberLoaded(Actor actor, Transform Transform) { }
        public virtual void OnAvailableSchemeLoaded(RuleResult result, Scheme scheme, Transform Transform) { }
        public virtual void OnPolicyLoaded(Policy policy, Transform Transform) { }

        public virtual void OnSchemeButtonTriggered(Transform Transform) {
            StartScheme();
        }

        protected void StartScheme() {
            if (Conspirator == null) return;
            Conspirator.StartScheme(Scheme, Target);
        }

        public void Init(Actor actor) {
            Actor = actor;
        }

        public void Init(Clan clan) {
            Clan = clan;
        }

        public void Init(Actor conspirator, Actor target, Scheme scheme) {
            Conspirator = conspirator;
            Target = target;
            Scheme = scheme;
        }
    }
}