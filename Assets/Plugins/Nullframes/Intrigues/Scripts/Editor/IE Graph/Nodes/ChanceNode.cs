using System;
using System.Globalization;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ChanceNode : INode
    {
        private FloatField chanceField;
        private Label titleLabel;

        public float Chance;
        
        protected override void OnOutputInit()
        {
            if (Outputs.Count > 0) return;
            AddOutput("Success");
            AddOutput("Failed");
            AddOutput("Display");
        }

        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Chance_Node";

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-mauve-node");
        }

        public override void Draw()
        {
            base.Draw();
            
            titleLabel = new Label()
            {
                text = $"Chance(%{Chance})"
            };
            titleLabel.AddClasses("ide-node__label__large");
            
            var modifierInput =
                this.CreatePort("<-", typeof(double), Orientation.Vertical, Direction.Input,
                    Port.Capacity.Multi);
            modifierInput.portColor = STATIC.Chance;
            titleContainer.Insert(0, modifierInput);

            chanceField = IEGraphUtility.CreateFloatField(Chance);

            chanceField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs(Chance - chanceField.value) < double.Epsilon) return;
                Chance = Mathf.Clamp(chanceField.value, 0f, 100f);
                chanceField.value = Chance;
                titleLabel.text = $"Chance(%{Chance})";
                var childs = outputContainer.Children().Where(i => i.GetType() == typeof(Port)).ToList();
                var p1 = (Port)childs[0];
                var p2 = (Port)childs[1];
                p1.portName = $"Success: %{Chance}";
                p2.portName = $"Failed: %{100f - Chance}";
                GraphSaveUtility.SaveCurrent();
            });

            chanceField.AddClasses("ide-node__float-field-chance");
            titleContainer.Insert(1, titleLabel);
            titleContainer.Insert(2, chanceField);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            for (var i = 0; i < Outputs.Count; i++)
            {
                var cPort = this.CreatePort(i switch
                {
                    0 => $"Success: %{Chance.ToString(CultureInfo.CurrentCulture)}",
                    1 => $"Failed: %{(100f - Chance).ToString(CultureInfo.CurrentCulture)}",
                    _ => Outputs[i].Name
                }, typeof(bool), i == 2 ? Orientation.Vertical : Orientation.Horizontal, Direction.Output, Port.Capacity.Multi);

                cPort.portColor = i switch
                {
                    0 => STATIC.SuccessPort,
                    1 => STATIC.FailedPort,
                    _ => STATIC.SoftYellow
                };

                cPort.userData = Outputs[i];
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}