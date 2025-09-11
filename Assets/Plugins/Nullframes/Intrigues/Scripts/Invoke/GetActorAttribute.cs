using System;

namespace Nullframes.Intrigues {
    [AttributeUsage(AttributeTargets.Method)]
    public class GetActorAttribute : Attribute, INamedAttribute {
        public string Name { get; }

        public GetActorAttribute(string name = null) {
            Name = name;
        }
    }
}