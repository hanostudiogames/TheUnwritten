using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

using Newtonsoft.Json;

using Infos;

namespace Repositories
{
    public interface IGameInfoRepository : IRepository
    {
        
    }
    
    public class GameInfoRepository : Repository<GameInfo>, IGameInfoRepository
    {
        protected override async UniTask OnLoadAsync()
        {
            await LoadAsync("GameInfo");
        }
    }
}

