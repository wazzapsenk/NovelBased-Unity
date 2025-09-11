using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SchemeIsActiveRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Scheme_Is_Active_Node";

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        public string SchemeID;
        public Actor.VerifyType VerifyType = Actor.VerifyType.ToTarget;
        
        protected override void OnOutputInit()
        {
            AddOutput("Active");
            AddOutput("Passive");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-green-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Scheme Is Active"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var schemeDict = GraphWindow.CurrentDatabase.schemeLibrary
                .Select(a => new {a.ID, a.SchemeName}).ToDictionary(d => d.ID, d => d.SchemeName);
            var schemeList = IEGraphUtility.CreateDropdown(null);
            schemeList.style.minWidth = 120f;

            schemeList.choices = new List<string>(schemeDict.Values);

            schemeList.choices.Insert(0, "None");

            var index = schemeDict.Keys.ToList().IndexOf(SchemeID);
            schemeList.index = index == -1 ? 0 : index + 1;

            if (schemeList.index == 0) SchemeID = string.Empty;
            
            var dropdownChild = schemeList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            schemeList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (schemeList.index < 1)
                {
                    SchemeID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                SchemeID = schemeDict.Keys.ElementAt(schemeList.index - 1);
                GraphSaveUtility.SaveCurrent();
            });
            schemeList.style.marginLeft = 0f;
            schemeList.style.marginBottom = 1f;
            schemeList.style.marginTop = 1f;
            schemeList.style.marginRight = 3f;
            schemeList.AddClasses("ide-node__set-role-dropdown-field");

            var enumField = new EnumFlagsField(VerifyType);
            enumField.AddClasses("ide-node__set-role-dropdown-field");
            
            enumField.RegisterCallback<ChangeEvent<Enum>>(e =>
            {
                VerifyType = (Actor.VerifyType)e.newValue;
                GraphSaveUtility.SaveCurrent();
            });
            
            dropdownChild = enumField.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, schemeList);
            titleContainer.Insert(2, enumField);

            var conspirator = this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            conspirator.tooltip = "Checks the schemes related to the Conspirator. Target: Current Target Actor defined in Scheme.";
            inputContainer.Add(conspirator);
            
            var target = this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            target.tooltip = "Checks the schemes related to the Target. Target: Current Conspirator Actor defined in Scheme.";
            inputContainer.Add(target);
            
            var dualPort = this.CreatePort("[Dual]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            dualPort.tooltip = "Checks the schemes related to the Primary Actor. Target: Secondary Actor.";
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