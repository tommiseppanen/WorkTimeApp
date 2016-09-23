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
            var endTime = startTime.AddMinutes(lenghtInMinutes + (LunchOver() ? Settings.LunchBreakInMinutes : 0));
            UpdateTile(CreateViewModel(startTime, endTime, lenghtInMinutes));
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
            return time == null ? DateTime.Now : DateTime.Parse(time, CultureInfo.InvariantCulture);
        }

        private static int GetWorkDayLenghtInMinutes(DateTime startTime)
        {
            var span = DateTime.Now - startTime;
            var totalMinutes = LunchOver() ? span.TotalMinutes - Settings.LunchBreakInMinutes : span.TotalMinutes;
            return (int)Math.Ceiling(totalMinutes / Settings.TimeStepInMinutes) * Settings.TimeStepInMinutes;
        }

        private static bool LunchOver()
        {
            return DateTime.Now.TimeOfDay > Settings.UsualLunchTime.TimeOfDay;
        }

        private static void UpdateTile(TileViewModel viewModel)
        {
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(false);
            updater.Clear();
            updater.Update(new TileNotification(BuildTileXml(viewModel)));
        }

        private static TileViewModel CreateViewModel(DateTime startTime, DateTime endTime, int lenghtInMinutes)
        {
            return new TileViewModel()
            {
                StartTime = startTime,
                UsualEndTime = startTime.AddMinutes(Settings.UsualDayLenghtInMinutes+Settings.LunchBreakInMinutes),
                EndTime = endTime,
                NextEndTime = endTime.AddMinutes(Settings.TimeStepInMinutes),
                Hours = FormatToHours(lenghtInMinutes),
                NextHours = FormatToHours(lenghtInMinutes + Settings.TimeStepInMinutes)
            };
        }

        private static string FormatToHours(double minutes)
        {
            return string.Format("{0:N2}", (minutes / 60.0d));
        }

        private static XmlDocument BuildTileXml(TileViewModel viewModel)
        {
            var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text01);
            SetElementInnerText(tileXml, 0, FormatTitle(viewModel.StartTime, viewModel.UsualEndTime));
            SetElementInnerText(tileXml, 1, FormatEndTime(viewModel.EndTime, viewModel.Hours));
            SetElementInnerText(tileXml, 2, FormatEndTime(viewModel.NextEndTime, viewModel.NextHours));
            SetElementInnerText(tileXml, 3, "Updated " + DateTime.Now.ToString("HH:mm"));
            return tileXml;
        }
        private static void SetElementInnerText(XmlDocument document, int index, string text)
        {
            document.GetElementsByTagName(TextElementName)[index].InnerText = text;
        }

        private static string FormatTitle(DateTime start, DateTime end)
        {
            return $"{start.ToString("HH:mm")} - {end.ToString("HH:mm")}";
        }

        private static string FormatEndTime(DateTime end, string hours)
        {
            return $"{end.ToString("HH:mm")} ({hours}h)";
        }
    }
}
