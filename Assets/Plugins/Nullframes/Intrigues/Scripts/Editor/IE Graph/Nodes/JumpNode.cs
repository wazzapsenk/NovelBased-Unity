using System.Collections.Generic;
using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class JumpNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Go_To_Node";

        public string LinkID { get; set; }
        protected Port outputPort;

        protected override void OnOutputInit() {
            AddOutput("");
        }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            mainContainer.AddClasses("uis-green-node");
        }

        private void Go()
        {
            var element = graphView.graphElements.OfType<INode>().FirstOrDefault(n => n.ID == LinkID);
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

            var titleLabel = new Label()
            {
                text = "Go To"
            };

            titleLabel.AddClasses("ide-node__label");
            
            mainContainer.parent.RegisterCallback<MouseOverEvent>(_ => {
                foreach (var edge in outputPort.connections) {
                    edge.Show();
                }
            });
            
            mainContainer.parent.RegisterCallback<MouseOutEvent>(_ => {
                foreach (var edge in outputPort.connections) {
                    edge.Hide();
                }
            });

            //Dropdown
            var linkDict = graphView.graphElements.OfType<LinkedNode>().Select(a => new { a.ID, a.Name })
                .ToDictionary(d => d.ID, d => d.Name);
            
            var linkList = IEGraphUtility.CreateDropdown(null);
            linkList.style.minWidth = 120f;

            linkList.choices = new List<string>(linkDict.Values);

            linkList.choices.Insert(0, "NULL");

            var index = linkDict.Keys.ToList().IndexOf(LinkID);
            linkList.index = index == -1 ? 0 : index + 1;

            var dropdownChild = linkList.GetChild<VisualElement>();
            dropdownChild.SetPadding(5);
            dropdownChild.style.paddingLeft = 10;
            dropdownChild.style.paddingRight = 10;
            dropdownChild.GetChild<TextElement>().style.color = new StyleColor(NullUtils.HTMLColor("#FFFFFF"));

            linkList.RegisterCallback<MouseDownEvent>(_ =>
            {
                linkDict = graphView.graphElements.OfType<LinkedNode>().Select(a => new { a.ID, a.Name })
                    .ToDictionary(d => d.ID, d => d.Name);

                linkList.choices = new List<string>(linkDict.Values);
                linkList.choices.Insert(0, "NULL");

                index = linkDict.Keys.ToList().IndexOf(LinkID);
                linkList.index = index == -1 ? 0 : index + 1;
            });

            linkList.RegisterCallback<ChangeEvent<string>>(_ =>
            {
                if (linkList.index < 1)
                {
                    LinkID = string.Empty;
                    
                    Connect();

                    GraphSaveUtility.SaveCurrent();
                    return;
                }

                LinkID = linkDict.Keys.ElementAt(linkList.index - 1);
                
                Connect();

                GraphSaveUtility.SaveCurrent();
            });
            linkList.style.marginLeft = 0f;
            linkList.style.marginBottom = 1f;
            linkList.style.marginTop = 1f;
            linkList.style.marginRight = 3f;
            linkList.AddClasses("ide-node__culture-dropdown-field");

            titleContainer.Insert(0, titleLabel);
            titleContainer.Insert(1, linkList);

            var input = this.CreatePort("Link", typeof(bool), Orientation.Horizontal, Direction.Input,
                Port.Capacity.Multi);
            input.portColor = STATIC.GreenPort;
            inputContainer.Add(input);
            
            foreach (var outputData in Outputs)
            {
                outputPort = this.CreatePort(outputData.Name, Port.Capacity.Multi);
                outputPort.portColor = STATIC.GreenPort;
                outputPort.userData = outputData;
                outputContainer.Add(outputPort);
                outputPort.Disable();
            }

            Connect();

            RefreshExpandedState();
        }

        private void Connect() {
            var ed = outputPort.connections.FirstOrDefault();
            if (ed != null) {
                var l = ((LinkedNode)ed.input.node);
                outputPort.DisconnectAll();
                l.inputPort.DisconnectAll();
                graphView.RemoveElement(ed);
                
                children.Remove(l);
                l.parents.Remove(this);
            }

            var linkedNode = graphView.graphElements.OfType<LinkedNode>().FirstOrDefault(n => n.ID == LinkID);
            if (linkedNode != null) {
                var edge = outputPort.ConnectTo(linkedNode.inputPort);
                graphView.AddElement(edge);

                linkedNode.parent.Add(this);
                children.Add(linkedNode);
                
                edge.Hide();
                
                RefreshPorts();
            }
        }
    }
}