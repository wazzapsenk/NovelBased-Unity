using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Is_Compatible_Node";

        public string RuleID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Success");
            AddOutput("Failed");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-jean-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Is Compatible"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var rules = GraphWindow.CurrentDatabase.groupDataList.OfType<RuleGroupData>().Select(s => new { s.ID, s.Title }).ToDictionary(d => d.ID, d => d.Title);
            var ruleList = IEGraphUtility.CreateDropdown(null);
            ruleList.style.minWidth = 120f;

            ruleList.choices = new List<string>(rules.Values);

            ruleList.choices.Insert(0, "NULL");

            var index = rules.Keys.ToList().IndexOf(RuleID);
            ruleList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = ruleList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            ruleList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (ruleList.index < 1)
                {
                    RuleID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                RuleID = rules.Keys.ElementAt(ruleList.index - 1);
                GraphSaveUtility.SaveCurrent();
            });
            ruleList.style.marginLeft = 0f;
            ruleList.style.marginBottom = 1f;
            ruleList.style.marginTop = 1f;
            ruleList.style.marginRight = 3f;
            ruleList.AddClasses("uis-rule-dropdown");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, ruleList);

            var conspirator = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(conspirator);

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