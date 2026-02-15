using System.Reflection;
using Common.Data.Units.UnitStatsByLevel;
using UnityEngine;

namespace Scenes.Battle.Feature.Ui.StatInfoPanel
{
    public static class UnitStatKindHelper
    {
        public static string GetDisplayName(UnitStatKind kind)
        {
            var field = typeof(UnitStatKind).GetField(kind.ToString());
            var attr = field?.GetCustomAttribute<InspectorNameAttribute>();
            return attr?.displayName ?? kind.ToString();
        }
    }
}
