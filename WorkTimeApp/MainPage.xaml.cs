using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace WorkTimeApp
{
    public sealed partial class MainPage : Page
    {
        private const string TaskName = "GetTimeBackgroundTask";
        private const string TaskEntryPoint = "TileUpdater.GetTimeBackgroundTask";
        private const string DefaultFileName = "login-times";

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HandleTaskRegistration();
        }

        private async void HandleTaskRegistration()
        {
            await BackgroundExecutionManager.RequestAccessAsync();
            UnregisterOldTasks();
            RegisterTasks();
        }

        private void UnregisterOldTasks()
        {
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == TaskName)
                {
                    task.Value.Unregister(true);
                }
            }
        }

        private void RegisterTasks()
        {
            var taskBuilder = new BackgroundTaskBuilder
            {
                Name = TaskName,
                TaskEntryPoint = TaskEntryPoint
            };
            RegisterTask(taskBuilder, new SystemTrigger(SystemTriggerType.UserPresent, false));
            RegisterTask(taskBuilder, new SystemTrigger(SystemTriggerType.SessionConnected, false));
            RegisterTask(taskBuilder, new TimeTrigger(15, false));
        }

        private void RegisterTask(BackgroundTaskBuilder builder, IBackgroundTrigger trigger)
        {
            builder.SetTrigger(trigger);
            builder.Register();
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = SetUpFilePicker();
            var file = await savePicker.PickSaveFileAsync();
            if (file == null)
                return;
            StoreFileAccessToken(file);
        }

        private FileSavePicker SetUpFilePicker()
        {
            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = DefaultFileName
            };
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            return savePicker;
        }

        private void StoreFileAccessToken(StorageFile file)
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var faToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
            localSettings.Values[TileUpdater.Settings.FileAccessToken] = faToken;
        }
    }
}
