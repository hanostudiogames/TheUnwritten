

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
        
        public bool HasNextAct(int act, out int nextAct)
        {
            nextAct = -1;

            var actRecords = _table?.ActRecords;
            if (actRecords == null)
                return false;
    
            var sorted = actRecords
                .Where(x => x != null)
                .OrderBy(x => x.Index)
                .ToArray();

            for (int i = 0; i < sorted.Length; ++i)
            {
                var record = sorted[i];
                if(record == null)
                    continue;
                
                if (record.Index > act)
                {
                    nextAct = record.Index;
                    return true;
                }
            }

            return false;
        }
        
        public bool HasNextScene(int act, int scene, out int nextScene)
        {
            nextScene = -1;

            var sceneRecords = GetActRecord(act)?.SceneRecords;
            if (sceneRecords == null)
                return false;

            var sorted = sceneRecords
                .Where(x => x != null)
                .OrderBy(x => x.Index)
                .ToArray();
            
            for (int i = 0; i < sorted.Length; ++i)
            {
                var record = sorted[i];
                if(record == null)
                    continue;
                
                if (record.Index > scene)
                {
                    nextScene = record.Index;
                    return true;
                }
            }

            return false;
        }
    }
}

