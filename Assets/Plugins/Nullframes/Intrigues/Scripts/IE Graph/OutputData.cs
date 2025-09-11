using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nullframes.Intrigues.Graph
{
    [Serializable]
    public class OutputData
    {
        public string Name = string.Empty;
        public Sprite Sprite;
        public bool Disabled;
        public ValidatorMode ValidatorMode = ValidatorMode.Passive;
        public bool Primary;
        public bool HideIfDisable;
        public List<PortData> DataCollection = new();
        public ChoiceData ChoiceData;
    }
}

namespace Nullframes.Intrigues {
    public class ChoiceData
    {
        public string Text1 { get; set; }
        public string Text2 { get; set; }
        public Rate Rate { get; set; }
    }

    public class Rate
    {
        public float SuccessRate { get; set; }
        public float FailRate { get; set; }
    }
}