using System;

namespace Nullframes.Intrigues
{
    [AttributeUsage(AttributeTargets.Method)]
    public class IInvokeAttribute : Attribute
    {
        public readonly string Name;

        public IInvokeAttribute(string name = null)
        {
            Name = name;
        }
    }
}