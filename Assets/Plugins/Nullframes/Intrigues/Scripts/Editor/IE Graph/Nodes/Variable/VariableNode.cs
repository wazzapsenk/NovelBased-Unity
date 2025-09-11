using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class VariableNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Variable_Node";

        public override bool IsGroupable()
        {
            return false;
        }

        public override GenericNodeType GenericType => GenericNodeType.Variable;

        private TextField stringField;
        private DropdownField enumValue;
        private IntegerField integerField;
        private FloatField floatField;
        private DropdownField boolField;
        private ObjectField objectField;
        private TextField enumField;
        private VisualElement enumParentField;
        private Label valueLabel;

        public string VariableName;
        public string Value;
        public NType Type;
        public NVar Variable;
        
        protected override void OnOutputInit() { }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("dark-gray-background");
            extensionContainer.AddClasses("ide-node__actor-extension-container");

            Transparent();

            style.minWidth = new StyleLength(StyleKeyword.Auto);
            titleContainer.style.height = new StyleLength(StyleKeyword.Auto);
            titleContainer.SetPadding(10);
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label
            {
                text = VariableName,
                style =
                {
                    fontSize = 30f,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    maxWidth = 400f
                }
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            #region NAME

            var variableNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var variableNameLabel = IEGraphUtility.CreateLabel("Variable Name");
            variableNameLabel.style.fontSize = 14;
            variableNameLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            variableNameLabel.style.paddingRight = 10f;
            variableNameLabel.style.minWidth = 120f;
            variableNameLabel.style.maxWidth = 120f;

            //TextField
            var variableName = IEGraphUtility.CreateTextField(VariableName);
            variableName.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                variableName.value = variableName.value.RemoveWhitespaces().RemoveSpecialCharacters();
            });
            variableName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(variableName.value))
                {
                    variableName.value = VariableName;
                    return;
                }
                if (VariableName == variableName.value) return;
                var lst = (from VariableNode element in graphView.graphElements.Where(e => e is VariableNode)
                    select element.VariableName).ToList();
                var exists = lst.Contains(variableName.value);
                if (exists)
                {
                    variableName.value = VariableName;
                    return;
                }

                var varDb = GraphWindow.CurrentDatabase.variablePool.Find(v => v.name == VariableName);
                if (varDb != null)
                {
                    Undo.RecordObject(GraphWindow.CurrentDatabase, "IE_VariableName");
                    varDb.name = variableName.value;
                    EditorUtility.SetDirty(GraphWindow.CurrentDatabase);
                }

                VariableName = variableName.value;
                Variable.name = VariableName;
                titleLabel.text = VariableName;
                GraphSaveUtility.SaveCurrent();
            });
            variableName.AddClasses("ide-node__text-field-actor");

            variableNameField.Add(variableNameLabel);
            variableNameField.Add(variableName);

            extensionContainer.Add(variableNameField);

            #endregion

            #region TYPE

            var variableTypeField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var variableTypeLabel = IEGraphUtility.CreateLabel("Type");
            variableTypeLabel.style.fontSize = 14;
            variableTypeLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            variableTypeLabel.style.paddingRight = 10f;
            variableTypeLabel.style.minWidth = 120f;
            variableTypeLabel.style.maxWidth = 120f;

            //Dropdown
            var variableType = IEGraphUtility.CreateDropdown(new[]
                { "String", "Integer", "Bool", "Float", "Object", "Enum" });
            variableType.index = (int)Type;

            var dropdownChild = variableType.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            variableType.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Type = variableType.index switch
                {
                    0 => NType.String,
                    1 => NType.Integer,
                    2 => NType.Bool,
                    3 => NType.Float,
                    4 => NType.Object,
                    5 => NType.Enum,
                    _ => NType.String
                };
                Value = string.Empty;
                Variable = NVar.CreateWithType(VariableName, Type);

                Display();
                GraphSaveUtility.SaveCurrent();
            });

            Variable ??= new NString(VariableName);

            variableType.style.marginLeft = 0f;
            variableType.style.marginBottom = 1f;
            variableType.style.marginTop = 1f;
            variableType.style.marginRight = 3f;
            variableType.AddClasses("ide-node__gender-dropdown-field");

            variableTypeField.Add(variableTypeLabel);
            variableTypeField.Add(variableType);

            extensionContainer.Add(variableTypeField);
            
            #endregion

            #region VALUE

            var valueField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            valueLabel = IEGraphUtility.CreateLabel("Value");
            valueLabel.style.fontSize = 14;
            valueLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            valueLabel.style.paddingRight = 10f;
            valueLabel.style.minWidth = 120f;
            valueLabel.style.maxWidth = 120f;
            valueField.Insert(0, valueLabel);

            //Integer
            integerField = IEGraphUtility.CreateIntField();
            integerField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if ((int)Variable == integerField.value) return;
                Variable.value = integerField.value;
                GraphSaveUtility.SaveCurrent();
            });
            integerField.style.marginLeft = 0f;
            integerField.style.marginBottom = 1f;
            integerField.style.marginTop = 1f;
            integerField.style.marginRight = 3f;
            integerField.AddClasses("ide-node__integer-field-actor-age");

            //Float
            floatField = IEGraphUtility.CreateFloatField();
            floatField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Math.Abs((float)Variable - floatField.value) < double.Epsilon) return;
                Variable.value = floatField.value;
                GraphSaveUtility.SaveCurrent();
            });
            floatField.style.marginLeft = 0f;
            floatField.style.marginBottom = 1f;
            floatField.style.marginTop = 1f;
            floatField.style.marginRight = 3f;
            floatField.AddClasses("ide-node__float-field-float-value");

            //String
            stringField = IEGraphUtility.CreateTextField();
            stringField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if ((string)Variable == stringField.value) return;
                Variable.value = stringField.value;
                GraphSaveUtility.SaveCurrent();
            });
            stringField.AddClasses("ide-node__text-field-actor");

            //Bool
            boolField = IEGraphUtility.CreateDropdown(new[] { "False", "True" });

            var boolFieldChild = boolField.GetChild<VisualElement>();
            boolFieldChild.SetPadding(5);
            boolFieldChild.style.paddingLeft = 10;
            boolFieldChild.style.paddingRight = 10;
            boolFieldChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            boolField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Variable.value = boolField.index switch
                {
                    0 => false,
                    1 => true,
                    _ => false
                };
                GraphSaveUtility.SaveCurrent();
            });
            boolField.style.marginLeft = 0f;
            boolField.style.marginBottom = 1f;
            boolField.style.marginTop = 1f;
            boolField.style.marginRight = 3f;
            boolField.AddClasses("ide-node__gender-dropdown-field");

            //Object
            objectField = IEGraphUtility.CreateObjectField(typeof(Object));
            objectField.allowSceneObjects = false;
            objectField.AddClasses("ide-node__object-field");

            objectField.RegisterValueChangedCallback(e =>
            {
                Variable.value = e.newValue;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            //Enum
            enumField = IEGraphUtility.CreateTextArea(Value);

            enumField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Value == enumField.value) return;
                if (string.IsNullOrEmpty(enumField.value)) enumField.value = "Male,Female";

                Value = enumField.value;
                EnumValueChanged(Value);
                GraphSaveUtility.SaveCurrent();
            });
            enumField.AddClasses("ide-node__text-field-actor");

            enumParentField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var enumParentLabel = IEGraphUtility.CreateLabel("Enum Value");
            enumParentLabel.style.fontSize = 14;
            enumParentLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            enumParentLabel.style.paddingRight = 10f;
            enumParentLabel.style.minWidth = 120f;
            enumParentLabel.style.maxWidth = 120f;
            enumParentField.Insert(0, enumParentLabel);

            //EnumValue
            enumValue = IEGraphUtility.CreateDropdown(null);

            var enumValueChild = enumValue.GetChild<VisualElement>();
            enumValueChild.SetPadding(5);
            enumValueChild.style.paddingLeft = 10;
            enumValueChild.style.paddingRight = 10;
            enumValueChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            enumValue.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Variable.value = enumValue.value.RemoveSpecialCharacters();
                GraphSaveUtility.SaveCurrent();
            });
            enumValue.style.marginLeft = 0f;
            enumValue.style.marginBottom = 1f;
            enumValue.style.marginTop = 1f;
            enumValue.style.marginRight = 3f;
            enumValue.AddClasses("ide-node__gender-dropdown-field");

            valueField.Add(floatField);
            valueField.Add(enumField);
            valueField.Add(integerField);
            valueField.Add(enumValue);
            valueField.Add(objectField);
            valueField.Add(enumField);
            valueField.Add(stringField);
            valueField.Add(boolField);

            enumParentField.Insert(0, enumParentLabel);
            enumParentField.Insert(1, enumValue);

            extensionContainer.Add(valueField);
            extensionContainer.Add(enumParentField);

            #endregion

            Display();

            if (Variable != null) Variable.name = VariableName;

            RefreshExpandedState();
        }

        private void EnumValueChanged(string value)
        {
            var items = value.Split(',');
            var c_items =
                (from item in items
                    where !string.IsNullOrEmpty(item)
                    select item).ToList();
            enumValue.choices = new List<string>(c_items);
            Variable = new NEnum(VariableName, c_items);
            if (enumValue.choices.Count > 0) enumValue.index = 0;
        }

        private void LoadEnumValues()
        {
            var items = Value.Split(',');
            var c_items =
                (from item in items
                    where !string.IsNullOrEmpty(item)
                    select item).ToList();
            enumValue.choices = new List<string>(c_items);
            enumValue.index = (int)Variable;
        }

        private void Display()
        {
            HideAll();
            valueLabel.text = "Base Value";
            switch (Type)
            {
                case NType.String:
                {
                    stringField.SetValueWithoutNotify((string)Variable);
                    stringField.Show();
                    break;
                }
                case NType.Integer:
                {
                    integerField.SetValueWithoutNotify((int)Variable);
                    integerField.Show();
                    break;
                }
                case NType.Float:
                {
                    floatField.SetValueWithoutNotify((float)Variable);
                    floatField.Show();
                    break;
                }
                case NType.Bool:
                {
                    boolField.SetValueWithoutNotify(string.IsNullOrEmpty((string)Variable)
                        ? false.ToString()
                        : (string)Variable);
                    boolField.Show();
                    break;
                }
                case NType.Object:
                {
                    objectField.SetValueWithoutNotify((Object)Variable);
                    objectField.Show();
                    break;
                }
                case NType.Enum:
                {
                    valueLabel.text = "Enums";
                    if (string.IsNullOrEmpty(Value))
                    {
                        Value = "Male,Female";
                        enumField.value = Value;
                        EnumValueChanged(Value);
                    }
                    else
                    {
                        LoadEnumValues();
                    }

                    enumField.Show();
                    enumValue.Show();
                    enumParentField.Show();
                    break;
                }
            }
        }

        private void HideAll()
        {
            enumValue.Hide();
            integerField.Hide();
            floatField.Hide();
            boolField.Hide();
            objectField.Hide();
            enumField.Hide();
            enumParentField.Hide();
            stringField.Hide();
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }

    }
}