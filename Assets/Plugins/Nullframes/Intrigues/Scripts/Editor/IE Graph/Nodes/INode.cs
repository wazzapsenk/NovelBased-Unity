using System;
using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes {
    public abstract class INode : Node {
        public string ID { get; set; }
        public virtual GenericNodeType GenericType => GenericNodeType.Scheme;
        protected IEGraphView graphView;
        public IGroup Group { get; set; }
        public bool IsDrawed { get; private set; }
        public Vector2 Pos { get; set; }
        public List<OutputData> Outputs { get; set; } = new();
        public bool Dirty { get; private set; }

        private VisualElement worker;
        private VisualElement sequencer;
        private VisualElement repeater;
        private VisualElement wait;
        private VisualElement flow;
        private VisualElement breakIcon;
        private VisualElement dead;
        private VisualElement iconsParent;

        public HashSet<INode> children;
        public HashSet<INode> parents;

        public bool MultiOutput => Outputs.Count > 1 || this is RandomNode or SequencerNode;
        
        protected abstract string DOCUMENTATION { get; }

        public virtual void Init(IEGraphView ieGraphView) {
            ID = GUID.Generate().ToString();

            this.SetBorderColor("#FF8B00");
            
            style.display = DisplayStyle.Flex;

            graphView = ieGraphView;

            mainContainer.SetBorderWidth(0);
            mainContainer.SetBorderRadius(0f);
            inputContainer.style.justifyContent = new StyleEnum<Justify>(Justify.Center);
            
            children = new HashSet<INode>();
            parents = new HashSet<INode>();
        }

        public virtual void OnCreated() {
            NDebug.Log("Node is created.");
            SetDirty();
        }

        public virtual void OnDestroy() {
            NDebug.Log("Node is destroyed.");
            GraphSaveUtility.RemoveNodeItem(this);
        }

        public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity,
            Type type) {
            var port = base.InstantiatePort(orientation, direction, capacity, type);

            foreach (var edge in port.connections) {
                edge.input.portColor = edge.output.portColor;
            }

            return port;
        }

        protected abstract void OnOutputInit();

        public virtual void Draw() {
            OnOutputInit();

            IsDrawed = true;

            EditorRoutine.StartRoutine(0.1f, () => {
                this.TraverseVisualElement<VisualElement>(element => {
                    if (element is Button btn) {
                        btn.clickable = new Clickable(_ => { SetDirty(); });
                        return;
                    }

                    element.RegisterCallback<MouseDownEvent>(DirtyEvent);
                });
            });

            if (this is CommentRightToLeftNode or CommentLeftToRightNode) return;

            iconsParent = new VisualElement {
                style = {
                    position = new StyleEnum<Position>(Position.Absolute), top = -12, left = -10,
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                },
                pickingMode = PickingMode.Ignore
            };

            var doc = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#DBDBDB"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "Go Documentation"
            };

            //Doc
            var docIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/doc.png");
            doc.style.backgroundImage = new StyleBackground(docIcon);
            iconsParent.Add(doc);

            mainContainer.parent.RegisterCallback<MouseOverEvent>(_ => { doc.Show(); });

            mainContainer.parent.RegisterCallback<MouseOutEvent>(_ => { doc.Hide(); });

            doc.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(DOCUMENTATION));

            //Repeater
            
            repeater = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#FAFF84"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is connected to the Repeater node.",
            };

            var repeaterIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/repeater.png");
            repeater.style.backgroundImage = new StyleBackground(repeaterIcon);
            iconsParent.Add(repeater);
            
            //Sequencer

            sequencer = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#967ACF"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is connected to the Sequencer node.",
            };

            var sequencerIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/sequential.png");
            sequencer.style.backgroundImage = new StyleBackground(sequencerIcon);
            iconsParent.Add(sequencer);
            
            //worker

            worker = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#00F5FF"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is connected to the Background Worker node."
            };

            var workerIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/background-worker.png");
            worker.style.backgroundImage = new StyleBackground(workerIcon);
            iconsParent.Add(worker);
            
            //wait

            wait = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#39FF00"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is connected to the Wait node."
            };

            var waitIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/wait.png");
            wait.style.backgroundImage = new StyleBackground(waitIcon);
            iconsParent.Add(wait);
            
            //new-flow

            flow = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#A4E290"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is connected to the New-Flow node."
            };

            var flowIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/new-flow.png");
            flow.style.backgroundImage = new StyleBackground(flowIcon);
            iconsParent.Add(flow);
            
            //break-icon

            breakIcon = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#C03401"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This node is breaks the flow."
            };

            var breakIcn = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/break.png");
            breakIcon.style.backgroundImage = new StyleBackground(breakIcn);
            iconsParent.Add(breakIcon);
            
            //dead

            dead = new VisualElement {
                style = {
                    width = 24, height = 24,
                    unityBackgroundImageTintColor = NullUtils.HTMLColor("#EC6C6C"),
                    display = new StyleEnum<DisplayStyle>(DisplayStyle.None),
                    alignSelf = new StyleEnum<Align>(Align.Center),
                },
                tooltip = "This character is dead."
            };

            var deadIcon = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/dead.png");
            dead.style.backgroundImage = new StyleBackground(deadIcon);
            iconsParent.Add(dead);

            //
            mainContainer.parent.Insert(1, iconsParent);
        }


        private void ToggleRepeaterIcon(bool toggle) {
            if (toggle) {
                repeater.Show();
            }
            else {
                repeater.Hide();
            }
        }

        private void ToggleSequencerIcon(bool toggle) {
            if (toggle) {
                sequencer.Show();
            }
            else {
                sequencer.Hide();
            }
        }

        private void ToggleWorkerIcon(bool toggle) {
            if (toggle) {
                worker.Show();
            }
            else {
                worker.Hide();
            }
        }
        
        private void ToggleWaitIcon(bool toggle) {
            if (toggle) {
                wait.Show();
            }
            else {
                wait.Hide();
            }
        }
        
        private void ToggleNewFlowIcon(bool toggle) {
            if (toggle) {
                flow.Show();
            }
            else {
                flow.Hide();
            }
        }
        
        private void ToggleBreakIcon(bool toggle) {
            if (toggle) {
                breakIcon.Show();
            }
            else {
                breakIcon.Hide();
            }
        }

        public void ActiveBorderLine()
        {
            this.SetBorderWidth(2);
        }
        
        public void DisableBorderLine()
        {
            this.SetBorderWidth(0);
        }

        public void HideAllIcons() {
            if (iconsParent != null) {
                foreach (var child in iconsParent.Children()) {
                    child.Hide();
                }
            }
        }

        private void DirtyEvent(MouseDownEvent evt) {
            SetDirty();
        }

        public void ReGenerateID() {
            ID = GUID.Generate().ToString();
        }

        public void SetDirty() => Dirty = true;
        public void ClearDirty() => Dirty = false;

        public void Execute() {
            if (this is BackgroundWorkerNode) {
                this.TraverseChildNode<INode>(n => {
                    if (n is ChoiceDataNode or ChanceNode) return;
                        n.ToggleWorkerIcon(true);
                });
            }
            
            // if (this is SequencerNode) {
            //     this.TraverseSequencerFlow(n =>
            //     {
            //         n.ToggleSequencerIcon(true);
            //     });
            // }
            
            if (this is NewFlowNode) {
                this.TraverseChildNode<INode>(n => {
                    if (n is ChoiceDataNode or ChanceNode) return;
                        n.ToggleNewFlowIcon(true);
                });
            }
            
            if (this is BreakNode or ContinueNode or BreakRepeaterNode or BreakSequencerNode) {
                ToggleBreakIcon(true);
            }
            
            if (this is FamilyMemberNode familyMemberNode) {
                if (familyMemberNode.actor.CurrentState == Actor.IState.Passive) {
                    dead.Show();
                }
            }

            if (this is WaitNode or WaitRandomNode or WaitTriggerNode or WaitUntilNode) {
                foreach (var child in children) {
                    child.ToggleWaitIcon(true);
                }
            }            
            
            if (this is RepeaterNode) {
                foreach (var child in children) {
                    child.ToggleRepeaterIcon(true);
                }
            }            
            
            if (this is SequencerNode) {
                foreach (var child in children) {
                    child.ToggleSequencerIcon(true);
                }
            }
        }

        #region Overrides

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            evt.menu.AppendAction("Disconnect Input Ports", _ => DisconnectInputPorts());
            evt.menu.AppendAction("Disconnect Output Ports", _ => DisconnectOutputPorts());

            base.BuildContextualMenu(evt);
        }

        #endregion

        #region Methods

        public void DisconnectAllPorts() {
            DisconnectInputPorts();
            DisconnectOutputPorts();
        }

        protected void DisconnectAllPorts(Port p) {
            graphView.DeleteElements(p.connections);
        }

        public void DisconnectInputPorts() {
            DisconnectPorts(false);
        }

        public void DisconnectOutputPorts() {
            DisconnectPorts(true);
        }

        private void DisconnectPorts(bool isInput) {
            foreach (var port in this.GetElements<Port>()) {
                if (isInput && port.direction != Direction.Output) continue;
                if (!isInput && port.direction != Direction.Input) continue;
                if (port is not { connected: true })
                    continue;
                graphView.DeleteElements(port.connections);
            }
        }

        protected void Transparent() {
            outputContainer.style.backgroundColor = Color.clear;
            inputContainer.style.backgroundColor = Color.clear;
            titleContainer.style.backgroundColor = Color.clear;
        }

        protected OutputData AddOutput(string portName) {
            var output = Outputs.FirstOrDefault(d => d.Name == portName);
            if (output != null) return output;
            var outputData = new OutputData() { Name = portName };
            Outputs.Add(outputData);
            return outputData;
        }

        #endregion
    }
}