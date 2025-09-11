using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RepeaterNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Repeater_Node";

        private IntegerField repeatField;

        public int RepetitionCount = 1;
        
        protected override void OnOutputInit()
        {
            AddOutput("Run");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-purple-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Repeater");
            titleLabel.AddClasses("ide-node__label");

            repeatField = IEGraphUtility.CreateIntField(RepetitionCount);

            tooltip = "The Repeater node repeats the flow once it's completed(!).\n(-1 = Infinite Repeat)";

            repeatField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (RepetitionCount == repeatField.value) return;
                if (repeatField.value < 1)
                {
                    if(repeatField.value != -1)
                        repeatField.value = 1;
                }
                RepetitionCount = repeatField.value;
                GraphSaveUtility.SaveCurrent();
            });

            repeatField.AddClasses("ius-repeater-int-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, repeatField);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(inputPort);
            
            foreach (var output in Outputs)
            {
                var cPort = this.CreatePort(output.Name, Port.Capacity.Single);
                cPort.portColor = STATIC.YellowPort;
                cPort.userData = output;
                outputContainer.Add(cPort);
            }
            
            RefreshExpandedState();
        }
    }
}