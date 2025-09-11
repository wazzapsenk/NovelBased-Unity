using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

using Nullframes.Intrigues.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class ClanCategory : IIEditor {

        [Role] public string role;

        public Button button;
        public Image portrait;

        public UnityEvent isDead;
        public UnityEvent isEmpty;
        public UnityEvent isNotEmpty;
        public bool showDeadActors;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(ClanCategory))]
    [CanEditMultipleObjects]
    public class ClanCategory_Editor : Editor {

        SerializedProperty isDead;
        SerializedProperty isEmpty;
        SerializedProperty isNotEmpty;
        SerializedProperty showDeadActors;
        SerializedProperty role;

        private void OnEnable() {
            isDead = serializedObject.FindProperty("isDead");
            isEmpty = serializedObject.FindProperty("isEmpty");
            isNotEmpty = serializedObject.FindProperty("isNotEmpty");
            showDeadActors = serializedObject.FindProperty("showDeadActors");
            role = serializedObject.FindProperty("role");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(role, new GUIContent("Role"));
            EditorGUILayout.PropertyField(showDeadActors, new GUIContent("Show Dead Actors"));
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(isDead, new GUIContent("Is Dead - Actor"));
            EditorGUILayout.PropertyField(isEmpty, new GUIContent("Is Empty - Role"));
            EditorGUILayout.PropertyField(isNotEmpty, new GUIContent("Is Not Empty - Role"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}