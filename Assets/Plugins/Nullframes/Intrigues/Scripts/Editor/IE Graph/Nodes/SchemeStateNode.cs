using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SchemeStateNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Scheme_State_Node";

        public int State;
        
        protected override void OnOutputInit()
        {
            AddOutput("Is");
            AddOutput("Is Not");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-green-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Scheme State"
            };

            titleLabel.AddClasses("ide-node__label");
            
            var states = IEGraphUtility.CreateDropdown(new [] { "Is Ended", "None", "Success", "Failed" });
            states.index = State;
            states.style.minWidth = 120f;

            var dropdownChild = states.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            states.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                State = states.index;
                GraphSaveUtility.SaveCurrent();
            });
            states.style.marginLeft = 0f;
            states.style.marginBottom = 1f;
            states.style.marginTop = 1f;
            states.style.marginRight = 3f;
            states.AddClasses("ide-node__set-role-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, states);

            var input = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(input);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}