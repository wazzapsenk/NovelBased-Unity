using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.EDITOR {
    public class NVarDrawers {
        [CustomPropertyDrawer(typeof(NInt))]
        public class NIntDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                EditorGUI.PropertyField(position, property.FindPropertyRelative("value"),
                    new GUIContent(property.displayName, icon.image));
            }
        }

        [CustomPropertyDrawer(typeof(NFloat))]
        public class NFloatDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                EditorGUI.PropertyField(position, property.FindPropertyRelative("value"),
                    new GUIContent(property.displayName, icon.image));
            }
        }

        [CustomPropertyDrawer(typeof(NBool))]
        public class NBoolDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                EditorGUI.PropertyField(position, property.FindPropertyRelative("value"),
                    new GUIContent(property.displayName, icon.image));
            }
        }

        [CustomPropertyDrawer(typeof(NObject))]
        public class NObjectDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                EditorGUI.PropertyField(position, property.FindPropertyRelative("value"),
                    new GUIContent(property.displayName, icon.image));
            }
        }

        [CustomPropertyDrawer(typeof(NString))]
        public class NStringDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                EditorGUI.PropertyField(position, property.FindPropertyRelative("value"),
                    new GUIContent(property.displayName, icon.image));
            }
        }

        [CustomPropertyDrawer(typeof(NActor))]
        public class NActorDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");
                var p = property.FindPropertyRelative("value");
                if (IM.Exists) {
                    GetActor();
                    return;
                }

                EditorRoutine.StartRoutine(() => IM.Exists, GetActor);


                void GetActor() {
                    var actor = IM.Actors.FirstOrDefault(a => a.ID == p.stringValue);
                    EditorGUI.BeginChangeCheck();
                    var selectedObject = EditorGUI.ObjectField(position,
                        new GUIContent(property.displayName, icon.image), actor, typeof(Actor), true);
                    if (EditorGUI.EndChangeCheck()) {
                        var actorObj = (Actor)selectedObject;
                        if (actorObj == null) {
                            property.FindPropertyRelative("value").stringValue = null;
                            return;
                        }

                        property.FindPropertyRelative("value").stringValue = actorObj.ID;
                    }
                }
            }
        }

        [CustomPropertyDrawer(typeof(NEnum))]
        public class NEnumDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var icon = EditorGUIUtility.IconContent("animationkeyframe");

                List<GUIContent> items = new List<GUIContent>();
                for (int i = 0; i < property.FindPropertyRelative("values").arraySize; i++) {
                    items.Add(new GUIContent(property.FindPropertyRelative("values").GetArrayElementAtIndex(i)
                        .stringValue));
                }

                if (items.Count < 1) {
                    EditorGUI.Popup(position, new GUIContent(property.displayName, icon.image), 0,
                        new[] { new GUIContent("UNIDENTIFIED") });
                    return;
                }

                EditorGUI.BeginChangeCheck();
                var selectedIndex = EditorGUI.Popup(position, new GUIContent(property.displayName, icon.image),
                    property.FindPropertyRelative("index").intValue, items.ToArray());
                if (EditorGUI.EndChangeCheck()) {
                    property.FindPropertyRelative("index").intValue = selectedIndex;
                }
            }
        }
    }
}