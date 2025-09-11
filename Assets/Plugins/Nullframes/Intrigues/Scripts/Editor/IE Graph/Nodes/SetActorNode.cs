using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SetActorNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Set_Actor_Node";

        public string VariableID;
        
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
                text = "Set Actor"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var actorVariables = IEGraphUtility.CreateDropdown(null);
            actorVariables.style.minWidth = 120f;
            
            Dictionary<string, string> variables;

            void LoadVariables()
            {
                variables = ((SchemeGroup)Group).Variables.Where(v => v.type == NType.Actor).Select(s => new { s.id, s.name }).ToDictionary(d => d.id, d => d.name);
                
                actorVariables.choices = new List<string>(variables.Values);
                
                actorVariables.choices.Insert(0, "NULL");
                
                var index = variables.Keys.ToList().IndexOf(VariableID);
                actorVariables.index = index == -1 ? 0 : index + 1;
            }

            LoadVariables();

            actorVariables.RegisterCallback<MouseDownEvent>(_ => { LoadVariables(); });

            var dropdownChild = actorVariables.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            actorVariables.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (actorVariables.index <= 0)
                {
                    VariableID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                VariableID = variables.ElementAt(actorVariables.index - 1).Key;
                GraphSaveUtility.SaveCurrent();
            });
            actorVariables.style.marginLeft = 0f;
            actorVariables.style.marginBottom = 1f;
            actorVariables.style.marginTop = 1f;
            actorVariables.style.marginRight = 3f;
            actorVariables.AddClasses("ide-node__get-factor-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, actorVariables);

            var inputPort = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.GreenPort;
            inputPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(inputPort);

            foreach (var outputData in Outputs) {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.BluePort;

                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}