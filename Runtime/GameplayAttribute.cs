using System;
using System.Collections.Generic;
using BandoWare.GameplayTags;
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.Assertions;
#endif

namespace GameplayAttributes {
/// <summary>
/// The struct that holds an attribute's base and adjusted value.
/// Users should generally interact with attributes via <see cref="GameplayAttributeContainer"/>.
/// </summary>
internal struct GameplayAttributeData {
    public float BaseValue { get; set; }
    public float CurrentValue { get; set; }

    public GameplayAttributeData(float baseValue, float currentValue) {
        BaseValue = baseValue;
        CurrentValue = currentValue;
    }

    public GameplayAttributeData(float value) : this(value, value) { }
}

/// <summary>
/// Used to initialize the base value for a gameplay attribute with the specified tag.
/// </summary>
[Serializable]
public struct GameplayAttributeInitializer {
    [field: SerializeField]
    public GameplayTag AttributeTag { get; set; }

    [field: SerializeField]
    public float BaseValue { get; set; }

    public GameplayAttributeInitializer(GameplayTag attributeTag, float baseValue) {
        AttributeTag = attributeTag;
        BaseValue = baseValue;
    }
}

[Serializable]
public enum ModifierType {
    [InspectorName(null)]
    None,

    /// <summary>
    /// A flat value that is added to the base value (pre-multiplication).
    /// </summary>
    AddBase,

    /// <summary>
    /// A flat value that is added to the final value (post-multiplication).
    /// </summary>
    AddFinal,

    /// <summary>
    /// A multiplier that is additive with other multipliers of the same type.
    /// A value of -0.5 would represent a 50% decrease, whereas a value of 0.5 would represent a 50% increase.
    /// </summary>
    MultiplyAdditive,

    /// <summary>
    /// A multiplier that is multiplicative with other modifiers of the same type.
    /// A value of -0.5 would represent a 50% decrease, whereas a value of 0.5 would represent a 50% increase.
    /// </summary>
    MultiplyCompound,
}

/// <summary>
/// A modifier that can be applied to an attribute.
/// Can be removed using a <see cref="GameplayAttributeModifierHandle"/>.
/// </summary>
[Serializable]
public struct GameplayAttributeModifier {
    public GameplayTag attributeTag;
    public ModifierType type;
    public float value;
}

/// <summary>
/// A handle to a <see cref="GameplayAttributeModifier"/> that has been applied to a <see cref="GameplayAttributeContainer"/>.
/// Can be used to remove the modifier.
/// </summary>
public struct GameplayAttributeModifierHandle : IEquatable<GameplayAttributeModifierHandle> {
    internal GameplayTag Tag { get; private set; }
    internal long HandleID { get; private set; }

    internal GameplayAttributeModifierHandle(GameplayTag tag, long handleID) {
        Tag = tag;
        HandleID = handleID;
    }

    public bool Equals(GameplayAttributeModifierHandle other) {
        return Tag.Equals(other.Tag) && HandleID == other.HandleID;
    }

    public override bool Equals(object obj) {
        return obj is GameplayAttributeModifierHandle other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Tag, HandleID);
    }
}

internal struct ActiveGameplayAttributeModifier {
    internal long HandleID { get; private set; }
    internal ModifierType Type { get; private set; }
    internal float Value { get; private set; }

    internal ActiveGameplayAttributeModifier(long handleID, ModifierType type, float value) {
        HandleID = handleID;
        Type = type;
        Value = value;
    }
}

public interface IGameplayAttributeContainer {
    /// <summary>
    /// Get the calculated value of an attribute, including all modifiers.
    /// </summary>
    float GetValue(GameplayTag attributeTag);

    /// <summary>
    /// Get the value of an attribute without considering its modifiers.
    /// </summary>
    float GetValueBase(GameplayTag attributeTag);

    /// <summary>
    /// Get the calculated value of an attribute minus its base value.
    /// </summary>
    float GetValueBonus(GameplayTag attributeTag);

    /// <summary>
    /// Set the value of an attribute, overwriting any existing value and clearing any existing modifiers.
    /// </summary>
    void SetValue(GameplayTag attributeTag, float value);

    /// <summary>
    /// Get whether this container has the specified attribute.
    /// </summary>
    bool HasAttribute(GameplayTag attributeTag);

    GameplayAttributeModifierHandle AddModifier(in GameplayAttributeModifier modifier);

    GameplayAttributeModifierHandle AddModifier(GameplayTag attributeTag, ModifierType type, float value);

    bool RemoveModifier(in GameplayAttributeModifierHandle handle);

    /// <summary>
    /// Set an attribute with tag currentTag to its maximum value as specified by maxTag.
    /// This will clear any modifiers for the specified tag.
    /// It is expected that a tag Some.Attribute will have a max tag of Some.Attribute.Max
    /// </summary>
    /// <param name="currentTag">Tag for the current value of the attribute</param>
    /// <param name="maxTag">Tag for the maximum value of the attribute</param>
    /// <param name="overwrite">Whether to overwrite any existing value for currentTag - if false and a value already exists, no action will be taken</param>
    void SetAttributeToMax(GameplayTag currentTag, GameplayTag maxTag, bool overwrite);
}

/// <summary>
/// Container that holds attributes and their modifications.
/// Handles all attribute change eventing, caching and recalculation.
/// </summary>
public class GameplayAttributeContainer : IGameplayAttributeContainer {
    private const long InvalidHandleID = 0;
    public static readonly GameplayAttributeModifierHandle InvalidHandle = new GameplayAttributeModifierHandle(GameplayTag.None, InvalidHandleID);

    //TODO add GameplayAttributeContainer eventing

    //NOTE - these will require custom serialization if saving/loading because the key hash is based on the GameplayTag's runtime index
    private readonly Dictionary<GameplayTag, GameplayAttributeData> _attributesByTag = new();
    private readonly Dictionary<GameplayTag, List<ActiveGameplayAttributeModifier>> _modifiersByAttributeTag = new();
    private long _handleID = InvalidHandleID;

    public GameplayAttributeContainer(IEnumerable<GameplayAttributeInitializer> initialAttributes) {
        foreach (var initializer in initialAttributes) {
            _attributesByTag[initializer.AttributeTag] = new GameplayAttributeData(initializer.BaseValue);
        }
    }

    public float GetValue(GameplayTag attributeTag) {
        return _attributesByTag.GetValueOrDefault(attributeTag).CurrentValue;
    }

    public float GetValueBase(GameplayTag attributeTag) {
        return _attributesByTag.GetValueOrDefault(attributeTag).BaseValue;
    }

    public float GetValueBonus(GameplayTag attributeTag) {
        var value = _attributesByTag.GetValueOrDefault(attributeTag);
        return value.CurrentValue - value.BaseValue;
    }

    public void SetValue(GameplayTag attributeTag, float value) {
        _attributesByTag[attributeTag] = new GameplayAttributeData(value);
        _modifiersByAttributeTag.Remove(attributeTag);
    }

    public bool HasAttribute(GameplayTag attributeTag) {
        return _attributesByTag.ContainsKey(attributeTag);
    }

    public GameplayAttributeModifierHandle AddModifier(in GameplayAttributeModifier modifier) {
        return AddModifier(modifier.attributeTag, modifier.type, modifier.value);
    }

    public GameplayAttributeModifierHandle AddModifier(GameplayTag attributeTag, ModifierType type, float value) {
        //get or create the list of modifiers for the requested attribute
        if (!_modifiersByAttributeTag.TryGetValue(attributeTag, out var modifiers)) {
            modifiers = new();
            _modifiersByAttributeTag[attributeTag] = modifiers;
        }

        var handleID = ++_handleID;
        modifiers.Add(new ActiveGameplayAttributeModifier(handleID, type, value));

        RecalculateAttribute(attributeTag, modifiers);

        return new GameplayAttributeModifierHandle(attributeTag, handleID);
    }

    public bool RemoveModifier(in GameplayAttributeModifierHandle handle) {
        //find the modifiers for the specified attribute tag
        if (_modifiersByAttributeTag.TryGetValue(handle.Tag, out var modifiers)) {
            //find a modifier for the specified attribute tag with the specified unique handle id
            for (var i = 0; i < modifiers.Count; ++i) {
                if (modifiers[i].HandleID == handle.HandleID) {
                    if (modifiers.Count == 1) {
                        //last modifier left - remove all entries for this attribute
                        modifiers.Clear();
                        _modifiersByAttributeTag.Remove(handle.Tag);
                    } else {
                        //swapremove the modifier
                        modifiers.Swap(i, modifiers.Count - 1);
                        modifiers.RemoveAt(modifiers.Count - 1);
                    }

                    RecalculateAttribute(handle.Tag, modifiers);

                    return true;
                }
            }
        }

        //couldn't find a modifier matching the handle
        return false;
    }

    public void SetAttributeToMax(GameplayTag currentTag, GameplayTag maxTag, bool overwrite) {
#if UNITY_EDITOR
        //max tag is always Some.Attribute.Tag.Max so the current tag should always be its parent
        Assert.AreEqual(maxTag.ParentTag, currentTag);
#endif
        if (overwrite || !HasAttribute(currentTag)) {
            var maxValue = GetValue(maxTag);
            SetValue(currentTag, maxValue);
        }
    }

    private void RecalculateAttribute(GameplayTag attributeTag, List<ActiveGameplayAttributeModifier> modifiers) {
        var data = _attributesByTag.GetValueOrDefault(attributeTag);

        //process all modifiers
        var currentBase = data.BaseValue;
        var currentAddFinal = 0f;
        var currentMultiplyAdditive = 1f;
        var currentMultiplyCompound = 1f;
        foreach (var modifier in modifiers) {
            switch (modifier.Type) {
                case ModifierType.AddBase:
                    currentBase += modifier.Value;
                    break;
                case ModifierType.AddFinal:
                    currentAddFinal += modifier.Value;
                    break;
                case ModifierType.MultiplyAdditive:
                    currentMultiplyAdditive += modifier.Value;
                    break;
                case ModifierType.MultiplyCompound:
                    currentMultiplyCompound *= 1f + modifier.Value;
                    break;
            }
        }

        //TODO ensure we can't go negative?

        //replace current value (preserve existing base value)
        var currentValue = currentBase * currentMultiplyAdditive * currentMultiplyCompound + currentAddFinal;
        _attributesByTag[attributeTag] = new GameplayAttributeData(data.BaseValue, currentValue);
    }
}
}