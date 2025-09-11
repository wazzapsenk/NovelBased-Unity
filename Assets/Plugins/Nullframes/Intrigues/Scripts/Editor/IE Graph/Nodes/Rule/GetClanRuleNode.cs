using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GetClanRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Clan_Node";

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public string ClanID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Is");
            AddOutput("Is Not");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-fern-node");
        }

         public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Check Clan"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var clanDict = GraphWindow.CurrentDatabase.groupDataList.OfType<ClanGroupData>()
                .Select(a => new {a.ID, a.Title}).ToDictionary(d => d.ID, d => d.Title.LocaliseText());
            var clanList = IEGraphUtility.CreateDropdown(null);
            clanList.style.minWidth = 120f;

            clanList.choices = new List<string>(clanDict.Values);

            clanList.choices.Insert(0, "NULL");

            var index = clanDict.Keys.ToList().IndexOf(ClanID);
            clanList.index = index == -1 ? 0 : index + 1;

            if (index == -1 && !string.IsNullOrEmpty(ClanID)) titleLabel.text += $" {ClanID} not found.";

            var dropdownChild = clanList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            clanList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (clanList.index < 1)
                {
                    ClanID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                ClanID = clanDict.Keys.ElementAt(clanList.index - 1);

                GraphSaveUtility.SaveCurrent();
            });
            clanList.style.marginLeft = 0f;
            clanList.style.marginBottom = 1f;
            clanList.style.marginTop = 1f;
            clanList.style.marginRight = 3f;
            clanList.AddClasses("ide-node__clan-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, clanList);

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