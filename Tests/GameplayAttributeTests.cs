using BandoWare.GameplayTags;
using NUnit.Framework;

[assembly: GameplayTag("Test.TagOne")]
[assembly: GameplayTag("Test.TagTwo")]

namespace GameplayAttributes {
public class GameplayAttributeTests {
    private static GameplayTag TagOne => AllGameplayTags.Test.TagOne.Get();
    private static GameplayTag TagTwo => AllGameplayTags.Test.TagTwo.Get();

    [Test]
    public void TestDefaults() {
        var testObj = new GameplayAttributeContainer(new GameplayAttributeInitializer[] {
            new(TagOne, 5f),
        });

        //T1 retrieved successfully without any modifiers
        Assert.AreEqual(5f, testObj.GetValue(TagOne));
        Assert.AreEqual(5f, testObj.GetValueBase(TagOne));
        Assert.AreEqual(0, testObj.GetValueBonus(TagOne));

        //T2 not in container => default to 0
        Assert.AreEqual(0f, testObj.GetValue(TagTwo));
        Assert.AreEqual(0f, testObj.GetValueBase(TagTwo));
        Assert.AreEqual(0f, testObj.GetValueBonus(TagTwo));
    }

    [Test]
    public void TestRemoveLastModifier() {
        var testObj = new GameplayAttributeContainer(new GameplayAttributeInitializer[] {
            new(TagOne, 1f),
        });

        var handle = testObj.AddModifier(TagOne, ModifierType.AddBase, 1f);
        Assert.AreEqual(2f, testObj.GetValue(TagOne));

        Assert.IsTrue(testObj.RemoveModifier(handle));
        Assert.AreEqual(1f, testObj.GetValue(TagOne));
    }

    [Test]
    public void TestAddRemoveModifiers() {
        var testObj = new GameplayAttributeContainer(new GameplayAttributeInitializer[] { });

        //add +100%
        //T1 = 0 * 2 = 0
        testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, 1f);
        Assert.AreEqual(0, testObj.GetValue(TagOne));
        Assert.AreEqual(0, testObj.GetValueBase(TagOne));
        Assert.AreEqual(0, testObj.GetValueBonus(TagOne));

        //add +10 base
        //(modifier that adds to base is still counted as bonus)
        //T1 = (0 + 10) * 2 = 20
        var addBaseHandle = testObj.AddModifier(TagOne, ModifierType.AddBase, 10f);
        Assert.AreEqual(20, testObj.GetValue(TagOne));
        Assert.AreEqual(0, testObj.GetValueBase(TagOne));
        Assert.AreEqual(20, testObj.GetValueBonus(TagOne));

        //remove +10 base
        //remove base +10 => T1 is 0 again
        Assert.IsTrue(testObj.RemoveModifier(addBaseHandle));
        Assert.AreEqual(0, testObj.GetValue(TagOne));
        Assert.AreEqual(0, testObj.GetValueBase(TagOne));
        Assert.AreEqual(0, testObj.GetValueBonus(TagOne));

        //add +5 final
        //T1 = 0 * 2 + 5 = 5
        testObj.AddModifier(TagOne, ModifierType.AddFinal, 5f);
        Assert.AreEqual(5, testObj.GetValue(TagOne));
        Assert.AreEqual(0, testObj.GetValueBase(TagOne));
        Assert.AreEqual(5, testObj.GetValueBonus(TagOne));

        //overwrite base value
        //T1 = 12
        testObj.SetValue(TagOne, 12f);
        Assert.AreEqual(12, testObj.GetValue(TagOne));
        Assert.AreEqual(12, testObj.GetValueBase(TagOne));
        Assert.AreEqual(0, testObj.GetValueBonus(TagOne));

        //add a bunch of modifiers: total +2 base, total +10 final, total +90%, total x1.75
        //T1 = (12 + 2) * 1.9 * 2.25 + 10 = 69.85
        testObj.AddModifier(TagOne, ModifierType.AddBase, 1.5f);
        testObj.AddModifier(TagOne, ModifierType.AddBase, 0.5f);
        testObj.AddModifier(TagOne, ModifierType.AddFinal, 5f);
        testObj.AddModifier(TagOne, ModifierType.AddFinal, 5f);
        var multAddHandle = testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, 0.15f);
        testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, 0.15f);
        testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, 0.30f);
        testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, 0.40f);
        testObj.AddModifier(TagOne, ModifierType.MultiplyAdditive, -0.10f);
        var multCompHandle = testObj.AddModifier(TagOne, ModifierType.MultiplyCompound, 0.50f);
        testObj.AddModifier(TagOne, ModifierType.MultiplyCompound, 0.50f);
        Assert.AreEqual(69.85f, testObj.GetValue(TagOne));
        Assert.AreEqual(12f, testObj.GetValueBase(TagOne));
        Assert.AreEqual(57.85f, testObj.GetValueBonus(TagOne));

        //reduce from +90% => +75%
        //reduce from x2.25 => x1.5
        //T1 = (12 + 2) * 1.75 * 1.5 + 10 = 46.75
        Assert.IsTrue(testObj.RemoveModifier(multAddHandle));
        Assert.IsTrue(testObj.RemoveModifier(multCompHandle));
        Assert.AreEqual(46.75f, testObj.GetValue(TagOne));
        Assert.AreEqual(12f, testObj.GetValueBase(TagOne));
        Assert.AreEqual(34.75f, testObj.GetValueBonus(TagOne));
    }
}
}