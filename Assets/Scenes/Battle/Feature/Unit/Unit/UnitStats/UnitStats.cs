using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scenes.Battle.Feature.Units.UnitStats
{
    public enum StatCalculationMode
    {
        Additive,
        SeparatedMultiplicative,
    }

    public class UnitStat
    {
        private readonly StatCalculationMode _mode;
        private float _baseValue;
        private float _currentValue;
        private readonly List<StatModifier> _modifiers = new();

        public event Action<float> OnChange;

        public UnitStat(StatCalculationMode mode = StatCalculationMode.Additive)
        {
            _mode = mode;
        }

        public float BaseValue => _baseValue;
        public float CurrentValue => _currentValue;

        public void SetBaseValue(float value)
        {
            _baseValue = value;
            Recalculate();
        }

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            Recalculate();
        }

        public void RemoveModifier(StatModifier modifier)
        {
            _modifiers.Remove(modifier);
            Recalculate();
        }

        public void RemoveModifiersBySource(object source)
        {
            _modifiers.RemoveAll(m => m.Source == source);
            Recalculate();
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
            Recalculate();
        }

        private void Recalculate()
        {
            float newValue = _mode switch
            {
                StatCalculationMode.Additive => CalculateAdditive(),
                StatCalculationMode.SeparatedMultiplicative => CalculateSeparatedMultiplicative(),
                _ => _baseValue,
            };

            if (Mathf.Approximately(_currentValue, newValue)) return;
            _currentValue = newValue;
            OnChange?.Invoke(_currentValue);
        }

        private float CalculateAdditive()
        {
            float flatSum = 0f;
            float percentSum = 0f;

            foreach (var mod in _modifiers)
            {
                if (mod.Type == StatModifierType.Flat)
                    flatSum += mod.Value;
                else
                    percentSum += mod.Value;
            }

            return _baseValue * (1f + percentSum) + flatSum;
        }

        private float CalculateSeparatedMultiplicative()
        {
            float increaseProduct = 1f;
            float decreaseProduct = 1f;

            foreach (var mod in _modifiers)
            {
                if (mod.Value > 0f)
                    increaseProduct *= (1f - mod.Value);
                else if (mod.Value < 0f)
                    decreaseProduct *= (1f - Mathf.Abs(mod.Value));
            }

            float increaseResult = 1f - increaseProduct;
            float decreaseResult = 1f - decreaseProduct;

            return _baseValue + increaseResult - decreaseResult;
        }
    }
}
