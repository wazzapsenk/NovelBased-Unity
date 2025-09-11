using System.Linq;
using Nullframes.Intrigues.Utils;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public abstract class IGroup : Group
    {
        public string ID { get; set; }
        protected string OldTitle { get; set; }

        protected IEGraphView _graphView;

        protected ScrollView scrollView;
        protected VisualElement contentField;
        protected VisualElement borderLine;

        public static IGroup selectedGroup;

        public override bool IsRenamable()
        {
            return false;
        }

        public virtual void OnCreated()
        {
            NDebug.Log("Group is created.");
        }

        public virtual void OnDestroy()
        {
            NDebug.Log("Group is destroyed.");
            GraphSaveUtility.RemoveGroupItem(this);
        }

        public override void UpdatePresenterPosition()
        {
            base.UpdatePresenterPosition();
            foreach (var node in containedElements.OfType<INode>())
            {
                node.SetDirty();
            }
        }
    }
}