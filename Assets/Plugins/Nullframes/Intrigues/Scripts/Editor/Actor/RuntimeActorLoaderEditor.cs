using Nullframes.Intrigues.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace Nullframes.Intrigues.EDITOR
{
    [CustomEditor(typeof(RuntimeActorLoader))]
    public class RuntimeActorLoaderEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var style = new GUIStyle()
            {
                wordWrap = true,
                fontSize = 16,
                normal =
                {
                    textColor = NullUtils.HTMLColor(EditorGUIUtility.isProSkin ? "#e3e3e3" : "#5c5c5c")
                }
            };
            
            GUILayout.Space(10);
            GUILayout.Label("This component, when the scene it is in is activated, creates <b>GameObjects</b> for all <b>RuntimeActor</b>s found in the save file, attaches the <b>RuntimeActor</b> component, and loads them.", style);
        }
    }
}