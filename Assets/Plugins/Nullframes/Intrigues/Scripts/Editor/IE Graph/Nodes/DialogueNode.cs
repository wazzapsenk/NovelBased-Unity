using System.Linq;
using Nullframes.Intrigues.EDITOR;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class DialogueNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Dialogue_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;
        private VisualElement rightBox;

        private DropdownField variableField;

        private Label valueLabel;
        private Button breakButton;

        private TextField valueField;
        
        public string Title = "Title";
        public string Content = "Content";
        public float Time;
        public bool Break;
        public bool TypeWriter;
        public Sprite Background;

        private Port timeOut;

        private Port consIn;
        private Port targetIn;
        private Port actorIn;
        
        private Port sound;

        protected override void OnOutputInit()
        {
            if (Outputs.Count > 0) return;
            AddOutput("Timeout");
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
            
            var dragger = content.Q<VisualElement>("unity-dragger");
            dragger.AddClasses("uis-dialogue-dragger");
            
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
                    minWidth = 110f,
                    minHeight = 90f,
                    borderRightColor = NullUtils.HTMLColor("#4D4D4D"),
                    borderRightWidth = 1f
                }
            };
            inputBox.SetPadding(5);

            consIn = this.CreatePort("Conspirator", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputBox.Add(consIn);

            targetIn = this.CreatePort("Target", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputBox.Add(targetIn);
            
            actorIn = this.CreatePort("[Actor]", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            actorIn.portColor = STATIC.GreenPort;
            actorIn.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            inputBox.Add(actorIn);

            foreach (var schemeVariable in ((SchemeGroup)Group).Variables.Where(v => v.type == NType.Actor)) {
                var port = this.CreatePort($"[{schemeVariable.name}]", typeof(bool), Orientation.Horizontal,
                    Direction.Input,
                    Port.Capacity.Multi);
                port.userData = schemeVariable.id;
                port.portColor = STATIC.BluePort;
                port.GetChild<Label>().style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                inputBox.Add(port);
            }
            
            rightBox = new VisualElement()
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
            rightBox.SetPadding(5);

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
            addChoice.style.fontSize = 14;
            addChoice.style.alignSelf = new StyleEnum<Align>(Align.Center);
            addChoice.AddClasses("ide-choice_btn");
            
            timeOut = this.CreatePort(Outputs[0].Name, Port.Capacity.Multi);
            timeOut.portColor = STATIC.RedPort;
            timeOut.style.alignSelf = new StyleEnum<Align>(Align.Center);
            timeOut.userData = Outputs[0];

            breakButton = IEGraphUtility.CreateButton("B", () =>
            {
                Break = !Break;
                RefreshBreakButton();

                GraphSaveUtility.SaveCurrent();
            });
            breakButton.userData ??= breakButton.style.backgroundColor;
            RefreshBreakButton();

            breakButton.SetMargin(0);
            breakButton.SetPadding(0);
            breakButton.style.fontSize = 14;
            breakButton.style.alignSelf = new StyleEnum<Align>(Align.Center);
            breakButton.AddClasses("ide-break_btn");
            
            var line = IEGraphUtility.CreateLabel("────");
            line.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            
            var line2 = IEGraphUtility.CreateLabel("────");
            line2.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

            rightBox.Add(timeOut);
            rightBox.Add(line);
            rightBox.Add(addChoice);
            rightBox.Add(breakButton);
            rightBox.Add(line2);

            var timeField = IEGraphUtility.CreateFloatField(Time);
            timeField.AddClasses("float__field-dialogue-time");
            
            rightBox.Add(timeField);
            
            var line3 = IEGraphUtility.CreateLabel("────");
            line3.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
            
            rightBox.Add(line3);

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
            
            rightBox.Add(bg);

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
            rightBox.Add(typeWriterContent);

            void TypeWriterIcon() {
                if (TypeWriter) {
                    typeWriterContent.Show();
                }
                else {
                    typeWriterContent.Hide();
                }
            }
            
            TypeWriterIcon();

            timeField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (timeField.value <= 0)
                {
                    timeField.value = 0;
                    timeOut.Disable();
                } else if (Break || Outputs.Count > 2)
                {
                    timeOut.Enable();
                }

                Time = timeField.value;
                GraphSaveUtility.SaveCurrent();
            });

            timeField.tooltip =
                "It sets a time-out for the dialogue. If a selection is not made within the specified time, the dialogue will time out. If set to 0, there is no time-out.";

            stageBox.SetPadding(15f);

            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#FFA500>\u25CF</color> Show Dialogue");
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
            rowGroup.Insert(2, rightBox);
            stageBox.Add(content);

            root.AddClasses("choice-main-container");

            Insert(0, root);
            root.Insert(1, rowGroup);

            // LoadPorts();
            foreach (var output in Outputs) AddChoice(output);

            RefreshPrimary();

            RefreshExpandedState();
            
            RefreshTargetBorder(true);
            
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
        }

        private void AddChoice(OutputData output, bool shouldAdd = false)
        {
            if (output.Name == "Timeout") return;
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
                name = "choiceBox",
                style =
                {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 50f,
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                }
            };

            var content = new VisualElement()
            {
                name = "Content",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            content.SetPadding(10f);

            var choiceMode = new VisualElement()
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

            void RefreshIcon() {
                var modeTexture = (Texture2D)EditorGUIUtility.Load(output.HideIfDisable
                    ? "Nullframes/Icons/hide.png"
                    : "Nullframes/Icons/show.png");
                choiceMode.style.backgroundImage = new StyleBackground(modeTexture);

                if (output.HideIfDisable) {
                    choiceMode.tooltip = "If the Choice button is disabled, the Choice will be hidden.";
                    choiceMode.style.unityBackgroundImageTintColor =
                        new StyleColor(new Color(1f, 0.5647058823529412f, 0.5411764705882353f, 0.8f));
                }
                else {
                    choiceMode.tooltip = "Even if the Choice button is disabled, the Choice will still be displayed but won't be selectable.";
                    choiceMode.style.unityBackgroundImageTintColor =
                        new StyleColor(new Color(0.4666666666666667f, 0.5098039215686274f, 0.8509803921568627f, 0.8f));
                }
            }
            
            RefreshIcon();

            content.Add(choiceMode);

            var inputBox = new VisualElement()
            {
                name = "input",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center),
                    minWidth = 110f,
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
                    minHeight = 50f,
                    borderLeftColor = NullUtils.HTMLColor("#4D4D4D"),
                    borderLeftWidth = 1f
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

            var text = IEGraphUtility.CreateTextField(output.Name);
            text.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                if (evt.newValue.Contains("\n"))
                {
                    text.value = evt.previousValue;
                }
            });
            text.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(text.value))
                {
                    text.value = output.Name;
                    return;
                }

                SetDirty();
                output.Name = text.value;
                GraphSaveUtility.SaveCurrent();
            });
            text.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
            text.AddClasses("ide-node-dialogue-choice-text");

            content.Add(text);

            #endregion

            #region Port

            if (shouldAdd) Outputs.Add(output);

            var cPort = this.CreatePort(string.Empty, Port.Capacity.Multi);
            cPort.portColor = STATIC.RandomColor;
            cPort.style.alignSelf = new StyleEnum<Align>(Align.Center);
            cPort.userData = output;
            outputBox.Add(cPort);

            #endregion

            choiceField.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Remove Choice", _ =>
                {
                    if (Outputs.Count < 3) return;
                    SetDirty();
                    Outputs.Remove(output);
                    RefreshBreakButton();
                    DisconnectAllPorts((Port)choiceField.userData);
                    Remove(choiceField);
                    RefreshBorders();
                    graphView.UpdateNodeIcons();
                });

                evt.menu.AppendAction("Clear Icon", _ =>
                {
                    if (output.Sprite == null) return;
                    SetDirty();
                    output.Sprite = null;
                    icon.style.backgroundImage = new StyleBackground(texture);
                    icon.style.unityBackgroundImageTintColor = new Color(255f, 255f, 255f, 0.5f);
                    GraphSaveUtility.SaveCurrent();
                });
                
                evt.menu.InsertAction(0, "Set Primary(FOR AI)", _ =>
                {
                    if (Outputs.Count < 3) return;
                    SetDirty();

                    output.Primary = !output.Primary;

                    RefreshPrimary();
                    GraphSaveUtility.SaveCurrent();
                });

                evt.menu.InsertAction(0, "Down", _ =>
                {
                    int index = Outputs.IndexOf(output);

                    if (index + 1 >= Outputs.Count) return;

                    var oldItem = Outputs[index];
                    Outputs.Remove(oldItem);
                    Outputs.Insert(index + 1, oldItem);
                    
                    var oldField = choiceField;
                    Remove(choiceField);
                    Insert(index + 1, oldField);
                    
                    SetDirty();
                    GraphSaveUtility.SaveCurrent();
                    
                    RefreshBorders();
                });
                
                evt.menu.InsertAction(0, "Up", _ =>
                {
                    int index = Outputs.IndexOf(output);

                    if (index == 0) return;

                    var oldItem = Outputs[index];
                    Outputs.Remove(oldItem);
                    Outputs.Insert(index - 1, oldItem);

                    var oldField = choiceField;
                    Remove(choiceField);
                    Insert(index - 1, oldField);
                    
                    SetDirty();
                    GraphSaveUtility.SaveCurrent();
                    
                    RefreshBorders();
                });

                if (!output.HideIfDisable) {
                    evt.menu.InsertAction(2, "Hide If Disabled", _ => {
                        output.HideIfDisable = true;
                        
                        SetDirty();
                        RefreshIcon();
                        
                        GraphSaveUtility.SaveCurrent();
                    });
                }
                else {
                    evt.menu.InsertAction(2, "Show If Disabled", _ => {
                        output.HideIfDisable = false;
                        
                        SetDirty();
                        RefreshIcon();
                        
                        GraphSaveUtility.SaveCurrent();
                    });
                }
            }));

            choiceRow.Insert(0, inputBox);
            choiceRow.Insert(1, choiceBox);
            choiceRow.Insert(2, outputBox);
            choiceBox.Add(content);

            choiceField.AddClasses("choice-container");
            choiceField.userData = cPort;

            Insert(Children().Count() - 1, choiceField);
            choiceField.Insert(0, choiceRow);
            RefreshBorders();
            RefreshBreakButton();
            
            graphView.UpdateNodeIcons();
        }

        private void RefreshBreakButton()
        {
            if (Break)
            {
                breakButton.style.backgroundColor = (StyleColor)breakButton.userData;
                breakButton.tooltip =
                    "The flow on this line will pause until a choice is made. (Applies to single-choice dialogues.)";
            }
            else
            {
                breakButton.style.backgroundColor = NullUtils.HTMLColor("#3A3636");
                breakButton.tooltip =
                    "The flow on this line will continue without waiting for a choice to be made. (Applies to single-choice dialogues.)";
            }

            if (Outputs.Count > 2)
            {
                breakButton.Disable();
                if(Time > 0)
                    timeOut.Enable();
                else
                    timeOut.Disable();
            }
            else
            {
                breakButton.Enable();
                if(Break && Time > 0)
                    timeOut.Enable();
                else
                    timeOut.Disable();
            }
        }

        private void RefreshBorders()
        {
            var last = Children().Last(v => v.name == "ChoiceField");
            last.RemoveFromClassList("choice-container");
            last.AddClasses("choice-last-container");

            foreach (var child in Children().Where(v => v.name == "ChoiceField"))
            {
                if (child == last) continue;
                child.RemoveFromClassList("choice-last-container");
                child.AddClasses("choice-container");
            }
        }

        public void RefreshTargetBorder(bool delay)
        {
            if (delay)
                EditorRoutine.StartRoutine(0.05f, Ref);
            else
                Ref();

            void Ref() {
                if (targetIn.connections.Any() && consIn.connections.Any() && actorIn.connections.Any() || !targetIn.connections.Any() && !consIn.connections.Any() && !actorIn.connections.Any() || consIn.connections.Any() && targetIn.connections.Any() || consIn.connections.Any() && actorIn.connections.Any() || targetIn.connections.Any() && actorIn.connections.Any())
                {
                    var defaultColor = NullUtils.HTMLColor("#52473F");
                    rowGroup.SetBorderColor(defaultColor);
                    foreach (var child in Children().Where(c => c.name == "ChoiceField"))
                    {
                        child.SetBorderColor(defaultColor);
                    }
                    return;
                }

                if (targetIn.connections.Any())
                {
                    var targetColor = NullUtils.HTMLColor("#71423B");
                    rowGroup.SetBorderColor(targetColor);
                    foreach (var child in Children().Where(c => c.name == "ChoiceField"))
                    {
                        child.SetBorderColor(targetColor);
                    }
                }
                
                if (consIn.connections.Any())
                {
                    var consColor = NullUtils.HTMLColor("#4F634B");
                    rowGroup.SetBorderColor(consColor);
                    foreach (var child in Children().Where(c => c.name == "ChoiceField"))
                    {
                        child.SetBorderColor(consColor);
                    }
                }

                if (actorIn.connections.Any())
                {
                    var actorColor = NullUtils.HTMLColor("#634b60");
                    rowGroup.SetBorderColor(actorColor);
                    foreach (var child in Children().Where(c => c.name == "ChoiceField"))
                    {
                        child.SetBorderColor(actorColor);
                    }
                }
            }
        }

        private void RefreshPrimary()
        {
            foreach (var child in Children().Where(v => v.name == "ChoiceField"))
            {
                var port = (Port)child.userData;
                var output = (OutputData)port.userData;

                child.style.backgroundColor = output.Primary ? new StyleColor(new Color(26 / 255f, 29 / 255f, 25 / 255f, 0.9f)) : new StyleColor(new Color(0.1294118f, 0.1294118f, 0.1294118f, 0.9f));
            }
        }
    }
}