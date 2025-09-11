using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GrandparentCountNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Grandparent_Count_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        public bool IncludePassiveCharacters;
        public int Count;
        
        protected override void OnOutputInit()
        {
            AddOutput("Equal");
            AddOutput("Not Equal");
            AddOutput("Greater Than");
            AddOutput("Less Than");
            AddOutput("Greater Than Or Equal");
            AddOutput("Less Than Or Equal");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-lake-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Grandparent Count"
            };

            var toggle = IEGraphUtility.CreateToggle("Passive characters included");
            toggle.tooltip =
                "If active, it includes passive (dead) characters.";
            toggle.style.alignSelf = new StyleEnum<Align>(Align.Center);
            toggle.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
            toggle.value = IncludePassiveCharacters;

            toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                IncludePassiveCharacters = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            var lbl = toggle.GetChild<Label>();
            lbl.style.minWidth = 110;
            lbl.style.fontSize = 14;
            lbl.style.marginLeft = 8;
            contentContainer.Add(toggle);

            var countField = IEGraphUtility.CreateIntField(Count);

            countField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Count == countField.value) return;
                if (countField.value < 0) countField.value = 0;

                Count = countField.value;
                GraphSaveUtility.SaveCurrent();
            });

            countField.AddClasses("uis-number-check");
            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, countField);

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