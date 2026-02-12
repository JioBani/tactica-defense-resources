using UnityEngine;
using System;

namespace Common.Data.Configs
{
    [Serializable]
    public struct LevelUpEntry
    {
        [Tooltip("레벨업 후 배치 상한")]
        public int placementLimit;

        [Tooltip("레벨업 비용 (마나)")]
        public int cost;
    }

    [CreateAssetMenu(menuName = "GameConfig/PlacementConfig")]
    public class PlacementConfig : ScriptableObject
    {
        [Tooltip("초기 배치 상한")]
        public int initialPlacementLimit = 3;

        [Tooltip("인덱스 순서대로 레벨업. 배치 상한과 비용을 정의")]
        public LevelUpEntry[] levelUpTable;
    }
}