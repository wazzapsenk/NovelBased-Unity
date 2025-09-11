using System;

namespace Nullframes.Intrigues {
    [AttributeUsage(AttributeTargets.Method)]
    public class GetClanAttribute : Attribute, INamedAttribute {
        public string Name { get; }

        public GetClanAttribute(string name = null) {
            Name = name;
        }
    }
}