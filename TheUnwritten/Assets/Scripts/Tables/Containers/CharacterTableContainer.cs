using UnityEngine;
using System.Collections.Generic;

using System.Linq;
using Common;
using Common.Models;
using Tables.Records;

namespace Tables.Containers
{
    public class CharacterTableContainer : TableContainer<CharacterTableContainer, CharacterTable>
    {

        public List<CharacterRecord> CharacterRecords
        {
            get
            {
                return _table?.CharacterRecords?.ToList();
            }
        }
    }
}
