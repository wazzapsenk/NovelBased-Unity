using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class ClanPanelNavigator : IIEditor, INavigator {
        public Clan clan { get; set; }

        public TextMeshProUGUI ClanName;
        public TextMeshProUGUI ClanDescription;
        public Image ClanBanner;
        public TextMeshProUGUI MemberCount;
        public TextMeshProUGUI ClanCulture;
        public Image ClanCultureIcon;

        public PolicyRefs PolicyRef;

        public List<ClanCategory> clanCategories = new();

        public Button CloseButton;
        public Button PreviousButton;

        public UnityEvent isEmpty_ClanMembers;
        public UnityEvent isNotEmpty_ClanMembers;
        public UnityEvent isEmpty_Policies;
        public UnityEvent isNotEmpty_Policies;

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
    [CustomEditor(typeof(ClanPanelNavigator))]
    public class ClanPanelNavigator_Editor : Editor {

        SerializedProperty isEmpty_ClanMembers;
        SerializedProperty isNotEmpty_ClanMembers;
        SerializedProperty isEmpty_Policies;
        SerializedProperty isNotEmpty_Policies;
        
        SerializedProperty onPageOpened;
        SerializedProperty onPageChanged;
        SerializedProperty onPageClosed;

        private void OnEnable() {
            isEmpty_ClanMembers = serializedObject.FindProperty("isEmpty_ClanMembers");
            isNotEmpty_ClanMembers = serializedObject.FindProperty("isNotEmpty_ClanMembers");
            isEmpty_Policies = serializedObject.FindProperty("isEmpty_Policies");
            isNotEmpty_Policies = serializedObject.FindProperty("isNotEmpty_Policies");

            onPageOpened = serializedObject.FindProperty("onPageOpened");
            onPageChanged = serializedObject.FindProperty("onPageChanged");
            onPageClosed = serializedObject.FindProperty("onPageClosed");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(isEmpty_ClanMembers, new GUIContent("Is Empty - Clan"));
            EditorGUILayout.PropertyField(isNotEmpty_ClanMembers, new GUIContent("Is Not Empty - Clan"));
            EditorGUILayout.PropertyField(isEmpty_Policies, new GUIContent("Is Empty - Policies"));
            EditorGUILayout.PropertyField(isNotEmpty_Policies, new GUIContent("Is Not Empty - Policies"));

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(onPageOpened);
            EditorGUILayout.PropertyField(onPageChanged);
            EditorGUILayout.PropertyField(onPageClosed);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}