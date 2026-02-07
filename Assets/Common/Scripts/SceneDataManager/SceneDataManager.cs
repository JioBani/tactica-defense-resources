using Common.Data.Battlefields;
using Common.Scripts.SceneSingleton;

namespace Common.Scripts.SceneDataManager
{
    /// <summary>
    /// 씬 간 데이터 전달을 관리하는 매니저
    /// DontDestroyOnLoad로 씬 전환 시에도 유지됨
    /// </summary>
    public class SceneDataManager : SceneSingleton<SceneDataManager>
    {
        public SceneData<BattlefieldData> selectedBattlefield = new SceneData<BattlefieldData>();

        /// <summary>
        /// 모든 씬 데이터 초기화
        /// </summary>
        public void ClearAll()
        {
            selectedBattlefield.Clear();
        }
    }
}
