using System;
using System.Linq;
using NUnit.Framework;

namespace HouseOf77.Tests
{
    public class RunRulesTests
    {
        sealed class MissRandom : Random { public override double NextDouble() => .99999; }
        [Test] public void NewRunStartsClean()
        {
            var r=new RunRules(); Assert.AreEqual(7,r.Chips); Assert.AreEqual(7,r.MaxHealth);
            Assert.AreEqual(0,r.Spent); Assert.AreEqual(0,r.Misses); Assert.AreEqual(0,r.Jackpots); Assert.IsTrue(r.Upgrades.All(n=>n==0));
        }
        [Test] public void WaveIncomeCapsAtTwentyOneAndCarriesForward()
        {
            var r=new RunRules(); r.BeginWave(new Random(1)); Assert.AreEqual(21,r.AwardChips(100)); Assert.AreEqual(0,r.AwardChips(5)); Assert.AreEqual(28,r.Chips);
            Assert.AreEqual(0,r.AwardChips(-10)); r.BeginWave(new Random(2)); Assert.AreEqual(0,r.WaveChips); r.AwardChips(3); Assert.AreEqual(31,r.Chips);
        }
        [Test] public void PurchasesChargeOnceAndStopAtCap()
        {
            var r=new RunRules(); var random=new Random(7);
            for(int i=0;i<3;i++) {r.BeginWave(random);r.AwardChips(7);Assert.IsTrue(r.Buy(UpgradeKind.Sharpen));int chips=r.Chips;Assert.IsFalse(r.Buy(UpgradeKind.Vitality));Assert.AreEqual(chips,r.Chips);}
            r.BeginWave(random); Assert.IsFalse(r.Buy(UpgradeKind.Sharpen)); Assert.AreEqual(21,r.Spent); Assert.AreEqual(3,r.Upgrades[0]); Assert.IsFalse(r.Available().Contains(UpgradeKind.Sharpen));
        }
        [Test] public void RigUsesDisplayedTiersAndCannotOverdraw()
        {
            var r=new RunRules();r.BeginWave(new Random(2)); Assert.AreEqual(.15,r.Chance); Assert.IsTrue(r.Rig()); Assert.AreEqual(.30,r.Chance);Assert.IsFalse(r.Rig());
            r.AwardChips(21);Assert.IsTrue(r.Rig());Assert.AreEqual(.50,r.Chance);Assert.IsTrue(r.Rig());Assert.AreEqual(.75,r.Chance);Assert.IsFalse(r.Rig());Assert.AreEqual(21,r.Spent);
        }
        [Test] public void TwoMissesGuaranteeThirdAndDisablePaidOdds()
        {
            var r=new RunRules();var random=new MissRandom();
            for(int i=0;i<2;i++){r.BeginWave(random);Assert.AreNotEqual(r.Target,r.Spin(random));}
            r.BeginWave(random);Assert.AreEqual(1,r.Chance);Assert.IsFalse(r.CanRig);Assert.IsFalse(r.Rig());Assert.AreEqual(r.Target,r.Spin(random));Assert.AreEqual(0,r.Misses);Assert.AreEqual(1,r.Jackpots);
        }
        [Test] public void SpinAndPurchasesLockAfterResolution()
        {
            var r=new RunRules();var random=new Random(7);r.BeginWave(random);r.Spin(random);
            Assert.Throws<InvalidOperationException>(()=>r.Spin(random));Assert.IsFalse(r.Buy(UpgradeKind.Vitality));Assert.IsFalse(r.Rig());
        }
        [Test] public void TargetsStayInRangeAndNeverRepeat()
        {
            var r=new RunRules();var random=new Random(77);int previous=0;
            for(int i=0;i<10000;i++){r.BeginWave(random);Assert.That(r.Target,Is.InRange(7,77));Assert.AreNotEqual(previous,r.Target);previous=r.Target;}
        }
        [Test] public void ObservedProbabilityMatchesEachDisplayedTier()
        {
            var random=new Random(777);const int trials=20000;
            for(int tier=0;tier<4;tier++)
            {
                int matches=0;
                for(int i=0;i<trials;i++) {var r=new RunRules();r.BeginWave(random);r.AwardChips(21);for(int j=0;j<tier;j++)r.Rig();int result=r.Spin(random);Assert.That(result,Is.InRange(7,77));if(result==r.Target)matches++;}
                Assert.That((double)matches/trials,Is.EqualTo(RunRules.Chances[tier]).Within(.015));
            }
        }
        [Test] public void OffersAreUniqueAndOnlyHealAfterEveryCap()
        {
            var r=new RunRules();var random=new Random(77);
            Assert.AreEqual(3,r.Offers(random).Distinct().Count());
            for(int kind=0;kind<6;kind++)for(int count=0;count<RunRules.UpgradeCaps[kind];count++){r.BeginWave(random);r.AwardChips(21);Assert.IsTrue(r.Buy((UpgradeKind)kind));}
            r.BeginWave(random);CollectionAssert.AreEqual(new[]{UpgradeKind.Heal},r.Offers(random));Assert.IsTrue(r.Buy(UpgradeKind.Heal));
        }
    }
}
