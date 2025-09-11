using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class CommentRuleNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Comment_Node";

        public string Note;

        public override GenericNodeType GenericType => GenericNodeType.Rule;

        protected override void OnOutputInit() { }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            extensionContainer.AddClasses("uis-comment-lr-extension");
            titleContainer.Hide();
        }

        public override void Draw()
        {
            base.Draw();
            var comment = IEGraphUtility.CreateTextArea(Note);

            comment.RegisterCallback<FocusInEvent>(_ => { comment.value = Note; });

            comment.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (Note == comment.value)
                {
                    return;
                }

                Note = comment.value;
                GraphSaveUtility.SaveCurrent();
            });

            comment.AddClasses("uis-comment-left-to-right");

            extensionContainer.Insert(0, comment);

            extensionContainer.parent.RemoveAt(0);

            RefreshExpandedState();
        }
    }
}