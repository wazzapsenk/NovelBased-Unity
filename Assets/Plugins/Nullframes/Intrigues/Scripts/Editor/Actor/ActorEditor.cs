using UnityEditor;

namespace Nullframes.Intrigues.EDITOR
{
    //Actor
    [CustomEditor(typeof(InitialActor))]
    public class AIActorEditor : EActor { }
    
    //==============================================================================================================================

    //Runtime
    [CustomEditor(typeof(RuntimeActor))]
    public class RuntimeActorEditor : EActor { }
}