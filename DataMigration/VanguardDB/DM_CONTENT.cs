using System;

namespace DataMigration.VanguardDB
{
    public class DmContent
    {
        public long DmId { get; set; }
        public short DmcPagecount { get; set; }

        public string DmcPath { get; set; }

        public string DmcType { get; set; }

        public bool DmcPi { get; set; }

        public byte XId { get; set; }

        public long DmcId { get; set; }

        public short? DmcVer { get; set; }

        public DateTime? DmcVerDate { get; set; }

        public byte? DmStorageId { get; set; }

        public short? DmContentTypeId { get; set; }

        public long? DmcSize { get; set; }
      
        public string DmcFormat { get; set; }

        public int? DmcStorageId { get; set; }

        public string DmcContent { get; set; }

        public string DmcStateType { get; set; }

        public int? DmcConfigId { get; set; }

        public bool? IsCommitted { get; set; }
        public string DisplayName { get; set; }

    }

}
