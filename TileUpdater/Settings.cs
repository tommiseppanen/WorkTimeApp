using System;

namespace TileUpdater
{
    public static class Settings
    {
        public static string FileAccessToken => "faToken";
        public static int TimeStepInMinutes => 15;
        public static int LunchBreakInMinutes => 30;
        public static int UsualDayLenghtInMinutes => 450;
        internal static DateTime UsualLunchTime => new DateTime(1, 1, 1, 11, 30, 0);
    }
}
