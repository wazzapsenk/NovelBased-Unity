using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class FamilyMemberNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Family_Member_Node";

        public override GenericNodeType GenericType => GenericNodeType.Family;

        public override bool IsCopiable()
        {
            return false;
        }

        private VisualElement contentEx;

        public VisualElement spouseContainer;
        public VisualElement childContainer;

        public VisualElement spouseInputContainer;
        private VisualElement parentContainer;
        private VisualElement characterRoot;

        private VisualElement characterSelectField;

        public IEActor actor;

        private VisualElement portrait;

        public string ActorID;

        protected override void OnOutputInit() { }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            if (Outputs.Count > 0) return;

            TreeOutputs();
        }

        private void LoadActor()
        {
            CreateTreeNode();

            DrawTreeNode();
            RefreshExpandedState();
        }

        public override void Draw()
        {
            base.Draw();
            actor = GraphWindow.CurrentDatabase.actorRegistry.FirstOrDefault(a => a.ID == ActorID);

            if (actor != null)
            {
                LoadActor();
                return;
            }

            extensionContainer.AddClasses("ide-node__actor-extension-container");

            characterSelectField = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column)
                }
            };

            characterSelectField.SetPadding(5f);

            Transparent();

            style.minWidth = new StyleLength(StyleKeyword.Auto);
            titleContainer.style.height = new StyleLength(StyleKeyword.Auto);
            titleContainer.SetPadding(10);

            var titleLabel = new Label
            {
                text = "Select Character",
                style =
                {
                    fontSize = 30f,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    maxWidth = 250f
                }
            };

            titleLabel.AddClasses("ide-node__label");

            titleContainer.Insert(0, titleLabel);

            var actors =
                (from FamilyMemberData element in GraphWindow.CurrentDatabase.nodeDataList.OfType<FamilyMemberData>()
                    select element.ActorID).ToList();

            foreach (var ideActor in GraphWindow.CurrentDatabase.actorRegistry.OrderByDescending(a => a.Age))
            {
                if (actors.Contains(ideActor.ID)) continue;
                var actorBtn = IEGraphUtility.CreateButton($"{ideActor.Name}({ideActor.Age})", () =>
                {
                    ActorID = ideActor.ID;
                    actor = ideActor;
                    LoadActor();

                    GraphSaveUtility.SaveCurrent();
                    GraphSaveUtility.LoadCurrent(graphView);
                });
                actorBtn.AddClasses("ide-button__select-actor");

                characterSelectField.Add(actorBtn);
                extensionContainer.Add(characterSelectField);
            }

            RefreshExpandedState();
        }

        #region TREE

        private void TreeOutputs()
        {
            var _childs = new OutputData()
            {
                Name = "Children"
            };
            Outputs.Add(_childs);

            var spouse = new OutputData()
            {
                Name = "Spouse"
            };
            Outputs.Add(spouse);

            var spouseInput = new OutputData()
            {
                Name = "Spouse"
            };
            Outputs.Add(spouseInput);

            var parentInput = new OutputData()
            {
                Name = "Parent"
            };
            Outputs.Add(parentInput);
        }

        private void CreateTreeNode()
        {
            RemoveAt(0);
            // Clear();
            characterRoot = new VisualElement()
            {
                name = "root"
            };

            var portraitSize = 200f;
            portrait = new VisualElement()
            {
                name = "portrait",
                style =
                {
                    minWidth = portraitSize,
                    minHeight = portraitSize,
                    maxHeight = portraitSize,
                    maxWidth = portraitSize,
                    justifyContent = new StyleEnum<Justify>(Justify.FlexEnd)
                }
            };

            portrait.SetMargin(5f);
            portrait.SetBorderWidth(2);
            portrait.SetBorderRadius(4);
            portrait.SetBorderColor(NullUtils.HTMLColor("#4F4F4F"));

            RefreshPortrait();

            var mainContainerEx = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            contentEx = new VisualElement()
            {
                name = "content",
                style =
                {
                    minWidth = 150f,
                    minHeight = 150f
                }
            };

            spouseContainer = new VisualElement()
            {
                name = "SpouseContainer",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                },
                pickingMode = PickingMode.Ignore
            };
            spouseContainer.AddClasses("left-right-port-container");

            spouseInputContainer = new VisualElement()
            {
                name = "SpouseInContainer",
                style =
                {
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                },
                pickingMode = PickingMode.Ignore
            };
            spouseInputContainer.AddClasses("left-right-port-container");

            var parentContents = new VisualElement()
            {
                name = "ParentContents",
                style =
                {
                    alignItems = new StyleEnum<Align>(Align.Center),
                    borderBottomWidth = 1f,
                    borderBottomColor = NullUtils.HTMLColor("#303030")
                },
                pickingMode = PickingMode.Ignore
            };
            parentContents.AddClasses("top-bottom-port-container");

            var childContents = new VisualElement()
            {
                name = "ChildContents",
                style =
                {
                    alignItems = new StyleEnum<Align>(Align.Center),
                    borderTopWidth = 1f,
                    borderTopColor = NullUtils.HTMLColor("#303030")
                },
                pickingMode = PickingMode.Ignore
            };
            childContents.AddClasses("top-bottom-port-container");

            parentContainer = new VisualElement()
            {
                name = "ParentContainer",
                style =
                {
                    flexBasis = new StyleLength(StyleKeyword.Auto),
                    flexGrow = 1,
                    flexShrink = 0,
                    fontSize = 12,
                    paddingBottom = 4,
                    paddingTop = 4,
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                },
                pickingMode = PickingMode.Ignore
            };

            childContainer = new VisualElement()
            {
                name = "ChildContainer",
                style =
                {
                    flexBasis = new StyleLength(StyleKeyword.Auto),
                    flexGrow = 1,
                    flexShrink = 0,
                    fontSize = 12,
                    paddingBottom = 4,
                    paddingTop = 4,
                    justifyContent = new StyleEnum<Justify>(Justify.Center)
                },
                pickingMode = PickingMode.Ignore
            };

            outputContainer.style.justifyContent = new StyleEnum<Justify>(Justify.Center);

            // root.AddClasses("ide-node__main-container__character-node");

            Transparent();

            spouseContainer.style.width = new StyleLength(Length.Percent(100));
            spouseContainer.SetBorderRadius(0f);
            spouseContainer.SetBorderWidth(0f);
            spouseContainer.style.marginLeft = 0f;
            spouseContainer.style.borderLeftWidth = 2f;
            spouseContainer.style.borderLeftColor = NullUtils.HTMLColor("#303030");
            spouseContainer.style.maxWidth = 60f;
            spouseContainer.style.minWidth = 60f;

            spouseInputContainer.style.width = new StyleLength(Length.Percent(100));
            spouseInputContainer.SetBorderRadius(0f);
            spouseInputContainer.SetBorderWidth(0f);
            spouseInputContainer.style.marginRight = 0f;
            spouseInputContainer.style.borderRightWidth = 2f;
            spouseInputContainer.style.borderRightColor = NullUtils.HTMLColor("#303030");
            spouseInputContainer.style.maxWidth = 60f;
            spouseInputContainer.style.minWidth = 60f;

            characterRoot.style.borderBottomWidth = 4f;
            characterRoot.SetBorderRadius(10f);

            parentContents.Add(parentContainer);
            childContents.Add(childContainer);

            mainContainerEx.Insert(0, spouseInputContainer);
            mainContainerEx.Insert(1, contentEx);
            mainContainerEx.Insert(2, spouseContainer);

            characterRoot.Insert(0, parentContents);
            characterRoot.Insert(1, mainContainerEx);
            characterRoot.Insert(2, childContents);
            // var selectionBorder = new VisualElement() { name = "selection-border", pickingMode = PickingMode.Ignore };
            // characterRoot.Insert(3, selectionBorder);

            Insert(0, characterRoot);

            titleContainer.Hide();
            topContainer.parent.style.maxHeight = 1f;
            topContainer.parent.GetChild<VisualElement>("divider").SetBorderColor("#828282");

            // RemoveAt(1);

            //Portrait
            contentEx.Add(portrait);

            SetGender(actor.Gender);
        }

        private void DrawTreeNode()
        {
            var infoLabel = new Label
            {
                text = actor.CurrentState == Actor.IState.Active ? $"{actor.Name.Shortener()}<size=12>({actor.Age})</size>" : $"{actor.Name.Shortener()}<size=12>(<color=#FD6F61>Died</color> {actor.Age})</size>",
                style =
                {
                    fontSize = 16f,
                    letterSpacing = -2f,
                    alignSelf = new StyleEnum<Align>(Align.FlexStart),
                    position = new StyleEnum<Position>(Position.Absolute),
                    unityParagraphSpacing = -40,
                    borderBottomRightRadius = 6,
                    borderTopRightRadius = 6
                },
                tooltip = actor.Name,
            };

            var culture = GraphWindow.CurrentDatabase.culturalProfiles.Find(c => c.ID == actor.CultureID);

            if (culture != null)
            {
                infoLabel.text += $"\n<color=#CFA688><size=12>{culture.CultureName}</size></color>";
            }

            if (actor.CurrentState == Actor.IState.Passive)
            {
                portrait.style.unityBackgroundImageTintColor = NullUtils.HTMLColor("#676767");
                portrait.SetBorderColor(NullUtils.HTMLColor("#BC5858"));
            }

            infoLabel.AddClasses("ide-node__label-character");
            infoLabel.style.maxWidth = 300f;

            parentContainer.parent.Add(infoLabel);

            #region Childs

            var childsPort = this.CreatePort(Outputs[0].Name,
                actor.Gender == Actor.IGender.Male ? typeof(string) : typeof(float), Orientation.Vertical,
                Direction.Output, Port.Capacity.Multi);

            childsPort.portColor =
                actor.Gender == Actor.IGender.Male ? STATIC.ChildPortMale : STATIC.ChildPortFemale;

            childsPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
            childsPort.userData = Outputs[0];
            var portLabel = childsPort.GetChild<Label>();
            portLabel.style.fontSize = 14f;
            portLabel.pickingMode = PickingMode.Position;
            portLabel.style.paddingBottom = 5f;

            childContainer.Add(childsPort);

            #endregion

            #region Spouses

            var spouseOutputPort = this.CreatePort(Outputs[1].Name, typeof(int), Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Multi);

            spouseOutputPort.portColor = STATIC.SpouseInPort;

            spouseOutputPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
            spouseOutputPort.userData = Outputs[1];
            portLabel = spouseOutputPort.GetChild<Label>();
            portLabel.style.fontSize = 12f;
            portLabel.pickingMode = PickingMode.Position;
            portLabel.style.paddingBottom = 5f;
            portLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

            spouseContainer.Add(spouseOutputPort);


            var spouseInputPort =
                this.CreatePort(Outputs[2].Name, typeof(int), Orientation.Horizontal, Direction.Input);

            spouseInputPort.portColor = STATIC.SpouseOutPort;

            spouseInputPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
            spouseInputPort.userData = Outputs[2];
            portLabel = spouseInputPort.GetChild<Label>();
            portLabel.style.fontSize = 12f;
            portLabel.pickingMode = PickingMode.Position;
            portLabel.style.paddingBottom = 5f;
            portLabel.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

            spouseInputContainer.Add(spouseInputPort);

            #endregion

            #region PARENT

            var parentPort = this.CreatePort(Outputs[3].Name, typeof(string), Orientation.Vertical, Direction.Input,
                Port.Capacity.Multi);

            parentPort.portColor =
                actor.Gender == Actor.IGender.Male ? STATIC.ParentPortMale : STATIC.ParentPortFemale;

            parentPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.ColumnReverse);
            parentPort.userData = Outputs[3];
            parentPort.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            var parentLabel = parentPort.GetChild<Label>();
            parentLabel.style.fontSize = 14f;
            parentLabel.pickingMode = PickingMode.Position;
            parentLabel.style.paddingBottom = 5f;
            
            parentContainer.Add(parentPort);

            #endregion
        }

        #endregion

        private void SetGender(Actor.IGender gender)
        {
            switch (gender)
            {
                case Actor.IGender.Male:
                    characterRoot.SetBorderColor("#384367");
                    break;
                case Actor.IGender.Female:
                    characterRoot.SetBorderColor("#673862");
                    break;
            }
        }

        private void RefreshPortrait()
        {
            if (actor.Portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(actor.Portrait);
                portrait.style.unityBackgroundImageTintColor = NullUtils.HTMLColor("#FFFFFF");
                portrait.style.backgroundColor = Color.clear;
            }
            else
            {
                // Texture2D texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/portrait.png");
                // portrait.style.backgroundImage = new StyleBackground(texture);
                // portrait.style.unityBackgroundImageTintColor = NUtils.HTMLColor("#484040");
                portrait.style.backgroundColor = NullUtils.HTMLColor("#1D1D1D");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            GraphWindow.instance.LoadFamilyRightMenu();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (actor != null)
            {
                evt.menu.InsertAction(0, "Go to Actor", _ => { GraphWindow.instance.GotoActor(actor.ID); });
                evt.menu.InsertAction(1, "Go to Clan", _ =>
                {
                    var groupId = GraphWindow.CurrentDatabase.nodeDataList.FirstOrDefault(n =>
                        n is ClanMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == ActorID);
                    if (groupId != null)
                    {
                        GraphWindow.instance.GotoClanMember(groupId.GroupId, ActorID);
                    }
                });
                evt.menu.InsertAction(2, "Remove Actor", _ =>
                {
                    Undo.RecordObject(GraphWindow.CurrentDatabase, "Remove Actor");
                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is ClanMemberData clanMemberData && clanMemberData.ActorID == ActorID);

                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is FamilyMemberData familyMemberData && familyMemberData.ID == ID);

                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is ActorData actorData && actorData.ID == ActorID);

                    GraphWindow.CurrentDatabase.actorRegistry.RemoveAll(n =>
                        n.ID == ActorID);

                    graphView.RemoveElement(this);
                    EditorUtility.SetDirty(GraphWindow.CurrentDatabase);
                });

                if (graphView.selection.Count(s => s is FamilyMemberNode) > 1)
                {
                    evt.menu.InsertAction(1, "Remove All Actor", _ =>
                    {
                        var list = graphView.selection.OfType<FamilyMemberNode>().ToList();

                        if (list.Count > 0)
                        {
                            Undo.RecordObject(GraphWindow.CurrentDatabase, "Remove All Actors");
                        }

                        foreach (var familyMemberNode in list)
                        {
                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is ClanMemberData clanMemberData &&
                                clanMemberData.ActorID == familyMemberNode.ActorID);

                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is FamilyMemberData familyMemberData && familyMemberData.ID == familyMemberNode.ID);

                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is ActorData actorData && actorData.ID == familyMemberNode.ActorID);

                            GraphWindow.CurrentDatabase.actorRegistry.RemoveAll(n =>
                                n.ID == familyMemberNode.ActorID);

                            graphView.RemoveElement(familyMemberNode);
                        }

                        EditorUtility.SetDirty(GraphWindow.CurrentDatabase);
                    });
                }
            }
        }
    }
}