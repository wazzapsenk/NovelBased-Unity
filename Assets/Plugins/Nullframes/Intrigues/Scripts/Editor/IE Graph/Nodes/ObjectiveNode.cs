using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ObjectiveNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Objective_Node";

        public override GenericNodeType GenericType => GenericNodeType.Scheme;

        private VisualElement root;
        private VisualElement rowGroup;
        private VisualElement stageBox;
        private VisualElement outputBox;

        private DropdownField variableField;
        private DropdownField dropdownField;

        private Label valueLabel;
        private Label errorLabel;

        private TextField valueField;

        private ObjectField objectInput;

        public string Objective = "Objective Description";
        
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
                name = "objectiveField",
                style =
                {
                    minWidth = 300f,
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

            var input = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputBox.Add(input);

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

            var titleLabel = IEGraphUtility.CreateLabel("<color=#80a35b>\u25CF</color> Objective");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            stageBox.Add(titleLabel);

            #endregion

            #region Objective

            var objectiveField = IEGraphUtility.CreateTextArea(Objective);
            objectiveField.AddClasses("ide-node-dialogue-content-text");
            objectiveField.style.fontSize = 16;

            objectiveField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Objective == objectiveField.value) return;
                Objective = objectiveField.value;
                GraphSaveUtility.SaveCurrent();
            });

            content.Add(objectiveField);

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

            rowGroup.Insert(0, inputBox);
            rowGroup.Insert(1, stageBox);
            rowGroup.Insert(2, outputBox);
            stageBox.Add(content);

            root.AddClasses("set-variable-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            LoadPorts();

            RefreshExpandedState();
        }

        private void LoadPorts()
        {
            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
                cPort.userData = outputData;
                outputBox.Add(cPort);
            }
        }
    }
}