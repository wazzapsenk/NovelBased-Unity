using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes {
    public class CultureNode : INode {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Culture_Node";

        public override bool IsGroupable() {
            return false;
        }

        public override GenericNodeType GenericType => GenericNodeType.Culture;

        public string CultureName;
        public Sprite CultureIcon;
        public string Description = "Description";
        
        public List<string> NamesForMale = new()
        {
            "Aldarion", "Bramdun", "Cedriv", "Darnath", "Eldric",
            "Fendrel", "Gorath", "Haldor", "Ivarn", "Jorund",
            "Kaelum", "Lorindol", "Morthil", "Narvindor", "Orin",
            "Pendrake", "Quorin", "Rathgar", "Sarvion", "Tyrion"
        };

        public List<string> NamesForFemale = new()
        {
            "Ariwyn", "Bryseis", "Ceridwen", "Delyth", "Eirwyn",
            "Fayra", "Gwendolyn", "Hilareth", "Ilyana", "Jenara",
            "Kythaela", "Lysara", "Mirelle", "Nyssa", "Orielle",
            "Prydwyn", "Quinara", "Rosalind", "Sarielle", "Taliyah"
        };

        enum Page {
            None,
            Male,
            Female,
        }

        private Page currentPage;

        protected override void OnOutputInit() { }

        public override void Draw() {
            base.Draw();
            RemoveAt(0);

            var root = new VisualElement {
                name = "root"
            };

            var rowGroup = new VisualElement {
                name = "boxGroup",
                style = {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column),
                    paddingTop = 5f,
                    paddingBottom = 5f
                }
            };

            var stageBox = new VisualElement {
                name = "stageBox",
                style = {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 200f
                }
            };
            stageBox.SetPadding(15f);

            var nameBox = new VisualElement {
                name = "nameBox",
                style = {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 200f,
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center)
                }
            };
            nameBox.SetPadding(15f);

            var content = new VisualElement() {
                name = "Content",
                style = {
                    height = new StyleLength(Length.Percent(100)),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            content.SetPadding(10f);

            var nameContent = new VisualElement() {
                name = "Content",
                style = {
                    height = new StyleLength(Length.Percent(100)),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            nameContent.SetPadding(10f);


            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#4C66C3>\u25CF</color> Culture");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

            #endregion

            #region Icon

            var icon = new IMGUIContainer() {
                style = {
                    minWidth = 32,
                    minHeight = 32,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    marginTop = 10,
                    unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f)
                }
            };
            
            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/action_choice.png");
            icon.style.backgroundImage = new StyleBackground(texture);

            icon.AddManipulator(new ContextualMenuManipulator(evt => {
                evt.menu.InsertAction(0, "Show In Project Window", _ => {
                    if (CultureIcon == null) return;
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(CultureIcon);
                });
            }));

            int currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;

            icon.RegisterCallback<MouseDownEvent>(evt => {
                if (evt.button != 0) return;
                EditorGUIUtility.ShowObjectPicker<Sprite>(CultureIcon, false, "", currentPickerWindow);
            });

            icon.onGUIHandler += () => {
                if (Event.current.commandName == "ObjectSelectorUpdated" &&
                    EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow) {
                    var _sprite = EditorGUIUtility.GetObjectPickerObject() as Sprite;

                    if (_sprite != null && _sprite != CultureIcon) {
                        CultureIcon = _sprite;

                        icon.style.backgroundImage = new StyleBackground(_sprite);
                        icon.style.unityBackgroundImageTintColor = Color.white;
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                        return;
                    }

                    if (_sprite == null) {
                        CultureIcon = null;
                        
                        icon.style.backgroundImage = new StyleBackground(texture);
                        icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                    }
                }
            };

            content.Add(icon);

            if (CultureIcon != null) {
                icon.style.backgroundImage = new StyleBackground(CultureIcon);
                icon.style.unityBackgroundImageTintColor = Color.white;
            }

            _ = new DragAndDropManipulator(icon, typeof(Texture2D), objects => {
                SetDirty();
                var iconObj = objects[0];
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconObj);
                CultureIcon = sprite;
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.unityBackgroundImageTintColor = Color.white;

                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            #region Title_NAME

            var titleName = IEGraphUtility.CreateLabel("Female Names");
            titleName.style.fontSize = 24f;

            titleName.AddClasses("ide-node-title-label");

            nameBox.Add(titleName);

            #endregion

            #region CultureName

            var cultureName = IEGraphUtility.CreateTextField(CultureName);
            cultureName.style.marginBottom = 10;
            cultureName.AddClasses("ide-node-dialogue-title-text");

            cultureName.RegisterCallback<MouseEnterEvent>(_ => { cultureName.tooltip = CultureName?.LocaliseText(); });

            cultureName.RegisterCallback<FocusOutEvent>(_ => {
                if (CultureName == cultureName.value) return;
                if (string.IsNullOrEmpty(cultureName.text)) {
                    cultureName.value = CultureName;
                    return;
                }

                var lst = (from CultureNode element in graphView.graphElements.Where(e => e is CultureNode)
                    select element.CultureName).ToList();
                var exists = lst.Contains(cultureName.value);
                if (exists) {
                    cultureName.value = CultureName;
                    return;
                }

                CultureName = cultureName.value;
                GraphSaveUtility.SaveCurrent();
            });

            content.Add(cultureName);

            #endregion

            #region Description

            var descriptionArea = IEGraphUtility.CreateTextArea(Description);
            descriptionArea.style.marginBottom = 10;
            descriptionArea.AddClasses("uis-policy-text-field");

            descriptionArea.RegisterCallback<MouseEnterEvent>(_ => {
                descriptionArea.tooltip = Description?.LocaliseText();
            });

            descriptionArea.RegisterCallback<FocusOutEvent>(_ => {
                if (Description == descriptionArea.value) return;
                if (string.IsNullOrEmpty(descriptionArea.text)) {
                    descriptionArea.value = Description;
                    return;
                }

                Description = descriptionArea.value;
                Debug.Log(Description);
                GraphSaveUtility.SaveCurrent();
            });

            content.Add(descriptionArea);

            #endregion

            #region NameList

            var nameList = IEGraphUtility.CreateTextArea();
            nameList.style.marginBottom = 10;
            nameList.AddClasses("ide-node-dialogue-content-text");

            nameList.RegisterCallback<MouseEnterEvent>(_ => { nameList.tooltip = nameList.value?.LocaliseText(); });

            nameList.RegisterCallback<FocusOutEvent>(_ => {
                var isFemale = currentPage == Page.Female;

                if (isFemale) NamesForFemale = new List<string>();
                else NamesForMale = new List<string>();

                var names = Regex.Split(nameList.value, @"\r?\n");
                foreach (var _name in names) {
                    if (string.IsNullOrEmpty(_name)) continue;
                    if (isFemale) NamesForFemale.Add(_name);
                    else NamesForMale.Add(_name);
                }

                GraphSaveUtility.SaveCurrent();
            });

            nameContent.Add(nameList);

            #endregion

            #region Names_F

            var femaleNamesBtn = IEGraphUtility.CreateButton("Female Name List", () => {
                if (currentPage == Page.Female) {
                    currentPage = Page.None;
                    nameBox.Hide();
                    return;
                }

                nameList.value = string.Empty;
                currentPage = Page.Female;
                titleName.text = "Female Names";
                for (int i = 0; i < NamesForFemale.Count; i++) {
                    nameList.value = $"{nameList.value}{NamesForFemale[i]}";
                    if (i < NamesForFemale.Count - 1) {
                        nameList.value += "\n";
                    }
                }

                nameBox.Show();
            });
            femaleNamesBtn.style.fontSize = 14;
            femaleNamesBtn.AddClasses("female-name-list");

            content.Add(femaleNamesBtn);

            #endregion

            #region Names_M

            var maleNamesBtn = IEGraphUtility.CreateButton("Male Name List", () => {
                if (currentPage == Page.Male) {
                    currentPage = Page.None;
                    nameBox.Hide();
                    return;
                }

                titleName.text = "Male Names";
                nameList.value = string.Empty;
                currentPage = Page.Male;
                for (int i = 0; i < NamesForMale.Count; i++) {
                    nameList.value = $"{nameList.value}{NamesForMale[i]}";
                    if (i < NamesForMale.Count - 1) {
                        nameList.value += "\n";
                    }
                }

                nameBox.Show();
            });
            maleNamesBtn.style.fontSize = 14;
            maleNamesBtn.AddClasses("male-name-list");

            content.Add(maleNamesBtn);

            #endregion

            rowGroup.Insert(0, stageBox);
            rowGroup.Insert(1, nameBox);
            stageBox.Add(content);
            nameBox.Add(nameContent);

            root.AddClasses("policy-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            RefreshExpandedState();

            // 1.0.3
            var culture = GraphWindow.CurrentDatabase.culturalProfiles.FirstOrDefault(p =>
                p.ID == ID && string.IsNullOrEmpty(p.CultureName) && p.CultureName != CultureName);

            if (culture != null) {
                SetDirty();

                EditorRoutine.StartRoutine(0.2f, () => {
                    GraphSaveUtility.SaveCurrent(true);
                    GraphSaveUtility.LoadCurrent(graphView);
                    NDebug.Log($"{CultureName} is Synchronized.", NLogType.Log, true);
                });
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }
    }
}