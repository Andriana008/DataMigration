using System;

namespace DataMigration.VanguardDB
{
    public class DM_OCR_PROCESS
    {
        public long DMC_ID { get; set; }

        public int OCR_STATE { get; set; }

        public int STATUS { get; set; }

        public short ERROR_COUNT { get; set; }

        public string LAST_ERROR { get; set; }

        public DateTime? START_DATE { get; set; }

        public int PRIORITY { get; set; }

        public byte INDEXING_STATUS { get; set; }

        public bool QUEUED { get; set; }

    }
}
