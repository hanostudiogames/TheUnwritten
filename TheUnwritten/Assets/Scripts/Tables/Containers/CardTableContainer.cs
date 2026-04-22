using Data;

using Tables.Records;

namespace Tables.Containers
{
    public class CardTableContainer : TableContainer<CardTableContainer, CardTable>
    {
        protected override void Initialize()
        {
            base.Initialize();
        }

        public CardRecord GetCardRecord(int id)
        {
            var records = _table?.CardRecords;
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