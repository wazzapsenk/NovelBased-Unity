using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nullframes.Intrigues.UI {
    public class ActorPanelNavigator : IIEditor, INavigator {
        public Actor actor { get; set; }

        public Image ActorPortrait;
        public TextMeshProUGUI ActorName;
        public TextMeshProUGUI ActorAge;
        public TextMeshProUGUI Relationship;
        
        public PolicyRefs PolicyRef;

        public TextMeshProUGUI RoleName;
        public Image RoleIcon;

        public TextMeshProUGUI CultureName;
        public Image CultureIcon;

        public Button ClanButton;
        public Image ClanBanner;

        public List<FamilyCategory> familyCategories;

        public SchemeRefs SchemeRef;

        public Button CloseButton;
        public Button PreviousButton;

        public UnityEvent isDead;
        public UnityEvent isEmpty_Schemes;
        public UnityEvent isNotEmpty_Schemes;
        public UnityEvent isEmpty_FamilyMembers;
        public UnityEvent isNotEmpty_FamilyMembers;
        public UnityEvent isEmpty_Policies;
        public UnityEvent isNotEmpty_Policies;

        public bool isDeadInvoked;

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
    [CustomEditor(typeof(ActorPanelNavigator))]
    public class ActorPanelNavigator_Editor : Editor {

        SerializedProperty isDead;
        SerializedProperty isEmpty_Schemes;
        SerializedProperty isNotEmpty_Schemes;
        SerializedProperty isEmpty_FamilyMembers;
        SerializedProperty isNotEmpty_FamilyMembers;
        SerializedProperty isEmpty_Policies;
        SerializedProperty isNotEmpty_Policies;

        SerializedProperty onPageOpened;
        SerializedProperty onPageChanged;
        SerializedProperty onPageClosed;

        private void OnEnable() {
            isDead = serializedObject.FindProperty("isDead");
            isEmpty_Schemes = serializedObject.FindProperty("isEmpty_Schemes");
            isNotEmpty_Schemes = serializedObject.FindProperty("isNotEmpty_Schemes");
            isEmpty_FamilyMembers = serializedObject.FindProperty("isEmpty_FamilyMembers");
            isNotEmpty_FamilyMembers = serializedObject.FindProperty("isNotEmpty_FamilyMembers");
            isEmpty_Policies = serializedObject.FindProperty("isEmpty_Policies");
            isNotEmpty_Policies = serializedObject.FindProperty("isNotEmpty_Policies");

            onPageOpened = serializedObject.FindProperty("onPageOpened");
            onPageChanged = serializedObject.FindProperty("onPageChanged");
            onPageClosed = serializedObject.FindProperty("onPageClosed");
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(isDead, new GUIContent("Is Dead - Actor"));
            EditorGUILayout.PropertyField(isEmpty_FamilyMembers, new GUIContent("Is Empty - Family"));
            EditorGUILayout.PropertyField(isNotEmpty_FamilyMembers, new GUIContent("Is Not Empty - Family"));
            EditorGUILayout.PropertyField(isEmpty_Schemes, new GUIContent("Is Empty - Available Schemes"));
            EditorGUILayout.PropertyField(isNotEmpty_Schemes, new GUIContent("Is Not Empty - Available Schemes"));
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