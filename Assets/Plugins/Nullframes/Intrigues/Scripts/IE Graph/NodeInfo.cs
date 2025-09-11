using System.Collections.Generic;

namespace Nullframes.Intrigues.Graph
{
    public class NodeInfo
    {
        public readonly string id;
        public NodeData node;
        public string inputName;
        public Actor actor;
        public (Actor, Actor) dualActor;
        public Clan clan;
        public Family family;
        public float time;
        public int index;
        public bool bgWorker;
        public NodeInfo sequencer;
        public NodeInfo repeater;
        public NodeInfo validator;
        public int repeatCount;
        public List<string> delays;
        public List<int> indexes;
        
        public Schemer.SchemerFactory SchemerFactory;
        public Ruler.RulerFactory RulerFactory;

        public NodeInfo(string id = null)
        {
            this.id = id ?? NullUtils.GenerateID();
            delays = new List<string>();
            indexes = new List<int>();
        }
    }
}