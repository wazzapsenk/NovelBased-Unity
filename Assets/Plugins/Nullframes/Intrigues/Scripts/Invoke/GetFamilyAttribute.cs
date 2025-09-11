using System;

namespace Nullframes.Intrigues {
    [AttributeUsage(AttributeTargets.Method)]
    public class GetFamilyAttribute : Attribute, INamedAttribute {
        public string Name { get; }

        public GetFamilyAttribute(string name = null) {
            Name = name;
        }
    }
}