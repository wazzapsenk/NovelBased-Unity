using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SetClanVariableNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Set_Clan_Variable_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;
        private VisualElement outputBox;

        private DropdownField variableField;
        private DropdownField dropdownField;
        private DropdownField mathField;

        private Label valueLabel;
        private Label errorLabel;

        private TextField stringField;
        private FloatField floatField;
        private IntegerField integerField;

        private ObjectField objectInput;
        
        public string VariableID;
        public string StringValue;
        public float FloatValue;
        public int IntegerValue;
        public MathOperation Operation;
        public Object ObjectValue;
        public NType VariableType;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

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
                    minWidth = 300f,
                    maxWidth = 500f,
                    minHeight = 200f
                }
            };

            var content = new ScrollView()
            {
                name = "Content",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    maxHeight = 400
                }
            };
            content.SetPadding(10f);
            
            var dragger = content.Q<VisualElement>("unity-dragger");
            dragger.AddClasses("uis-dialogue-dragger");

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
            
            var actor = this.CreatePort("[Clan]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actor.portColor = STATIC.GreenPort;
            actor.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputBox.Add(actor);
            
            foreach (var schemeVariable in ((SchemeGroup)Group).Variables.Where(v => v.type == NType.Actor)) {
                var port = this.CreatePort($"[{schemeVariable.name}]", typeof(bool), Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi);
                port.userData = schemeVariable.id;
                port.portColor = STATIC.BluePort;
                port.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                inputBox.Add(port);
            }

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

            var titleLabel = IEGraphUtility.CreateLabel("<color=#80a35b>\u25CF</color> Set [Clan] Variable");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");
            
            #endregion

            #region VARIABLES

            variableField = IEGraphUtility.CreateDropdown(null);

            Dictionary<string, string> variables;

            void loadVariables()
            {
                variables = GraphWindow.CurrentDatabase.variablePool.Select(s => new { s.id, s.name }).ToDictionary(d => d.id, d => d.name);
                
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
            
            #endregion

            #region DROPDOWN

            dropdownField = IEGraphUtility.CreateDropdown(null);

            var genderFieldChild = dropdownField.GetChild<VisualElement>();
            genderFieldChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            genderFieldChild.SetPadding(5);
            genderFieldChild.style.paddingLeft = 10;
            genderFieldChild.style.paddingRight = 10;
            genderFieldChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));

            dropdownField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                StringValue = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });
            dropdownField.style.marginLeft = 0f;
            dropdownField.style.marginBottom = 1f;
            dropdownField.style.marginTop = 1f;
            dropdownField.style.marginRight = 3f;
            dropdownField.AddClasses("ide-node__story-dropdown-field");


            #endregion

            #region MATH

            mathField = IEGraphUtility.CreateDropdown(new[] { "Set", "Add", "Subtract", "Multiply" });

            var mathIndex = mathField.choices.IndexOf(Operation.ToString());
            mathField.index = mathIndex == -1 ? 0 : mathIndex;

            var mathFieldChild = mathField.GetChild<VisualElement>();
            mathFieldChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            mathFieldChild.SetPadding(5);
            mathFieldChild.style.paddingLeft = 10;
            mathFieldChild.style.paddingRight = 10;
            mathFieldChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));

            mathField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Operation = (MathOperation)mathField.index;
                GraphSaveUtility.SaveCurrent();
            });
            mathField.style.marginLeft = 0f;
            mathField.style.marginBottom = 1f;
            mathField.style.marginTop = 1f;
            mathField.style.marginRight = 3f;
            mathField.AddClasses("ide-node__story-dropdown-field");


            #endregion

            #region OBJECTFIELD

            objectInput = IEGraphUtility.CreateObjectField(typeof(Object));
            objectInput.AddClasses("object-field");

            objectInput.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                ObjectValue = objectInput.value;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });
            

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
            
            stageBox.Insert(0, titleLabel);
            stageBox.Insert(1, content);
            stageBox.Insert(2, variableField);
            
            content.Insert(0, stringField);
            content.Insert(1, integerField);
            content.Insert(2, floatField);
            content.Insert(3, dropdownField);
            content.Insert(4, objectInput);
            content.Insert(5, mathField);
            
            root.AddClasses("set-variable-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);
            
            LoadPorts();

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
                dropdownField.Hide();
                mathField.Hide();
                objectInput.Hide();
                floatField.Hide();
                integerField.Hide();
                outputBox.Hide();
                if(!string.IsNullOrEmpty(VariableID))
                    ShowError("Variable is missing..");

                return;
            }

            variableField.Enable();
            errorLabel.Hide();
            outputBox.Show();

            VariableType = variable.Type;

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

            if (variable.Type is NType.Float or NType.Integer)
                mathField.Show();
            else
                mathField.Hide();

            if (variable.Type is NType.Float)
                floatField.Show();
            else
                floatField.Hide();

            if (variable.Type is NType.Integer)
                integerField.Show();
            else
                integerField.Hide();

            if (variable.Type is NType.Enum)
            {
                dropdownField.choices = new List<string>(((NEnum)variable).Values);
                var index = dropdownField.choices.IndexOf(StringValue);
                dropdownField.index = index == -1 ? 0 : index;
            }

            if (variable.Type == NType.Bool)
            {
                dropdownField.choices = new List<string>() { "True", "False" };
                var index = dropdownField.choices.IndexOf(StringValue);
                dropdownField.index = index == -1 ? 0 : index;
            }

            if (variable.Type is NType.Object) objectInput.objectType = typeof(Object);

            if (variable.Type is NType.Enum or NType.Bool)
                dropdownField.Show();
            else
                dropdownField.Hide();

            if (variable.Type is NType.Object)
                objectInput.Show();
            else
                objectInput.Hide();
        }

        private void LoadPorts()
        {
            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
                cPort.userData = outputData;
                outputBox.Add(cPort);
            }
        }
    }
}