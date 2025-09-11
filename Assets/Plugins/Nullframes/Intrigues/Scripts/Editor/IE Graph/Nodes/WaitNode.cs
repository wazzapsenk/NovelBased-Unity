using System;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class WaitNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Wait_Node";

        public float Delay = 1f;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-blue-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Wait(sc)"
            };

            tooltip = "Waits for the specified time (sc). (-1 infinite)";
            
            var breakInput =
                this.CreatePort("[STOP]", typeof(bool), Orientation.Vertical, Direction.Input,
                    Port.Capacity.Multi);
            breakInput.portColor = STATIC.Chance;

            var delayField = IEGraphUtility.CreateFloatField(Delay);

            delayField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (delayField.value < .000001 && !delayField.value.Equals(-1))
                {
                    delayField.value = 1;
                }
                if (Math.Abs(Delay - delayField.value) < double.Epsilon) return;

                Delay = delayField.value;
                GraphSaveUtility.SaveCurrent();
            });

            delayField.AddClasses("ide-node__float-field-wait");
            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, delayField);
            titleContainer.Insert(2, breakInput);
            
            titleContainer.RemoveAt(3);

            var inputPort = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.BluePort;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}