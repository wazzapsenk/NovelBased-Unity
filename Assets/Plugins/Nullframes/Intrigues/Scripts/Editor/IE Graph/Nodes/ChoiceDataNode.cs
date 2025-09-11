using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ChoiceDataNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Choice_Data_Node";

        public string Text;
        public string Text2;
        public string ChanceID;

        protected override void OnOutputInit() { }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-dark-red-node");
            topContainer.style.backgroundColor = NullUtils.HTMLColor("#373131");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Choice Data");
            titleLabel.AddClasses("ide-node__label");
            
            titleContainer.Insert(0, titleLabel);

            var textField = IEGraphUtility.CreateTextArea(Text);

            textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Text == textField.value) return;
                Text = textField.value;
                GraphSaveUtility.SaveCurrent();
            });

            textField.AddClasses("uis-choice-data-text");
            
            var textField2 = IEGraphUtility.CreateTextArea(Text2);

            textField2.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Text2 == textField2.value) return;
                Text2 = textField2.value;
                GraphSaveUtility.SaveCurrent();
            });

            textField2.AddClasses("uis-choice-data-text2");

            topContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            topContainer.Insert(1, textField);
            topContainer.Insert(2, textField2);

            var inputPort =
                this.CreatePort("In", typeof(bool), Orientation.Vertical, Direction.Input,
                    Port.Capacity.Multi);
            inputPort.style.flexGrow = 1;
            inputPort.pickingMode = PickingMode.Ignore;
            inputPort.style.justifyContent = new StyleEnum<Justify>(Justify.FlexEnd);
            
            var displayPort =
                this.CreatePort("Display", typeof(bool), Orientation.Vertical, Direction.Input);
            displayPort.pickingMode = PickingMode.Ignore;
            displayPort.style.justifyContent = new StyleEnum<Justify>(Justify.FlexEnd);
            
            titleContainer.Insert(1, inputPort);
            titleContainer.Insert(2, displayPort);
            
            RefreshExpandedState();
        }
    }
}