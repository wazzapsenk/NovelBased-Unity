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
    public class GetSchemeTableVariableRuleNode : INode
    {
        protected override string DOCUMENTATION => "...";

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;
        private VisualElement outputBox;

        private Label valueLabel;
        private Label errorLabel;

        private TextField stringField;
        private IntegerField integerField;
        private FloatField floatField;


        public string VariableID;
        public string SchemeID;
        public string StringValue;
        public int IntegerValue;
        public float FloatValue;
        public NType Type;

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
                    maxWidth = 350f,
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

            var conspirator = this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputBox.Add(conspirator);

            var targetIn = this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputBox.Add(targetIn);
            
            var actor = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actor.portColor = STATIC.GreenPort;
            actor.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputBox.Add(actor);

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

            var titleLabel = IEGraphUtility.CreateLabel("<color=#CCC377>\u25CF</color> Get Active Scheme Variable<size=12> | Table");
            titleLabel.style.fontSize = 18f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

            #endregion

            #region VARIABLES

            var variableField = IEGraphUtility.CreateDropdown(null);

            Dictionary<string, string> variables = null;

            void LoadVariables()
            {
                var schemeGroupData = GraphWindow.CurrentDatabase.groupDataList.OfType<SchemeGroupData>()
                    .FirstOrDefault(g => g.ID == SchemeID);

                variableField.choices = new List<string>();

                if (schemeGroupData != null)
                {
                    variables = schemeGroupData.Variables.Select(s => new { s.id, s.name })
                        .ToDictionary(d => d.id, d => d.name);

                    variableField.choices = new List<string>(variables.Values);
                }
                else
                {
                    variableField.choices.Insert(0, "NULL");
                    variableField.index = 0;
                    return;
                }

                variableField.choices.Insert(0, "NULL");

                var variableIndex = variables.Keys.ToList().IndexOf(VariableID);
                variableField.index = variableIndex == -1 ? 0 : variableIndex + 1;
            }

            LoadVariables();

            variableField.RegisterCallback<MouseDownEvent>(_ => LoadVariables());

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

            #region SCHEMES

            var schemeField = IEGraphUtility.CreateDropdown(null);
            schemeField.style.marginLeft = 0f;
            schemeField.style.marginBottom = 1f;
            schemeField.style.marginTop = 1f;
            schemeField.style.marginRight = 3f;
            schemeField.AddClasses("ide-node__story-dropdown-field");

            var schemes = GraphWindow.CurrentDatabase.schemeLibrary.Select(s => new { s.ID, s.SchemeName })
                .ToDictionary(d => d.ID, d => d.SchemeName);

            schemeField.choices = new List<string>(schemes.Values);

            schemeField.choices.Insert(0, "NULL");

            var schemeIndex = schemes.Keys.ToList().IndexOf(SchemeID);
            schemeField.index = schemeIndex == -1 ? 0 : schemeIndex + 1;

            dropdownChild = schemeField.GetChild<VisualElement>();
            dropdownChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));

            schemeField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                ClearPorts();

                VariableID = string.Empty;
                LoadVariable(VariableID);
                variableField.SetValueWithoutNotify("NULL");

                if (schemeField.index < 1)
                {
                    SchemeID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                SchemeID = schemes.Keys.ElementAt(schemeField.index - 1);

                LoadVariables();
                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            content.Add(schemeField);
            content.Add(variableField);

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
            var schemeGroupData = GraphWindow.CurrentDatabase.groupDataList.OfType<SchemeGroupData>()
                .FirstOrDefault(g => g.ID == SchemeID);

            if (schemeGroupData == null)
            {
                stringField.Hide();
                integerField.Hide();
                floatField.Hide();
                outputBox.Hide();
                if (!string.IsNullOrEmpty(VariableID))
                    ShowError("Variable is missing..");
                return;
            }
            
            var variable = schemeGroupData.Variables.FirstOrDefault(e => e.id == variableId);

            if (variable == null)
            {
                stringField.Hide();
                integerField.Hide();
                floatField.Hide();
                outputBox.Hide();
                if (!string.IsNullOrEmpty(VariableID))
                    ShowError("Variable is missing..");
                return;
            }

            errorLabel.Hide();
            outputBox.Show();

            Type = variable.type;

            if (Type == NType.String) valueLabel.text = "String Value";

            if (string.IsNullOrEmpty(StringValue)) valueLabel.Show();

            CreatePorts();

            if (variable.type is NType.String)
            {
                stringField.Show();
                if (string.IsNullOrEmpty(StringValue)) valueLabel.Show();
            }
            else
            {
                stringField.Hide();
                valueLabel.Hide();
            }

            if (variable.type is NType.Float)
                floatField.Show();
            else
                floatField.Hide();

            if (variable.type is NType.Integer)
                integerField.Show();
            else
                integerField.Hide();
        }

        private void CreatePorts()
        {
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
            }

            LoadPorts();
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