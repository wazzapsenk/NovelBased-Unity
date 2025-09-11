using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class RoleAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RoleAttribute))]
    public class RoleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IM.Exists && IM.DatabaseExists)
            {
                var list = IM.IEDatabase.roleDefinitions.OrderBy(c => c.RoleName)
                    .Select(g => new { g.ID, g.RoleName }).ToDictionary(c => c.ID, c => c.RoleName);
                if (list.Count == 0) return;

                var dict = new Dictionary<string, string> { { string.Empty, "NULL" } };
                foreach (var c in list)
                {
                    dict.Add(c.Key, c.Value);
                }

                var index = dict.Keys.ToList().IndexOf(property.stringValue);
                index = index == -1 ? 0 : index;

                bool isMissing = !string.IsNullOrEmpty(property.stringValue) && index == 0;

                EditorGUI.BeginChangeCheck();
                var selectedIndex = EditorGUI.Popup(position, isMissing ? $"{property.displayName} (Missing)" : property.displayName, index, dict.Values.ToArray());
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