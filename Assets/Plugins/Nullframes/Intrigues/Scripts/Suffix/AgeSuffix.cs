using UnityEngine;

namespace Nullframes.Intrigues
{
    [System.Serializable]
    public class AgeSuffix
    {
        [field: SerializeField] public string Suffix { get; set; }
        [field: SerializeField] public int MinValue { get; set; }
        [field: SerializeField] public int MaxValue { get; set; }

        public AgeSuffix(string suffix, int minValue, int maxValue)
        {
            Suffix = suffix;
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}