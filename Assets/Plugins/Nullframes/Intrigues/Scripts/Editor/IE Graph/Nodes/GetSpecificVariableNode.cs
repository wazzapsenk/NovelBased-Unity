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
    public class GetSpecificVariableNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Get_Specific_Variable";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;
        private VisualElement outputBox;

        private DropdownField variableField;
        private DropdownField enumTypeField;

        private Label valueLabel;
        private Label errorLabel;

        private TextField stringField;
        private FloatField floatField;
        private IntegerField integerField;


        public string VariableID;
        public string StringValue;
        public float FloatValue;
        public int IntegerValue;
        public NType Type;
        public EnumType EnumType;

        protected override void OnOutputInit() { }

        public override void Draw()
        {
            base.Draw();
            RemoveAt(0);

            root = new VisualElement
            {
                name = "root"
            };

            rowGroup = new VisualElement
            {
                name = "boxGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    paddingTop = 5f,
                    paddingBottom = 5f
                }
            };

            stageBox = new VisualElement
            {
                name = "variableField",
                style =
                {
                    minWidth = 350f,
                    maxWidth = 500f,
                    minHeight = 200f
                }
            };

            var content = new VisualElement()
            {
                name = "Content",
                style =
                {
                    height = new StyleLength(Length.Percent(100)),
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                }
            };
            content.SetPadding(10f);

            var inputBox = new VisualElement()
            {
                name = "input",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    minWidth = 90f,
                    minHeight = 90f,
                    borderRightColor = NullUtils.HTMLColor("#4D4D4D"),
                    borderRightWidth = 1f
                }
            };
            inputBox.SetPadding(5);

            var dualPort = this.CreatePort("[Dual]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            dualPort.portColor = STATIC.GreenPort;
            dualPort.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputBox.Add(dualPort);

            outputBox = new VisualElement()
            {
                name = "output",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    minWidth = 90f,
                    minHeight = 90f,
                    borderLeftColor = NullUtils.HTMLColor("#4D4D4D"),
                    borderLeftWidth = 1f
                }
            };
            outputBox.SetPadding(5);

            stageBox.SetPadding(15f);

            #region Title

            var titleLabel = IEGraphUtility.CreateLabel("<color=#CCC377>\u25CF</color> Get Specific Variable");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

            #endregion

            #region VARIABLES

            variableField = IEGraphUtility.CreateDropdown(null);
            Dictionary<string, string> variables;

            void loadVariables()
            {
                variables = GraphWindow.CurrentDatabase.variablePool.Select(s => new { s.id, s.name })
                    .ToDictionary(d => d.id, d => d.name);

                variableField.choices = new List<string>(variables.Values);

                variableField.choices.Insert(0, "NULL");

                var index = variables.Keys.ToList().IndexOf(VariableID);
                variableField.index = index == -1 ? 0 : index + 1;
            }

            loadVariables();

            variableField.RegisterCallback<MouseDownEvent>(_ => { loadVariables(); });

            var dropdownChild = variableField.GetChild<VisualElement>();
            dropdownChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));

            variableField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                ClearPorts();
                if (variableField.index < 1)
                {
                    VariableID = string.Empty;
                    LoadVariable(VariableID);
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                VariableID = variables.Keys.ElementAt(variableField.index - 1);
                LoadVariable(VariableID);
                GraphSaveUtility.SaveCurrent();
            });
            variableField.style.marginLeft = 0f;
            variableField.style.marginBottom = 1f;
            variableField.style.marginTop = 1f;
            variableField.style.marginRight = 3f;
            variableField.AddClasses("ide-node__story-dropdown-field");

            content.Add(variableField);

            #endregion

            #region ENUMTYPE

            enumTypeField = IEGraphUtility.CreateDropdown(new []{ "Is", "Is Not" });

            enumTypeField.index = (int)EnumType;

            dropdownChild = enumTypeField.GetChild<VisualElement>();
            dropdownChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));

            enumTypeField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                EnumType = (EnumType)enumTypeField.index;
                GraphSaveUtility.SaveCurrent();
            });
            enumTypeField.style.marginLeft = 0f;
            enumTypeField.style.marginBottom = 1f;
            enumTypeField.style.marginTop = 1f;
            enumTypeField.style.marginRight = 3f;
            enumTypeField.AddClasses("ide-node__story-dropdown-field");

            //Work in progress..
            // content.Add(enumTypeField);

            #endregion

            #region LABEL

            valueLabel = new Label("String Value")
            {
                name = "textLabel",
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    position = new StyleEnum<Position>(Position.Absolute),
                    width = new StyleLength(Length.Percent(100)),
                    height = new StyleLength(Length.Percent(100)),
                    fontSize = 18f,
                    unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                    color = NullUtils.HTMLColor("#676767"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None)
                },
                pickingMode = PickingMode.Ignore
            };

            #endregion

            #region STRING

            stringField = IEGraphUtility.CreateTextArea(StringValue);

            stringField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (string.IsNullOrEmpty(stringField.text))
                {
                    valueLabel.Show();
                    return;
                }

                valueLabel.Hide();
            });

            stringField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrWhiteSpace(stringField.value))
                {
                    stringField.value = StringValue;
                    return;
                }

                if (StringValue == stringField.value) return;

                StringValue = stringField.value;
                GraphSaveUtility.SaveCurrent();
            });
            stringField.AddClasses("uis-variable-text-field");
            stringField.Insert(0, valueLabel);

            content.Insert(0, stringField);

            #endregion

            #region INTEGER

            integerField = IEGraphUtility.CreateIntField(IntegerValue);

            integerField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (IntegerValue == integerField.value) return;

                IntegerValue = integerField.value;
                GraphSaveUtility.SaveCurrent();
            });
            integerField.AddClasses("integer__field-variable");

            content.Insert(0, integerField);

            #endregion

            #region FLOAT

            floatField = IEGraphUtility.CreateFloatField(FloatValue);

            floatField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs(FloatValue - floatField.value) < double.Epsilon) return;

                FloatValue = floatField.value;
                GraphSaveUtility.SaveCurrent();
            });
            floatField.AddClasses("float__field-scheme-time");

            content.Insert(0, floatField);

            #endregion

            errorLabel = new Label()
            {
                style =
                {
                    unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter),
                    fontSize = 14f,
                    marginTop = 10f,
                    unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold),
                    color = NullUtils.HTMLColor("#BE6B6B"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None)
                }
            };

            content.Add(errorLabel);

            LoadVariable(VariableID);

            rowGroup.Insert(0, inputBox);
            rowGroup.Insert(1, stageBox);
            rowGroup.Insert(2, outputBox);
            stageBox.Add(content);

            root.AddClasses("get-variable-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            RefreshExpandedState();
        }

        private void ShowError(string message)
        {
            errorLabel.Show();
            errorLabel.text = message;
        }

        private void LoadVariable(string variableId)
        {
            var variable = GraphWindow.CurrentDatabase.variablePool.FirstOrDefault(e => e.id == variableId);

            if (variable == null)
            {
                stringField.Hide();
                floatField.Hide();
                integerField.Hide();
                enumTypeField.Hide();
                outputBox.Hide();
                if (!string.IsNullOrEmpty(VariableID))
                    ShowError("Variable is missing..");
                return;
            }

            variableField.Enable();
            errorLabel.Hide();
            outputBox.Show();

            Type = variable.Type;

            if (Type == NType.String) valueLabel.text = "String Value";

            if (string.IsNullOrEmpty(StringValue)) valueLabel.Show();

            CreatePorts(variable);

            if (variable.Type is NType.String)
            {
                stringField.Show();
                if (string.IsNullOrEmpty(StringValue)) valueLabel.Show();
            }
            else
            {
                stringField.Hide();
                valueLabel.Hide();
            }

            if (variable.Type is NType.Float)
                floatField.Show();
            else
                floatField.Hide();

            if (variable.Type is NType.Integer)
                integerField.Show();
            else
                integerField.Hide();
            
            if(variable.Type is NType.Enum)
                enumTypeField.Show();
            else
                enumTypeField.Hide();
        }

        private void ClearPorts()
        {
            var ports = new List<Port>();
            outputBox.GetChilds<Port>((p) => { ports.Add(p); });

            foreach (var p in ports)
            {
                if (p.connected) graphView.DeleteElements(p.connections);

                graphView.RemoveElement(p);
            }

            Outputs.Clear();
        }

        private void CreatePorts(NVar variable)
        {
            bool saveisNecessary = false;
            if (Outputs.Count == 0)
            {
                if (Type == NType.String)
                {
                    var equals = new OutputData()
                    {
                        Name = "Equals"
                    };
                    var notEquals = new OutputData()
                    {
                        Name = "Not Equals"
                    };
                    Outputs.Add(equals);
                    Outputs.Add(notEquals);
                }

                if (Type is NType.Integer or NType.Float)
                {
                    var equal = new OutputData()
                    {
                        Name = "Equal"
                    };
                    Outputs.Add(equal);

                    var notequal = new OutputData()
                    {
                        Name = "Not Equal"
                    };
                    Outputs.Add(notequal);

                    var greater = new OutputData()
                    {
                        Name = "Greater Than"
                    };
                    Outputs.Add(greater);

                    var less = new OutputData()
                    {
                        Name = "Less Than"
                    };
                    Outputs.Add(less);

                    var greaterorequal = new OutputData()
                    {
                        Name = "Greater Than Or Equal"
                    };
                    Outputs.Add(greaterorequal);

                    var lessorequal = new OutputData()
                    {
                        Name = "Less Than Or Equal"
                    };
                    Outputs.Add(lessorequal);
                }

                if (Type is NType.Bool)
                {
                    var tr = new OutputData()
                    {
                        Name = "True"
                    };
                    Outputs.Add(tr);

                    var fl = new OutputData()
                    {
                        Name = "False"
                    };
                    Outputs.Add(fl);
                }

                if (Type is NType.Enum)
                    foreach (var enumValue in ((NEnum)variable).Values)
                    {
                        var enumData = new OutputData()
                        {
                            Name = enumValue
                        };
                        Outputs.Add(enumData);
                    }
            }

            if (Type is NType.Enum)
            {
                var enums = ((NEnum)variable).Values.ToList();
                
                for (int i = 0; i < enums.Count; i++)
                {
                    var outputData = Outputs.ElementAtOrDefault(i);

                    if (outputData == null)
                    {
                        var enumData = new OutputData()
                        {
                            Name = enums[i]
                        };
                        Outputs.Add(enumData);

                        saveisNecessary = true;
                        continue;
                    }

                    if (!outputData.Name.Equals(enums[i]))
                    {
                        var oD = Outputs.FirstOrDefault(o => o.Name == enums[i]);
                        saveisNecessary = true;
                        if (oD != null)
                        {
                            Outputs.Remove(oD);
                            Outputs.Insert(i, oD);
                            continue;
                        }
                        outputData.Name = enums[i];
                    }

                    if (Outputs.Count(o => o.Name == enums[i]) > 1)
                    {
                        var firstItem = Outputs.First(o => o.Name == enums[i]);

                        Outputs.RemoveAll(o => o != firstItem && o.Name.Equals(enums[i]));
                        
                        saveisNecessary = true;
                    }
                }

                if (Outputs.RemoveAll(o => !enums.Contains(o.Name)) > 0)
                {
                    saveisNecessary = true;
                }
            }

            LoadPorts();
            
            if (saveisNecessary)
            {
                SetDirty();
            }
        }

        private void LoadPorts()
        {
            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
                cPort.portColor = STATIC.RandomColor;
                cPort.userData = outputData;
                outputBox.Add(cPort);
            }
        }
    }
}