using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class AddSpouseNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Marry_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;
        
        public bool JoinSpouseFamily;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-royal-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Marry"
            };

            titleLabel.AddClasses("ide-node__label");

            var toggle = IEGraphUtility.CreateToggle("Join Secondary Actor's Family");
            toggle.tooltip =
                "If enabled, the primary actor joins the family of the secondary actor.";
            toggle.style.alignSelf = new StyleEnum<Align>(Align.Center);
            toggle.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
            toggle.value = JoinSpouseFamily;

            toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                JoinSpouseFamily = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            var lbl = toggle.GetChild<Label>();
            lbl.style.minWidth = 110;
            lbl.style.fontSize = 12;
            lbl.style.marginLeft = 8;

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, toggle);

            var dualPort = this.CreatePort("[Dual]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            dualPort.tooltip = "Marriage occurs between the Primary Actor and the Secondary Actor. The Secondary Actor joins the family of the Primary Actor.";
            dualPort.portColor = STATIC.GreenPort;
            dualPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(dualPort);

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