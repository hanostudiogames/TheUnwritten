using System;
using System.Collections.Generic;
using UnityEngine;

using Repositories;

namespace Services
{
    public interface IServiceLocator
    {
        TService Get<TService>() where TService : class;
    }
    
    public class ServiceLocator : IServiceLocator
    {
        private readonly Dictionary<Type, IService> _services = new();
        
        // private readonly IRepositoryLocator _repositoryLocator = null;
        
        public ServiceLocator(IRepositoryLocator repositoryLocator)
        {
            // _repositoryLocator = repositoryLocator;
            
            _services[typeof(GameInfoService)] = new GameInfoService(repositoryLocator.Get<GameInfoRepository>());
        }


        public TService Get<TService>() where TService : class
        {
            if (_services != null)
            {
                if (_services.TryGetValue(typeof(TService), out var service))
                    return service as TService;
            }

            return null;
        }
    }
}

