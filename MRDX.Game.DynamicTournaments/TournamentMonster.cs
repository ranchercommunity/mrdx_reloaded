using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.DynamicTournaments {
    public class TournamentMonster {
        public byte[] raw_bytes = new byte[ 60 ];
        #region Raw Variables
        private string _name;
        private MonsterGenus _breed_main;
        private MonsterGenus _breed_sub;
        private ushort _stat_lif;
        private ushort _stat_pow;
        private ushort _stat_def;
        private ushort _stat_ski;
        private ushort _stat_spd;
        private ushort _stat_int;

        private byte _per_nature;
        private byte _per_fear;
        private byte _per_spoil;
        private uint _techniques;
        private byte _arena_movespeed;
        private byte _arena_gutsrate;
        private ushort _battle_specials;
        #endregion

        // 0 - 25 : Monster Name - MUST END IN FF00 - (12 Characters Long + FF00)
        public string name {
            get => _name; set {
                _name = value.Substring( 0, Math.Min( value.Length, 12 ) );
                var b = Mr2StringExtension.AsBytes( Mr2StringExtension.AsMr2( value ) );
                for ( var i = 0; i < 26; i++ ) {
                    raw_bytes[ i ] = (byte) ( i < b.Length ? b[ i ] : 0 );
                }
            }
        }

        // 26, 27 : Monster Species / Subspecies
        public MonsterGenus breed_main { get => _breed_main; set { _breed_main = value; raw_bytes[ 26 ] = (byte) value; } }
        public MonsterGenus breed_sub { get => _breed_sub; set { _breed_sub = value; raw_bytes[ 27 ] = (byte) value; } }

        // 2 Bytes Per Stat, 28-39, Life-Pow-Def-Ski-Spd-Int
        public ushort stat_lif { get => _stat_lif; set { _stat_lif = value; raw_bytes[ 28 ] = (byte) ( value & 0xff ); raw_bytes[ 29 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }
        public ushort stat_pow { get => _stat_pow; set { _stat_pow = value; raw_bytes[ 30 ] = (byte) ( value & 0xff ); raw_bytes[ 31 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }
        public ushort stat_def { get => _stat_def; set { _stat_def = value; raw_bytes[ 32 ] = (byte) ( value & 0xff ); raw_bytes[ 33 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }
        public ushort stat_ski { get => _stat_ski; set { _stat_ski = value; raw_bytes[ 34 ] = (byte) ( value & 0xff ); raw_bytes[ 35 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }
        public ushort stat_spd { get => _stat_spd; set { _stat_spd = value; raw_bytes[ 36 ] = (byte) ( value & 0xff ); raw_bytes[ 37 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }
        public ushort stat_int { get => _stat_int; set { _stat_int = value; raw_bytes[ 38 ] = (byte) ( value & 0xff ); raw_bytes[ 39 ] = (byte) ( ( value & 0xff00 ) >> 8 ); } }

        public int stat_total { get { return _stat_lif + _stat_pow + _stat_ski + _stat_spd + _stat_def + _stat_int; } }

        public byte per_nature { get => _per_nature; set { _per_nature = value; raw_bytes[ 40 ] = value; } }
        public byte per_fear { get => _per_fear; set { _per_fear = value; raw_bytes[ 41 ] = value; } }
        public byte per_spoil { get => _per_spoil; set { _per_spoil = value; raw_bytes[ 42 ] = value; } }

        // 43-46 Techniques, Represented by Bits (First Byte is always empty)
        public uint techniques {
            get => _techniques; set {
                _techniques = value;
                raw_bytes[ 43 ] = 0;
                raw_bytes[ 44 ] = (byte) ( value & 0xff );
                raw_bytes[ 45 ] = (byte) ( ( value & 0xff00 ) >> 8 );
                raw_bytes[ 46 ] = (byte) ( ( value & 0xff0000 ) >> 16 );
            }
        }

        public byte arena_movespeed { get => _arena_movespeed; set { _arena_movespeed = value; raw_bytes[ 48 ] = value; } }
        public byte arena_gutsrate { get => _arena_gutsrate; set { _arena_gutsrate = value; raw_bytes[ 49 ] = value; } }

        // 52-53 Battle Specials, Represented by Bits (Only Last 13 Bits Used | 0001111111111111 )
        public ushort battle_specials { get => _battle_specials; set { _battle_specials = value; raw_bytes[ 52 ] = (byte) ( ( value & 0xff00 ) >> 8 ); raw_bytes[ 53 ] = (byte) ( value & 0xff ); } }

        public override string ToString () {
            return name + ": " +
                "[LIF:" + stat_lif + ", POW:" + stat_pow + ", SKI:" + stat_ski + ", SPD:" + stat_spd + ", DEF:" + stat_def + ", INT:" + stat_int + "], " +
                "[NAT:" + per_nature + ", FEAR:" + per_fear + ", SPOIL:" + per_spoil + "], " +
                "[TECHS:" + techniques + ", MOV:" + arena_movespeed + ", GUTS:" + arena_gutsrate + ", BATSPC:" + battle_specials + "]";
        }
        public TournamentMonster ( byte[] raw ) {
            for ( var i = 0; i < 60; i++ ) raw_bytes[ i ] = 0;
            parseBytesToMonster( raw );
        }


        private void parseBytesToMonster ( byte[] raw ) {

            int lc = 1;
            for ( var i = 0; i < raw.Length; i++ ) {
                if ( raw[ i ] == 255 ) { lc = i + 1; i = 9999; }
            }

            byte[] rawname = new byte[ lc ];
            for ( var i = 0; i < lc; i++ ) {
                rawname[ i ] = raw[ i ];
            }

            name = Mr2StringExtension.AsString( Mr2StringExtension.AsShorts( rawname ) );

            breed_main = (MonsterGenus) raw[ 26 ];
            breed_sub = (MonsterGenus) raw[ 27 ];

            stat_lif = (ushort) ( raw[ 28 ] + ( raw[ 29 ] << 8 ) );
            stat_pow = (ushort) ( raw[ 30 ] + ( raw[ 31 ] << 8 ) );
            stat_def = (ushort) ( raw[ 32 ] + ( raw[ 33 ] << 8 ) );
            stat_ski = (ushort) ( raw[ 34 ] + ( raw[ 35 ] << 8 ) );
            stat_spd = (ushort) ( raw[ 36 ] + ( raw[ 37 ] << 8 ) );
            stat_int = (ushort) ( raw[ 38 ] + ( raw[ 39 ] << 8 ) );

            per_nature = raw[ 40 ];
            per_fear = raw[ 41 ];
            per_spoil = raw[ 42 ];

            techniques = (uint) ( raw[ 44 ] + ( raw[ 45 ] << 8 ) + ( raw[ 46 ] << 16 ) );

            arena_movespeed = raw[ 48 ];
            arena_gutsrate = raw[ 49 ];

            battle_specials = (ushort) ( ( raw[ 52 ] << 8 ) + raw[ 53 ] );

            raw_bytes[ 58 ] = 0xFF;
            raw_bytes[ 59 ] = 0xFF;

        }
    }
}