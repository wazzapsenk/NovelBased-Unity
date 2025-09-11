using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GetCultureNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Culture_Node";

        public string CultureID;
        
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
                text = "Check Culture"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var cultureDict = GraphWindow.CurrentDatabase.culturalProfiles.Select(a => new { a.ID, a.CultureName }).ToDictionary(d => d.ID, d => d.CultureName);
            var cultureList = IEGraphUtility.CreateDropdown(null);
            cultureList.style.minWidth = 120f;

            cultureList.choices = new List<string>(cultureDict.Values);

            cultureList.choices.Insert(0, "NULL");

            var index = cultureDict.Keys.ToList().IndexOf(CultureID);
            cultureList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = cultureList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            cultureList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (cultureList.index < 1)
                {
                    CultureID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                CultureID = cultureDict.Keys.ElementAt(cultureList.index - 1);

                GraphSaveUtility.SaveCurrent();
            });
            cultureList.style.marginLeft = 0f;
            cultureList.style.marginBottom = 1f;
            cultureList.style.marginTop = 1f;
            cultureList.style.marginRight = 3f;
            cultureList.AddClasses("ide-node__culture-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, cultureList);

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