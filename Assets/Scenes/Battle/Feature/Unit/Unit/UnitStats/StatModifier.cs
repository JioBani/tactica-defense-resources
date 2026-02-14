namespace Scenes.Battle.Feature.Units.UnitStats
{
    public enum StatModifierType
    {
        Flat,
        Percent,
    }

    public class StatModifier
    {
        public readonly object Source;
        public readonly StatModifierType Type;
        public readonly float Value;

        public StatModifier(object source, StatModifierType type, float value)
        {
            Source = source;
            Type = type;
            Value = value;
        }
    }
}
