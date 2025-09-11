using System;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class WaitRandomNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Wait_Random_Node";

        public float Min = 1f;
        public float Max = 2f;

        private FloatField minDelay;
        private FloatField maxDelay;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-blue-node");
            mainContainer.style.backgroundColor = NullUtils.HTMLColor("#28314D");
            titleContainer.style.height = new StyleLength(StyleKeyword.Auto);
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Wait(sc)",
                style =
                {
                    minWidth = new StyleLength(StyleKeyword.None),
                    maxWidth = new StyleLength(StyleKeyword.None),
                    flexGrow = 1,
                    flexShrink = 1
                }
            };
            
            var breakInput =
                this.CreatePort("[STOP]", typeof(bool), Orientation.Vertical, Direction.Input,
                    Port.Capacity.Multi);
            breakInput.portColor = STATIC.Chance;

            var minmaxField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    backgroundColor = NullUtils.HTMLColor("#2B303F")
                }
            };

            var minField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    flexGrow = 1,
                    flexShrink = 0,
                }
            };
            
            var maxField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    flexGrow = 1,
                    flexShrink = 0,
                    marginRight = 9
                }
            };

            var minLabel = new Label()
            {
                text = "Min: ",
            };
            
            var maxLabel = new Label()
            {
                text = "Max: ",
            };

            var separator = new VisualElement()
            {
                name = "divider"
            };
            
            var separatorVert = new VisualElement()
            {
                name = "divider"
            };
            
            separator.AddClasses("horizontal");
            separatorVert.AddClasses("vertical");
            
            minDelay = IEGraphUtility.CreateFloatField(Min);

            minDelay.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs(Min - minDelay.value) < double.Epsilon) return;

                if (minDelay.value < .000001)
                {
                    minDelay.value = 1;
                }

                if (minDelay.value > maxDelay.value)
                {
                    maxDelay.value = minDelay.value;
                    Max = maxDelay.value;
                }
                
                Min = minDelay.value;
                GraphSaveUtility.SaveCurrent();
            });

            minDelay.AddClasses("ide-node__float-field-minmax");
            minDelay.SetMargin(0);
            
            maxDelay = IEGraphUtility.CreateFloatField(Max);

            maxDelay.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs(Max - maxDelay.value) < double.Epsilon) return;

                if (maxDelay.value < .000001)
                {
                    maxDelay.value = 1;
                }

                if (maxDelay.value < minDelay.value)
                {
                    minDelay.value = maxDelay.value;
                    Min = minDelay.value;
                }

                Max = maxDelay.value;
                GraphSaveUtility.SaveCurrent();
            });

            maxDelay.AddClasses("ide-node__float-field-minmax");
            maxDelay.SetMargin(0);
            
            titleLabel.AddClasses("ide-node__label");
            minLabel.AddClasses("uis-minmax-label");
            maxLabel.AddClasses("uis-minmax-label");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, breakInput);
            titleContainer.RemoveAt(2);

            minField.Insert(0, minLabel);
            maxField.Insert(0, maxLabel);
            
            mainContainer.Insert(1, separator);
            mainContainer.Insert(2, minmaxField);
            minmaxField.Insert(0, minField);
            minmaxField.Insert(1, separatorVert);
            minmaxField.Insert(2, maxField);
            minField.Insert(1, minDelay);
            maxField.Insert(1, maxDelay);

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