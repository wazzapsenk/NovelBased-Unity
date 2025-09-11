using UnityEngine;

namespace Nullframes.Intrigues.Demo
{
    public class Example : MonoBehaviour {
        
        public NActor actor = new NActor("actorr", "a307ee03-6e7b-425f-83ec-d95d7238976d");
        
        [IInvoke("Test Invoke")]
        private bool Test(Scheme scheme)
        {
            Debug.Log($"Conspirator: {scheme.Schemer.Conspirator.Name}");
            Debug.Log($"Target: {scheme.Schemer.Target.Name}");
            return true;
        }

        [GetActor("Random Actor")]
        private Actor GetActor(Scheme scheme)
        {
            int random = Random.Range(0, 2); // 0 or 1

            if (random == 0) return scheme.Schemer.Conspirator;
            if (random == 1) return scheme.Schemer.Target;

            return null;
        }
    }
}