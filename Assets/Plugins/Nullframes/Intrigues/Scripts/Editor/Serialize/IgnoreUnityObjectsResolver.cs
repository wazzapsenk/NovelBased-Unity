using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Serialization;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class IgnoreUnityObjectsResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member,
            MemberSerialization memberSerialization)
        {
            var prop = base.CreateProperty(member, memberSerialization);

            if (typeof(UnityEngine.Object).IsAssignableFrom(prop.PropertyType))
            {
                prop.ShouldSerialize = _ => false;
            }

            return prop;
        }
    }
}