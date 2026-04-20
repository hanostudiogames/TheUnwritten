using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

using Newtonsoft.Json;

using Infos;

namespace Repositories
{
    public interface IRepositoryInitializer
    {
        UniTask InitializeAsync();
    }

    public interface IRepository
    {
        
    }
    
    public abstract class Repository : IRepositoryInitializer
    {
        protected string LocalFilePath
        {
            get { return Path.Combine(Application.persistentDataPath, "Infos"); }
        } 
        
        public abstract UniTask InitializeAsync();
    }
    
    public abstract class Repository<TInfo> : Repository where TInfo : Info
    {
        protected TInfo _info = null;
        
        public override async UniTask InitializeAsync()
        {
            await OnLoadAsync();
        }

        protected abstract UniTask OnLoadAsync();

        protected async UniTask LoadAsync(string fileName)
        {
            string fullPath = Path.Combine(LocalFilePath, $"{fileName}.json");

            // 3. 디렉토리가 없으면 생성 (최초 실행 대비)
            if (!Directory.Exists(LocalFilePath))
                Directory.CreateDirectory(LocalFilePath);

            // 4. 파일이 없으면 초기값(null) 설정 후 종료 (최초 실행 대비)
            if (!File.Exists(fullPath))
            {
                Debug.Log($"[Repository] 저장된 데이터가 없습니다. 새로 시작합니다: {fileName}");
                _info = null; // 필요하다면 Data = new TInfo(); 로 기본값을 생성해도 됩니다.
                return;
            }

            try
            {
                // 5. 안전하게 읽고 역직렬화
                string jsonString = await File.ReadAllTextAsync(fullPath);
                
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                };
                
                _info = JsonConvert.DeserializeObject<TInfo>(jsonString, settings);
                Debug.Log($"[Repository] {fileName} 로드 성공!");
            }
            catch (System.Exception e)
            {
                // Json 파싱 에러나 파일 잠김 등의 예외 처리
                Debug.LogError($"[Repository] {fileName} 로드 실패: {e.Message}");
                _info = null; 
            }
        }
    }
}

