using Microsoft.Toolkit.Uwp.Notifications;

namespace CleanDownloadFolder.Helpers
{
    public static class Notifier
    {
        private const string NotifyTitle = "Ultimate Clean Folder";

        public static void Notify(string content)
        {
            var notification = new ToastContentBuilder()
            .AddToastActivationInfo("app-defined-string", ToastActivationType.Foreground)
            .AddAttributionText(content)
            .AddText(NotifyTitle);

            notification.Show();
        }
    }
}
