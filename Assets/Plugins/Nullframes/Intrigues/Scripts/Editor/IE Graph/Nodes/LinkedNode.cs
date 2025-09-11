using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class LinkedNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Go_To_Node";

        public string Name;

        public Port inputPort;
        
        protected override void OnOutputInit()
        {
            AddOutput("Go To");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);
            
            mainContainer.AddClasses("uis-forest-node");
        }
        
        private void Go()
        {
            var element = graphView.graphElements.OfType<JumpNode>().FirstOrDefault(n => n.LinkID == ID);
            if (element == null) return;
            graphView.Fit(e => e == element);
            graphView.AddToSelection(element);
        }

        public override void Draw()
        {
            base.Draw();

            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.InsertAction(0, "Go", _ =>
                {
                    Go();
                });
            }));
            
            RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount > 1)
                {
                    Go();
                }
            });
            
            mainContainer.parent.RegisterCallback<MouseOverEvent>(_ => {
                foreach (var edge in inputPort.connections) {
                    edge.Show();
                }
            });
            
            mainContainer.parent.RegisterCallback<MouseOutEvent>(_ => {
                foreach (var edge in inputPort.connections) {
                    edge.Hide();
                }
            });
            
            var titleLabel = IEGraphUtility.CreateLabel("Link");
            titleLabel.AddClasses("ide-node__label");

            var nameField = IEGraphUtility.CreateTextField(Name);

            nameField.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Name == nameField.value) return;
                Name = nameField.value;
                GraphSaveUtility.SaveCurrent();
            });

            nameField.AddClasses("uis-linked-text-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, nameField);
            
            inputPort = this.CreatePort("", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            inputPort.portColor = STATIC.GreenPort;
            inputPort.Disable();
            inputContainer.Add(inputPort);

            foreach (var outputData in Outputs)
            {
                var cPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                cPort.portColor = STATIC.GreenPort;
                cPort.userData = outputData;
                outputContainer.Add(cPort);
            }

            RefreshExpandedState();
        }
    }
}