using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class FamilyGroup : IGroup
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

        public void Init(string groupName, Vector2 position, string description, string cultureId, List<string> policies, Sprite emblem,
            IEGraphView graphView)
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
            style.backgroundColor = new Color(0.1226415f, 0.1226415f, 0.1226415f, 0.3843137f);
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

            var titleLabel = IEGraphUtility.CreateLabel("Family Parameters");
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

            var familyNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var familyLabel = IEGraphUtility.CreateLabel("Name");
            familyLabel.style.fontSize = 14;
            familyLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            familyLabel.style.paddingRight = 10f;
            familyLabel.style.minWidth = 100f;
            familyLabel.style.maxWidth = 100f;

            //TextField
            var familyName = IEGraphUtility.CreateTextField(title);
            familyName.RegisterCallback<MouseEnterEvent>(_ => { familyName.tooltip = title?.LocaliseText(); });
            familyName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(familyName.value))
                {
                    familyName.value = OldTitle;
                    return;
                }

                var lst = (from FamilyGroup element in _graphView.graphElements.Where(e => e is FamilyGroup)
                    select element.title).ToList();
                var exists = lst.Contains(familyName.value);
                if (exists)
                {
                    familyName.value = OldTitle;
                    return;
                }

                title = familyName.value;
                OldTitle = title;
                titleTextField.value = title;
                _titleLabel.text = title;
                GraphSaveUtility.SaveCurrent();
            });
            familyName.AddClasses("ide-node__text-field-family");

            familyNameField.Add(familyLabel);
            familyNameField.Add(familyName);
            scrollView.Add(familyNameField);

            #endregion

            #region DESC

            var familyDescriptionField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var familyDescLabel = IEGraphUtility.CreateLabel("Description");
            familyDescLabel.style.fontSize = 14;
            familyDescLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            familyDescLabel.style.paddingRight = 10f;
            familyDescLabel.style.minWidth = 100f;
            familyDescLabel.style.maxWidth = 100f;

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
            var familyDesc = IEGraphUtility.CreateTextArea(Description);
            familyDesc.style.minHeight = 50f;
            familyDesc.AddClasses("ide-node__text-field-family");

            familyDesc.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (familyDesc.value.Length < 60) familyDesc.tooltip = Description?.LocaliseText();
            });

            familyDesc.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Description == familyDesc.value) return;
                Description = familyDesc.value;
                GraphSaveUtility.SaveCurrent();
            });

            scrollField.Add(familyDesc);

            familyDescriptionField.Add(familyDescLabel);
            familyDescriptionField.Add(scrollField);
            scrollView.Add(familyDescriptionField);

            #endregion

            #region BANNER

            var familyBannerField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var familyBannerLabel = IEGraphUtility.CreateLabel("Banner");
            familyBannerLabel.style.fontSize = 14;
            familyBannerLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            familyBannerLabel.style.paddingRight = 10f;
            familyBannerLabel.style.minWidth = 100f;
            familyBannerLabel.style.maxWidth = 100f;

            //TextField
            var familyBanner = IEGraphUtility.CreateObjectField(typeof(Sprite));
            familyBanner.value = Emblem;
            familyBanner.AddClasses("ide-node__object-field");

            familyBanner.RegisterValueChangedCallback(e =>
            {
                bannerElement.style.backgroundImage = new StyleBackground((Sprite)e.newValue);
                Emblem = (Sprite)e.newValue;
                GraphSaveUtility.SaveCurrent();
            });

            familyBannerField.Add(familyBannerLabel);
            familyBannerField.Add(familyBanner);
            scrollView.Add(familyBannerField);

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
            foreach (var policy in GraphWindow.CurrentDatabase.policyCatalog.Where(p => p.Type is PolicyType.Generic or PolicyType.Family))
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