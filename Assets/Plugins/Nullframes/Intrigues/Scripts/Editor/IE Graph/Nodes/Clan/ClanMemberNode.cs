using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ClanMemberNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Clan_Member_Node";

        public override GenericNodeType GenericType => GenericNodeType.Clan;

        public override bool IsCopiable()
        {
            return false;
        }

        private VisualElement characterRoot;
        private VisualElement characterSelectField;

        public IEActor actor;

        private VisualElement portrait;

        public Label roleName;
        public VisualElement roleIcon;

        public string ActorID;
        public string RoleID;

        private void LoadActor()
        {
            CreateTreeNode();

            DrawTreeNode();
            RefreshExpandedState();
        }

        protected override void OnOutputInit() { }

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
                (from ClanMemberData element in GraphWindow.CurrentDatabase.nodeDataList.OfType<ClanMemberData>()
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
                });
                actorBtn.AddClasses("ide-button__select-actor");

                characterSelectField.Add(actorBtn);
                extensionContainer.Add(characterSelectField);
            }

            RefreshExpandedState();
        }

        #region TREE

        private void CreateTreeNode()
        {
            RemoveAt(0);
            characterRoot = new VisualElement()
            {
                name = "root"
            };

            portrait = new VisualElement()
            {
                name = "portrait",
                style =
                {
                    minWidth = 150f,
                    minHeight = 150f,
                    maxHeight = 150f,
                    maxWidth = 150f
                }
            };

            portrait.SetMargin(5f);

            RefreshPortrait();

            var mainContainerEx = new VisualElement()
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };

            var contentEx = new VisualElement()
            {
                name = "content",
                style =
                {
                    minWidth = 150f,
                    minHeight = 150f,
                    paddingLeft = 20f,
                    paddingRight = 20f
                }
            };

            Transparent();

            characterRoot.style.borderBottomWidth = 4f;
            characterRoot.SetBorderRadius(10f);

            mainContainerEx.Insert(0, contentEx);

            characterRoot.Insert(0, mainContainerEx);
            // characterRoot.Insert(1, new VisualElement() { name = "selection-border", pickingMode = PickingMode.Ignore });

            Insert(0, characterRoot);

            titleContainer.Hide();

            //Portrait
            contentEx.Add(portrait);

            SetGender(actor.Gender);
        }

        private void DrawTreeNode()
        {
            var infoLabel = new Label
            {
                text = $"{actor.Name}({actor.Age})",
                style =
                {
                    fontSize = 22f,
                    letterSpacing = -2f,
                    marginBottom = 5f,
                    color = NullUtils.HTMLColor("#C8C88F")
                }
            };

            if (actor.CurrentState == Actor.IState.Passive)
            {
                infoLabel.text += " / <color=#9c2a3b>Dead</color>";
            }

            var cultureName = new Label
            {
                style =
                {
                    fontSize = 18f,
                    letterSpacing = -2f,
                    marginTop = -30f,
                    color = NullUtils.HTMLColor("#CFA688")
                }
            };

            roleName = new Label
            {
                style =
                {
                    fontSize = 18f,
                    letterSpacing = -2f,
                    marginTop = -30f,
                    color = NullUtils.HTMLColor("#82B073")
                }
            };

            roleIcon = new VisualElement()
            {
                style =
                {
                    width = 24f,
                    height = 24f,
                    marginTop = -24,
                    marginRight = 52f,
                    alignSelf = new StyleEnum<Align>(Align.FlexEnd)
                }
            };

            var culture = GraphWindow.CurrentDatabase.culturalProfiles.Find(c => c.ID == actor.CultureID);
            if (culture != null)
                cultureName.text += $"<size=14>\n{culture.CultureName}</size>";

            var role = GraphWindow.CurrentDatabase.roleDefinitions.FirstOrDefault(c => c.ID == RoleID);
            if (role != null)
            {
                roleName.text += $"<size=14>\n{role.RoleName}</size>";
                roleIcon.style.backgroundImage = new StyleBackground(role.Icon);
                roleIcon.RegisterCallback<MouseEnterEvent>(_ => { roleIcon.tooltip = role.RoleName; });
            }

            infoLabel.AddClasses("ide-node__label-character");
            infoLabel.style.maxWidth = 300f;

            roleName.AddClasses("ide-node__label-character");
            roleName.style.maxWidth = 300f;

            cultureName.AddClasses("ide-node__label-character");
            cultureName.style.maxWidth = 300f;

            var changeMission = new VisualElement()
            {
                style =
                {
                    width = 24f,
                    height = 24f,
                    marginTop = -24,
                    marginRight = 24f,
                    alignSelf = new StyleEnum<Align>(Align.FlexEnd)
                },
                tooltip = "Change Character Role"
            };

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/change.png");
            changeMission.style.backgroundImage = new StyleBackground(texture);

            changeMission.RegisterCallback<MouseDownEvent>(_ =>
            {
                if (!GraphWindow.CurrentDatabase.roleDefinitions.Any()) return;

                var oldMission = RoleID;
                RoleID = string.Empty;
                var lst = GraphWindow.CurrentDatabase.roleDefinitions
                    .Where(m => m.Capacity > graphView.GetActiveSlot(Group.ID, m.ID)).ToList();
                if (actor.CurrentState == Actor.IState.Passive)
                {
                    lst = GraphWindow.CurrentDatabase.roleDefinitions;
                }

                if (lst.Count == 0) return;
                RoleID = oldMission;
                var index = lst.FindIndex(e => e.ID == RoleID) + 1;
                var selectedMission = index >= lst.Count ? lst[0] : lst[index];
                if (index >= lst.Count)
                {
                    roleName.text = string.Empty;
                    roleIcon.style.backgroundImage = null;
                    RoleID = string.Empty;
                    GraphSaveUtility.processCount--;
                    SetDirty();
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                RoleID = lst[index].ID;

                if (oldMission == RoleID)
                {
                    roleName.text = string.Empty;
                    roleIcon.style.backgroundImage = null;
                    RoleID = string.Empty;
                    GraphSaveUtility.processCount--;
                    SetDirty();
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                roleName.text = $"<size=14>\n{lst[index].RoleName}</size>";
                roleIcon.style.backgroundImage = new StyleBackground(selectedMission?.Icon);
                GraphSaveUtility.processCount--;
                SetDirty();
                GraphSaveUtility.SaveCurrent();
            });

            Insert(0, infoLabel);
            Insert(1, cultureName);
            Insert(2, roleName);
            Insert(3, roleIcon);
            Insert(4, changeMission);
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
                portrait.style.backgroundColor = Color.clear;
            }
            else
            {
                portrait.style.backgroundColor = NullUtils.HTMLColor("#1D1D1D");
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (actor != null)
            {
                evt.menu.InsertAction(0, "Go to Actor", _ => { GraphWindow.instance.GotoActor(actor.ID); });
                evt.menu.InsertAction(1, "Go to Family", _ =>
                {
                    var groupId = GraphWindow.CurrentDatabase.nodeDataList.FirstOrDefault(n =>
                        n is FamilyMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == ActorID);
                    if (groupId != null)
                    {
                        GraphWindow.instance.GotoFamilyMember(groupId.GroupId, ActorID);
                    }
                });
                evt.menu.InsertAction(2, "Remove Actor", _ =>
                {
                    Undo.RecordObject(GraphWindow.CurrentDatabase, "Remove Actor");

                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is FamilyMemberData familyMemberData && familyMemberData.ActorID == ActorID);

                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is ClanMemberData clanMemberData && clanMemberData.ID == ID);

                    GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                        n is ActorData actorData && actorData.ID == ActorID);

                    GraphWindow.CurrentDatabase.actorRegistry.RemoveAll(n =>
                        n.ID == ActorID);

                    graphView.RemoveElement(this);
                    EditorUtility.SetDirty(GraphWindow.CurrentDatabase);
                });

                if (graphView.selection.Count(s => s is ClanMemberNode) > 1)
                {
                    evt.menu.InsertAction(3, "Remove All Actor", _ =>
                    {
                        var list = graphView.selection.OfType<ClanMemberNode>().ToList();

                        if (list.Count > 0)
                        {
                            Undo.RecordObject(GraphWindow.CurrentDatabase, "Remove All Actors");
                        }

                        foreach (var clanMemberNode in list)
                        {
                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is FamilyMemberData familyMemberData &&
                                familyMemberData.ActorID == clanMemberNode.ActorID);

                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is ClanMemberData clanMemberData && clanMemberData.ID == clanMemberNode.ID);

                            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                                n is ActorData actorData && actorData.ID == clanMemberNode.ActorID);

                            GraphWindow.CurrentDatabase.actorRegistry.RemoveAll(n =>
                                n.ID == clanMemberNode.ActorID);

                            graphView.RemoveElement(clanMemberNode);

                            EditorUtility.SetDirty(GraphWindow.CurrentDatabase);
                        }
                    });
                }
            }
        }


        public override void OnDestroy()
        {
            base.OnDestroy();
            GraphWindow.instance.LoadClanRightMenu();
        }
    }
}