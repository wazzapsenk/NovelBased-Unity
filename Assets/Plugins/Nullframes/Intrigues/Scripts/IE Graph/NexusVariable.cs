using System;

namespace Nullframes.Intrigues
{
    [Serializable]
    public sealed class NexusVariable
    {
        public string id;
        public string name;
        public NType type;

        public NexusVariable(string id, string name, NType type = NType.String)
        {
            this.id = id ?? NullUtils.GenerateID();
            this.name = name;
            this.type = type;
        }
    }
}