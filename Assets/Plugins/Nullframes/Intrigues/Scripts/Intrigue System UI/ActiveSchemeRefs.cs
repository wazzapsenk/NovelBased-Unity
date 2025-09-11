using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class ActiveSchemeRefs : IIEditor {
        public TextMeshProUGUI SchemeName;
        public TextMeshProUGUI SchemeDescription;
        public Image SchemeIcon;
        public TextMeshProUGUI SchemeObjective;

        public Button ConspiratorButton;
        public Image ConspiratorPortrait;

        public Button TargetButton;
        public Image TargetPortrait;
        
        public UnityEvent targetIsNull;
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(ActiveSchemeRefs))]
    public class ActiveSchemeRefs_Editor : Editor {

        SerializedProperty targetIsNull;

        private void OnEnable() {
            targetIsNull = serializedObject.FindProperty("targetIsNull");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(targetIsNull, new GUIContent("Target Is Null - Active Schemes"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}