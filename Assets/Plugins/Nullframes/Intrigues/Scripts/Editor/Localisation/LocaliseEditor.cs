using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using Nullframes.Intrigues.Graph;
using UnityEditor;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Localisation.IE
{
    [CustomEditor(typeof(ILocalise))]
    public class LocaliseEditor : Editor
    {
        public override bool UseDefaultMargins()
        {
            return false;
        }

        private VisualElement root;
        private DropdownField dropdownField;

        private ILocalise localise;

        // private Label previewLabel;
        // private TextField searchField;

        private void OnEnable()
        {
            root = new VisualElement();
            localise = (ILocalise)target;

            var visualTree = (VisualTreeAsset)EditorGUIUtility.Load("Nullframes/Localisation.uxml");
            visualTree.CloneTree(root);

            var styleSheet = (StyleSheet)EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? "Nullframes/LocalisationStyles_Dark.uss" : "Nullframes/LocalisationStyles_Light.uss");
            root.styleSheets.Add(styleSheet);
        }

        private void LoadEditor()
        {
            root.SetEnabled(true);

            dropdownField = root.Q<DropdownField>("keyDropdown");
            dropdownField.choices = new List<string> { "<SELECT>" };
            if (IM.IEDatabase.localisationTexts.Any())
                dropdownField.choices.AddRange(IM.IEDatabase.localisationTexts.First().Value.Keys);

            //DropdownStyle
            var dropdownChild = dropdownField.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#FFFFFF" : "#212121"));

            dropdownField.style.marginLeft = 0f;
            dropdownField.style.marginBottom = 1f;
            dropdownField.style.marginTop = 1f;
            dropdownField.style.marginRight = 3f;
            dropdownField.AddClasses("inspector-enum-dropdown-field");

            dropdownField.SetMargin(0);

            var index = dropdownField.choices.IndexOf(localise.displayText);
            dropdownField.index = index == -1 ? 0 : index;

            dropdownField.RegisterCallback<ChangeEvent<string>>(item => DropdownChanged(item.newValue));
        }

        private void DropdownChanged(string selectedItem)
        {
            if (dropdownField.index == 0) return;
            Undo.RecordObject(target, "Language");
            localise.displayText = selectedItem;
            EditorUtility.SetDirty(target);
        }

        public override VisualElement CreateInspectorGUI()
        {
            root.SetEnabled(false);
            EditorRoutine.StartRoutine(() => IM.Exists, LoadEditor);
            Undo.undoRedoPerformed += LoadEditor;
            return root;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= LoadEditor;
            EditorApplication.update -= LoadEditor;
        }
    }
}