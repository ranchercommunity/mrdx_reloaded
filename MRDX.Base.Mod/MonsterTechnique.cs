using System.ComponentModel.Design;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Base.Mod;

public record MonsterTechnique : IMonsterTechnique {
    public MonsterTechnique ( string atkName, byte rawId, TechSlots slot, Span<byte> data ) {
        var dat = data.ToArray();
        Name = atkName;
        Id = rawId;
        Slot = slot;
        JpnName = dat[ ..16 ];
        Type = (ErrantryType) dat[ 16 ];
        Range = (TechRange) dat[ 17 ];
        Nature = (TechNature) dat[ 18 ];
        Scaling = (TechType) dat[ 19 ];
        Available = dat[ 20 ] == 1;
        HitPercent = (sbyte) dat[ 21 ];
        Force = dat[ 22 ];
        Withering = dat[ 23 ];
        Sharpness = dat[ 24 ];
        GutsCost = dat[ 25 ];
        GutsSteal = dat[ 26 ];
        LifeSteal = dat[ 27 ];
        LifeRecovery = dat[ 28 ];
        ForceMissSelf = dat[ 29 ];
        ForceHitSelf = dat[ 30 ];

        ErrantryInformation = new List<ITechniqueErrantryInformation>();
    }

    // Slot is not a real value on the monster tech data, but rather the position in the attack
    // list where it belongs. This way we can build the actual tech list easier.
    public byte Id { get; set; }
    public TechSlots Slot { get; set; }

    public byte[] JpnName { get; set; }
    public string Name { get; set; }
    public ErrantryType Type { get; set; }
    public TechRange Range { get; set; }
    public TechNature Nature { get; set; }
    public TechType Scaling { get; set; }
    public bool Available { get; set; }
    public sbyte HitPercent { get; set; }
    public byte Force { get; set; }
    public byte Withering { get; set; }
    public byte Sharpness { get; set; }
    public byte GutsCost { get; set; }
    public byte GutsSteal { get; set; }
    public byte LifeSteal { get; set; }
    public byte LifeRecovery { get; set; }
    public byte ForceMissSelf { get; set; }
    public byte ForceHitSelf { get; set; }

    public List<ITechniqueErrantryInformation> ErrantryInformation { get; set; }
}

public record TechniqueErrantryData : ITechniqueErrantryInformation {
    /// <summary>
    /// Creates a new TechniqueErrantryData
    /// </summary>
    /// <param name="breedTechniques"></param>
    /// <param name="errantryData">The full errantry data file</param>
    /// <param name="dPos">The starting position of the technique</param>
    public TechniqueErrantryData ( List<IMonsterTechnique> breedTechniques, ErrantryLocation location, byte errantrySlot,
        Span<byte> errantryData, int dPos ) {

        var tID = errantryData[ dPos ] + ( errantryData[ dPos + 1 ] << 8 );
        Technique = breedTechniques.Find( tech => tech.Id == tID );
        dPos += 2;

        Location = location;
        ErrantrySlot = errantrySlot;

        StatRequirements = new List<ITechniqueErrantryInformation._statRequirements>();
        while ( errantryData[ dPos ] != 0xFF && errantryData[ dPos + 1 ] != 0xFF ) {
            Console.WriteLine( $"{dPos} : {errantryData[ dPos ]}" );
            ushort statType = (ushort) ( errantryData[ dPos ] + ( errantryData[ dPos + 1 ] << 8 ) );
            ushort statTotal = (ushort) ( errantryData[ dPos + 2 ] + ( errantryData[ dPos + 3 ] << 8 ) );
            ushort statOff = (ushort) ( errantryData[ dPos + 4 ] + ( errantryData[ dPos + 5 ] << 8 ) );

            StatRequirements.Add( new ITechniqueErrantryInformation._statRequirements( statType, statTotal, statOff ) );
            dPos += 6;
        }
        dPos += 2; // Skip the FF

        AutolearnPercent = ( errantryData[ dPos ] + ( errantryData[ dPos + 1 ] << 8 ) );
        dPos += 4; // The 2 skipped bytes may be something that is unused or otherwise always 0. 

        NatureRequirement = (ushort) ( errantryData[ dPos ] + ( errantryData[ dPos + 1 ] << 8 ) );
        dPos += 2;

        if ( errantryData[ dPos ] != 0xFF || errantryData[ dPos + 1 ] != 0xFF ) {
            tID = errantryData[ dPos ] + ( errantryData[ dPos + 1 ] << 8 );
            ChainTechRequired = breedTechniques.Find( tech => tech.Id == tID );
            ChainUsesRequired = (ushort) ( errantryData[ dPos + 2 ] + ( errantryData[ dPos + 3 ] << 8 ) );
        }
        dPos += 6; // Tech Chain is followed by an ignored FFFF

        SubsRequired = new List<MonsterGenus>();
        SubsLocked = new List<MonsterGenus>();

        // FFFFFFFF indicates the end of the technique.
        while ( errantryData[ dPos ] != 0xFF && errantryData[ dPos + 1 ] != 0xFF
            && errantryData[ dPos + 2 ] != 0xFF && errantryData[ dPos + 3 ] != 0xFF ) {

            // First two bytes indicate whether its allowed or locked, followed by 2 bytes for the specific sub.
            // 1 == Subs can Learn the technique
            if ( errantryData[ dPos ] == 1 ) {
                if ( errantryData[ dPos + 2 ] == 0xFF && errantryData[ dPos + 3 ] == 0xFF ) { } // All subs can learn. Continue

                else {
                    SubRequirements = true;
                    SubsRequired.Add( (MonsterGenus) ( errantryData[ dPos + 2 ] + ( errantryData[ dPos + 3 ] << 8 ) ) );
                }
            }
            else {
                SubRequirements = true;
                SubsLocked.Add( (MonsterGenus) ( errantryData[ dPos + 2 ] + ( errantryData[ dPos + 3 ] << 8 ) ) );
            }

            dPos += 4;
        }
    }

    public IMonsterTechnique Technique { get; set; }
    public ErrantryLocation Location { get; set; }

    public byte ErrantrySlot { get; set; }
    public List<ITechniqueErrantryInformation._statRequirements> StatRequirements { get; set; }
    public int AutolearnPercent { get; set; }
    public ushort NatureRequirement { get; set; }
    public IMonsterTechnique? ChainTechRequired { get; set; }
    public ushort ChainUsesRequired { get; set; }
    public bool SubRequirements { get; set; }
    public List<MonsterGenus> SubsRequired { get; set; }
    public List<MonsterGenus> SubsLocked { get; set; }
}