using Common.Data.Units.UnitStatsByLevel;
using Scenes.Battle.Feature.Units.UnitStats;
using TMPro;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public class StatCell : MonoBehaviour
    {
        [SerializeField] private TMP_Text statLabel;
        [SerializeField] private TMP_Text statValue;

        public void Bind(UnitStatKind kind, UnitStat stat)
        {
            statLabel.text = UnitStatKindHelper.GetDisplayName(kind);
            statValue.text = UnitStatKindHelper.FormatStatValue(kind, stat.CurrentValue);
        }
    }
}
