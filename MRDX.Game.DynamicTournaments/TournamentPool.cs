using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.DynamicTournaments
{
    public class TournamentPool {
        public TournamentData tournamentData;
        public EMonsterRanks _monsterRank;
        public ETournamentPools _tournamentPool;

        public string _name = "Pool";
        public int _minimumSize = 0;
        public int _indexStart = 0;
        public int _indexEnd = 0;

        public int stat_start = 0;
        public int stat_end = 0;

        public int tournament_tier = 0; // Used for adding techniques and specials to new monsters generated for this tournament

        public List<ABD_TournamentMonster> monsters = new List<ABD_TournamentMonster>();

        public TournamentPool ( TournamentData data, EMonsterRanks rank, ETournamentPools pool, string name, int size, int istart, int iend, int sstart, int send, int tier ) {
            tournamentData = data;
            _monsterRank = rank;
            _tournamentPool = pool;

            _name = name;
            _minimumSize = size;
            _indexStart = istart;
            _indexEnd = iend;

            stat_start = sstart;
            stat_end = send;

            tournament_tier = tier;
        }

        public void MonsterAdd ( ABD_TournamentMonster m ) {
            if ( !monsters.Contains( m ) ) {
                monsters.Add( m );
                m.pools.Add( this );
            }
        }

        public void MonsterRemove ( ABD_TournamentMonster m ) {
            if ( monsters.Contains( m ) ) {
                monsters.Remove( m );
                m.pools.Remove( this );
            }
        }

        /// <summary>
        /// This function promotes two sets of monsters. 
        /// 1. A random monster from the class, extremely weighted towards any monsters approaching the top of the soft stat cap.
        /// 2. Any monsters remaining in the class that are over the soft stat cap by a small amount.
        /// </summary>
        /// <param name="newPool"></param>
        public void MonstersPromoteToNewPool ( TournamentPool newPool ) {
            TournamentData._mod.DebugLog( 1, "Promoting monsters from " + _name + " to " + newPool._name, Color.LightBlue );
            int stattotal = 0; ABD_TournamentMonster promoted;
            promoted = monsters[ 0 ];

            foreach ( ABD_TournamentMonster abdm in monsters ) {
                stattotal += Math.Max(abdm.monster.stat_total - (stat_end - 100), 1);
            }

            if ( stattotal < 20 ) { TournamentData._mod.DebugLog( 1, "Tournament Stat Totals dangeorusly low. Growth rates may be too low.", Color.Yellow ); }

            stattotal = Random.Shared.Next() % stattotal;
            for ( var i = 0; i < monsters.Count; i++ ) {
                int mvalue = Math.Max( monsters[ i ].monster.stat_total - ( stat_end - 100 ), 1 ); ;
                stattotal -= mvalue;
                promoted = monsters[ i ];
                if ( stattotal <= 0 ) { break; }
            }

            MonsterPromoteToNewPool( promoted, newPool );

            for ( var i = monsters.Count() - 1; i >= 0; i-- ) { 
                if ( monsters[i].monster.stat_total - 100 > stat_end ) {
                    MonsterPromoteToNewPool( monsters[ i ], newPool );
                }
            }
        }

        private void MonsterPromoteToNewPool( ABD_TournamentMonster monster, TournamentPool newPool ) {
            monster.LearnTechnique();
            monster._monsterRank = newPool._monsterRank;

            MonsterRemove( monster );
            newPool.MonsterAdd( monster );
            TournamentData._mod.DebugLog( 1, monster.monster.name + " promoted.", Color.LightBlue );
        }

        public void GenerateNewValidMonster ( List<MonsterGenus> available ) {
            TournamentData._mod.DebugLog( 1, "TP: Getting Breed", Color.AliceBlue );
            MonsterBreed breed = MonsterBreed.AllBreeds[ 0 ];

            // TODO: Smarter Logic for deciding which breed to make the monster!
            // // // // // // 

            if ( _tournamentPool == ETournamentPools.A_Phoenix ) {
                for ( var i = 0; i < 500; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( breed.breed_id == MonsterGenus.Phoenix ) {
                        break;
                    }
                }
            }

            else if ( _tournamentPool == ETournamentPools.B_Dragon ) {
                for ( var i = 0; i < 500; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( breed.breed_id == MonsterGenus.Dragon ) {
                        break;
                    }
                }
            }

            else if ( _tournamentPool == ETournamentPools.A_DEdge ) {
                for ( var i = 0; i < 500; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( breed.breed_id == MonsterGenus.Durahan ) {
                        break;
                    }
                }
            }

            else if ( _tournamentPool == ETournamentPools.F_Elder ) {
                for ( var i = 0; i < 250; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( available.Contains( breed.breed_id ) && available.Contains( breed.sub_id ) ) {
                        if ( breed.breed_id == MonsterGenus.Plant || breed.breed_id == MonsterGenus.Mew || breed.breed_id == MonsterGenus.Ape || breed.breed_id == MonsterGenus.Arrowhead
                            || breed.breed_id == MonsterGenus.Durahan || breed.breed_id == MonsterGenus.ColorPandora || breed.breed_id == MonsterGenus.Mock || breed.breed_id == MonsterGenus.Wracky ) {
                            break;
                        }
                    }
                }
            }

            else if ( _tournamentPool == ETournamentPools.F_Hero ) {
                for ( var i = 0; i < 250; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( available.Contains( breed.breed_id ) && available.Contains( breed.sub_id ) ) {
                        if ( breed.breed_id == MonsterGenus.Baku || breed.breed_id == MonsterGenus.Centaur || breed.breed_id == MonsterGenus.ColorPandora || breed.breed_id == MonsterGenus.Ducken ||
                                breed.breed_id == MonsterGenus.Gali || breed.breed_id == MonsterGenus.Hare || breed.breed_id == MonsterGenus.Henger || breed.breed_id == MonsterGenus.Mocchi ||
                                breed.breed_id == MonsterGenus.Niton || breed.breed_id == MonsterGenus.Tiger || breed.breed_id == MonsterGenus.Undine
                            ) {
                            break;
                        }
                    }
                }
            }

            else if ( _tournamentPool == ETournamentPools.F_Heel ) {
                for ( var i = 0; i < 250; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( available.Contains( breed.breed_id ) && available.Contains( breed.sub_id ) ) {
                        if ( breed.breed_id == MonsterGenus.Ape || breed.breed_id == MonsterGenus.Dragon || breed.breed_id == MonsterGenus.Joker || breed.breed_id == MonsterGenus.Kato
                            || breed.breed_id == MonsterGenus.Monol || breed.breed_id == MonsterGenus.Naga || breed.breed_id == MonsterGenus.Pixie || breed.breed_id == MonsterGenus.Suezo || breed.breed_id == MonsterGenus.Wracky ) {
                            break;
                        }
                    }
                }
            }

           else {
                for ( var i = 0; i < 100; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( available.Contains( breed.breed_id ) && available.Contains( breed.sub_id ) ) {
                        break;
                    }
                }
            }
            TournamentData._mod.DebugLog( 1, "Breed chosen " + breed.breed_id + "/" + breed.sub_id, Color.AliceBlue );
            GenerateNewValidMonster( breed );

        }
        private void GenerateNewValidMonster ( MonsterBreed breed ) {
            TournamentData._mod.DebugLog( 2, "TP: Generating", Color.AliceBlue );
            byte[] nmraw = new byte[ 60 ];// This doesn't matter, it gets completely overwritten below anyways.
            TournamentMonster nm = new TournamentMonster( nmraw );


            nm.breed_main = breed.breed_id; nm.breed_sub = breed.sub_id;
            TournamentData._mod.DebugLog( 3, "TP: Breed " + nm.breed_main + " " + nm.breed_sub, Color.AliceBlue );
            // // // // // //

            ABD_TournamentMonster abdm = new ABD_TournamentMonster( nm );

            abdm.monster.name = TournamentData._random_name_list[ Random.Shared.Next() % TournamentData._random_name_list.Length ];
            abdm.monster.stat_lif = 90;
            abdm.monster.stat_pow = 50;
            abdm.monster.stat_ski = 50;
            abdm.monster.stat_spd = 50;
            abdm.monster.stat_def = 50;
            abdm.monster.stat_int = 50;

            abdm.monster.per_nature = (byte) ( Random.Shared.Next() % 255 );
            abdm.monster.per_fear = (byte) ( Random.Shared.Next() % 25 );
            abdm.monster.per_spoil = (byte) ( Random.Shared.Next() % 25 );

            abdm.monster.arena_movespeed = (byte) ( Random.Shared.Next() % 4 ); // TODO: Where does this come from?
            abdm.monster.arena_gutsrate = (byte) ( 7 + Random.Shared.Next() % 14 ); // 7 - 20?

            abdm.monster.battle_specials = (byte) (Random.Shared.Next() % 4);

            // Assign the first two basics, then 25% for each additional basic.
            var firstbasic = 0;
            for ( var j = 0; j < 24; j++ ) {
                if ( ( ( abdm.breedInfo.technique_basics >> j ) & 1 ) == 1 ) { 
                    if ( firstbasic < 2 || Random.Shared.Next() % 4 == 0 ) { firstbasic++; abdm.monster.techniques = abdm.monster.techniques + (uint) ( 1 << j ); }
                }
            }
            TournamentData._mod.DebugLog( 3, "TP: Basics Setup", Color.AliceBlue );

            // This is significantly messing with growth rates across the board. Going to manually set the lifespan afterwards based upon the rank.
            while ( abdm.monster.stat_total < stat_start ) {
                abdm.AdvanceMonth();
            }
            TournamentData._mod.DebugLog( 3, "TP: Stats Generated", Color.AliceBlue );

            for ( int i = 0; i < tournament_tier; i++ ) {
                abdm.LearnTechnique();
            }
            TournamentData._mod.DebugLog( 3, "TP: Techs", Color.AliceBlue );

            // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
                    if ( _monsterRank == EMonsterRanks.L ) { abdm.lifespan = (ushort) ( 8 + ( TournamentData.LifespanRNG.Next() % 5 ) ); }
            else    if ( _monsterRank == EMonsterRanks.M ) { abdm.lifespan = (ushort) ( 10 + ( TournamentData.LifespanRNG.Next() % 5 ) ); }
            else    if ( _monsterRank == EMonsterRanks.S ) { abdm.lifespan = (ushort) ( 12 + ( TournamentData.LifespanRNG.Next() % 7 ) ); }
            else    if ( _monsterRank == EMonsterRanks.A ) { abdm.lifespan = (ushort) ( 14 + ( TournamentData.LifespanRNG.Next() % 7 ) ); }
            else    if ( _monsterRank == EMonsterRanks.B ) { abdm.lifespan = (ushort) ( abdm.lifetotal - ( 16 + ( TournamentData.LifespanRNG.Next() % 9 ) ) ); }
            else    if ( _monsterRank == EMonsterRanks.C ) { abdm.lifespan = (ushort) ( abdm.lifetotal - ( 6 + ( TournamentData.LifespanRNG.Next() % 7 ) ) ); }
            else    if ( _monsterRank == EMonsterRanks.D ) { abdm.lifespan = (ushort) (abdm.lifetotal - ( 2 + ( TournamentData.LifespanRNG.Next() % 5 ) ) ); }
            else    if ( _monsterRank == EMonsterRanks.E ) { abdm.lifespan = abdm.lifetotal; }
            abdm.alive = true;

            abdm.PromoteToRank( _monsterRank );
            
                TournamentData._mod.DebugLog( 2, "TP: Complete", Color.AliceBlue );
            tournamentData.monsters.Add( abdm );
            this.MonsterAdd( abdm );
        }


        /// <summary>
        /// Adds the tournament participants to the provided list. This is a random selection of [_minimumSize] monsters.
        /// </summary>
        /// <param name="participants"></param>
        public void AddTournamentParticipants ( List<ABD_TournamentMonster> participants ) {
            for ( var i = 0; i < _minimumSize; i++ ) { participants.Add( monsters[ i ] ); }
        }

    }
}
