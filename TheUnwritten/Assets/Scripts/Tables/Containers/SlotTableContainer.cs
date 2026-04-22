using Data;

using Tables.Records;

namespace Tables.Containers
{
    public class SlotTableContainer : TableContainer<SlotTableContainer, SlotTable>
    {
        protected override void Initialize()
        {
            base.Initialize();
        }

        public SlotRecord GetSlotRecord(int id)
        {
            var records = _table?.SlotRecords;
            if (records == null)
                return null;

            for (int i = 0; i < records.Length; ++i)
            {
                var record = records[i];
                if(record == null)
                    continue;
                
                if (records[i].Id == id)
                    return records[i];
            }

            return null;
        }
    }
}