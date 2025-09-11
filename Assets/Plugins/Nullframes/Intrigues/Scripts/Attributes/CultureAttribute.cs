using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CultureAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(CultureAttribute))]
    public class CultureDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IM.Exists && IM.DatabaseExists)
            {
                var list = IM.IEDatabase.culturalProfiles.OrderBy(c => c.CultureName)
                    .Select(g => new { g.ID, g.CultureName }).ToDictionary(c => c.ID, c => c.CultureName);
                if (list.Count == 0) return;

                var dict = new Dictionary<string, string> { { string.Empty, "NULL" } };
                foreach (var c in list)
                {
                    dict.Add(c.Key, c.Value);
                }

                var index = dict.Keys.ToList().IndexOf(property.stringValue);
                index = index == -1 ? 0 : index;
                
                EditorGUI.BeginChangeCheck();
                var selectedIndex = EditorGUI.Popup(position, property.displayName, index, dict.Values.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    property.stringValue = dict.ElementAt(selectedIndex).Key;
                }
            }
            else
            {
                EditorGUI.LabelField(position, $"[{property.displayName}]: IM not found. Please add an IM to the scene.");
            }
        }
    }
#endif
}