using System;
using UnityEngine;

using Common.Models;

namespace Infos
{
    [Serializable]
    public class GameInfo : Info
    {
        public int Turn = 0;
        
        public FoodEntry[] FoodEntries = null;
        public ResourceEntry[] ResourceEntries = null;
    }
}

