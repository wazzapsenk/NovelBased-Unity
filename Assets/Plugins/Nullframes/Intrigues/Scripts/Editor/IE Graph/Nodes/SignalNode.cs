using System.Collections.Generic;
using Nullframes.Intrigues.EDITOR.Utils;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class SignalNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Signal_Node";

        public Signal Signal;
        
        protected override void OnOutputInit()
        {
            AddOutput("Out");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-forest-node");
        }

        public override void Draw()
        {
            base.Draw();
            var titleLabel = new Label()
            {
                text = "Signal"
            };

            titleLabel.AddClasses("ide-node__label");

            //Dropdown
            List<Signal> signals;
            List<string> signalNames;
            
            var signalList = IEGraphUtility.CreateDropdown(null);
            signalList.style.minWidth = 120f;

            Reload();

            signalList.choices = new List<string>(signalNames);

            var dropdownChild = signalList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));
            
            signalList.RegisterCallback<MouseDownEvent>(_ =>
            {
                Reload();
            });

            void Reload()
            {
                signals = new List<Signal>();
                signalNames = new List<string>();
                foreach (var _signal in EditorUtilities.FindAssetsByType<Signal>())
                {
                    signals.Add(_signal);
                }
            
                signalNames.Add("None");
                for (int i = 0; i < signals.Count; i++)
                {
                    signalNames.Add($"{i}:{signals[i].name}");
                }

                signalNames.Add("Create Signal...");

                signalList.choices = new List<string>(signalNames);
                
                var signalIndex = signals.IndexOf(Signal);
                signalList.index = signalIndex == -1 ? 0 : signalIndex + 1;
            }

            signalList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (signalList.index == signalNames.Count - 1)
                {
                    //Create Signal
                    signalList.SetValueWithoutNotify("None");
                    var path = EditorUtility.SaveFilePanel("Create Signal", "Assets", "New Signal.asset", "asset");
                    if (string.IsNullOrEmpty(path)) return;
                    path = EditorUtilities.ToRelativePath(path);
                    
                    var obj = ScriptableObject.CreateInstance<Signal>();
                    UnityEditor.AssetDatabase.CreateAsset(obj, path);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    Signal = obj;
                    GraphSaveUtility.SaveCurrent();

                    Reload();
                    return;
                }

                if (signalList.index == 0)
                {
                    Signal = null;
                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                //Change Signal
                Signal = signals[signalList.index - 1];
                GraphSaveUtility.SaveCurrent();
            });
            signalList.style.marginLeft = 0f;
            signalList.style.marginBottom = 1f;
            signalList.style.marginTop = 1f;
            signalList.style.marginRight = 3f;
            signalList.AddClasses("ide-node__signal-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, signalList);

            var input = this.CreatePort("In", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputContainer.Add(input);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}