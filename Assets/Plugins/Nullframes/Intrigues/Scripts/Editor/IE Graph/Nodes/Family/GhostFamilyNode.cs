namespace Nullframes.Intrigues.Graph.Nodes
{
    public class GhostFamilyNode : INode
    {
        protected override string DOCUMENTATION => "...";

        public override bool IsMovable()
        {
            return false;
        }

        public override bool IsSelectable()
        {
            return false;
        }
        
        protected override void OnOutputInit() { }

        public override GenericNodeType GenericType => GenericNodeType.Family;

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            visible = false;
        }

        public override void Draw()
        {
            base.Draw();

            RefreshExpandedState();
        }
    }
}