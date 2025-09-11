using System.Linq;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class PolicyNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Policy_Node";

        public override bool IsGroupable()
        {
            return false;
        }
        
        public override GenericNodeType GenericType => GenericNodeType.Policy;

        protected override void OnOutputInit() { }

        public string PolicyName = "Policy Name";
        public string Description = "Description";
        public Sprite PolicyIcon;
        public PolicyType Type;

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

            var stageBox = new VisualElement
            {
                name = "stageBox",
                style =
                {
                    minWidth = 500f,
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
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            content.SetPadding(10f);

            stageBox.SetPadding(15f);

            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#4C66C3>\u25CF</color> Policy");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

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
                    if (PolicyIcon == null) return;
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(PolicyIcon);
                });
            }));
            
            int currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;

            icon.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                EditorGUIUtility.ShowObjectPicker<Sprite>(PolicyIcon, false, "", currentPickerWindow);
            });
            
            icon.onGUIHandler += () =>
            {
                if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow)
                {
                    var _sprite = EditorGUIUtility.GetObjectPickerObject() as Sprite;

                    if (_sprite != null && _sprite != PolicyIcon)
                    {
                        PolicyIcon = _sprite;
                        
                        icon.style.backgroundImage = new StyleBackground(_sprite);
                        icon.style.unityBackgroundImageTintColor = Color.white;
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                        return;
                    }

                    if (_sprite == null) {
                        PolicyIcon = null;
                        
                        icon.style.backgroundImage = new StyleBackground(texture);
                        icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                    }
                }
            };

            content.Add(icon);
            
            if (PolicyIcon != null)
            {
                icon.style.backgroundImage = new StyleBackground(PolicyIcon);
                icon.style.unityBackgroundImageTintColor = Color.white;
            }

            _ = new DragAndDropManipulator(icon, typeof(Texture2D), objects =>
            {
                SetDirty();
                var iconObj = objects[0];
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconObj);
                PolicyIcon = sprite;
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.unityBackgroundImageTintColor = Color.white;

                GraphSaveUtility.SaveCurrent();
            });

            #endregion
            
            #region Title

            var titleField = IEGraphUtility.CreateTextField(PolicyName);
            titleField.AddClasses("ide-node-dialogue-title-text");
            
            titleField.RegisterCallback<MouseEnterEvent>(_ =>
            {
                titleField.tooltip = PolicyName?.LocaliseText();
            });

            titleField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (PolicyName == titleField.value) return;
                if (string.IsNullOrEmpty(titleField.text))
                {
                    titleField.value = PolicyName;
                    return;
                }
                PolicyName = titleField.value;

                GraphSaveUtility.SaveCurrent();
                GraphSaveUtility.LoadCurrent(graphView);
            });

            content.Add(titleField);

            #endregion

            #region Description

            var descriptionArea = IEGraphUtility.CreateTextArea(Description);
            descriptionArea.AddClasses("uis-policy-desc");

            descriptionArea.RegisterCallback<MouseEnterEvent>(_ =>
            {
                descriptionArea.tooltip = Description?.LocaliseText();
            });

            descriptionArea.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Description == descriptionArea.value) return;
                if (string.IsNullOrEmpty(descriptionArea.text))
                {
                    descriptionArea.value = Description;
                    return;
                }
                Description = descriptionArea.value;
                GraphSaveUtility.SaveCurrent();
            });

            content.Add(descriptionArea);

            #endregion
            
            #region Type

            var typeField = IEGraphUtility.CreateDropdown(new []{ "Generic", "Family Policy", "Clan Policy" });

            typeField.index = (int)Type;

            var dropdownChild = typeField.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;

            typeField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Type = typeField.index switch
                {
                    0 => PolicyType.Generic,
                    1 => PolicyType.Family,
                    _ => PolicyType.Clan
                };
                GraphSaveUtility.SaveCurrent();
            });
            typeField.style.marginLeft = 6f;
            typeField.style.marginTop = 8f;
            typeField.style.marginRight = 6f;
            typeField.AddClasses("ide-node__gender-dropdown-field");

            content.Add(typeField);

            #endregion
            
            rowGroup.Insert(0, stageBox);
            stageBox.Add(content);

            root.AddClasses("policy-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            RefreshExpandedState();

            // 1.0.3
            var policy = GraphWindow.CurrentDatabase.policyCatalog.FirstOrDefault(p => p.ID == ID && string.IsNullOrEmpty(p.PolicyName) && p.PolicyName != PolicyName);
            
            if(policy != null) {
                SetDirty();

                EditorRoutine.StartRoutine(0.2f, () => {
                    GraphSaveUtility.SaveCurrent(true);
                    GraphSaveUtility.LoadCurrent(graphView);
                    NDebug.Log($"{PolicyName} is Synchronized.", NLogType.Log, true);
                });
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }
    }
}