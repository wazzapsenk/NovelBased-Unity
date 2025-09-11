using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class ClanGroup : IGroup
    {
        public string CultureID;
        public string Description;
        public Sprite Emblem;
        public List<string> Policies;

        private VisualElement policyField;

        public override bool IsCopiable()
        {
            return false;
        }

        public void Init(string groupName, Vector2 position, string description, string cultureId,
            List<string> policies, Sprite emblem, IEGraphView graphView)
        {
            ID = GUID.Generate().ToString();
            title = groupName;
            OldTitle = title;
            Description = description;
            Emblem = emblem;
            CultureID = cultureId;

            SetPosition(new Rect(position, Vector2.zero));

            Policies = new List<string>();

            foreach (var policy in policies) Policies.Add(policy);

            _graphView = graphView;

            contentContainer.RegisterCallback<MouseEnterEvent>(_ => { selectedGroup = this; });

            contentContainer.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                EditorRoutine.StartRoutine(.05f, () => selectedGroup = null);
            });
        }

        public void Draw()
        {
            #region Broke

            contentContainer.SetBorderRadius(0f);
            contentContainer.SetBorderWidth(3f);
            style.backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.1843137f);
            //style.backgroundColor = Color.clear;
            var _titleContainer = this.Q<VisualElement>("titleContainer");
            var _titleLabel = this.Q<Label>("titleLabel");
            var titleTextField = this.Q<TextField>("titleField");

            //titleContainer
            _titleContainer.SetPadding(60);
            _titleContainer.style.paddingTop = 30f;
            _titleContainer.style.backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.8843137f);
            _titleContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            //titleLabel
            _titleLabel.style.color = NullUtils.HTMLColor("#B28700");
            _titleLabel.style.fontSize = 32f;
            _titleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            
            _titleContainer.RegisterCallback<MouseEnterEvent>(_ =>
            {
                _titleContainer.tooltip = title?.LocaliseText();
            });

            //titleField
            titleTextField.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                title = OldTitle;
                titleTextField.value = OldTitle;
                _titleLabel.text = OldTitle;
            });

            #endregion

            #region BORDERLINE

            borderLine = new VisualElement
            {
                style =
                {
                    minHeight = 50,
                    backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.8843137f),
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };
            borderLine.SetPadding(60);
            Insert(1, borderLine);

            #endregion

            #region TITLE

            var titleLabel = IEGraphUtility.CreateLabel("Clan Parameters");
            titleLabel.AddClasses("ide-node__label__large");
            titleLabel.style.fontSize = 28f;

            #endregion

            #region BANNER

            var bannerElement = new VisualElement
            {
                style =
                {
                    minWidth = 72,
                    minHeight = 72,
                    maxHeight = 72,
                    maxWidth = 72,
                    marginLeft = 60f,
                    marginTop = 10f,
                    backgroundImage = new StyleBackground(Emblem)
                }
            };

            #endregion

            #region SETTINGSPANEL

            var mainContainer = new VisualElement();
            borderLine.Add(mainContainer);
            mainContainer.AddClasses("ide-family-field");

            var titleContainer = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row)
                }
            };
            titleContainer.AddClasses("ide-table-title");

            titleContainer.Add(titleLabel);
            titleContainer.Add(bannerElement);

            contentField = new VisualElement();
            contentField.AddClasses("ide-table-content");

            //ScrollView
            scrollView = new ScrollView()
            {
                mode = ScrollViewMode.Vertical
            };
            scrollView.SetPadding(6);
            var dragger = scrollView.Q<VisualElement>("unity-dragger");
            dragger.style.backgroundColor = new StyleColor(NullUtils.HTMLColor("#262626"));
            dragger.SetBorderColor("#525252");

            #region NAME

            var clanNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var clanLabel = IEGraphUtility.CreateLabel("Name");
            clanLabel.style.fontSize = 14;
            clanLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            clanLabel.style.paddingRight = 10f;
            clanLabel.style.minWidth = 100f;
            clanLabel.style.maxWidth = 100f;

            //TextField
            var clanName = IEGraphUtility.CreateTextField(title);

            clanName.RegisterCallback<MouseEnterEvent>(_ => { clanName.tooltip = title?.LocaliseText(); });

            clanName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(clanName.value))
                {
                    clanName.value = OldTitle;
                    return;
                }

                var lst = (from ClanGroup element in _graphView.graphElements.Where(e => e is ClanGroup)
                    select element.title).ToList();
                var exists = lst.Contains(clanName.value);
                if (exists)
                {
                    clanName.value = OldTitle;
                    return;
                }

                title = clanName.value;
                OldTitle = title;
                titleTextField.value = title;
                _titleLabel.text = title;
                GraphSaveUtility.SaveCurrent();
            });
            clanName.AddClasses("ide-node__text-field-family");

            clanNameField.Add(clanLabel);
            clanNameField.Add(clanName);
            scrollView.Add(clanNameField);

            #endregion

            #region DESC

            var clanDescriptionField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var clanDescLabel = IEGraphUtility.CreateLabel("Description");
            clanDescLabel.style.fontSize = 14;
            clanDescLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            clanDescLabel.style.paddingRight = 10f;
            clanDescLabel.style.minWidth = 100f;
            clanDescLabel.style.maxWidth = 100f;

            var scrollField = new ScrollView()
            {
                style =
                {
                    minHeight = 50f,
                    maxHeight = 150f
                },
                mode = ScrollViewMode.Vertical
            };

            dragger = scrollField.Q<VisualElement>("unity-dragger");
            dragger.style.backgroundColor = new StyleColor(NullUtils.HTMLColor("#262626"));
            dragger.SetBorderColor("#525252");

            //TextField
            var clanDesc = IEGraphUtility.CreateTextArea(Description);
            clanDesc.style.minHeight = 50f;
            clanDesc.AddClasses("ide-node__text-field-family");

            clanDesc.RegisterCallback<MouseEnterEvent>(_ =>
            {
                clanDesc.tooltip = Description?.LocaliseText();
            });

            clanDesc.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Description == clanDesc.value) return;
                Description = clanDesc.value;
                GraphSaveUtility.SaveCurrent();
            });

            scrollField.Add(clanDesc);

            clanDescriptionField.Add(clanDescLabel);
            clanDescriptionField.Add(scrollField);
            scrollView.Add(clanDescriptionField);

            #endregion

            #region BANNER

            var clanBannerField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var clanBannerLabel = IEGraphUtility.CreateLabel("Banner");
            clanBannerLabel.style.fontSize = 14;
            clanBannerLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            clanBannerLabel.style.paddingRight = 10f;
            clanBannerLabel.style.minWidth = 100f;
            clanBannerLabel.style.maxWidth = 100f;

            //TextField
            var clanBanner = IEGraphUtility.CreateObjectField(typeof(Sprite));
            clanBanner.value = Emblem;
            clanBanner.AddClasses("ide-node__object-field");

            clanBanner.RegisterValueChangedCallback(e =>
            {
                bannerElement.style.backgroundImage = new StyleBackground((Sprite)e.newValue);
                Emblem = (Sprite)e.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            clanBannerField.Add(clanBannerLabel);
            clanBannerField.Add(clanBanner);
            scrollView.Add(clanBannerField);

            #endregion

            #region CULTURE

            var cultureList = GraphWindow.CurrentDatabase.culturalProfiles.OrderBy(a => a.ID)
                .Select(a => new { a.ID, a.CultureName }).ToDictionary(t => t.ID, t => t.CultureName);
            if (cultureList.Count > 0)
            {
                var cultureField = new VisualElement
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        alignItems = new StyleEnum<Align>(Align.Center),
                        marginTop = 3f,
                        marginBottom = 3f
                    }
                };

                var cultureLabel = IEGraphUtility.CreateLabel("Culture");
                cultureLabel.style.fontSize = 14;
                cultureLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                cultureLabel.style.paddingRight = 10f;
                cultureLabel.style.minWidth = 100f;
                cultureLabel.style.maxWidth = 100f;

                //DropDown
                var dropdown = IEGraphUtility.CreateDropdown(null);
                dropdown.choices = new List<string>(cultureList.Values);
                dropdown.choices.Insert(0, "NULL");

                var index = cultureList.Keys.ToList().IndexOf(CultureID);
                dropdown.index = index == -1 ? 0 : index + 1;

                var dropdownChild = dropdown.GetChild<VisualElement>();
                dropdownChild.SetPadding(5);
                dropdownChild.style.paddingLeft = 10;
                dropdownChild.style.paddingRight = 10;
                dropdown.style.maxWidth = 250f;
                dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#A3C396"));

                dropdown.RegisterCallback<ChangeEvent<string>>(_ =>
                {
                    if (dropdown.index < 1)
                    {
                        CultureID = string.Empty;
                        GraphSaveUtility.SaveCurrent();
                        return;
                    }
                    
                    CultureID = cultureList.ElementAt(dropdown.index - 1).Key;
                    GraphSaveUtility.SaveCurrent();
                });

                dropdown.AddClasses("ide-node__variable-dropdown-field");

                cultureField.Add(cultureLabel);
                cultureField.Add(dropdown);
                scrollView.Add(cultureField);
            }

            #endregion

            #region POLICY

            LoadPolicies();

            #endregion

            var bottomContainer = new VisualElement();
            bottomContainer.AddClasses("ide-table-bottom");

            contentField.Add(scrollView);

            mainContainer.Insert(0, titleContainer);
            mainContainer.Insert(1, contentField);
            mainContainer.Insert(2, bottomContainer);

            #endregion
        }

        private void LoadPolicies()
        {
            var policyLabelText = "Policy";
            foreach (var policy in GraphWindow.CurrentDatabase.policyCatalog.Where(p => p.Type is PolicyType.Generic or PolicyType.Clan))
            {
                policyField = new VisualElement
                {
                    style =
                    {
                        flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                        alignItems = new StyleEnum<Align>(Align.Center)
                    }
                };

                var policyLabel = IEGraphUtility.CreateLabel(policyLabelText);
                policyLabel.style.fontSize = 14;
                policyLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                policyLabel.style.paddingRight = 10f;
                policyLabel.style.minWidth = 100f;
                policyLabel.style.maxWidth = 100f;

                policyField.Add(policyLabel);

                var policyToggle = AddPolicy(policy, evt =>
                {
                    if (evt.newValue)
                        Policies.Add(policy.ID);
                    else
                        Policies.Remove(policy.ID);

                    GraphSaveUtility.SaveCurrent();
                });

                policyField.Add(policyToggle);
                policyLabelText = string.Empty;
                scrollView.Add(policyField);
            }
        }

        private Toggle AddPolicy(Policy policy, EventCallback<ChangeEvent<bool>> callback)
        {
            var policyToggle = IEGraphUtility.CreateToggle(policy.PolicyName);
            if (Policies.Contains(policy.ID)) policyToggle.value = true;

            policyToggle.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
            policyToggle.userData = policy.ID;
            
            var toggleLabel = policyToggle.GetChild<Label>();
            if (toggleLabel != null) {
                toggleLabel.style.fontSize = 12;
                toggleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                toggleLabel.style.color = NullUtils.HTMLColor("#A6A8A4");
                toggleLabel.style.minWidth = new StyleLength(StyleKeyword.Auto);
                toggleLabel.style.paddingLeft = 5f;

                policyToggle.RegisterValueChangedCallback(callback);
            }

            return policyToggle;
        }
    }
}