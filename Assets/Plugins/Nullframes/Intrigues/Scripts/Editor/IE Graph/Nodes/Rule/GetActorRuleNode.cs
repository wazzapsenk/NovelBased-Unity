using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GetActorRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Actor_Node";

        public override GenericNodeType GenericType => GenericNodeType.Rule;
        
        public string ActorID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Is");
            AddOutput("Is Not");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-dark-brown");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Check Actor"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var i = 1;
            var actors = GraphWindow.CurrentDatabase.actorRegistry.OrderBy(a => a.Name)
                .Select(a => new { a.ID, a.Name, a.Age }).ToDictionary(t => t.ID, t => $"[{i++}]: {t.Name}({t.Age})");
            var actorList = IEGraphUtility.CreateDropdown(null);
            actorList.style.minWidth = 120f;

            actorList.choices = new List<string>(actors.Values);

            actorList.choices.Insert(0, "NULL");

            var index = actors.Keys.ToList().IndexOf(ActorID);
            actorList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = actorList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            actorList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (actorList.index < 1)
                {
                    ActorID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                ActorID = actors.ElementAt(actorList.index - 1).Key;
                GraphSaveUtility.SaveCurrent();
            });
            actorList.style.marginLeft = 0f;
            actorList.style.marginBottom = 1f;
            actorList.style.marginTop = 1f;
            actorList.style.marginRight = 3f;
            actorList.AddClasses("ide-node__actor-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, actorList);

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