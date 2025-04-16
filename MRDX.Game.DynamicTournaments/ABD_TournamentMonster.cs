using System;
using System.Collections.Generic;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments
{
    public class ABD_TournamentMonster {
        public static Config _configuration;

        public TournamentMonster monster;
        public MonsterBreed breedInfo;
        public List<MonsterTechnique> techniques = new List<MonsterTechnique>();

        public List<TournamentPool> pools = new List<TournamentPool>();
        public byte[] _rawpools;

        public bool alive = true;
        public ushort lifetotal = 0;
        public ushort lifespan = 0;

        ushort growth_rate = 0;
        private byte growth_intensity;

        private byte _growth_lif;
        private byte _growth_pow;
        private byte _growth_ski;
        private byte _growth_spd;
        private byte _growth_def;
        private byte _growth_int;

        private List<byte> growth_options;

        private Config.E_ConfABD_TechInt _trainer_intelligence;

        public EMonsterRanks _monsterRank;

        public enum growth_groups { balanced, power, intel, defend, wither, speedy }
        growth_groups growth_group = growth_groups.balanced;


        public ABD_TournamentMonster ( TournamentMonster m ) {
            TournamentData._mod.DebugLog( 1, "Creating monster from game data.", Color.Lime );
            monster = m;
            breedInfo = MonsterBreed.GetBreedInfo( monster.breed_main, monster.breed_sub );

            lifetotal = (ushort) ( 40 + TournamentData.LifespanRNG.Next() % 31 ); // 40-70

            lifespan = lifetotal;
            lifespan -= (ushort) ( 4 * ( monster.stat_total / 500 ) ); // Take an arbitrary amount of life off for starting stats.

            growth_rate = (ushort) TournamentData._configuration._confABD_growth_monthly;
            for ( var i = 0; i < 4; i++ ) {
                growth_rate -= (ushort) TournamentData._configuration._confABD_growth_monthlyvariance;
                growth_rate += (ushort) ( TournamentData.GrowthRNG.Next() % ( 2 * TournamentData._configuration._confABD_growth_monthlyvariance ) );
            }

            growth_rate = Math.Clamp( growth_rate, (ushort) ( TournamentData._configuration._confABD_growth_monthly / 2.4 ), (ushort) ( TournamentData._configuration._confABD_growth_monthly * 1.8 ) );
            TournamentData._mod.DebugLog( 3, "Growth Post Variance" + growth_rate, Color.Lime );

            // This section applies special bonuses to monster breeds. These numbers needed to be way lower. Double Rare species were busted.
            List<MonsterGenus> bonuses = new List<MonsterGenus>(); List<MonsterGenus> bonuses2 = new List<MonsterGenus>();
            bonuses.AddRange( [MonsterGenus.Dragon, MonsterGenus.Centaur, MonsterGenus.Beaclon, MonsterGenus.Henger, MonsterGenus.Wracky, MonsterGenus.Durahan, MonsterGenus.Gali, MonsterGenus.Zilla, MonsterGenus.Bajarl, MonsterGenus.Phoenix,
            MonsterGenus.Metalner, MonsterGenus.Jill, MonsterGenus.Joker, MonsterGenus.Undine, MonsterGenus.Mock, MonsterGenus.Unknown1, MonsterGenus.Unknown2, MonsterGenus.Unknown3, MonsterGenus.Unknown4, MonsterGenus.Unknown5, MonsterGenus.Unknown6] );

            bonuses2.AddRange( [MonsterGenus.Dragon, MonsterGenus.Centaur, MonsterGenus.Beaclon, MonsterGenus.Durahan, MonsterGenus.Zilla, MonsterGenus.Phoenix, MonsterGenus.Metalner, MonsterGenus.Joker, MonsterGenus.Undine,
            MonsterGenus.Unknown1, MonsterGenus.Unknown2, MonsterGenus.Unknown3, MonsterGenus.Unknown4, MonsterGenus.Unknown5, MonsterGenus.Unknown6] );

            if ( bonuses.Contains( monster.breed_main ) ) { growth_rate += 3; }
            if ( bonuses2.Contains( monster.breed_main ) ) { growth_rate += 3; }
            if ( bonuses.Contains( monster.breed_sub ) ) { growth_rate += 2; }
            if ( bonuses2.Contains( monster.breed_sub ) ) { growth_rate += 2; }

            growth_rate = (ushort) ( 1 + ( growth_rate / 4 ) ); // Account for Prime Bonus, 4x
            TournamentData._mod.DebugLog( 3, "Growth Post Prime" + growth_rate, Color.Lime );

            TournamentData._mod.DebugLog( 2, "Monster Created: " + m.name + ", " + m.breed_main + "/" + m.breed_sub + ", LIFE: " + lifespan + ", GROWTH: " + growth_rate, Color.Lime );

            growth_group = (growth_groups) ( TournamentData.GrowthRNG.Next() % 6 );
            growth_intensity = (byte) ( TournamentData.GrowthRNG.Next() % 4 );

            SetupGrowthOptions();

            _trainer_intelligence = _configuration._confABD_techIntelligence;
            if ( Random.Shared.Next() % 10 == 0 ) { _trainer_intelligence = (Config.E_ConfABD_TechInt) (Random.Shared.Next() % 4);  }

            // Read through the techniques uint to add the correct technique IDs. TODO: I should have just done a static list and had an invalid tech slot in the empty ones.
            for ( var i = 0; i < 24; i++ ) {
                if ( (monster.techniques << i) % 2 == 1 ) {
                    for ( var j = 0; j < breedInfo._techniques.Count; j++ ) {
                        if ( breedInfo._techniques[j]._id == i ) { 
                            techniques.Add( breedInfo._techniques[ j ] ); 
                        }
                    }
                }
            }
        }

        public ABD_TournamentMonster ( byte[] rawabd ) {
            TournamentData._mod.DebugLog( 1, "Loading monster from ABD Save File.", Color.Lime );
            byte[] rawmonster = new byte[ 60 ];
            for ( var i = 0; i < 60; i++ ) { rawmonster[ i ] = rawabd[ i + 40 ]; }

            monster = new TournamentMonster( rawmonster );
            breedInfo = MonsterBreed.GetBreedInfo( monster.breed_main, monster.breed_sub );

            lifetotal = rawabd[ 0 ];
            lifespan = rawabd[ 2 ];
            growth_rate = rawabd[ 4 ];
            growth_group = (growth_groups) rawabd[ 6 ];
            growth_intensity = rawabd[ 7 ];

            _monsterRank = (EMonsterRanks) rawabd[ 8 ];

            _rawpools = new byte[ 4 ];
            for ( var i = 0; i < 4; i++ ) {
                _rawpools[ i ] = rawabd[ 10 + i ];
            }

            _growth_lif = rawabd[ 14 ];
            _growth_pow = rawabd[ 15 ];
            _growth_ski = rawabd[ 16 ];
            _growth_spd = rawabd[ 17 ];
            _growth_def = rawabd[ 18 ];
            _growth_int = rawabd[ 19 ];

            growth_options = new List<byte>();
            for ( var i = 14; i < 19; i++ ) {
                if ( rawabd[ i ] == 0 ) rawabd[ i ] = 1;
                for ( var j = 0; j < rawabd[ i ]; j++ ) {
                    growth_options.Add( (byte) (i - 14) );
                }
            }

            _trainer_intelligence = (Config.E_ConfABD_TechInt) rawabd[ 20 ];

            // Read through the techniques uint to add the correct technique IDs. TODO: I should have just done a static list and had an invalid tech slot in the empty ones.
            for ( var i = 0; i < 24; i++ ) {
                if ( ( monster.techniques << i ) % 2 == 1 ) {
                    for ( var j = 0; j < breedInfo._techniques.Count; j++ ) {
                        if ( breedInfo._techniques[ j ]._id == i ) { 
                            techniques.Add( breedInfo._techniques[ j ] ); 
                        }
                    }
                }
            }

            // DEBUG DEBUG DEBUG
            // for ( var i = 0; i < techniques.Count; i++ ) { TournamentData._mod.DebugLog( 2, monster.name + " has " + techniques[ i ], Color.Orange ); }
        }

        private void SetupGrowthOptions () {
            // Life, Pow, Skill, Speed, Def, Int

            growth_options = new List<byte>();
            byte[] gopts = [0];

            if ( growth_group == growth_groups.balanced ) { 
                gopts = [ 1, 1, 1, 1, 1, 1 ];

                if ( growth_intensity == 1 )        { gopts = [ 4, 3, 3, 3, 4, 3 ]; }
                else if ( growth_intensity == 2 )   { gopts = [ 3, 4, 3, 4, 3, 3 ]; }
                else if ( growth_intensity == 3 )   { gopts = [ 3, 3, 4, 3, 3, 4 ]; }
            }

            else if ( growth_group == growth_groups.power ) {
                gopts = [ 4, 5, 3, 1, 3, 2 ];

                if ( growth_intensity == 1 )      { gopts = [ 8, 11, 6, 2, 6, 3 ]; }
                else if ( growth_intensity == 2 ) { gopts = [ 10, 14, 7, 2, 6, 3 ]; }
                else if ( growth_intensity == 3 ) { gopts = [ 12, 20, 8, 2, 7, 2 ]; }
            }

            else if ( growth_group == growth_groups.intel ) { 
                gopts = [ 3, 1, 3, 4, 1, 5 ];

                if ( growth_intensity == 1 ) { gopts = [ 5, 2, 6, 5, 3, 9 ]; }
                else if ( growth_intensity == 2 ) { gopts = [ 8, 2, 9, 6, 3, 12 ]; }
                else if ( growth_intensity == 3 ) { gopts = [ 12, 2, 12, 8, 3, 18 ]; }
            }

            else if ( growth_group == growth_groups.defend ) { 
                gopts = [ 4, 2, 3, 2, 4, 2 ];

                if ( growth_intensity == 1 ) { gopts = [ 5, 2, 3, 2, 6, 2 ]; }
                else if ( growth_intensity == 2 ) { gopts = [ 13, 4, 5, 2, 15, 4 ]; }
                else if ( growth_intensity == 3 ) { gopts = [ 16, 4, 8, 3, 18, 4 ]; }
            }

            else if ( growth_group == growth_groups.wither ) { 
                gopts = [ 4, 3, 6, 5, 2, 4 ];

                if ( growth_intensity == 1 ) { gopts = [ 5, 3, 9, 7, 2, 5 ]; }
                else if ( growth_intensity == 2 ) { gopts = [ 6, 4, 13, 8, 2, 5 ]; }
                else if ( growth_intensity == 3 ) { gopts = [ 7, 4, 15, 9, 3, 5 ]; }
            }

            else if ( growth_group == growth_groups.speedy ) { 
                gopts = [ 4, 4, 5, 6, 2, 3 ];

                if ( growth_intensity == 1 ) { gopts = [ 5, 4, 5, 8, 2, 3 ]; }
                else if ( growth_intensity == 2 ) { gopts = [ 6, 4, 7, 10, 2, 4 ]; }
                else if ( growth_intensity == 3 ) { gopts = [ 6, 5, 9, 14, 2, 4 ]; }
            }

            // This is some fun variance. 16% chance of a stat (1 on average) getting a slight penalty or boost.
            // 5% chance of a stat being effectively completely randomized with no rhyme or reason.
            for ( var i = 0; i < gopts.Length; i++ ) {
                if ( Random.Shared.Next() % 6 == 0 ) { gopts[ i ] = (byte) ( gopts[ i ] - 1 + ( Random.Shared.Next() % 5 ) ); }
                if ( Random.Shared.Next() % 20 == 0 ) {
                    if ( i == 0 ) { gopts[ i ] = (byte) ( 4 + Random.Shared.Next() % 17 ); }
                    else { gopts[ i ] = (byte) ( 1 + Random.Shared.Next() % 20 ); }
                }
                if ( gopts[i] == 0 ) { gopts[ i ] = 1; }
            }

            _growth_lif = gopts[ 0 ];
            _growth_pow = gopts[ 1 ];
            _growth_ski = gopts[ 2 ];
            _growth_spd = gopts[ 3 ];
            _growth_def = gopts[ 4 ];
            _growth_int = gopts[ 5 ];

            for ( var i = 0; i < gopts.Length; i++ ) {
                for ( var j = 0; j < gopts[ i ]; j++ ) {
                    growth_options.Add( (byte) i );
                }
            }
        }

        public void AdvanceMonth () {
            int agegroup = 1;
            lifespan--;
            if ( lifespan == 0 ) { alive = false; }

            // Added an additonal prime grouping. This can result in a situation where a monster gets like, 2-3 months of optimal growth for ultra short life creatures but whatever, that's the price they pay. Longer life is better in this world objectively. Sorry! :(
            if ( ( lifetotal - lifespan ) > 9 ) { agegroup++; }
            if ( ( lifetotal - lifespan ) > 15 ) { agegroup++; }
            if ( ( lifetotal - lifespan ) > 22 ) { agegroup++; }
            if ( lifespan < 16 ) { agegroup--; }
            if ( lifespan < 6 ) { agegroup -= 2; }

            agegroup *= growth_rate;

            TournamentData._mod.DebugLog( 3, "Monster " + monster.name + " Advancing Month: [STATS: " + monster.stat_total + ", GROWTH:" + growth_rate + " CGROW: " + agegroup + ", LIFE: " + lifespan + "]", Color.Yellow );

            for ( var i = 0; i < agegroup; i++ ) {
                var stat = growth_options[ ( TournamentData.GrowthRNG.Next() % growth_options.Count() )];

                if ( stat == 0 && monster.stat_lif <= 999 ) { monster.stat_lif++; }
                else if ( stat == 1 && monster.stat_pow <= 999 ) { monster.stat_pow++; }
                else if ( stat == 2 && monster.stat_ski <= 999 ) { monster.stat_ski++; }
                else if ( stat == 3 && monster.stat_spd <= 999 ) { monster.stat_spd++; }
                else if ( stat == 4 && monster.stat_def <= 999 ) { monster.stat_def++; }
                else if ( stat == 5 && monster.stat_int <= 999 ) { monster.stat_int++; }
                else { i -= ( TournamentData.GrowthRNG.Next() % 5 > 0 ? 1 : 0 ); }
            }

            if ( monster.per_fear < 100 ) { monster.per_fear += (byte) ( TournamentData.GrowthRNG.Next() % 2 ); }
            if ( monster.per_spoil < 100 ) { monster.per_spoil += (byte) ( TournamentData.GrowthRNG.Next() % 2 ); }

            TournamentData._mod.DebugLog( 3, "Monster " + monster.name + " Completed Growth: [STATS: " + monster.stat_total + ", GROWTH:" + growth_rate + ", LIFE: " + lifespan + ", ALIVE: " + alive + "]", Color.Yellow );
        }

        public void LearnTechnique () { // TODO: Smarter Logic About which tech to get
            TournamentData._mod.DebugLog( 2, "Monster " + monster.name + " attempting to learn technique.", Color.Orange );

            Config.E_ConfABD_TechInt techint = _trainer_intelligence;
            MonsterTechnique tech = breedInfo._techniques[ 0 ];
            var techvariance = 25;  
            if ( techint == Config.E_ConfABD_TechInt.Minimal ) { techvariance = 30; }
            else if ( techint == Config.E_ConfABD_TechInt.Average ) { techvariance = 25; }
            else if ( techint == Config.E_ConfABD_TechInt.Smart ) { techvariance = 15; }
            else if ( techint == Config.E_ConfABD_TechInt.Genius ) { techvariance = 10; }

            List<int> weightedLearnPool = new List<int>();

            for ( var i = 0; i < breedInfo._techniques.Count; i++ ) {
                tech = breedInfo._techniques[ i ];

                if ( !techniques.Contains( tech ) ) {
                    var techval = tech._techValue + ( Random.Shared.Next() % techvariance );

                    if ( techint != Config.E_ConfABD_TechInt.Minimal ) {
                        if ( tech._scaling == TechType.Power && ( monster.stat_pow < monster.stat_int ) ) {
                            techval = (int) ( techval * 0.8 * ( monster.stat_pow / monster.stat_int ) );
                        }

                        else if ( tech._scaling == TechType.Intelligence && ( monster.stat_int < monster.stat_pow ) ) {
                            techval = (int) ( techval * 0.8 * ( monster.stat_int / monster.stat_pow ) );
                        }
                    }

                    if ( tech._errantry == ErrantryType.Basic && techniques.Count <= 4 ) { techval *= 2; }
                    if ( growth_group == growth_groups.power && tech._errantry == ErrantryType.Heavy ) { techval *= 2; }
                    else if ( growth_group == growth_groups.intel && tech._errantry == ErrantryType.Skill ) { techval *= 2; }
                    else if ( growth_group == growth_groups.wither && tech._errantry == ErrantryType.Withering ) { techval *= 2; }
                    else if ( growth_group == growth_groups.speedy && tech._errantry == ErrantryType.Sharp ) { techval *= 2; }

                    if ( tech._errantry == ErrantryType.Special ) {
                        if ( ( _monsterRank == EMonsterRanks.S || _monsterRank == EMonsterRanks.A || _monsterRank == EMonsterRanks.B ) ) { techval *= 2; }
                        else { techval *= (int) 0.2; }
                    }

                    if ( techint == Config.E_ConfABD_TechInt.Smart || techint == Config.E_ConfABD_TechInt.Genius ) { techval = (int) (techval * 1.1); }

                    for ( var j = 0; j < techval; j++ ) {
                        weightedLearnPool.Add( tech._id );
                    }
                }
            }

            if ( weightedLearnPool.Count > 0 ) {
                var chosen = weightedLearnPool[ Random.Shared.Next() % weightedLearnPool.Count ];
                for ( var i = 0; i < breedInfo._techniques.Count; i++ ) { if ( breedInfo._techniques[ i ]._id == chosen ) { tech = breedInfo._techniques[ i ]; } }
                monster.techniques += (uint) ( 1 << tech._id );
                techniques.Add( tech );
                TournamentData._mod.DebugLog( 2, "Monster " + monster.name + " learned " + tech, Color.Orange );
            }

            if ( techint == Config.E_ConfABD_TechInt.Genius && techniques.Count > 6 && Random.Shared.Next() % 10 < techniques.Count ) {
                UnlearnTechnique();
            }

            

            /*List<byte> toLearn = new List<byte>();
            for ( var i = 0; i < 24; i++ ) {
                var type = breedInfo.technique_types[ i ];
                int bonusTech = 1;
                if ( type != 6 && ( ( monster.techniques >> i ) & 1 ) == 0 ) {
                    if ( growth_group == growth_groups.power && type == 2 ) { bonusTech += growth_intensity + 1; }
                    else if ( growth_group == growth_groups.intel && type == 1 ) { bonusTech += growth_intensity + 1; }
                    else if ( growth_group == growth_groups.wither && type == 3 ) { bonusTech += growth_intensity + 1; }
                    else if ( growth_group == growth_groups.speedy && type == 4 ) { bonusTech += growth_intensity + 1; }

                    if ( breedInfo.technique_types[ i ] == 5 ) {
                        if ( ( _monsterRank == EMonsterRanks.S || _monsterRank == EMonsterRanks.A || _monsterRank == EMonsterRanks.B ) ) { bonusTech = 3; }
                    }

                    for ( var t = 0; t < bonusTech; t++ ) { toLearn.Add( (byte) i ); }
                }
            }

            if ( toLearn.Count > 0 ) {
                monster.techniques += (uint) ( 1 << toLearn[ Random.Shared.Next() % toLearn.Count ] );
            }*/
        }

        public void UnlearnTechnique () {
            TournamentData._mod.DebugLog( 2, "Monster " + monster.name + " is attempting an unlearn a tech.", Color.Orange );
            if ( techniques.Count <= 6 ) { return; }

            List<int> weightedPool = new List<int>();
            var minVal = 1000;
            var tech = techniques[ 0 ];

            for ( var i = 0; i < techniques.Count; i++ ) {
                tech = techniques[ i ];
                if ( minVal > tech._techValue ) { minVal = tech._techValue; }
            }

            for ( var i = 0; i < techniques.Count; i++ ) {
                tech = techniques[ i ];
                var weight = 25 - ( tech._techValue - minVal );
                for ( var j = 0; j < weight; j++ ) { weightedPool.Add( tech._id ); }
            }

            if ( weightedPool.Count > 0 ) {
                var chosen = weightedPool[ Random.Shared.Next() % weightedPool.Count ];
                for ( var i = 0; i < breedInfo._techniques.Count; i++ ) { if ( breedInfo._techniques[ i ]._id == chosen ) { tech = breedInfo._techniques[ i ]; } }
                monster.techniques -= (uint) ( 1 << tech._id );
                techniques.Remove( tech );
                TournamentData._mod.DebugLog( 2, "Monster " + monster.name + " has unlearned " + tech, Color.Orange );
            }
        }


        /// <summary>
        /// Promotes a Monster to a specifc rank, learning specials at D and A ranks. 
        /// Relies on the order of the enum to work properly!
        /// </summary>
        public void PromoteToRank ( EMonsterRanks rank ) {
            if ( rank >= _monsterRank ) { _monsterRank = rank; return; }

            else {
                while ( _monsterRank != rank ) {
                    _monsterRank--;
                    if ( _monsterRank == EMonsterRanks.A ) { LearnBattleSpecial(); }
                    if ( _monsterRank == EMonsterRanks.D ) { LearnBattleSpecial(); }
                }
            }
        }

        public void LearnBattleSpecial () {
            monster.battle_specials |= (ushort) ( 1 << TournamentData.GrowthRNG.Next() % 13 );
        }

        public byte[] ToSaveFile () {
            // ABD_TournamentMonster data will consist of 40 new bytes, followed by the standard 60 tracked by the game for Tournament Monsters.
            // 0-1, Lifetotal
            // 2-3, Current Remaining Lifespan
            // 4-5, Growth Rate Per Months
            // 6, Growth Group (Enum)
            // 7, Growth Intensity (Enum)
            // 8-9, Monster Rank (Enum)
            // 10-13, TournamentPools (Enums)
            // 14-19, Growth Weights (Generated from 6/7)
            // 20, Trainer Intelligence

            byte[] data = new byte[ 40 + 60 ];

            data[ 0 ] = (byte) lifetotal;
            data[ 2 ] = (byte) lifespan;
            data[ 4 ] = (byte) growth_rate;
            data[ 6 ] = (byte) growth_group;
            data[ 7 ] = (byte) growth_intensity;
            data[ 8 ] = (byte) _monsterRank;

            data[ 10 ] = 0xFF; data[ 11 ] = 0xFF; data[ 12 ] = 0xFF; data[ 13 ] = 0xFF;
            for ( var i = 0; ( i < pools.Count() && i < 4 ); i++ ) {
                data[ 10 + i ] = (byte) pools[ i ]._tournamentPool;
            }

            data[ 14 ] = _growth_lif;
            data[ 15 ] = _growth_pow;
            data[ 16 ] = _growth_ski;
            data[ 17 ] = _growth_spd;
            data[ 18 ] = _growth_def;
            data[ 19 ] = _growth_int;

            data[ 20 ] = (byte) _trainer_intelligence;

            for ( var i = 0; i < 60; i++ ) {
                data[ i + 40 ] = monster.raw_bytes[ i ];
            }
            return data;
        }
    }

}
