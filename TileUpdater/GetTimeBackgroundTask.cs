using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace TileUpdater
{
    public sealed class GetTimeBackgroundTask : IBackgroundTask
    {
        private const string TextElementName = "text";
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            await UpdateTimesToTile();
            deferral.Complete();
        }

        private static async Task UpdateTimesToTile()
        {
            var startTime = await GetTodaysLoginTime();
            var lenghtInMinutes = GetWorkDayLenghtInMinutes(startTime);
            var dayLenght = string.Format("{0:N2}", (lenghtInMinutes / 60.0d));
            var nextEndTime = startTime.AddMinutes(lenghtInMinutes + (LunchOver() ? Settings.LunchBreakInMinutes : 0));
            UpdateTile(startTime, nextEndTime, dayLenght);
        }

        private static async Task<DateTime> GetTodaysLoginTime()
        {
            var token = GetFileAccessToken();
            if (token == null)
                return DateTime.Now;
            return await ReadLoginTimeFromFile(token);
        }

        private static string GetFileAccessToken()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            return localSettings.Values[Settings.FileAccessToken]?.ToString();
        }

        private static async Task<DateTime> ReadLoginTimeFromFile(string token)
        {
            var file = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFileAsync(token);
            var lines = await Windows.Storage.FileIO.ReadLinesAsync(file);
            var currentDate = DateTime.Now.Date.ToString("MM/dd/yyyy");
            var time = lines.FirstOrDefault(l => l.Contains(currentDate));
            if (time == null)
                return DateTime.Now;
            return DateTime.Parse(time, CultureInfo.InvariantCulture);
        }

        private static double GetWorkDayLenghtInMinutes(DateTime startTime)
        {
            var span = DateTime.Now - startTime;
            var totalMinutes = LunchOver() ? span.TotalMinutes - Settings.LunchBreakInMinutes : span.TotalMinutes;
            //round to next 15 minutes
            return Math.Ceiling(totalMinutes / 15.0d) * 15;
        }

        private static bool LunchOver()
        {
            return DateTime.Now.TimeOfDay > Settings.UsualLunchTime.TimeOfDay;
        }

        private static void UpdateTile(DateTime startTime, DateTime nextEndTime, string dayLenght)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(false);
            updater.Clear();
            updater.Update(new TileNotification(BuildTileXml(startTime, nextEndTime, dayLenght)));
        }

        private static XmlDocument BuildTileXml(DateTime startTime, DateTime nextEndTime, string dayLenght)
        {
            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text01);
            SetElementInnerText(tileXml, 0, startTime.ToString("HH:mm"));
            SetElementInnerText(tileXml, 1, $"{nextEndTime.ToString("HH:mm")} ({dayLenght}h)");
            SetElementInnerText(tileXml, 2, "Updated " + DateTime.Now.ToString("HH:mm"));
            return tileXml;
        }

        private static void SetElementInnerText(XmlDocument document,int index, string text)
        {
            document.GetElementsByTagName(TextElementName)[index].InnerText = text;
        }
    }
}
