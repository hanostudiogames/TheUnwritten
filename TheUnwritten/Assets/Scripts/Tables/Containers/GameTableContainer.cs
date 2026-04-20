using Data;

using Tables.Records;

namespace Tables.Containers
{
    public class GameTableContainer : TableContainer<GameTableContainer, GameTable>
    {
        protected override void Initialize()
        {
            base.Initialize();
        }

        public TurnRecord GetTurnRecord(int turn)
        {
            var turnRecords = _table?.TurnRecords;
            if (turnRecords == null)
                return null;
            
            for (int i = 0; i < turnRecords.Length; i++)
            {
                var turnData = turnRecords[i];
                if (turnData == null)
                    continue;

                if (turnData.Turn == turn)
                    return turnData;
            }

            return null;
        }
    }
}

