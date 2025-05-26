using System.Collections.Generic;
using UnityEngine;

namespace GameplayAttributes {
[CreateAssetMenu(fileName = "AttributesPreset", menuName = "Gameplay Attributes/Attributes Preset")]
public class GameplayAttributesPreset : ScriptableObject {
    [field: SerializeField]
    public List<GameplayAttributeInitializer> Attributes { get; private set; }
}
}