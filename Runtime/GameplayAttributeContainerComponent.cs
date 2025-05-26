using System.Collections.Generic;
using System.Linq;
using BandoWare.GameplayTags;
using UnityEngine;

namespace GameplayAttributes {
public class GameplayAttributeContainerComponent : MonoBehaviour, IGameplayAttributeContainer {
    [field: SerializeField]
    public GameplayAttributesPreset Preset { get; set; }

    [field: SerializeField]
    public List<GameplayAttributeInitializer> AttributeOverrides { get; private set; }

    private GameplayAttributeContainer _container;
    private GameplayAttributeContainer Container => _container ??= new(Preset ? Preset.Attributes.Concat(AttributeOverrides) : AttributeOverrides);

    public float GetValue(GameplayTag attributeTag) => Container.GetValue(attributeTag);
    public float GetValueBase(GameplayTag attributeTag) => Container.GetValueBase(attributeTag);
    public float GetValueBonus(GameplayTag attributeTag) => Container.GetValueBonus(attributeTag);
    public void SetValue(GameplayTag attributeTag, float value) => Container.SetValue(attributeTag, value);
    public bool HasAttribute(GameplayTag attributeTag) => Container.HasAttribute(attributeTag);
    public GameplayAttributeModifierHandle AddModifier(in GameplayAttributeModifier modifier) => Container.AddModifier(modifier);
    public GameplayAttributeModifierHandle AddModifier(GameplayTag attributeTag, ModifierType type, float value) => Container.AddModifier(attributeTag, type, value);
    public bool RemoveModifier(in GameplayAttributeModifierHandle handle) => Container.RemoveModifier(in handle);
    public void SetAttributeToMax(GameplayTag currentTag, GameplayTag maxTag, bool overwrite) => Container.SetAttributeToMax(currentTag, maxTag, overwrite);
}
}