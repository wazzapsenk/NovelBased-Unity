using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RemovePolicyNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Remove_Policy_Node";

        public string PolicyID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-byzantine-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Remove Policy"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var policyDict = GraphWindow.CurrentDatabase.policyCatalog.Select(a => new { a.ID, a.PolicyName }).ToDictionary(d => d.ID, d => d.PolicyName);
            var policyList = IEGraphUtility.CreateDropdown(null);
            policyList.style.minWidth = 120f;

            policyList.choices = new List<string>(policyDict.Values);

            policyList.choices.Insert(0, "NULL");

            var index = policyDict.Keys.ToList().IndexOf(PolicyID);
            policyList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = policyList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            policyList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (policyList.index < 1)
                {
                    PolicyID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                PolicyID = policyDict.Keys.ElementAt(policyList.index - 1);

                GraphSaveUtility.SaveCurrent();
            });
            policyList.style.marginLeft = 0f;
            policyList.style.marginBottom = 1f;
            policyList.style.marginTop = 1f;
            policyList.style.marginRight = 3f;
            policyList.AddClasses("ide-node__remove-policy-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, policyList);

            var conspirator = this.CreatePort("Conspirator Clan", typeof(bool), Orientation.Horizontal,
                Direction.Input, Port.Capacity.Multi);
            inputContainer.Add(conspirator);
            var target = this.CreatePort("Target Clan", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(target);
            var clan = this.CreatePort("[Clan]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            clan.portColor = STATIC.GreenPort;
            clan.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputContainer.Add(clan);

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