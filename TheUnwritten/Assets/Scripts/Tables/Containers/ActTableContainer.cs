

using System.Linq;
using Tables.Records;

namespace Tables.Containers
{
    public class ActTableContainer : TableContainer<ActTableContainer, ActTable>
    {
        public SceneRecord GetSceneRecord(int act, int scene)
        {
            var sceneRecords = GetActRecord(act)?.SceneRecords;
            if (sceneRecords == null)
                return null;

            for (int i = 0; i < sceneRecords.Length; ++i)
            {
                var sceneRecord = sceneRecords[i];
                if (sceneRecord.Index == scene)
                    return sceneRecord;
            }

            return null;
        }

        public ActRecord GetActRecord(int act)
        {
            var actRecords = _table?.ActRecords;
            if (actRecords == null)
                return null;

            for (int i = 0; i < actRecords.Length; ++i)
            {
                var actRecord = actRecords[i];
                if (actRecord == null)
                    continue;
                
                if (actRecord.Index == act)
                    return actRecord;
            }

            return null;
        }
    }
}

