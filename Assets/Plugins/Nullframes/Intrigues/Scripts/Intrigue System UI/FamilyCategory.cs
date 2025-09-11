using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class FamilyCategory : IIEditor {

        public enum Category { 
            Parent,
            Child,
            Spouse,
            Grandparent,
            Grandchild,
            Sibling,
            Nephew,
            Niece,
            Uncle,
            Aunt,
            BrotherInLaw,
            SisterInLaw
        }

        public Category category;
        public Button button;
        public Image portrait;

        public UnityEvent isDead;
        public UnityEvent isEmpty;
        public UnityEvent isNotEmpty;
        public bool showDeadActors;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(FamilyCategory))]
    [CanEditMultipleObjects]
    public class FamilyCategory_Editor : Editor {

        SerializedProperty isDead;
        SerializedProperty isEmpty;
        SerializedProperty isNotEmpty;
        SerializedProperty category;
        SerializedProperty showDeadActors;

        private void OnEnable() {
            isDead = serializedObject.FindProperty("isDead");
            isEmpty = serializedObject.FindProperty("isEmpty");
            isNotEmpty = serializedObject.FindProperty("isNotEmpty");
            category = serializedObject.FindProperty("category");
            showDeadActors = serializedObject.FindProperty("showDeadActors");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(category, new GUIContent("Category"));
            EditorGUILayout.PropertyField(showDeadActors, new GUIContent("Show Dead Actors"));
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(isDead, new GUIContent("Is Dead - Actor"));
            EditorGUILayout.PropertyField(isEmpty, new GUIContent("Is Empty - Family"));
            EditorGUILayout.PropertyField(isNotEmpty, new GUIContent("Is Not Empty - Family"));

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}