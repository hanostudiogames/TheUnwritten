using UnityEngine;

using Common;
using Common.Models;

namespace Data
{
    [CreateAssetMenu(fileName = "InitializeData", menuName = "Scriptable Objects/InitializeData")]
    public class InitializeData : ScriptableObject
    {
        public FoodEntry[] FoodEntries = null;
        public ResourceEntry[] ResourceEntries = null;
    }
}
