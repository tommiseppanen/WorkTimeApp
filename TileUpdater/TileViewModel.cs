using System;

namespace TileUpdater
{
    internal class TileViewModel
    {
        public DateTime StartTime { get; set; }
        public DateTime UsualEndTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime NextEndTime { get; set; }
        public string Hours { get; set; }
        public string NextHours { get; set; }
    }
}
