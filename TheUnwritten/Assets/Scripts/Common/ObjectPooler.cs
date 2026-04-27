using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Common
{
    public interface IPoolable
    {
        string Key { get; } // (Clone) 체크 대신 사용할 고유 키
        GameObject GameObject { get; }
        bool IsActive { get; } 
        
        void Activate();   // 꺼낼 때 호출
        void Deactivate(); // 넣을 때 호출
    }

    public class ObjectPooler : Element 
    {
        private readonly Dictionary<string, Queue<IPoolable>> _queues = new();
        private readonly Dictionary<string, HashSet<IPoolable>> _sets = new();
        
        // Add: 처음 생성된 객체를 풀에 강제 등록할 때 사용 (Prewarm 용도)
        public void Add(IPoolable poolable)
        {
            if (poolable == null || string.IsNullOrEmpty(poolable.Key))
                return;

            // // 첫 등록 시에는 IsActive를 강제로 true로 만들어 Return의 방어 로직을 통과하게 함
            // // (혹은 Return 로직 중 일부를 분리해도 되지만, 이 방식이 가장 심플합니다)
            // if (!poolable.IsActive)
            //     poolable.Activate(); 

            Return(poolable);
        }

        public void Return(IPoolable poolable)
        {
            if (poolable == null || string.IsNullOrEmpty(poolable.Key))
                return;

            if (!poolable.IsActive) 
                return; 
            
            // 1. 해당 키의 관리 셋(Set) 확보
            var set = GetOrCreate(_sets, poolable.Key, () => new HashSet<IPoolable>());
    
            // 2. 중요: 이미 풀 내부에 존재하는 객체라면 중복 Enqueue를 막음
            if (set.Contains(poolable)) 
            {
                Debug.LogWarning($"[Pool] {poolable.Key} 이미 풀에 반납된 객체입니다.");
                return; 
            }

            // 3. 풀 상태 설정
            poolable.Deactivate();
            poolable.GameObject.SetActive(false);
            poolable.GameObject.transform.SetParent(transform);

            // 4. 데이터 등록
            set.Add(poolable);
            
            var queue = GetOrCreate(_queues, poolable.Key, () => new Queue<IPoolable>());
            queue.Enqueue(poolable);
        }

        // 중복(추가) 생성을 지원하는 Get 메서드
        public TPoolable Get<TPoolable>(string key, Transform rootTr = null) where TPoolable : class, IPoolable
        {
            // 1. 풀에 대기 중인 객체가 있다면 꺼내기
            if (!_queues.TryGetValue(key, out var queue)
                || queue.Count <= 0)
                return null;

            while (queue.Count > 0)
            {
                var poolable = queue.Dequeue() as TPoolable;
        
                // 1. 타입 캐스팅 실패나 null 체크
                if (poolable == null) continue;

                // 2. [핵심] 이미 활성화된 객체라면 누군가 풀 밖에서 쓰고 있다는 뜻 -> 버리고 다음 것 확인
                if (poolable.IsActive)
                {
                    Debug.LogWarning($"[Pool] {key} 객체가 활성화 상태로 큐에 있었습니다. 건너멉니다.");
                    continue;
                }

                // 3. HashSet에서 안전하게 제거
                if (_sets.TryGetValue(key, out var set))
                    set.Remove(poolable);

                // 4. 위치 및 부모 설정
                if (rootTr != null) 
                    poolable.GameObject.transform.SetParent(rootTr);

                // 5. 활성화 및 상태 변경 (순서 중요: Active 상태가 먼저 되어야 함)
                poolable.GameObject.SetActive(true);
                poolable.Activate(); 
            
                return poolable;
            }

            return null;
        }

        private TValue GetOrCreate<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
        {
            if (!dict.TryGetValue(key, out var value))
                dict[key] = value = factory();
            
            return value;
        }
    }
}