using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Cysharp.Threading.Tasks;

namespace Repositories
{
    public interface IRepositoryLocator
    {
        TRepository Get<TRepository>() where TRepository : class;
    }
    
    public class RepositoryLocator : IRepositoryLocator
    {
        private readonly Dictionary<Type, Repository> _repositories = new();

        public RepositoryLocator()
        {
            // Register(new GameInfoRepository());
        }

        public async UniTask InitializeAsync()
        {
            var initializers = new List<IRepositoryInitializer>();
            
            initializers.Add(Register(new GameInfoRepository()));
            
            await UniTask.WhenAll(initializers
                .Where(initializer => initializer != null)
                .Select(initializer => initializer.InitializeAsync()));
        }

        private IRepositoryInitializer Register<TRepository>(TRepository repository) where TRepository : Repository
        {
            if (repository == null)
                return null;
            
            if (_repositories == null)
                return null;
            
            _repositories[repository.GetType()] = repository;
            
            return repository;
        }

        public TRepository Get<TRepository>() where TRepository : class
        {
            if (_repositories != null)
            {
                if (_repositories.TryGetValue(typeof(TRepository), out var repository))
                    return repository as TRepository;
            }

            return null;
        }
    }
}

