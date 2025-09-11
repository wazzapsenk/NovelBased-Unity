using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues
{
    [CustomEditor(typeof(SignalReceiver))]
    public class SignalEditor : Editor
    {

        private SerializedProperty signals;
        private SerializedProperty reactions;

        private SignalReceiver receiver;

        private readonly int[] indexes = new int[100];

        private List<Signal> signalList;

        private void OnEnable()
        {
            reactions = serializedObject.FindProperty("reactions");

            receiver = (SignalReceiver)target;

            signalList = new List<Signal>();
            
            foreach (var signal in EditorUtilities.FindAssetsByType<Signal>())
            {
                signalList.Add(signal);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            for (int i = 0; i < reactions.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Signal", GUILayout.MaxWidth(42));
                var signalNames = new List<string> { "None" };
                signalNames.AddRange(signalList.Select(signal => signal.name));

                signalNames.Add("Create Signal...");

                Signal signalObj = (Signal)reactions.GetArrayElementAtIndex(i).FindPropertyRelative("m_signal")
                    .objectReferenceValue;
                int signalIndex = signalList.IndexOf(signalObj);
                if (signalIndex != -1)
                {
                    indexes[i] = signalIndex + 1;
                }

                indexes[i] = EditorGUILayout.Popup(new GUIContent(string.Empty), indexes[i], signalNames.ToArray(),
                    GUILayout.MaxWidth(180));
                EditorGUILayout.PropertyField(reactions.GetArrayElementAtIndex(i).FindPropertyRelative("m_event"), new GUIContent("Reaction"));
                var style = new GUIStyle()
                {
                    fixedWidth = 16,
                    fixedHeight = 16,
                };
                var remove = GUILayout.Button(EditorGUIUtility.IconContent("winbtn_mac_min_h"), style);

                if (remove)
                {
                    receiver.RemoveSignal(i);
                    Repaint();
                    continue;
                }
                GUILayout.EndHorizontal();

                if (indexes[i] == signalNames.Count - 1)
                {
                    //Create Signal
                    indexes[i] = 0;
                    var path = EditorUtility.SaveFilePanel("Create Signal", "Assets", "New Signal.asset", "asset");
                    if (string.IsNullOrEmpty(path)) return;
                    path = EditorUtilities.ToRelativePath(path);
                    
                    var obj = CreateInstance<Signal>();
                    UnityEditor.AssetDatabase.CreateAsset(obj, path);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    
                    signalList = new List<Signal>();
            
                    foreach (var signal in EditorUtilities.FindAssetsByType<Signal>())
                    {
                        signalList.Add(signal);
                    }
                    
                    reactions.GetArrayElementAtIndex(i).FindPropertyRelative("m_signal").objectReferenceValue = obj;
                    indexes[i] = signalNames.Count - 2;
                    continue;
                }

                if (indexes[i] == 0)
                {
                    reactions.GetArrayElementAtIndex(i).FindPropertyRelative("m_signal").objectReferenceValue = null;
                    continue;
                }

                //Change Signal
                reactions.GetArrayElementAtIndex(i).FindPropertyRelative("m_signal").objectReferenceValue =
                    signalList[indexes[i] - 1];
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var button = GUILayout.Button("Add Reaction", GUILayout.MaxWidth(100));
            GUILayout.EndHorizontal();

            if (button)
            {
                receiver.AddSignal();
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}