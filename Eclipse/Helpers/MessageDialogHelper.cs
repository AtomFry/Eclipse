using System.Windows;

namespace Eclipse.Helpers
{
    public class MessageDialogHelper
    {
        public static MessageDialogResult ShowOKCancelDialog(string text, string title)
        {
            return MessageBox.Show(text, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK
                ? MessageDialogResult.OK
                : MessageDialogResult.Cancel;
        }

        public static void ShowOKDialog(string text, string title)
        {
            MessageBox.Show(text, title, MessageBoxButton.OK);
        }
    }

    public enum MessageDialogResult
    {
        OK,
        Cancel
    }
}
