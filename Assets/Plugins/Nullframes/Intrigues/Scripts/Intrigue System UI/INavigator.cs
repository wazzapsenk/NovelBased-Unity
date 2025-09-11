namespace Nullframes.Intrigues.UI {
    public interface INavigator {
        public void Close(bool withoutNotification = false);
        public void Show();
        public void Hide();
    }
}