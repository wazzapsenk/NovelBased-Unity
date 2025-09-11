using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GetRoleRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Role_Node";

        public override GenericNodeType GenericType => GenericNodeType.Rule;
        
        public string RoleID;
        
        protected override void OnOutputInit()
        {
            AddOutput("Is");
            AddOutput("Is Not");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-dark-green-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Check Role"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var roleDict = GraphWindow.CurrentDatabase.nodeDataList.OfType<RoleData>()
                .Select(a => new { a.ID, a.RoleName }).ToDictionary(d => d.ID, d => d.RoleName.LocaliseText());
            var roleList = IEGraphUtility.CreateDropdown(null);
            roleList.style.minWidth = 120f;

            roleList.choices = new List<string>(roleDict.Values);

            roleList.choices.Insert(0, "NULL");

            var index = roleDict.Keys.ToList().IndexOf(RoleID);
            roleList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = roleList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            roleList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (roleList.index < 1)
                {
                    RoleID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                RoleID = roleDict.Keys.ElementAt(roleList.index - 1);

                GraphSaveUtility.SaveCurrent();
            });
            roleList.style.marginLeft = 0f;
            roleList.style.marginBottom = 1f;
            roleList.style.marginTop = 1f;
            roleList.style.marginRight = 3f;
            roleList.AddClasses("ide-node__role-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, roleList);

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