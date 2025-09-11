using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SetStateNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Set_State_Node";

        public int State;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-caramel-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Set State"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var state = IEGraphUtility.CreateDropdown(new[] { "Active", "Passive", "Passive - No Inheritance" });
            state.index = State;

            state.style.minWidth = 120f;

            var dropdownChild = state.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            state.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                State = state.index;
                GraphSaveUtility.SaveCurrent();
            });
            state.style.marginLeft = 0f;
            state.style.marginBottom = 1f;
            state.style.marginTop = 1f;
            state.style.marginRight = 3f;
            state.AddClasses("ide-node__set-state-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, state);

            var conspirator = this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(conspirator);
            var target = this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(target);
            var actor = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actor.portColor = STATIC.GreenPort;
            actor.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(actor);
            
            foreach (var schemeVariable in ((SchemeGroup)Group).Variables.Where(v => v.type == NType.Actor)) {
                var port = this.CreatePort($"[{schemeVariable.name}]", typeof(bool), Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi);
                port.userData = schemeVariable.id;
                port.portColor = STATIC.BluePort;
                port.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                inputContainer.Add(port);
            }

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