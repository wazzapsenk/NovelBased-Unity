using UnityEngine.UIElements;

namespace Nullframes.Intrigues.Graph.Nodes
{
    public class CommentRightToLeftNode : INode
    {
        protected override string DOCUMENTATION => "https://www.wlabsocks.com/wiki/index.php/Comment_Node";

        public string Note;

        protected override void OnOutputInit() { }

        public override void Init(IEGraphView ieGraphView)
        {
            base.Init(ieGraphView);

            extensionContainer.AddClasses("uis-comment-rl-extension");
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

            comment.AddClasses("uis-comment-right-to-left");

            extensionContainer.Insert(0, comment);

            extensionContainer.parent.RemoveAt(0);

            RefreshExpandedState();
        }
    }
}