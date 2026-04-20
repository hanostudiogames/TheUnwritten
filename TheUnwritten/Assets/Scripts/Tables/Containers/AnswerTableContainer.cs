using Tables.Records;
using UnityEngine;

namespace Tables.Containers
{
    public class AnswerTableContainer : TableContainer<AnswerTableContainer, AnswerTable>
    {
        public AnswerRecord GetRecord(int id)
        {
            var records = _table?.AnswerRecords;
            if (records == null)
                return null;

            for (int i = 0; i < records.Length; ++i)
            {
                var record = records[i];
                if(record == null)
                    continue;

                var answerEntry = record.AnswerEntry;
                if(answerEntry == null)
                    continue;
                
                if(answerEntry.Id == id)
                    return record;
            }
            
            return null;
        }
    }
}

