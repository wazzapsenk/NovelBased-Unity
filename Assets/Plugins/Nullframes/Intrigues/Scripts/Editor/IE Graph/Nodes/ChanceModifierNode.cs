using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ChanceModifierNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Chance_Modifier_Node";

        public string VariableID;
        public bool Positive;
        public bool Negative;
        public bool Opposite;
        public int Mode;

        private Toggle positiveToggle;
        private Toggle negativeToggle;

        private bool defaultMode;

        protected override void OnOutputInit()
        {
            if (Outputs.Count > 0) return;
            AddOutput("Conspirator");
            AddOutput("Target");
            AddOutput("Global");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-purple-node");
            extensionContainer.style.backgroundColor = NullUtils.HTMLColor("#373748");

            outputContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            outputContainer.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Chance Modifier"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            var modifierDict = GraphWindow.CurrentDatabase.variablePool.Where(v => v is NFloat)
                .Select(a => new { a.id, a.name }).ToDictionary(d => d.id, d => d.name);

            var igroup = (SchemeGroup)Group;
            foreach (var nvar in igroup.Variables.Where(n => n.type == NType.Float))
            {
                modifierDict.Add(nvar.id, nvar.name);
            }
            
            defaultMode = !igroup.Variables.Exists(v => v.id == VariableID);

            var modifierList = IEGraphUtility.CreateDropdown(null);
            modifierList.style.minWidth = 120f;

            modifierList.choices = new List<string>(modifierDict.Values);

            modifierList.choices.Insert(0, "NULL");

            var index = modifierDict.Keys.ToList().IndexOf(VariableID);
            modifierList.index = index == -1 ? 0 : index + 1;

            if (modifierList.index == 0) VariableID = string.Empty;

            var dropdownChild = modifierList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            modifierList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (modifierList.index < 1)
                {
                    VariableID = string.Empty;
                    
                    DisconnectAllPorts();
                    Outputs.Clear();
                    defaultMode = true;
                    
                    OnOutputInit();
                    
                    ReloadPorts();
                    
                    SetDirty();

                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                VariableID = modifierDict.Keys.ElementAt(modifierList.index - 1);

                var oldMode = defaultMode;
                
                defaultMode = !igroup.Variables.Exists(v => v.id == VariableID);

                if (oldMode != defaultMode)
                {
                    DisconnectAllPorts();
                    Outputs.Clear();

                    if (defaultMode)
                    {
                        OnOutputInit();
                    }
                    else
                    {
                        AddOutput("Out");
                    }
                    
                    ReloadPorts();
                }
                
                SetDirty();

                GraphSaveUtility.SaveCurrent();
            });
            modifierList.style.marginLeft = 0f;
            modifierList.style.marginBottom = 1f;
            modifierList.style.marginTop = 1f;
            modifierList.style.marginRight = 3f;
            modifierList.AddClasses("uis-chance-value-dropdown");

            var modifierType = IEGraphUtility.CreateDropdown(null);

            modifierType.choices = new List<string>() { "None", "Always Positive", "Always Negative", "Opposite" };
            modifierType.index = Positive ? 1 : Negative ? 2 : Opposite ? 3 : 0;

            var typeDropdownChild = modifierType.GetChild<VisualElement>();
            typeDropdownChild.SetPadding(5);
            typeDropdownChild.style.paddingLeft = 10;
            typeDropdownChild.style.paddingRight = 10;
            typeDropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            modifierType.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                switch (modifierType.index)
                {
                    case < 1:
                        Positive = false;
                        Opposite = false;
                        Negative = false;
                        break;
                    case 1:
                        Negative = false;
                        Opposite = false;
                        
                        Positive = true;
                        break;
                    case 2:
                        Positive = false;
                        Opposite = false;
                        
                        Negative = true;
                        break;
                    case 3:
                        Positive = false;
                        Negative = false;
                        
                        Opposite = true;
                        break;
                }

                GraphSaveUtility.SaveCurrent();
            });
            modifierType.style.marginLeft = 0f;
            modifierType.style.marginBottom = 1f;
            modifierType.style.marginTop = 1f;
            modifierType.style.marginRight = 3f;
            modifierType.AddClasses("uis-chance-value-dropdown");
            
            var modeDropdown = IEGraphUtility.CreateDropdown(null);

            modeDropdown.choices = new List<string>() { "Direct", "Percentage" };
            modeDropdown.index = Mode == 0 ? 0 : 1;

            var modeDropdownChild = modeDropdown.GetChild<VisualElement>();
            modeDropdownChild.SetPadding(5);
            modeDropdownChild.style.paddingLeft = 10;
            modeDropdownChild.style.paddingRight = 10;
            modeDropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            modeDropdown.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Mode = modeDropdown.index switch
                {
                    0 => 0,
                    1 => 1,
                    _ => Mode
                };

                GraphSaveUtility.SaveCurrent();
            });
            modeDropdown.style.marginLeft = 0f;
            modeDropdown.style.marginBottom = 1f;
            modeDropdown.style.marginTop = 1f;
            modeDropdown.style.marginRight = 3f;
            modeDropdown.AddClasses("uis-chance-value-dropdown");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, modifierList);
            titleContainer.Insert(2, modifierType);
            titleContainer.Insert(3, modeDropdown);

            ReloadPorts();

            RefreshExpandedState();
        }

        private void ReloadPorts()
        {
            outputContainer.Clear();
            
            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, typeof(double), Orientation.Vertical, Direction.Output,
                    Port.Capacity.Multi);
                cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
                cPort.portColor = STATIC.ChanceModifier;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }
        }
    }
}