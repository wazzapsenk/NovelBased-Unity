using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class SchemesPanelNavigator : IIEditor, INavigator {
        public Actor actor { get; set; }
        public ActiveSchemeRefs ActiveSchemeRefs;

        public Button CloseButton;
        public Button PreviousButton;

        public UnityEvent isEmpty;
        public UnityEvent isNotEmpty;

        public UnityEvent onPageOpened;
        public UnityEvent onPageChanged;
        public UnityEvent onPageClosed;

        public void Close(bool withoutNotification = false) {
            Destroy(gameObject);
            if(!withoutNotification)
                onPageClosed?.Invoke();
        }

        public void Show() {
            gameObject.SetActive(true);
        }

        public void Hide() {
            gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SchemesPanelNavigator))]
    public class SchemesPanelNavigator_Editor : Editor {

        SerializedProperty isEmpty;
        SerializedProperty isNotEmpty;
        SerializedProperty onPageOpened;
        SerializedProperty onPageChanged;
        SerializedProperty onPageClosed;

        private void OnEnable() {
            isEmpty = serializedObject.FindProperty("isEmpty");
            isNotEmpty = serializedObject.FindProperty("isNotEmpty");

            onPageOpened = serializedObject.FindProperty("onPageOpened");
            onPageChanged = serializedObject.FindProperty("onPageChanged");
            onPageClosed = serializedObject.FindProperty("onPageClosed");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(isEmpty, new GUIContent("Is Empty - Active Schemes"));
            EditorGUILayout.PropertyField(isNotEmpty, new GUIContent("Is Not Empty - Active Schemes"));

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(onPageOpened);
            EditorGUILayout.PropertyField(onPageChanged);
            EditorGUILayout.PropertyField(onPageClosed);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}