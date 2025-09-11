using UnityEngine;

namespace Nullframes.Intrigues.Demo
{
    public class OnRuntimeActorCreated_Demo : IRuntime
    {

        [Note("With this sample script, we access the <b>OnRuntimeActorCreated</b> method.\nThe <b>OnRuntimeActorCreated</b> method is called only when a <b>RuntimeActor</b> is created.\n\nIn this example, we add the <b>DemoCharacter</b> component, which is found in every actor, to the <b>GameObject</b> where the <b>RuntimeActor</b> is attached.")]
        public bool ok;
        
        protected override void OnRuntimeActorCreated(Actor runtimeActor, GameObject actorGameObject)
        {
            actorGameObject.AddComponent<DemoCharacter>();
        }
    }
}