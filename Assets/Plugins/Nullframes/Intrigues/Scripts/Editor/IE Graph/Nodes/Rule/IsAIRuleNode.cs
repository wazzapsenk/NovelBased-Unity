using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class IsAIRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Is_AI_Node";

        private TextField methodField;

        public override GenericNodeType GenericType => GenericNodeType.Rule;
        
        protected override void OnOutputInit()
        {
            AddOutput("True");
            AddOutput("False");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-eggnog-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = IEGraphUtility.CreateLabel("Is AI");
            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            var conspirator =
                this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(conspirator);
            conspirator.tooltip = "If the conspirator is an AI.";

            var target =
                this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                    Port.Capacity.Multi);
            inputContainer.Add(target);
            target.tooltip = "If the target is an AI.";
            
            var actor = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actor.tooltip = "If the actor is an AI.";
            actor.portColor = STATIC.GreenPort;
            actor.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(actor);

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