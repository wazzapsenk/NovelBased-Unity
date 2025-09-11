using System;

namespace Nullframes.Intrigues {
    [AttributeUsage(AttributeTargets.Method)]
    public class GetDualActorAttribute : Attribute, INamedAttribute {
        public string Name { get; }

        public GetDualActorAttribute(string name = null) {
            Name = name;
        }
    }
}