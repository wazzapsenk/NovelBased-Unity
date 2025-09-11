using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ActorNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Actor_Node";

        public override bool IsGroupable()
        {
            return false;
        }

        public override GenericNodeType GenericType => GenericNodeType.Actor;

        private const float iconSize = 72f;

        private VisualElement portrait;

        public string ActorName = "Actor";
        public string CultureID;
        public int Age;
        public Actor.IGender Gender;
        public Actor.IState State;
        public Sprite Portrait;
        public bool IsPlayer;


        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("dark-gray-background");
            extensionContainer.AddClasses("ide-node__actor-extension-container");

            Transparent();

            style.minWidth = new StyleLength(StyleKeyword.Auto);
            titleContainer.style.height = new StyleLength(StyleKeyword.Auto);
            titleContainer.SetPadding(10);
        }

        protected override void OnOutputInit() { }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label
            {
                text = ActorName.Shortener(),
                style =
                {
                    fontSize = 30f,
                    alignSelf = new StyleEnum<Align>(Align.Center),
                    maxWidth = 250f
                },
                tooltip = ActorName
            };

            titleLabel.AddClasses("ide-node__label");

            portrait = new VisualElement
            {
                style =
                {
                    minWidth = iconSize,
                    minHeight = iconSize,
                    maxHeight = iconSize,
                    maxWidth = iconSize,
                    marginLeft = 190f
                }
            };

            RefreshPortrait();

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, portrait);

            #region NAME

            var actorNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var actorNameLabel = IEGraphUtility.CreateLabel("Actor Name");
            actorNameLabel.style.fontSize = 14;
            actorNameLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            actorNameLabel.style.paddingRight = 10f;
            actorNameLabel.style.minWidth = 120f;
            actorNameLabel.style.maxWidth = 120f;

            //TextField
            var actorName = IEGraphUtility.CreateTextField(ActorName);
            actorName.RegisterCallback<FocusOutEvent>(_ =>
            {
                ActorName = actorName.value;
                titleLabel.text = ActorName.Shortener();
                titleLabel.tooltip = ActorName;
                GraphSaveUtility.SaveCurrent();
            });
            actorName.AddClasses("ide-node__text-field-actor");

            var randomName = new VisualElement()
            {
                style =
                {
                    maxWidth = 24,
                    maxHeight = 24,
                    minHeight = 24,
                    minWidth = 24
                }
            };

            randomName.RegisterCallback<MouseDownEvent>(_ =>
            {
                if (string.IsNullOrEmpty(CultureID)) return;
                var culture = GraphWindow.CurrentDatabase.culturalProfiles.FirstOrDefault(c => c.ID == CultureID);
                if (culture != null)
                {
                    if (Gender == Actor.IGender.Male && culture.MaleNames.Any())
                    {
                        actorName.value = culture.MaleNames.ToList()[Random.Range(0, culture.MaleNames.Count())];
                        ActorName = actorName.value;
                        titleLabel.text = ActorName.Shortener();
                        titleLabel.tooltip = ActorName;
                        GraphSaveUtility.SaveCurrent();
                    }

                    if (Gender == Actor.IGender.Female && culture.FemaleNames.Any())
                    {
                        actorName.value = culture.FemaleNames.ToList()[Random.Range(0, culture.FemaleNames.Count())];
                        ActorName = actorName.value;
                        titleLabel.text = ActorName.Shortener();
                        titleLabel.tooltip = ActorName;
                        GraphSaveUtility.SaveCurrent();
                    }
                }
            });

            var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/random.png");
            randomName.style.backgroundImage = new StyleBackground(texture);

            actorNameField.Add(actorNameLabel);
            actorNameField.Add(actorName);
            actorNameField.Add(randomName);

            extensionContainer.Add(actorNameField);

            #endregion

            #region AGE

            var actorAgeField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var actorAgeLabel = IEGraphUtility.CreateLabel("Age");
            actorAgeLabel.style.fontSize = 14;
            actorAgeLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            actorAgeLabel.style.paddingRight = 10f;
            actorAgeLabel.style.minWidth = 120f;
            actorAgeLabel.style.maxWidth = 120f;

            //TextField
            var actorAge = IEGraphUtility.CreateIntField(Age);
            actorAge.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (actorAge.value < 0) actorAge.value = 0;

                Age = actorAge.value;
                GraphSaveUtility.SaveCurrent();
            });
            actorAge.style.marginLeft = 0f;
            actorAge.style.marginBottom = 1f;
            actorAge.style.marginTop = 1f;
            actorAge.style.marginRight = 3f;
            actorAge.AddClasses("ide-node__integer-field-actor-age");

            actorAgeField.Add(actorAgeLabel);
            actorAgeField.Add(actorAge);

            extensionContainer.Add(actorAgeField);

            #endregion

            #region PORTRAIT

            var portraitField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var portraitLabel = IEGraphUtility.CreateLabel("Portrait");
            portraitLabel.style.fontSize = 14;
            portraitLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            portraitLabel.style.paddingRight = 10f;
            portraitLabel.style.minWidth = 120f;
            portraitLabel.style.maxWidth = 120f;

            //Object
            var clanBanner = IEGraphUtility.CreateObjectField(typeof(Sprite));
            clanBanner.value = Portrait;
            clanBanner.AddClasses("ide-node__object-field");

            clanBanner.RegisterValueChangedCallback(e =>
            {
                SetDirty();
                if (GraphWindow.CurrentDatabase.actorRegistry.Any(a => a.Portrait == (Sprite)e.newValue))
                {
                    if (EditorUtility.DisplayDialog("Same Portrait",
                            "There is another actor with the same portrait. Do you want to proceed?", "Yes",
                            "Cancel"))
                    {
                        Portrait = (Sprite)e.newValue;
                        RefreshPortrait();
                        GraphSaveUtility.SaveCurrent();
                    }
                    else
                    {
                        clanBanner.SetValueWithoutNotify(Portrait);
                    }

                    return;
                }

                Portrait = (Sprite)e.newValue;
                RefreshPortrait();
                GraphSaveUtility.SaveCurrent();
            });

            portraitField.Add(portraitLabel);
            portraitField.Add(clanBanner);
            extensionContainer.Add(portraitField);

            #endregion

            #region GENDER

            var actorGenderField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var actorGenderLabel = IEGraphUtility.CreateLabel("Gender");
            actorGenderLabel.style.fontSize = 14;
            actorGenderLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            actorGenderLabel.style.paddingRight = 10f;
            actorGenderLabel.style.minWidth = 120f;
            actorGenderLabel.style.maxWidth = 120f;

            //Dropdown
            var actorGender = IEGraphUtility.CreateDropdown(new[] { "Male", "Female" });

            actorGender.index = (int)Gender;

            var dropdownChild = actorGender.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            actorGender.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                Gender = actorGender.index switch
                {
                    0 => Actor.IGender.Male,
                    1 => Actor.IGender.Female,
                    _ => Actor.IGender.Male
                };
                GraphSaveUtility.SaveCurrent();
            });
            actorGender.style.marginLeft = 0f;
            actorGender.style.marginBottom = 1f;
            actorGender.style.marginTop = 1f;
            actorGender.style.marginRight = 3f;
            actorGender.AddClasses("ide-node__gender-dropdown-field");

            actorGenderField.Add(actorGenderLabel);
            actorGenderField.Add(actorGender);

            extensionContainer.Add(actorGenderField);

            #endregion

            #region STATE

            var stateField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var stateLabel = IEGraphUtility.CreateLabel("Actor State");
            stateLabel.style.fontSize = 14;
            stateLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            stateLabel.style.paddingRight = 10f;
            stateLabel.style.minWidth = 120f;
            stateLabel.style.maxWidth = 120f;

            //Dropdown
            var state = IEGraphUtility.CreateDropdown(new[] { "Active", "Passive" });

            state.index = (int)State;

            var stateChild = state.GetChild<VisualElement>();
            stateChild.SetPadding(5);
            stateChild.style.paddingLeft = 10;
            stateChild.style.paddingRight = 10;
            stateChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

            state.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                State = (Actor.IState)state.index;
                var clanMemberData = GraphWindow.CurrentDatabase.nodeDataList.OfType<ClanMemberData>()
                    .FirstOrDefault(a => a.ActorID == ID);
                if (state.index == 0)
                {
                    if (clanMemberData != null)
                    {
                        clanMemberData.RoleID = string.Empty;
                    }
                }

                GraphSaveUtility.SaveCurrent();
            });
            state.style.marginLeft = 0f;
            state.style.marginBottom = 1f;
            state.style.marginTop = 1f;
            state.style.marginRight = 3f;
            state.AddClasses("ide-node__gender-dropdown-field");

            stateField.Add(stateLabel);
            stateField.Add(state);

            extensionContainer.Add(stateField);

            #endregion

            #region CULTURE

            if (GraphWindow.CurrentDatabase.culturalProfiles.Any())
            {
                var actorCultureField = new VisualElement
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        alignItems = new StyleEnum<Align>(Align.Center)
                    }
                };

                var actorCultureLabel = IEGraphUtility.CreateLabel("Culture");
                actorCultureLabel.style.fontSize = 14;
                actorCultureLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                actorCultureLabel.style.paddingRight = 10f;
                actorCultureLabel.style.minWidth = 120f;
                actorCultureLabel.style.maxWidth = 120f;

                //Dropdown
                var cultureList = GraphWindow.CurrentDatabase.culturalProfiles.OrderBy(a => a.ID)
                    .Select(a => new { a.ID, a.CultureName }).ToDictionary(t => t.ID, t => t.CultureName);

                var actorCulture = IEGraphUtility.CreateDropdown(null);
                actorCulture.choices = new List<string>(cultureList.Values);
                actorCulture.choices.Insert(0, "NULL");

                var index = cultureList.Keys.ToList().IndexOf(CultureID);
                actorCulture.index = index == -1 ? 0 : index + 1;

                var actorCultureChild = actorCulture.GetChild<VisualElement>();
                actorCultureChild.SetPadding(5);
                actorCultureChild.style.paddingLeft = 10;
                actorCultureChild.style.paddingRight = 10;
                actorCultureChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

                actorCulture.RegisterCallback<ChangeEvent<string>>(_ =>
                {
                    if (actorCulture.index < 1)
                    {
                        CultureID = string.Empty;
                        GraphSaveUtility.SaveCurrent();
                        return;
                    }

                    CultureID = cultureList.ElementAt(actorCulture.index - 1).Key;
                    GraphSaveUtility.SaveCurrent();
                });

                actorCulture.style.marginLeft = 0f;
                actorCulture.style.marginBottom = 1f;
                actorCulture.style.marginTop = 1f;
                actorCulture.style.marginRight = 3f;
                actorCulture.AddClasses("ide-node__gender-dropdown-field");

                actorCultureField.Add(actorCultureLabel);
                actorCultureField.Add(actorCulture);

                extensionContainer.Add(actorCultureField);
            }

            #endregion

            #region ISPLAYER

            var isPlayerField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var isPlayerLabel = IEGraphUtility.CreateLabel("Is Player");
            isPlayerLabel.style.fontSize = 14;
            isPlayerLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            isPlayerLabel.style.paddingRight = 10f;
            isPlayerLabel.style.minWidth = 120f;
            isPlayerLabel.style.maxWidth = 120f;

            //Dropdown
            var isPlayer = IEGraphUtility.CreateToggle(null);
            isPlayer.value = IsPlayer;
            isPlayer.SetPadding(5);

            isPlayer.RegisterValueChangedCallback(_ =>
            {
                IsPlayer = isPlayer.value;
                GraphSaveUtility.SaveCurrent();
            });

            isPlayer.style.marginLeft = 0f;
            isPlayer.style.marginBottom = 1f;
            isPlayer.style.marginTop = 1f;
            isPlayer.style.marginRight = 3f;

            isPlayerField.Add(isPlayerLabel);
            isPlayerField.Add(isPlayer);

            extensionContainer.Add(isPlayerField);

            #endregion

            RefreshExpandedState();
        }

        private void RefreshPortrait()
        {
            if (Portrait != null)
            {
                portrait.style.backgroundImage = new StyleBackground(Portrait);
                portrait.style.unityBackgroundImageTintColor = NullUtils.HTMLColor("#FFFFFF");
            }
            else
            {
                var texture = (Texture2D)EditorGUIUtility.Load("Nullframes/Icons/portrait.png");
                portrait.style.backgroundImage = new StyleBackground(texture);
                portrait.style.unityBackgroundImageTintColor = NullUtils.HTMLColor("#484040");
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                n is ClanMemberData clanMemberData && clanMemberData.ActorID == ID);

            GraphWindow.CurrentDatabase.nodeDataList.RemoveAll(n =>
                n is FamilyMemberData familyMemberData && familyMemberData.ActorID == ID);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.InsertAction(0, "Go to Family", _ =>
            {
                var groupId = GraphWindow.CurrentDatabase.nodeDataList.FirstOrDefault(n =>
                    n is FamilyMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == ID);
                if (groupId != null)
                {
                    GraphWindow.instance.GotoFamilyMember(groupId.GroupId, ID);
                }
            });

            evt.menu.InsertAction(1, "Go to Clan", _ =>
            {
                var groupId = GraphWindow.CurrentDatabase.nodeDataList.FirstOrDefault(n =>
                    n is ClanMemberData fData && !string.IsNullOrEmpty(n.GroupId) && fData.ActorID == ID);
                if (groupId != null)
                {
                    GraphWindow.instance.GotoClanMember(groupId.GroupId, ID);
                }
            });
        }
    }
}