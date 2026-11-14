using System;
using System.Collections.Generic;

namespace HouseOf77
{
    public enum UpgradeKind { Sharpen, Quickcast, SplitShot, Pierce, Vitality, SecondWind, Heal }
    public enum RunPhase { Title, Wave, Intermission, Boss, Results }

    // Contains no engine state. A spin is decided once; the reel only presents its result.
    public sealed class RunRules
    {
        public static readonly int[] UpgradeCaps = { 3, 3, 1, 2, 2, 2 };
        public static readonly double[] Chances = { .15, .30, .50, .75 };
        public readonly int[] Upgrades = new int[6];
        public int Chips { get; private set; } = 7;
        public int Spent { get; private set; }
        public int Misses { get; private set; }
        public int Jackpots { get; private set; }
        public int OddsTier { get; private set; }
        public int Target { get; private set; }
        public int WaveChips { get; private set; }
        public bool Purchased { get; private set; }
        public bool Spun { get; private set; }
        public double Chance => Misses >= 2 ? 1 : Chances[OddsTier];
        public int MaxHealth => 7 + Upgrades[(int)UpgradeKind.Vitality];
        public float DamageMultiplier => 1 + .2f * Upgrades[0];
        public float FireRateMultiplier => 1 + .15f * Upgrades[1];
        public float DashCooldown => 2 * (float)Math.Pow(.8, Upgrades[5]);

        public void BeginWave(Random random)
        {
            int previous = Target;
            do { Target = random.Next(7, 78); } while (Target == previous);
            OddsTier = 0; WaveChips = 0; Purchased = false; Spun = false;
        }
        public int AwardChips(int amount)
        {
            int awarded = Math.Max(0, Math.Min(amount, 21 - WaveChips));
            WaveChips += awarded; Chips += awarded; return awarded;
        }
        public bool CanBuy(UpgradeKind kind)
        {
            if (Purchased || Spun || Chips < 7) return false;
            if (kind == UpgradeKind.Heal) return Available().Count == 0;
            return Upgrades[(int)kind] < UpgradeCaps[(int)kind];
        }
        public bool Buy(UpgradeKind kind)
        {
            if (!CanBuy(kind)) return false;
            Chips -= 7; Spent += 7; Purchased = true;
            if (kind != UpgradeKind.Heal) Upgrades[(int)kind]++;
            return true;
        }
        public bool CanRig => !Spun && Misses < 2 && OddsTier < 3 && Chips >= 7;
        public bool Rig()
        {
            if (!CanRig) return false;
            Chips -= 7; Spent += 7; OddsTier++; return true;
        }
        public int Spin(Random random)
        {
            if (Spun) throw new InvalidOperationException("This wave has already spun.");
            Spun = true;
            if (random.NextDouble() < Chance) { Misses = 0; Jackpots++; return Target; }
            Misses++;
            int result = random.Next(7, 77); // 70 non-target values, without rejection bias.
            return result >= Target ? result + 1 : result;
        }
        public List<UpgradeKind> Available()
        {
            var result = new List<UpgradeKind>(6);
            for (int i = 0; i < 6; i++) if (Upgrades[i] < UpgradeCaps[i]) result.Add((UpgradeKind)i);
            return result;
        }
        public List<UpgradeKind> Offers(Random random)
        {
            var result = Available();
            for (int i = result.Count - 1; i > 0; i--) { int j = random.Next(i + 1); var v = result[i]; result[i] = result[j]; result[j] = v; }
            if (result.Count == 0) result.Add(UpgradeKind.Heal);
            if (result.Count > 3) result.RemoveRange(3, result.Count - 3);
            return result;
        }
    }
}
