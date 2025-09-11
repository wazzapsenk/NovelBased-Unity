using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RoleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Role_Node";

        public override bool IsGroupable()
        {
            return false;
        }

        public override GenericNodeType GenericType => GenericNodeType.Clan;

        protected override void OnOutputInit() { }

        public string RoleName;
        public string FilterID;
        public string Description;
        public int RoleSlot = 1;
        public bool Legacy;
        public string TitleForMale;
        public string TitleForFemale;
        public Sprite RoleIcon;
        public int Priority;

        public override void Draw()
        {
            base.Draw();
            RemoveAt(0);

            var root = new VisualElement
            {
                name = "root"
            };

            var rowGroup = new VisualElement
            {
                name = "boxGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    paddingTop = 5f,
                    paddingBottom = 5f
                }
            };

            var mainBox = new VisualElement
            {
                name = "mainBox",
                style =
                {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 200f,
                }
            };

            mainBox.SetPadding(15f);

            var content = new VisualElement()
            {
                name = "Content",
                style =
                {
                    height = new StyleLength(Length.Percent(100)),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            content.SetPadding(10f);

            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#E58D62>\u25CF</color> Role");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            mainBox.Add(titleLabel);

            #endregion

            #region Icon

            var icon = new IMGUIContainer()
            {
                style =
                {
                    minWidth = 32,
                    minHeight = 32,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    marginTop = 10,
                    unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f)
                }
            };
            
            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/action_choice.png");
            icon.style.backgroundImage = new StyleBackground(texture);

            icon.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.InsertAction(0, "Show In Project Window", _ =>
                {
                    if (RoleIcon == null) return;
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(RoleIcon);
                });
            }));

            int currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;

            icon.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                EditorGUIUtility.ShowObjectPicker<Sprite>(RoleIcon, false, "", currentPickerWindow);
            });

            icon.onGUIHandler += () =>
            {
                if (Event.current.commandName == "ObjectSelectorUpdated" &&
                    EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow)
                {
                    var _sprite = EditorGUIUtility.GetObjectPickerObject() as Sprite;

                    if (_sprite != null && _sprite != RoleIcon)
                    {
                        RoleIcon = _sprite;

                        icon.style.backgroundImage = new StyleBackground(_sprite);
                        icon.style.unityBackgroundImageTintColor = Color.white;
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                        return;
                    }

                    if (_sprite == null) {
                        RoleIcon = null;
                        
                        icon.style.backgroundImage = new StyleBackground(texture);
                        icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                    }
                }
            };

            content.Add(icon);

            if (RoleIcon != null)
            {
                icon.style.backgroundImage = new StyleBackground(RoleIcon);
                icon.style.unityBackgroundImageTintColor = Color.white;
            }

            _ = new DragAndDropManipulator(icon, typeof(Texture2D), objects =>
            {
                SetDirty();
                var iconObj = objects[0];
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconObj);
                RoleIcon = sprite;
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.unityBackgroundImageTintColor = Color.white;

                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            #region RoleName

            var roleName = IEGraphUtility.CreateTextField($"Role Name: {RoleName}");
            roleName.AddClasses("ide-node-role-title-text");
            
            roleName.RegisterCallback<MouseEnterEvent>(_ =>
            {
                roleName.tooltip = RoleName?.LocaliseText();
            });

            roleName.RegisterCallback<FocusOutEvent>(_ =>
            {
                var exists = graphView.graphElements.OfType<RoleNode>().Any(c => c.RoleName == roleName.value);
                if (exists)
                {
                    roleName.value = RoleName;
                    return;
                }

                if (string.IsNullOrEmpty(roleName.text))
                {
                    roleName.value = RoleName;
                    return;
                }

                foreach (var graphElement in graphView.graphElements.Where(e => e is ClanMemberNode))
                {
                    var clanMember = (ClanMemberNode)graphElement;
                    if (clanMember.RoleID == ID)
                    {
                        clanMember.roleName.text = $"<size=14>\n{roleName.value}</size>";
                    }
                }

                RoleName = roleName.value;
                GraphSaveUtility.SaveCurrent();
            });

            roleName.RegisterCallback<FocusOutEvent>(_ => { roleName.value = $"Role Name: {RoleName}"; });

            roleName.RegisterCallback<FocusInEvent>(_ => { roleName.value = RoleName; });

            content.Add(roleName);

            #endregion

            #region Description

            var rDesc = string.IsNullOrEmpty(Description) ? "Description" : $"Description: {Description}";
            var roleDesc = IEGraphUtility.CreateTextArea(rDesc);
            roleDesc.AddClasses("ide-node-role-content-text");

            roleDesc.RegisterCallback<MouseEnterEvent>(_ =>
            {
                roleDesc.tooltip = Description?.LocaliseText();
            });

            roleDesc.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Description == roleDesc.value) return;

                if (string.IsNullOrEmpty(roleDesc.text))
                {
                    roleDesc.value = Description;
                    return;
                }

                Description = roleDesc.value;
                GraphSaveUtility.SaveCurrent();
            });

            roleDesc.RegisterCallback<FocusOutEvent>(_ =>
            {
                rDesc = string.IsNullOrEmpty(Description) ? "Description" : $"Description: {Description}";
                roleDesc.value = rDesc;
            });

            roleDesc.RegisterCallback<FocusInEvent>(_ => { roleDesc.value = Description; });

            content.Add(roleDesc);

            #endregion

            #region TitleForFemale

            var mrTitle = string.IsNullOrEmpty(TitleForFemale)
                ? "Title For Female"
                : $"Title For Female: {TitleForFemale}";
            var titleForFemale = IEGraphUtility.CreateTextField(mrTitle);
            titleForFemale.AddClasses("ide-node-role-normal-text");

            titleForFemale.RegisterCallback<MouseEnterEvent>(_ =>
            {
                titleForFemale.tooltip = TitleForFemale?.LocaliseText();
            });

            titleForFemale.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (TitleForFemale == titleForFemale.value) return;
                TitleForFemale = titleForFemale.value;
                GraphSaveUtility.SaveCurrent();
            });

            titleForFemale.RegisterCallback<FocusOutEvent>(_ =>
            {
                mrTitle = string.IsNullOrEmpty(TitleForFemale)
                    ? "Title For Female"
                    : $"Title For Female: {TitleForFemale}";
                titleForFemale.value = mrTitle;
            });

            titleForFemale.RegisterCallback<FocusInEvent>(_ => { titleForFemale.value = TitleForFemale; });

            content.Add(titleForFemale);

            #endregion

            #region TitleForMale

            var frTitle = string.IsNullOrEmpty(TitleForMale) ? "Title For Male" : $"Title For Male: {TitleForMale}";
            var titleForMale = IEGraphUtility.CreateTextField(frTitle);
            titleForMale.AddClasses("ide-node-role-normal-text");

            titleForMale.RegisterCallback<MouseEnterEvent>(
                _ => { titleForMale.tooltip = TitleForMale?.LocaliseText(); });
            titleForMale.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (TitleForMale == titleForMale.value) return;
                TitleForMale = titleForMale.value;
                GraphSaveUtility.SaveCurrent();
            });

            titleForMale.RegisterCallback<FocusOutEvent>(_ =>
            {
                frTitle = string.IsNullOrEmpty(TitleForMale) ? "Title For Male" : $"Title For Male: {TitleForMale}";
                titleForMale.value = frTitle;
            });

            titleForMale.RegisterCallback<FocusInEvent>(_ => { titleForMale.value = TitleForMale; });

            content.Add(titleForMale);

            #endregion

            #region MaxSlot

            var rSlot = string.IsNullOrEmpty(RoleSlot.ToString()) ? "Capacity" : $"Capacity: {RoleSlot.ToString()}";
            var maxSlot = IEGraphUtility.CreateTextField(rSlot);
            maxSlot.AddClasses("ide-node-role-normal-text");

            maxSlot.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                maxSlot.value = Regex.Replace(maxSlot.value, @"[^0-9]", string.Empty);
            });

            maxSlot.RegisterCallback<FocusOutEvent>(_ =>
            {
                var slot = string.IsNullOrEmpty(maxSlot.value) ? 0 : int.Parse(maxSlot.value, NumberStyles.Integer);
                if (RoleSlot == slot) return;
                if (slot < 1)
                {
                    slot = 1;
                    maxSlot.value = slot.ToString();
                }

                RoleSlot = slot;
                GraphSaveUtility.SaveCurrent();
            });

            maxSlot.RegisterCallback<FocusOutEvent>(_ =>
            {
                rSlot = string.IsNullOrEmpty(RoleSlot.ToString()) ? "Capacity" : $"Capacity: {RoleSlot.ToString()}";
                maxSlot.SetValueWithoutNotify(rSlot);
            });

            maxSlot.RegisterCallback<FocusInEvent>(_ => { maxSlot.value = RoleSlot.ToString(); });

            content.Add(maxSlot);

            #endregion

            #region Priority

            var rPriority = string.IsNullOrEmpty(Priority.ToString()) ? "Priority" : $"Priority: {Priority.ToString()}";
            var priority = IEGraphUtility.CreateTextField(rPriority);
            priority.AddClasses("ide-node-role-normal-text");

            priority.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                priority.value = Regex.Replace(priority.value, @"[^0-9]", string.Empty);
            });

            priority.RegisterCallback<FocusOutEvent>(_ =>
            {
                var slot = string.IsNullOrEmpty(priority.value) ? 0 : int.Parse(priority.value, NumberStyles.Integer);
                if (Priority == slot) return;
                Priority = slot;
                GraphSaveUtility.SaveCurrent();
            });

            priority.RegisterCallback<FocusOutEvent>(_ =>
            {
                rPriority = string.IsNullOrEmpty(Priority.ToString()) ? "Priority" : $"Priority: {Priority.ToString()}";
                priority.SetValueWithoutNotify(rPriority);
            });

            priority.RegisterCallback<FocusInEvent>(_ => { priority.value = Priority.ToString(); });

            content.Add(priority);

            #endregion

            #region Heir

            var legacyField = new VisualElement();
            legacyField.AddClasses("ide-node-role-legacy-field");

            var legacy = IEGraphUtility.CreateToggle("Has Heir:");
            legacy.value = Legacy;
            legacy.AddClasses("ide-node-role-legacy-toggle");

            legacy.RegisterValueChangedCallback(_ =>
            {
                Legacy = legacy.value;
                GraphSaveUtility.SaveCurrent();
            });

            legacyField.Add(legacy);
            content.Add(legacyField);

            #endregion

            #region Filter

            var filterField = new VisualElement();
            filterField.AddClasses("ide-node-role-legacy-field");

            var filterLabel = IEGraphUtility.CreateLabel("Heir Filter");
            filterLabel.style.color = NullUtils.HTMLColor("#CCCCCC");
            filterLabel.style.fontSize = 16;
            filterLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft);
            filterLabel.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.NoWrap);
            
            filterLabel.AddClasses("uis-role-label");
            
            Dictionary<string, string> rules;
            var ruleList = IEGraphUtility.CreateDropdown(null);

            RefreshList();

            void RefreshList()
            {
                rules = new Dictionary<string, string>(GraphWindow.CurrentDatabase.nodeDataList.OfType<HeirFilterData>().Select(s => new { s.ID, s.FilterName }).ToDictionary(d => d.ID, d => d.FilterName));
                ruleList.choices = new List<string>(rules.Values);
                ruleList.choices.Insert(0, "Filter: NULL");
                var index = rules.Keys.ToList().IndexOf(FilterID);
                ruleList.index = index == -1 ? 0 : index + 1;
            }
            
            ruleList.RegisterCallback<MouseDownEvent>(_ => RefreshList());

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
                    FilterID = string.Empty;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }
                FilterID = rules.Keys.ElementAt(ruleList.index - 1);
                GraphSaveUtility.SaveCurrent();
            });
            ruleList.style.marginLeft = 4f;
            ruleList.style.marginBottom = 1f;
            ruleList.style.marginTop = 1f;
            ruleList.style.marginRight = 3f;
            ruleList.AddClasses("ide-node__story-dropdown-field");

            filterField.Add(filterLabel);
            filterField.Add(ruleList);
            content.Add(filterField);
            
            #endregion

            rowGroup.Insert(0, mainBox);
            mainBox.Add(content);

            root.AddClasses("role-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            RefreshExpandedState();
            
            //1.0.3
            var role = GraphWindow.CurrentDatabase.roleDefinitions.FirstOrDefault(p => p.ID == ID && string.IsNullOrEmpty(p.RoleName) && p.RoleName != RoleName);
            
            if(role != null) {
                SetDirty();

                EditorRoutine.StartRoutine(0.2f, () => {
                    GraphSaveUtility.SaveCurrent(true);
                    GraphSaveUtility.LoadCurrent(graphView);
                    NDebug.Log($"{RoleName} is Synchronized.", NLogType.Log, true);
                });
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }
    }
}