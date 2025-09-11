using System.Linq;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GlobalMessageNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Global_Message_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;

        private DropdownField variableField;

        private Label valueLabel;

        private TextField valueField;
        
        public string Title = "Title";
        public string Content = "Content";
        
        public bool TypeWriter;
        public Sprite Background;
        
        private Port sound;

        protected override void OnOutputInit()
        {
            if(Outputs.Count == 0)
                AddOutput("Ok");
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
                    paddingBottom = 5f,
                    borderTopWidth = 3f,
                }
            };

            rowGroup.SetBorderColor(NullUtils.HTMLColor("#414552"));

            stageBox = new VisualElement
            {
                name = "stageBox",
                style =
                {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 200f
                }
            };

            var content = new ScrollView()
            {
                name = "Content",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart),
                    maxHeight = 1000f
                }
            };
            content.SetPadding(10f);
            content.AddClasses("uis-dialogue-scroller");
            
            var topOutputField = new VisualElement() {
                name = "topField",
                style = {
                    alignItems = new StyleEnum<Align>(Align.Center),
                    borderBottomWidth = 0,
                    borderBottomColor = new StyleColor(NullUtils.HTMLColor("#4D4D4D")),
                    backgroundColor = NullUtils.HTMLColor("#1F1F1F"),
                    borderTopLeftRadius = 10f,
                    borderTopRightRadius = 10f
                }
            };
            
            sound = this.CreatePort("[Class]", typeof(bool), Orientation.Vertical, Direction.Input,
                Port.Capacity.Multi);
            sound.portColor = STATIC.ClassPort;
            sound.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            
            sound.Children().ToList()[1].PlaceBehind(sound.Children().ToList()[0]);
            
            topOutputField.Add(sound);
            
            root.Add(topOutputField);

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

            var input = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            input.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
            inputBox.Add(input);

            var outputBox = new VisualElement()
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

            var addChoice = IEGraphUtility.CreateButton("+", () =>
            {
                var choice = new OutputData
                {
                    Name = "Ok"
                };
                AddChoice(choice, true);
                GraphSaveUtility.SaveCurrent();
            });
            addChoice.tooltip = "Add choice";
            addChoice.SetMargin(0);
            addChoice.SetPadding(0);
            addChoice.style.marginTop = 20f;
            addChoice.style.fontSize = 14;
            addChoice.style.alignSelf = new StyleEnum<Align>(Align.Center);
            addChoice.AddClasses("ide-choice_btn");

            stageBox.SetPadding(15f);
            
            var line = IEGraphUtility.CreateLabel("────");
            line.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            
            // var bgLabel = IEGraphUtility.CreateLabel("Theme");
            // bgLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            // bgLabel.tooltip =
            //     "It sets the background theme of the dialog window. You can choose a unique background for each dialog window.";
            
            var bg = IEGraphUtility.CreateObjectField(typeof(Sprite));
            bg.style.maxWidth = 72f;
            bg.GetChild<VisualElement>().style.maxWidth = 72f;
            bg.style.alignSelf = new StyleEnum<Align>(Align.Center);
            bg.tooltip =
                "It sets the background theme of the dialog window. You can choose a unique background for each dialog window.";

            var bgChild = bg.Children().FirstOrDefault();

            if (bgChild != null) {
                bgChild.style.backgroundColor = Color.clear;
                bgChild.SetBorderWidth(0);
                var circle = bgChild.Children().ElementAtOrDefault(1);

                if (circle != null) {
                    circle.style.backgroundColor = new StyleColor(Color.clear);
                }
            }

            bg.value = Background;

            bg.RegisterValueChangedCallback(_ => {
                SetDirty();
                if (bg.value == null) {
                    Background = null;
                }
                else {
                    Background = (Sprite)bg.value;
                }
                
                GraphSaveUtility.SaveCurrent();
            });
            
            var typeWriterContent = new VisualElement {
                tooltip = "This dialog window will have a Typewriter Effect applied to it."
            };

            var line4 = IEGraphUtility.CreateLabel("────");
            line4.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            
            typeWriterContent.Add(line4);
            
            var typeWriter = new VisualElement()
            {
                style =
                {
                    width = 24,
                    height = 24,
                    marginRight = 3f,
                    minHeight = 24,
                    maxHeight = 24,
                    minWidth = 24,
                    maxWidth = 24,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f)
                }
            };
            
            var typeWriterTexture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/Typewriter.png");
            typeWriter.style.backgroundImage = new StyleBackground(typeWriterTexture);
            
            typeWriterContent.Add(typeWriter);

            void TypeWriterIcon() {
                if (TypeWriter) {
                    typeWriterContent.Show();
                }
                else {
                    typeWriterContent.Hide();
                }
            }
            
            TypeWriterIcon();

            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#718EFD>\u25CF</color> Global Message");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

            #endregion

            #region Title

            var titleField = IEGraphUtility.CreateTextField(Title);
            titleField.AddClasses("ide-node-dialogue-title-text");

            titleField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Title == titleField.value) return;
                Title = titleField.value;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            stageBox.Add(titleField);

            #endregion

            #region Content

            var contentField = IEGraphUtility.CreateTextArea(Content);
            contentField.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            contentField.AddClasses("ide-node-dialogue-content-text");

            contentField.RegisterCallback<MouseEnterEvent>(_ =>
            {
                contentField.tooltip = Content?.LocaliseText();
            });
            
            contentField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Content == contentField.value)
                    return;
                Content = contentField.value;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            content.Add(contentField);

            #endregion


            rowGroup.Insert(0, inputBox);
            rowGroup.Insert(1, stageBox);
            rowGroup.Insert(2, outputBox);
            stageBox.Add(content);
            stageBox.Add(addChoice);
            stageBox.Add(line);
            stageBox.Add(bg);
            stageBox.Add(typeWriterContent);

            root.AddClasses("global-choice-main-container");

            Insert(0, root);
            root.Insert(1, rowGroup);

            // LoadPorts();
            for (int i = 0; i < Outputs.Count; i++)
            {
                if (i == 0)
                {
                    var cPort = this.CreatePort("Out", Port.Capacity.Multi);
                    cPort.userData = Outputs[i];
                    cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
                    outputBox.Add(cPort);
                }

                AddChoice(Outputs[i]);
            }
            
            root.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                if (TypeWriter) {
                    evt.menu.InsertAction(0, "Disable Typewriter Effect", _ => {
                        TypeWriter = false;
                        TypeWriterIcon();
                        GraphSaveUtility.SaveCurrent();
                    });
                }
                else {
                    evt.menu.InsertAction(0, "Enable Typewriter Effect", _ => {
                        TypeWriter = true;
                        TypeWriterIcon();
                        GraphSaveUtility.SaveCurrent();
                    });
                }
            }));

            RefreshExpandedState();
        }

        private void AddChoice(OutputData output, bool shouldAdd = false)
        {
            var choiceField = new VisualElement()
            {
                name = "ChoiceField",
                tooltip = "Right-click for actions."
            };

            var choiceRow = new VisualElement
            {
                name = "boxGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    paddingTop = 5f,
                    paddingBottom = 5f
                }
            };

            var choiceBox = new VisualElement
            {
                name = "stageBox",
                style =
                {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 50f
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
                    minHeight = 50f,
                    borderRightColor = NullUtils.HTMLColor("#4D4D4D"),
                    borderRightWidth = 1f
                }
            };
            inputBox.SetPadding(5);

            var outputBox = new VisualElement()
            {
                name = "output",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    minWidth = 90f,
                    maxWidth = 90f,
                    minHeight = 50f
                }
            };
            outputBox.SetPadding(5);

            #region Icon

            var icon = new IMGUIContainer()
            {
                style =
                {
                    width = 32,
                    height = 32,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f)
                }
            };

            icon.RegisterCallback<MouseDownEvent>(_ =>
            {
                if (output.Sprite == null) return;
                EditorUtility.FocusProjectWindow();
                EditorGUIUtility.PingObject(output.Sprite);
            });

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/action_choice.png");
            icon.style.backgroundImage = new StyleBackground(texture);
            
            icon.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.InsertAction(0, "Show In Project Window", _ =>
                {
                    if (output.Sprite == null) return;
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(output.Sprite);
                });
            }));
            
            int currentPickerWindow = GUIUtility.GetControlID(FocusType.Passive) + 100;

            icon.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                EditorGUIUtility.ShowObjectPicker<Sprite>(output.Sprite, false, "", currentPickerWindow);
            });

            icon.onGUIHandler += () =>
            {
                if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == currentPickerWindow)
                {
                    var _sprite = EditorGUIUtility.GetObjectPickerObject() as Sprite;

                    if (_sprite != null && _sprite != output.Sprite)
                    {
                        output.Sprite = _sprite;
                        
                        icon.style.backgroundImage = new StyleBackground(_sprite);
                        icon.style.unityBackgroundImageTintColor = Color.white;
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                    } else if (_sprite == null)
                    {
                        output.Sprite = null;
                        icon.style.backgroundImage = new StyleBackground(texture);
                        icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                        SetDirty();
                        GraphSaveUtility.SaveCurrent();
                    }
                }
            };

            inputBox.Add(icon);

            if (output.Sprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(output.Sprite);
                icon.style.unityBackgroundImageTintColor = Color.white;
            }

            _ = new DragAndDropManipulator(icon, typeof(Texture2D), objects =>
            {
                SetDirty();
                var iconObj = objects[0];
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(iconObj);
                output.Sprite = sprite;
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.unityBackgroundImageTintColor = Color.white;

                GraphSaveUtility.SaveCurrent();
            });

            #endregion

            #region Text

            var text = IEGraphUtility.CreateTextArea(output.Name);
            text.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(text.value))
                {
                    text.value = output.Name;
                    return;
                }

                output.Name = text.value;
                GraphSaveUtility.SaveCurrent();
            });
            text.AddClasses("ide-node-dialogue-content-text");

            content.Add(text);

            #endregion

            #region Port

            if (shouldAdd) Outputs.Add(output);

            #endregion

            choiceField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Remove Choice", _ =>
                {
                    if (Outputs.Count < 2) return;
                    Outputs.Remove(output);
                    Remove(choiceField);
                    RefreshBorders();
                    GraphSaveUtility.SaveCurrent();
                });

                evt.menu.AppendAction("Clear Icon", _ =>
                {
                    output.Sprite = null;
                    icon.style.backgroundImage = new StyleBackground(texture);
                    icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                    GraphSaveUtility.SaveCurrent();
                });
            }));

            choiceRow.Insert(0, inputBox);
            choiceRow.Insert(1, choiceBox);
            choiceRow.Insert(2, outputBox);
            choiceBox.Add(content);

            choiceField.AddClasses("choice-container-v2");

            Insert(Children().Count() - 1, choiceField);
            choiceField.Insert(0, choiceRow);
            RefreshBorders();
        }

        private void RefreshBorders()
        {
            var last = Children().Last(v => v.name == "ChoiceField");
            last.RemoveFromClassList("choice-container-v2");
            last.AddClasses("choice-last-container-v2");

            foreach (var child in Children().Where(v => v.name == "ChoiceField"))
            {
                if (child == last) continue;
                child.RemoveFromClassList("choice-last-container-v2");
                child.AddClasses("choice-container-v2");
            }
        }
    }
}