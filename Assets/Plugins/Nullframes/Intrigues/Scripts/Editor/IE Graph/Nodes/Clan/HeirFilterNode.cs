using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class HeirFilterNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Heir_Filter_Node";

        public override bool IsGroupable()
        {
            return false;
        }

        public override GenericNodeType GenericType => GenericNodeType.Clan;

        private Button genderToggleBtn;
        private Button clanToggleBtn;
        private Button ageToggleButton;
        public Dictionary<Button, int> relativeButtons;

        public string FilterName;
        public int Gender;
        public int Clan;
        public int Age;
        public List<int> Relatives { get; set; } = new();

        protected override void OnOutputInit() { }

        public override void Draw()
        {
            base.Draw();
            RemoveAt(0);

            relativeButtons = new Dictionary<Button, int>();

            var root = new VisualElement
            {
                name = "root"
            };

            var rowGroup = new VisualElement
            {
                name = "boxGroup",
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    paddingTop = 5f,
                    paddingBottom = 5f
                }
            };

            var mainBox = new VisualElement
            {
                name = "mainBox",
                style =
                {
                    minWidth = 500f,
                    maxWidth = 500f,
                    minHeight = 200f,
                }
            };

            mainBox.SetPadding(15f);

            var content = new VisualElement()
            {
                name = "Content",
                style =
                {
                    height = new StyleLength(Length.Percent(100)),
                    justifyContent = new StyleEnum<Justify>(Justify.FlexStart)
                }
            };
            content.SetPadding(10f);

            #region Title_NODE

            var titleLabel = IEGraphUtility.CreateLabel("<color=#E58D62>\u25CF</color> Heir Filter");
            titleLabel.style.fontSize = 24f;

            titleLabel.AddClasses("ide-node-title-label");

            mainBox.Add(titleLabel);

            #endregion
            
            #region FilterName

            var filterName = IEGraphUtility.CreateTextField($"Filter Name: {FilterName}");
            filterName.AddClasses("ide-node-role-title-text");

            filterName.RegisterCallback<FocusOutEvent>(_ =>
            {
                var exists = graphView.graphElements.OfType<HeirFilterNode>().Any(c => c.FilterName == filterName.value);
                if (exists)
                {
                    filterName.value = FilterName;
                    return;
                }

                if (string.IsNullOrEmpty(filterName.text))
                {
                    filterName.value = FilterName;
                    return;
                }

                FilterName = filterName.value;
                GraphSaveUtility.SaveCurrent();
            });

            filterName.RegisterCallback<FocusOutEvent>(_ => { filterName.value = $"Filter Name: {FilterName}"; });

            filterName.RegisterCallback<FocusInEvent>(_ => { filterName.value = FilterName; });

            content.Add(filterName);

            #endregion

            #region Filter

            var firstField = new VisualElement();
            firstField.AddClasses("ide-node-role-legacy-field");

            genderToggleBtn = IEGraphUtility.CreateButton(null, () =>
            {
                if (Gender == 4)
                    Gender = 0;
                else
                    Gender++;

                UpdateButtons(true);
            });

            genderToggleBtn.style.fontSize = 14;

            genderToggleBtn.AddClasses("uis-filter-toggle-btn");

            ageToggleButton = IEGraphUtility.CreateButton(null, () =>
            {
                if (Age == 2)
                    Age = 0;
                else
                    Age++;

                UpdateButtons(true);
            });

            ageToggleButton.style.fontSize = 14;

            ageToggleButton.AddClasses("uis-filter-toggle-btn");
            
            clanToggleBtn = IEGraphUtility.CreateButton(null, () =>
            {
                if (Clan == 1)
                    Clan = 0;
                else
                    Clan++;

                UpdateButtons(true);
            });

            clanToggleBtn.style.fontSize = 14;

            clanToggleBtn.AddClasses("uis-filter-toggle-btn");

            firstField.Add(genderToggleBtn);
            firstField.Add(ageToggleButton);
            firstField.Add(clanToggleBtn);
            content.Add(firstField);

            #endregion

            #region RelativeField

            var relativeField = new VisualElement();
            relativeField.AddClasses("ide-node-role-legacy-field");

            for (int i = 0; i < 12; i++)
            {
                var relativeBtn = IEGraphUtility.CreateButton(null);
                relativeBtn.style.fontSize = 14;
                relativeButtons.Add(relativeBtn, Relatives.Count > 0 ? Relatives[i] : 0);
                
                relativeBtn.AddClasses("uis-filter-toggle-btn");

                relativeBtn.RegisterCallback<ClickEvent>(_ =>
                {
                    if (relativeButtons[relativeBtn] == 12)
                        relativeButtons[relativeBtn] = 0;
                    else
                        relativeButtons[relativeBtn] += 1;
                    
                    UpdateButtons(true);
                });

                relativeField.Add(relativeBtn);
            }

            content.Add(relativeField);

            #endregion

            UpdateButtons(false);

            rowGroup.Insert(0, mainBox);
            mainBox.Add(content);

            root.AddClasses("heir-filter-main-container");

            Insert(0, root);
            root.Insert(0, rowGroup);

            RefreshExpandedState();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) { }

        private void UpdateButtons(bool save)
        {
            genderToggleBtn.text = Gender switch
            {
                0 => "Gender: None",
                1 => "Gender: Male",
                2 => "Gender: Female",
                3 => "Gender: Male or Female",
                _ => "Gender: Female or Male"
            };

            ageToggleButton.text = Age switch
            {
                0 => "Age: None",
                1 => "Age: Older",
                _ => "Age: Youngest"
            };

            clanToggleBtn.text = Clan switch
            {
                0 => "Clan: Any",
                _ => "Same Clan"
            };

            foreach (var relativeButton in relativeButtons)
            {
                relativeButton.Key.text = ((RelativeFilter)relativeButton.Value).ToString();
            }

            if (save)
            {
                SetDirty();
                GraphSaveUtility.SaveCurrent();
                GraphSaveUtility.processCount--;
            }
        }
    }
}