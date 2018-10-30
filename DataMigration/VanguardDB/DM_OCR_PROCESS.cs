using System;

namespace DataMigration.VanguardDB
{
    public class DmOcrProcess
    {
        public long DmcId { get; set; }

        public int OcrState { get; set; }

        public int Status { get; set; }

        public short ErrorCount { get; set; }

        public string LastError { get; set; }

        public DateTime? StartDate { get; set; }

        public int Priority { get; set; }

        public byte IndexingStatus { get; set; }

        public bool Queued { get; set; }

    }
}
