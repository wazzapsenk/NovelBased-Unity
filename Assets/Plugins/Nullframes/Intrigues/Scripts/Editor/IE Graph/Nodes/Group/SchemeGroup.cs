using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SchemeGroup : IGroup
    {
        public string Description;
        public string RuleID;
        public Sprite Icon;
        public bool HideIfNotCompatible;
        public bool TargetNotRequired;
        public bool HideOnUI;
        public List<NexusVariable> Variables;
        
        public void Init(string groupName, Vector2 position, string description, string ruleId, List<NexusVariable> variables,
            bool hideIfNotCompatible, bool targetNotRequired, bool hideOnUI, Sprite icon, IEGraphView graphView)
        {
            SetPosition(new Rect(position, Vector2.zero));
            
            ID = GUID.Generate().ToString();
            title = groupName;
            OldTitle = title;
            
            Description = description;
            HideIfNotCompatible = hideIfNotCompatible;
            TargetNotRequired = targetNotRequired;
            HideOnUI = hideOnUI;
            RuleID = ruleId;
            Icon = icon;
            Variables = new List<NexusVariable>();
            foreach (var v in variables) Variables.Add(new NexusVariable(v.id, v.name, v.type));

            _graphView = graphView;

            contentContainer.RegisterCallback<MouseEnterEvent>(_ => { selectedGroup = this; });

            contentContainer.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                EditorRoutine.StartRoutine(.05f, () => selectedGroup = null);
            });
        }

        public void Draw()
        {
            #region Broke

            contentContainer.SetBorderRadius(0f);
            contentContainer.SetBorderWidth(2f);
            style.backgroundColor = new Color(0.1132075f, 0.0945176f, 0.1041736f, 0.1411765f);
            //style.backgroundColor = Color.clear;
            var _titleContainer = this.Q<VisualElement>("titleContainer");
            var _titleLabel = this.Q<Label>("titleLabel");
            var titleTextField = this.Q<TextField>("titleField");

            //titleContainer
            _titleContainer.SetPadding(60);
            _titleContainer.style.paddingTop = 30f;
            _titleContainer.style.backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.8843137f);
            _titleContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            //titleLabel
            _titleLabel.style.color = NullUtils.HTMLColor("#B28700");
            _titleLabel.style.fontSize = 32f;
            _titleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);

            //titleField
            titleTextField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                title = OldTitle;
                titleTextField.value = OldTitle;
                _titleLabel.text = OldTitle;
            });

            #endregion

            #region BORDERLINE

            borderLine = new VisualElement
            {
                style =
                {
                    minHeight = 50,
                    backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.8843137f),
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };
            borderLine.SetPadding(60);
            Insert(1, borderLine);

            #endregion

            #region TITLE

            var titleLabel = IEGraphUtility.CreateLabel("Settings<size=14> | Table");
            titleLabel.AddClasses("ide-node__label__large");
            titleLabel.style.fontSize = 28f;

            #endregion
            
            #region ICON

            var titleIcon = new VisualElement
            {
                style =
                {
                    width = 64,
                    height = 64,
                    marginLeft = 110f,
                    backgroundImage = new StyleBackground(Icon)
                }
            };
            
            #endregion

            #region SETTINGSPANEL

            var mainContainer = new VisualElement();
            borderLine.Add(mainContainer);
            mainContainer.AddClasses("ide-table-field");

            var titleContainer = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };
            titleContainer.AddClasses("ide-table-title");
            titleContainer.Add(titleLabel);
            titleContainer.Add(titleIcon);

            var topContainer = new VisualElement();
            topContainer.AddClasses("ide-table-top");

            var hideToggle = IEGraphUtility.CreateToggle("Hide If Not Compatible(On UI)");
            hideToggle.value = HideIfNotCompatible;

            hideToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                HideIfNotCompatible = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            var lbl = hideToggle.GetChild<Label>();
            lbl.style.minWidth = 220;
            lbl.style.fontSize = 14;
            topContainer.Add(hideToggle);
            
            var hideOnUI = IEGraphUtility.CreateToggle("Hide On UI");
            hideOnUI.value = HideOnUI;

            hideOnUI.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                HideOnUI = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            lbl = hideOnUI.GetChild<Label>();
            lbl.style.minWidth = 220;
            lbl.style.fontSize = 14;
            topContainer.Add(hideOnUI);
            
            var targetNotRequired = IEGraphUtility.CreateToggle("Target Actor Not Required");
            targetNotRequired.value = TargetNotRequired;

            targetNotRequired.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                TargetNotRequired = evt.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            lbl = targetNotRequired.GetChild<Label>();
            lbl.style.minWidth = 220;
            lbl.style.fontSize = 14;
            topContainer.Add(targetNotRequired);

            #region NAME

            var schemeNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center),
                    marginLeft = 3f,
                    marginTop = 3f
                }
            };

            var schemeNameLabel = IEGraphUtility.CreateLabel("Name");
            schemeNameLabel.style.fontSize = 14;
            schemeNameLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            schemeNameLabel.style.paddingRight = 10f;
            schemeNameLabel.style.minWidth = 100f;
            schemeNameLabel.style.maxWidth = 100f;

            //TextField
            var schemeName = IEGraphUtility.CreateTextField(title);
            schemeName.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                schemeName.value = schemeName.value.RemoveSpecialCharacters();
            });
            schemeName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(schemeName.value))
                {
                    schemeName.value = OldTitle;
                    return;
                }

                var lst = (from SchemeGroup element in _graphView.graphElements.Where(e => e is SchemeGroup)
                    select element.title).ToList();
                var exists = lst.Contains(schemeName.value);
                if (exists)
                {
                    schemeName.value = OldTitle;
                    return;
                }

                title = schemeName.value;
                OldTitle = title;
                titleTextField.value = title;
                _titleLabel.text = title;
                GraphSaveUtility.SaveCurrent();
            });
            schemeName.AddClasses("ide-node__text-field-family");

            schemeNameField.Add(schemeNameLabel);
            schemeNameField.Add(schemeName);
            topContainer.Add(schemeNameField);

            #endregion
            
            #region DESC

            var nexusDescField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center),
                    marginLeft = 3f,
                    marginTop = 3f
                }
            };

            var nexusDescLabel = IEGraphUtility.CreateLabel("Description");
            nexusDescLabel.style.fontSize = 14;
            nexusDescLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            nexusDescLabel.style.paddingRight = 10f;
            nexusDescLabel.style.minWidth = 100f;
            nexusDescLabel.style.maxWidth = 100f;

            //TextField
            var nexusDescription = IEGraphUtility.CreateTextArea(Description);
            nexusDescription.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(nexusDescription.value))
                {
                    nexusDescription.value = Description;
                    return;
                }

                Description = nexusDescription.value;
                GraphSaveUtility.SaveCurrent();
            });
            nexusDescription.AddClasses("ide-node__text-field-family");

            nexusDescField.Add(nexusDescLabel);
            nexusDescField.Add(nexusDescription);
            topContainer.Add(nexusDescField);

            #endregion
            
            #region ICON

            var nexusIconField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center),
                    marginLeft = 3f,
                    marginTop = 3f
                }
            };

            var nexusIconLabel = IEGraphUtility.CreateLabel("Icon");
            nexusIconLabel.style.fontSize = 14;
            nexusIconLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            nexusIconLabel.style.paddingRight = 10f;
            nexusIconLabel.style.minWidth = 100f;
            nexusIconLabel.style.maxWidth = 100f;

            var nexusIcon = IEGraphUtility.CreateObjectField(typeof(Sprite));
            nexusIcon.value = Icon;
            nexusIcon.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Icon = (Sprite)nexusIcon.value;
                titleIcon.style.backgroundImage = new StyleBackground(Icon);
                GraphSaveUtility.SaveCurrent();
            });
            nexusIcon.AddClasses("scheme-object-field");

            nexusIconField.Add(nexusIconLabel);
            nexusIconField.Add(nexusIcon);
            topContainer.Add(nexusIconField);

            #endregion

            #region RULE

            var ruleField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center),
                    marginLeft = 3f,
                    marginTop = 3f
                }
            };

            var ruleLabel = IEGraphUtility.CreateLabel("Rule");
            ruleLabel.style.fontSize = 14;
            ruleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            ruleLabel.style.paddingRight = 10f;
            ruleLabel.style.minWidth = 100f;
            ruleLabel.style.maxWidth = 100f;

            var rules = GraphWindow.CurrentDatabase.groupDataList.OfType<RuleGroupData>().Select(s => new { s.ID, s.Title }).ToDictionary(d => d.ID, d => d.Title);
            var ruleList = IEGraphUtility.CreateDropdown(null);

            ruleList.choices = new List<string>(rules.Values);
            ruleList.choices.Insert(0, "Rule: NULL");
            var index = rules.Keys.ToList().IndexOf(RuleID);
            ruleList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = ruleList.GetChild<VisualElement>();
            dropdownChild.style.backgroundColor = NullUtils.HTMLColor("#3A3A3A");
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            var textElement = dropdownChild.GetChild<TextElement>();
            textElement.style.color = new StyleColor(NullUtils.HTMLColor("#FFF5BA"));
            textElement.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

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
            ruleList.AddClasses("ide-node__story-dropdown-field");

            ruleField.Add(ruleLabel);
            ruleField.Add(ruleList);
            topContainer.Add(ruleField);

            #endregion

            contentField = new VisualElement();
            contentField.AddClasses("ide-table-content");

            //ScrollView
            scrollView = new ScrollView()
            {
                mode = ScrollViewMode.Vertical
            };
            scrollView.SetPadding(6);
            var dragger = scrollView.Q<VisualElement>("unity-dragger");
            dragger.style.backgroundColor = new StyleColor(NullUtils.HTMLColor("#262626"));
            dragger.SetBorderColor("#525252");

            var bottomContainer = new VisualElement();
            bottomContainer.AddClasses("ide-table-bottom");

            var btnParent = new VisualElement();
            btnParent.SetPadding(4f);

            var createBtn = IEGraphUtility.CreateButton("Create Variable", ClickCreateButton);
            createBtn.AddClasses("ide-button__create-variable");
            btnParent.Add(createBtn);

            bottomContainer.Add(createBtn);
            contentField.Add(scrollView);

            mainContainer.Insert(0, titleContainer);
            mainContainer.Insert(1, topContainer);
            mainContainer.Insert(2, contentField);
            mainContainer.Insert(3, bottomContainer);

            #endregion
            
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                // evt.menu.InsertAction(0, "Import JSON", _ =>
                // {
                //     
                // });
                // evt.menu.InsertAction(1, "Export JSON", _ =>
                // {
                //     var filteredNodes = GraphWindow.CurrentDatabase.Nodes.Where(n => n.GroupId == ID).ToList();
                //     
                //     var json = JsonConvert.SerializeObject(filteredNodes, Formatting.Indented,
                //         new JsonSerializerSettings
                //         {
                //             TypeNameHandling = TypeNameHandling.Auto,
                //             PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                //             ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                //             NullValueHandling = NullValueHandling.Ignore,
                //             ContractResolver = new IgnoreUnityObjectsResolver()
                //         });
                //     
                //     // string json = JsonUtility.ToJson(new NodeListWrapper { nodes = filteredNodes }, true);
                //     
                //     string path = Path.Combine(Application.dataPath, title + ".json");
                //     
                //     File.WriteAllText(path, json);
                //     
                //     AssetDatabase.Refresh();
                // });
                // evt.menu.InsertAction(2, "Auto Layout", _ =>
                // {
                //     // float horizontalSpacing = 100f;
                //     // float verticalSpacing = 200f;
                //     //
                //     // var start = _graphView.nodes.OfType<StartNode>().First();
                //     //
                //     // start.TraverseChildNode<INode>(c =>
                //     // {
                //     //     var parentPos = c.GetPosition().position;
                //     //     var parentWidth = c.GetPosition().width;
                //     //
                //     //     for (int i = 0; i < c.children.Count; i++)
                //     //     {
                //     //         var child = c.children.ElementAt(i);
                //     //
                //     //         Vector2 childPos = new Vector2(
                //     //             parentPos.x + parentWidth + horizontalSpacing,
                //     //             parentPos.y + i * verticalSpacing
                //     //         );
                //     //
                //     //         child.SetPosition(new Rect(childPos, child.GetPosition().size));
                //     //     }
                //     // });
                // });
            }));

            RefreshPanel();
            LoadVariables();
        }

        private void RefreshPanel()
        {
            if (Variables.Count > 0)
                contentField.Show();
            else
                contentField.Hide();
        }

        private void LoadVariables()
        {
            foreach (var variable in Variables) CreateVariable(variable);
        }

        private void ClickCreateButton()
        {
            CreateVariable();
        }

        private void CreateVariable(NexusVariable variable = null)
        {
            var variableRow = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            if (variable == null)
            {
                variable = new NexusVariable(null, GenerateName());
                Variables.Add(variable);
                GraphSaveUtility.SaveCurrent();
            }

            //TextField
            var textField = IEGraphUtility.CreateTextField(variable.name);
            textField.AddClasses("ide-node__text-field-variable");

            textField.RegisterCallback<ChangeEvent<string>>(e =>
            {
                var niceText = e.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
                textField.value = niceText;
            });

            //DropDown
            var dropdown = IEGraphUtility.CreateDropdown(new[] { "String", "Integer", "Bool", "Float", "", "", "Actor", "Clan", "Family", "Dual" });
            dropdown.index = (int)variable.type;

            var dropdownChild = dropdown.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdown.style.minWidth = 100f;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            dropdown.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                variable.type = dropdown.index switch
                {
                    0 => NType.String,
                    1 => NType.Integer,
                    2 => NType.Bool,
                    3 => NType.Float,
                    4 => NType.Object,
                    5 => NType.Enum,
                    6 => NType.Actor,
                    7 => NType.Clan,
                    _ => NType.Family,
                };
                GraphSaveUtility.SaveCurrent();
                if (dropdown.index == 6) {
                    GraphSaveUtility.LoadCurrent(_graphView);
                }
            });
            
            textField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(textField.value) || Variables.Exists(e => e.name == textField.value))
                {
                    textField.value = variable.name;
                    return;
                }

                variable.name = textField.value;
                GraphSaveUtility.SaveCurrent();
                if (dropdown.index == 6) {
                    GraphSaveUtility.LoadCurrent(_graphView);
                }
            });

            dropdown.AddClasses("ide-node__variable-dropdown-field");

            //Button
            var delBtn = IEGraphUtility.CreateButton("X", () =>
            {
                if (variable.type == NType.Actor) {
                    if (!EditorUtility.DisplayDialog("Are you sure?",
                            "If you remove this actor variable; the input ports of this actor in the nodes will be removed.",
                            "Yes",
                            "Nope")) return;
                }
                Variables.Remove(variable);
                scrollView.Remove(variableRow);
                RefreshPanel();
                GraphSaveUtility.SaveCurrent();
                if(variable.type == NType.Actor)
                    GraphSaveUtility.LoadCurrent(_graphView);
            });
            delBtn.AddClasses("ide-variable__button-delete");

            variableRow.Insert(0, textField);
            variableRow.Insert(1, dropdown);
            variableRow.Insert(2, delBtn);

            scrollView.Add(variableRow);

            RefreshPanel();
        }

        private string GenerateName()
        {
            const string variableName = "Variable";
            var i = 1;
            while (Variables.Exists(e => e.name == $"{variableName}{i}")) i++;

            return $"{variableName}{i}";
        }
    }
}