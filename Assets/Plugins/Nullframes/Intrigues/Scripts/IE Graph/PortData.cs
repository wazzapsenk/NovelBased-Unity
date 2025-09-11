using System;

namespace Nullframes.Intrigues.Graph
{
    [Serializable]
    public class PortData
    {
        public string NextID;
        public string NextName;
        public string ActorID;

        public PortData(string nextID, string nextName, string actorID)
        {
            NextID = nextID;
            NextName = nextName;
            ActorID = actorID;
        }
    }
}