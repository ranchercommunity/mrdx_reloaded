using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.BattleSimulator;

public enum Attacker
{
    Left,
    Right
}

public record struct CombatResult
{
    public int CritChance;
    public Range<int> Damage;
    public int HitChance;
    public int Withering;
}

public class CombatSimulation(IBattleMonster left, IBattleMonster right)
{
    public Range Damage(Attacker user, IMonsterTechnique tech)
    {
        var attacker = user == Attacker.Left ? left : right;
        var defender = user == Attacker.Left ? right : left;

        const double d = 12.5;
        const double f = 4;

        int apow = tech.Scaling == TechType.Power ? attacker.Power : attacker.Intelligence;
        int dpow = tech.Scaling == TechType.Power ? defender.Power : defender.Intelligence;
        int ddef = defender.AdjustedDefense;

        if (apow == 999) apow = 1000;
        if (dpow == 999) dpow = 1000;
        if (ddef == 999) ddef = 1000;

        apow = (int)Math.Floor(Math.Round(apow / d * 10000) / 10000);
        ddef = (int)Math.Floor(Math.Round(Math.Floor(Math.Round((2d * ddef + dpow) / 3 * 10000) / 10000) / d * 10000) /
                               10000);

        var baseDmg =
            Math.Floor(Math.Round(Math.Floor(Math.Round((2 * f + apow) * tech.Force / 3 * 10000) / 10000) / f * 10000) /
                       10000);
        var adj = Math.Floor(
            Math.Round((100 * f + Math.Floor(Math.Round(10d * (apow - ddef) * 100000) / 100000)) / f * 10000) / 10000);

        var dmg = Math.Floor(Math.Round(baseDmg * adj * 100) / 10000);

        var min = Math.Floor(
            Math.Round(Math.Floor(Math.Round((1 * f + apow) * tech.Force / 20 * 10000) / 10000) / f * 10000) / 10000);
        var max = Math.Floor(
            Math.Round(Math.Floor(Math.Round(3 * (1 * f + apow) * tech.Force / 4 * 10000) / 10000) / f * 10000) /
            10000);

        dmg = Math.Clamp(dmg, min, max);

        int mul;
        if (attacker.Guts <= 50)
            mul = (int)Math.Floor(Math.Round(Math.Floor((attacker.Guts + 200) * 65536.0 / 10000) / 16 * 10000) / 10000);
        else
            mul = (int)Math.Floor(Math.Round(Math.Floor((attacker.Guts + 200) * 65536.0 / 10000) / 16 * 10000) / 10000)
                  + (int)Math.Floor(Math.Round((attacker.GutsRate - 6) * (attacker.Guts - 50) / 5.0 * 10000) / 10000);

        int div;
        if (defender.Guts <= 50)
            div = (int)Math.Floor(Math.Round(Math.Floor((defender.Guts + 200) * 65536.0 / 10000) / 16 * 10000) / 10000);
        else
            div = (int)Math.Floor(Math.Round(Math.Floor((defender.Guts + 200) * 65536.0 / 10000) / 16 * 10000) / 10000)
                  + (int)Math.Floor(Math.Round((defender.GutsRate - 6) * (defender.Guts - 50) / 5.0 * 10000) / 10000);

        dmg = (int)Math.Floor(
            Math.Round(10240000 * Math.Floor(Math.Round(dmg * mul / 1024.0 * 1000000) / 1000000) / div) / 10000);

        // Battle Special Effects - Attacker
        if (attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Power) ||
            attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Fury))
            dmg *= 2;
        if (attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Guard) ||
            attacker.InactiveBattleSpecial.HasFlag(BattleSpecials.Real))
            dmg = (int)Math.Ceiling(dmg / 2.0);
        if (attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Real)) dmg = (int)Math.Floor(dmg * 1.5);

        // Battle Special Effects - Defender
        if (defender.ActiveBattleSpecial.HasFlag(BattleSpecials.Guard) ||
            defender.ActiveBattleSpecial.HasFlag(BattleSpecials.Real))
            dmg = (int)(dmg * 0.65625);

        if (defender.ActiveBattleSpecial.HasFlag(BattleSpecials.Ease) ||
            defender.InactiveBattleSpecial.HasFlag(BattleSpecials.Real))
            dmg *= 2;

        var lo = (int)Math.Clamp(dmg - 5, 1, 999);
        var hi = (int)Math.Clamp(dmg + 5, 1, 999);

        return new Range(lo, hi);
    }

    public int CritChance(Attacker user, IMonsterTechnique tech)
    {
        var attacker = user == Attacker.Left ? left : right;
        var defender = user == Attacker.Left ? right : left;

        int c = tech.Sharpness;
        if (attacker.Nature < 0) // Bad natured monsters
        {
            c += attacker.Stress / 10;
            c += attacker.Fame / 20;
            c += defender.Fame / 20;
        }
        else // Good natured monsters
        {
            c += attacker.Fame / 10;
        }

        if (attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Hurry))
            c *= 2;
        if (attacker.ActiveBattleSpecial.HasFlag(BattleSpecials.Real))
            c = (int)(Math.Round(c * 15000.0) / 10000);
        if (attacker.InactiveBattleSpecial.HasFlag(BattleSpecials.Real))
            c = (int)(Math.Round(c * 5000.0) / 10000);

        if (c > 100)
            c = 100;

        return c;
    }

    // public static int CalcHitChance(Attacker user, IMonsterTechnique tech)
    // {
    //     // Calculate base hit contribution from TEC and guts
    //     var atkGutsComponent = Math.Round(atkGuts / (14.0 - Math.Floor(atkRate / 2.0)) * 10000) / 10000;
    //     var defGutsComponent = Math.Round(defGuts / (14.0 - Math.Floor(defRate / 2.0)) * 10000) / 10000;
    //
    //     var c = tec + Math.Floor(atkGutsComponent) - Math.Floor(defGutsComponent);
    //
    //     if (isDX)
    //     {
    //         c += Math.Floor(Math.Round((atkSki * 8 - defAdjS * 8 + 5000) * 100) / 10000);
    //     }
    //     else
    //     {
    //         var normAtkSki = (int)Math.Floor(Math.Round(atkSki / 50.0 * 10000) / 10000);
    //         var normDefSpd = (int)Math.Floor(Math.Round(defAdjS / 50.0 * 10000) / 10000);
    //         c += 50 + Math.Floor(Math.Round((normAtkSki - normDefSpd) * 4 * 10000) / 10000);
    //     }
    //
    //     // Count hit modifiers
    //     var h = 0;
    //     if (defFool) h++; // Foolery
    //     if (atkBsp == 3) h++; // Will
    //     if (defBsp == 1) h++; // Anger
    //     if (defBsp == 7) h--; // Ease
    //
    //     if (h > 0)
    //     {
    //         double i = h + 1;
    //         c = 100 - Math.Pow(100 - c, i) / Math.Pow(100, h);
    //     }
    //     else if (h < 0)
    //     {
    //         c = Math.Pow(c, 2) / 100;
    //     }
    //
    //     // Clamp the hit chance
    //     if (c > 99) c = 99;
    //     else if (c < 1) c = 1;
    //
    //     return (int)Math.Floor(c); // You can format it as $"{(int)Math.Floor(c)}%" if needed
    // }
}