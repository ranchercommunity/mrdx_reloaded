namespace MRDX.Base.Mod.Interfaces;

public interface IMonsterAttack
{
    string Name { get; set; }
    TechType Tech { get; set; }
    ErrantyType Erranty { get; set; }
    int GutsCost { get; set; }
    int Force { get; set; }
    int Hit { get; set; }
    int Wither { get; set; }
    int Sharp { get; set; }
    SpecialTech Special { get; set; }
}

public interface IBattleAttack
{
    byte SelectedTechSlot1 { get; set; }
    byte SelectedTechSlot2 { get; set; }
    byte SelectedTechSlot3 { get; set; }
    byte SelectedTechSlot4 { get; set; }
}