using System.Drawing;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.DynamicTournaments
{
    public class MonsterTechnique : IMonsterAttack
    {
        byte[] _rawTechnique; // TODO: Use getters and setters to dynamically update the raw technique information if you change the values.
        public int _id;

        public int _techValue = 0;
        
        string IMonsterAttack.Name { get => _name; set => _name = value; }
        ErrantryType IMonsterAttack.Errantry { get => _errantry; set => _errantry = value; }
        TechRange IMonsterAttack.Range { get => _range; set => _range = value; }
        TechNature IMonsterAttack.Nature { get => _nature; set => _nature = value; }
        TechType IMonsterAttack.Tech { get => _scaling; set => _scaling = value; }
        int IMonsterAttack.Hit { get => _hit; set => _hit = value; }
        int IMonsterAttack.Force { get => _force; set => _force = value; }
        int IMonsterAttack.Wither { get => _wither; set => _wither = value; }
        int IMonsterAttack.Sharp { get => _sharp; set => _sharp = value; }
        int IMonsterAttack.GutsCost { get => _gutsCost; set => _gutsCost = value; }
        bool IMonsterAttack.SGutsSteal { get => _sGutsSteal; set => _sGutsSteal = value; }
        bool IMonsterAttack.SLifeSteal { get => _sLifeSteal; set => _sLifeSteal = value; }
        bool IMonsterAttack.SLifeRecovery { get => _sLifeRecovery; set => _sLifeRecovery = value; }
        bool IMonsterAttack.SDamageSelfMiss { get => _sDamageSelfMiss; set => _sDamageSelfMiss = value; }
        bool IMonsterAttack.SDamageSelfHit { get => _sDamageSelfHit; set => _sDamageSelfHit = value; }
        public string _name { get; private set; }
        public ErrantryType _errantry { get; private set; }
        public TechRange _range { get; private set; }
        public TechNature _nature { get; private set; }
        public TechType _scaling { get; private set; }
        public int _hit { get; private set; }
        public int _force { get; private set; }
        public int _wither { get; private set; }
        public int _sharp { get; private set; }
        public int _gutsCost { get; private set; }

        public bool _sGutsSteal { get; private set; }
        public bool _sLifeSteal { get; private set; }
        public bool _sLifeRecovery { get; private set; }
        public bool _sDamageSelfMiss { get; private set; }
        public bool _sDamageSelfHit { get; private set; }


        public MonsterTechnique (int id, byte[] rawtech) {
            _rawTechnique = rawtech;

            _id = id;

            _name = "0thru15NDONE"; // 0-15 : There is a special string parse required for techniques. Not complete.
            _errantry = (ErrantryType) rawtech[ 16 ];
            _range = (TechRange) rawtech[ 17 ];
            _nature = (TechNature) rawtech[ 18 ];
            _scaling = (TechType) rawtech[ 19 ];

            // 20 is 'Available' which means effectively nothing.

            _hit = (sbyte) rawtech[ 21 ];
            _force = (sbyte) rawtech[ 22 ];
            _wither = (sbyte) rawtech[ 23 ];
            _sharp = (sbyte) rawtech[ 24 ];
            _gutsCost = (sbyte)  rawtech[ 25 ];

            _sGutsSteal = ( rawtech[ 26 ] == 1 );
            _sLifeSteal = ( rawtech[ 27 ] == 1 );
            _sLifeRecovery = ( rawtech[ 28 ] == 1 );
            _sDamageSelfMiss = (rawtech[ 29 ] == 1);
            _sDamageSelfHit = ( rawtech[ 30 ] == 1 );

            // 31 is always FF, indicating the end of the technique.

            _techValue = 30 + (int) ( ( _hit * 1.5 ) + ( _force * 1.8 ) + ( _wither * 1.2 ) + ( _sharp / 1.25 ) - _gutsCost);

            if ( _sGutsSteal || _sLifeSteal || _sLifeRecovery ) { _techValue = (int) (_techValue * 1.3); }
            if ( _sDamageSelfMiss || _sDamageSelfHit ) _techValue = (int) (_techValue * 0.85);
        }

        public override string ToString () {
            return "TECH: { NAME: " + _name + ", ERR: " + _errantry + ", RANGE: " + _range + ", NATURE: " + _nature + ", SCALE: " + _scaling + ", HIT%: " + _hit + ", FORCE: " + _force + ", WITH: " + _wither + ", SHARP: " + _sharp + ", GUTS: " + _gutsCost + ", VALUE: " + _techValue + "}";
        }


        /// <summary> Reads from the provided FileStream and returns a byte list. 0 = Basic, 1 Hit, 2 Heavy, 3 Withering, 4 Sharp, 5 Special, 6 Invalid </summary>
        public static List<MonsterTechnique> ParseTechniqueFile ( FileStream fs ) {
            List<MonsterTechnique> techniques = new List<MonsterTechnique>();

            long tpos = 0;

            for ( var i = 0; i < 24; i++ ) {
                fs.Position = i * 4;
                tpos = (long) fs.ReadByte(); tpos += (long) fs.ReadByte() * 256;
                TournamentData._mod.DebugLog( 2, "TECH HEADER: " + i + " - tpos " + tpos, Color.MediumOrchid );

                if ( tpos != 0xFFFF ) {
                    fs.Position = (long) tpos;

                    byte[] rawtech = new byte[32];
                    for ( var nb = 0; nb < 32; nb++ ) {
                        rawtech[ nb ] = (byte) fs.ReadByte(); }

                    byte slot = (byte) ( ( tpos - 0x60 ) / 0x20 );
                    MonsterTechnique mt = new MonsterTechnique(slot, rawtech);
                    TournamentData._mod.DebugLog( 2, "TECHNIQUE READ " + mt, Color.MediumOrchid );
                    techniques.Add( mt );
                }
            }

            fs.Close();
            return techniques;
        }


        // TODO: Move these functions into the base mod.

    }
}
