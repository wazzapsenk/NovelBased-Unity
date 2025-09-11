using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GetFamilyNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Family_Node";

        public string FamilyID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Is");
            AddOutput("Is Not");
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
                text = "Check Family"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var familyDict = GraphWindow.CurrentDatabase.groupDataList.OfType<FamilyGroupData>()
                .Select(a => new {a.ID, a.Title}).ToDictionary(d => d.ID, d => d.Title);
            var familyList = IEGraphUtility.CreateDropdown(null);
            familyList.style.minWidth = 120f;

            familyList.choices = new List<string>(familyDict.Values);

            familyList.choices.Insert(0, "NULL");

            var index = familyDict.Keys.ToList().IndexOf(FamilyID);
            familyList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = familyList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            familyList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (familyList.index < 1)
                {
                    FamilyID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                FamilyID = familyDict.Keys.ElementAt(familyList.index - 1);

                GraphSaveUtility.SaveCurrent();
            });
            familyList.style.marginLeft = 0f;
            familyList.style.marginBottom = 1f;
            familyList.style.marginTop = 1f;
            familyList.style.marginRight = 3f;
            familyList.AddClasses("ide-node__family-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, familyList);

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