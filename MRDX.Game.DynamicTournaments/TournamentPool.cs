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
            Logger.Info("Promoting monsters from " + _name + " to " + newPool._name, Color.LightBlue );
            int stattotal = 0; ABD_TournamentMonster promoted;
            promoted = monsters[ 0 ];

            foreach ( ABD_TournamentMonster abdm in monsters ) {
                stattotal += Math.Max(abdm.monster.stat_total - (stat_end - 100), 1);
            }

            if ( stattotal > 50 ) {
                stattotal = Random.Shared.Next() % stattotal;
                for ( var i = 0; i < monsters.Count; i++ ) {
                    int mvalue = Math.Max( monsters[ i ].monster.stat_total - ( stat_end - 100 ), 1 ); ;
                    stattotal -= mvalue;
                    promoted = monsters[ i ];
                    if ( stattotal <= 0 ) { break; }
                }

                MonsterPromoteToNewPool( promoted, newPool );
            }
            else { Logger.Info("Tournament Stat Totals dangeorusly low. Growth rates may be too low.", Color.Yellow ); } 

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
            Logger.Info(monster.monster.name + " promoted.", Color.LightBlue );
        }

        public void GenerateNewValidMonster ( List<MonsterGenus> available ) {
            Logger.Info("TP: Getting Breed", Color.AliceBlue );
            MonsterBreed breed = MonsterBreed.AllBreeds[ 0 ];

            if ( _tournamentPool == ETournamentPools.A_Phoenix ) {
                for ( var i = 0; i < 500; i++ ) {
                    breed = MonsterBreed.AllBreeds[ Random.Shared.Next() % MonsterBreed.AllBreeds.Count ];
                    if ( breed.breed_id == MonsterGenus.Phoenix || breed.sub_id == MonsterGenus.Phoenix ) {
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
                        if ( breed.sub_id == MonsterGenus.Unknown1 || breed.sub_id == MonsterGenus.Unknown2 || breed.sub_id == MonsterGenus.Unknown3 ||
                            breed.sub_id == MonsterGenus.Unknown4 || breed.sub_id == MonsterGenus.Unknown5 || breed.sub_id == MonsterGenus.Unknown6 ) {
                            if ( Random.Shared.NextDouble() < TournamentData._configuration._confDTP_species_unique ) { break; }
                        }

                        else { break; }
                    }
                }
            }
            Logger.Info("Breed chosen " + breed.breed_id + "/" + breed.sub_id, Color.AliceBlue );
            GenerateNewValidMonster( breed );

        }
        private void GenerateNewValidMonster ( MonsterBreed breed ) {
            Logger.Debug("TP: Generating", Color.AliceBlue );
            byte[] nmraw = new byte[ 60 ];// This doesn't matter, it gets completely overwritten below anyways.
            TournamentMonster nm = new TournamentMonster( nmraw );


            nm.breed_main = breed.breed_id; nm.breed_sub = breed.sub_id;
            Logger.Trace("TP: Breed " + nm.breed_main + " " + nm.breed_sub, Color.AliceBlue );
            // // // // // //

            ABD_TournamentMonster abdm = new ABD_TournamentMonster( nm );

            abdm.monster.name = TournamentData._random_name_list[ Random.Shared.Next() % TournamentData._random_name_list.Length ];
            abdm.monster.stat_lif = 80;
            abdm.monster.stat_pow = 1;
            abdm.monster.stat_ski = 1;
            abdm.monster.stat_spd = 1;
            abdm.monster.stat_def = 1;
            abdm.monster.stat_int = 1;

            abdm.monster.per_nature = (byte) Random.Shared.Next(255);
            abdm.monster.per_fear = (byte) Random.Shared.Next(25);
            abdm.monster.per_spoil = (byte) Random.Shared.Next(25);

            abdm.monster.arena_movespeed = (byte) Random.Shared.Next(4); // TODO: Where does this come from?
            abdm.monster.arena_gutsrate = (byte) Random.Shared.Next(7, 21); // 7 - 20?

            abdm.monster.battle_specials = (byte) Random.Shared.Next(4);

            // Attempt to assign three basics, weighted generally towards worse basic techs with variance.
            if ( abdm.breedInfo._techniques[ 0 ]._errantry == ErrantryType.Basic ) { 
                abdm.monster.techniques = abdm.monster.techniques | (uint) ( 1 << abdm.breedInfo._techniques[ 0 ]._id );
                abdm.techniques.Add( abdm.breedInfo._techniques[ 0 ] );
            }

            for ( var tc = 0; tc < 3; tc++ ) {
                IMonsterAttack tech = abdm.breedInfo._techniques[ 0 ];

                for ( var j = 1; j < abdm.breedInfo._techniques.Count; j++ ) {
                    var nt = abdm.breedInfo._techniques[ j ];
                    if ( nt._errantry == ErrantryType.Basic ) {
                        if ( nt._techValue - ( Random.Shared.Next() % 20 ) < tech._techValue ) {
                            tech = nt;
                        }
                    }
                }

                abdm.MonsterAddTechnique( tech );
            }
            Logger.Trace("TP: Basics Setup " + abdm.techniques.Count, Color.AliceBlue );

            // This is significantly messing with growth rates across the board. Going to manually set the lifespan afterwards based upon the rank.
            while ( abdm.monster.stat_total < stat_start ) {
                abdm.AdvanceMonth();
            }
            Logger.Trace("TP: Stats Generated", Color.AliceBlue );

            for ( int i = 0; i < tournament_tier; i++ ) {
                abdm.LearnTechnique();
            }
            Logger.Trace("TP: Techs", Color.AliceBlue );

            abdm.lifespan = _monsterRank switch
            {
                // Need this to account for bad growth rate monsters. At minimum monsters should be living for at least 8 months. Not a lot of time but enough to reduce churn.
                EMonsterRanks.L => (ushort)TournamentData.LifespanRNG.Next(12, 17),
                EMonsterRanks.M => (ushort)TournamentData.LifespanRNG.Next(16, 21),
                EMonsterRanks.S => (ushort)TournamentData.LifespanRNG.Next(20, 27),
                EMonsterRanks.A => (ushort)TournamentData.LifespanRNG.Next(24, 31),
                EMonsterRanks.B => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(14, 23)),
                EMonsterRanks.C => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(6, 13)),
                EMonsterRanks.D => (ushort)(abdm.lifetotal - TournamentData.LifespanRNG.Next(2, 7)),
                EMonsterRanks.E => abdm.lifetotal,
                _ => abdm.lifespan
            };
            abdm.alive = true;

            abdm.PromoteToRank( _monsterRank );
            
            Logger.Debug("TP: Complete", Color.AliceBlue );
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
