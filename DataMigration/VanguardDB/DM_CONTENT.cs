using System;

namespace DataMigration.VanguardDB
{
    public class DM_CONTENT
    {
        public long DM_ID { get; set; }
        public short DMC_PAGECOUNT { get; set; }

        public string DMC_PATH { get; set; }

        public string DMC_TYPE { get; set; }

        public bool DMC_PI { get; set; }

        public byte X_ID { get; set; }

        public long DMC_ID { get; set; }

        public short? DMC_VER { get; set; }

        public DateTime? DMC_VER_DATE { get; set; }

        public byte? DM_STORAGE_ID { get; set; }

        public short? DM_CONTENT_TYPE_ID { get; set; }

        public long? DMC_SIZE { get; set; }
      
        public string DMC_FORMAT { get; set; }

        public int? DMC_STORAGE_ID { get; set; }

        public string DMC_CONTENT { get; set; }

        public string DMC_STATE_TYPE { get; set; }

        public int? DMC_CONFIG_ID { get; set; }

        public bool? IS_COMMITTED { get; set; }
        public string DISPLAY_NAME { get; set; }

    }

}
