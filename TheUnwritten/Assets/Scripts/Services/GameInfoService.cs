using UnityEngine;

using Repositories;

namespace Services
{
    public interface IGameInfoService : IService
    {
        
    }
    
    public class GameInfoService : IGameInfoService
    {
        private readonly IGameInfoRepository _gameInfoRepository = null;

        public GameInfoService(IGameInfoRepository gameInfoRepository)
        {
            _gameInfoRepository = gameInfoRepository;
        }
    }
}

