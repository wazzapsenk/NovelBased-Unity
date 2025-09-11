namespace Nullframes.Intrigues.UI {
    public abstract class IChoiceData : IIEditor {
        public ChoiceData ChoiceData { get; private set; }

        public void Init(ChoiceData data) {
            ChoiceData = data;
        }

        public abstract void OnDisabled();

        public abstract void OnEnabled();
    }
}
