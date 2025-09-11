using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class RuleGroup : IGroup
    {
        public void Init(string groupName, Vector2 position, IEGraphView graphView)
        {
            ID = GUID.Generate().ToString();
            title = groupName;
            OldTitle = title;

            SetPosition(new Rect(position, Vector2.zero));

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
            style.backgroundColor = new Color(0.1132075f, 0.0945176f, 0.1041736f, 0.1411765f);
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

            var titleLabel = IEGraphUtility.CreateLabel("Rule");
            titleLabel.AddClasses("ide-node__label__large");
            titleLabel.style.fontSize = 28f;

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

            var ruleNameField = new VisualElement
            {
                style =
                {
                    flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row),
                    alignItems = new StyleEnum<Align>(Align.Center)
                }
            };

            var ruleLabel = IEGraphUtility.CreateLabel("Name");
            ruleLabel.style.fontSize = 14;
            ruleLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
            ruleLabel.style.paddingRight = 10f;
            ruleLabel.style.minWidth = 100f;
            ruleLabel.style.maxWidth = 100f;

            //TextField
            var ruleName = IEGraphUtility.CreateTextField(title);
            ruleName.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (string.IsNullOrEmpty(ruleName.value))
                {
                    ruleName.value = OldTitle;
                    return;
                }

                var lst = (from RuleGroup element in _graphView.graphElements.Where(e => e is RuleGroup)
                    select element.title).ToList();
                var exists = lst.Contains(ruleName.value);
                if (exists)
                {
                    ruleName.value = OldTitle;
                    return;
                }

                title = ruleName.value;
                OldTitle = title;
                titleTextField.value = title;
                _titleLabel.text = title;
                GraphSaveUtility.SaveCurrent();
            });
            ruleName.AddClasses("ide-node__text-field-family");

            ruleNameField.Add(ruleLabel);
            ruleNameField.Add(ruleName);
            scrollView.Add(ruleNameField);

            #endregion

            var bottomContainer = new VisualElement();
            bottomContainer.AddClasses("ide-table-bottom");

            contentField.Add(scrollView);

            mainContainer.Insert(0, titleContainer);
            mainContainer.Insert(1, contentField);
            mainContainer.Insert(2, bottomContainer);

            #endregion
        }
    }
}