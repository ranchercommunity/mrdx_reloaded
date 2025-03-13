using System;
using System.Collections;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices.Marshalling;
using System.Xml.Linq;
using MRDX.Base.ExtractDataBin.Interface;
using MRDX.Base.Mod;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Mod.Interfaces;
//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public enum EMonsterRanks { L, M, S, A, B, C, D, E };
public enum ETournamentPools { L, M, S, A, B, C, D, E,
    A_Phoenix, A_DEdge, B_Dragon, F_Hero, F_Heel, F_Elder,
    S_FIMBA, A_FIMBA, B_FIMBA, C_FIMBA, D_FIMBA,
    S_FIMBA2, A_FIMBA2, B_FIMBA2, C_FIMBA2, D_FIMBA2, X_MOO
}
public class TournamentData
{
    public static Mod _mod;
    public static ILogger _logger;
    public static Config _configuration;
    
    public static string[] _random_name_list = ["Cimasio", "Kyrades", "Ambroros", "Teodeus", "Lazan", "Pegetus", "Perseos", "Asandrou", "Agametrios", 
        "Lazion", "Morphosyne", "Gelantinos", "Narkelous", "Taloclus", "Baltsalus", "Hypnaeon", "Atrol", "Alexede", "Baccinos", "Idastos", "Ophyroe","Larissa", "Asperata",
        "Alnifolia","Dentala","Celsa","Hempera","Laurel","Haldiphe","Saffronea", "Quinn",
        "Poplarbush","Snowdrop","Funnyfluff","Firo","Limespice","Herb","Twinklespa","Spring","Shinyglade",
        "Almond","Foggytree","Pecan","Jesterfeet","Skylark","Rainbowhor","Snow","Oakswamp","Liri", "Briarpuff",
        "Extos","Grimes","Talis","Anemia","Tinder","Neige","Lec","Chinook","Graund","Greidax","Pigatt",
        "Kuanezz","Nidar","Danuzz","Razodrug","Krorodurr","Galae",
        "Tepua","Uvle","Ujay","Surlul","Razsa","Dezunu","Urabu","Sholgokoh","Abedin","Yetsi","Jaedrey",
        "Leadeth","Baudr","Araldyng","Gilparymr","Sawah","Mazraeh",
        "Aayuh","Grimstriker","Twistmight","Pyregaze","Heatmarch","Omega","Dalton","Alpha","Beta","Gamma","Zeta","Phi","Jaeger",
        "Dimbranch","Raindust","Hillbrace","Storm","Sohish","Sunhol","Ehtae","Lilsof","Ghostsign","Snowlock","Mystic","Nevi","Azahz","Owen","Denzel","Robinson"];

    public bool _initialized = false;
    public bool _firstweek = false;
    public static Random GrowthRNG = new Random(0);
    public static Random LifespanRNG = new Random(1);

    public uint _currentWeek = 0;
    public List<MonsterGenus> _unlockedTournamentBreeds = new List<MonsterGenus>();

    public Dictionary<ETournamentPools, TournamentPool> tournamentPools = new Dictionary<ETournamentPools, TournamentPool>();
    public List<ABD_TournamentMonster> monsters = new List<ABD_TournamentMonster>();

    public List<byte[]> startingMonsterTemplates = new List<byte[]>();
   
    public TournamentData(Mod m, ILogger logger, Config config) 
    {
        TournamentData._mod = m;
        TournamentData._logger = logger;
        TournamentData._configuration = config;

        tournamentPools.Add( ETournamentPools.L, new TournamentPool( this, EMonsterRanks.L, ETournamentPools.L, "Legend", 2, 52, 53,     _configuration._confABD_tournament_rank_m4,        9999, 10 ) );
        tournamentPools.Add( ETournamentPools.M, new TournamentPool( this, EMonsterRanks.M, ETournamentPools.M, "Major 4", 8, 54, 61,    _configuration._confABD_tournament_rank_s,         _configuration._confABD_tournament_rank_m4 - 1, 8 ) );
        tournamentPools.Add( ETournamentPools.S, new TournamentPool( this, EMonsterRanks.S, ETournamentPools.S, "S Rank", 8, 1, 8,       _configuration._confABD_tournament_rank_a,         _configuration._confABD_tournament_rank_s - 1, 6));
        tournamentPools.Add( ETournamentPools.A, new TournamentPool( this, EMonsterRanks.A, ETournamentPools.A, "A Rank", 8, 9, 16,      _configuration._confABD_tournament_rank_b,         _configuration._confABD_tournament_rank_a - 1, 5));
        tournamentPools.Add( ETournamentPools.B, new TournamentPool( this, EMonsterRanks.B, ETournamentPools.B, "B Rank", 10, 17, 26,    _configuration._confABD_tournament_rank_c,         _configuration._confABD_tournament_rank_b - 1, 4));
        tournamentPools.Add( ETournamentPools.C, new TournamentPool( this, EMonsterRanks.C, ETournamentPools.C, "C Rank", 10, 27, 36,    _configuration._confABD_tournament_rank_d,         _configuration._confABD_tournament_rank_c - 1, 2));
        tournamentPools.Add( ETournamentPools.D, new TournamentPool( this, EMonsterRanks.D, ETournamentPools.D, "D Rank", 8, 37, 44,     _configuration._confABD_tournament_rank_e,         _configuration._confABD_tournament_rank_d - 1, 1));
        tournamentPools.Add( ETournamentPools.E, new TournamentPool( this, EMonsterRanks.E, ETournamentPools.E, "E Rank", 6, 45, 50,     _configuration._confABD_tournament_rank_e - 250,   _configuration._confABD_tournament_rank_e - 1, 0));

        tournamentPools.Add( ETournamentPools.X_MOO, new TournamentPool( this, EMonsterRanks.L, ETournamentPools.X_MOO, "L - Moo", 6, 51, 51, _configuration._confABD_tournament_rank_m4 + 250, 9999, 10 ) );

        tournamentPools.Add( ETournamentPools.A_Phoenix, new TournamentPool( this, EMonsterRanks.A,     ETournamentPools.A_Phoenix, "A - Phoenix", 3, 62, 64, _configuration._confABD_tournament_rank_b, _configuration._confABD_tournament_rank_a, 6 ) );
        tournamentPools.Add( ETournamentPools.A_DEdge, new TournamentPool( this, EMonsterRanks.A,       ETournamentPools.A_DEdge,   "A - Double Edge", 1, 66, 66, _configuration._confABD_tournament_rank_b, _configuration._confABD_tournament_rank_a, 6 ) );
        tournamentPools.Add( ETournamentPools.B_Dragon, new TournamentPool( this, EMonsterRanks.B,      ETournamentPools.B_Dragon,  "B - Dragon Tusk", 1, 65, 65, _configuration._confABD_tournament_rank_c, _configuration._confABD_tournament_rank_b, 6 ) );
        tournamentPools.Add( ETournamentPools.F_Hero,  new TournamentPool( this, EMonsterRanks.A,       ETournamentPools.F_Hero,    "F - Hero", 5, 67, 71, _configuration._confABD_tournament_rank_b, 9999, 7 ) );
        tournamentPools.Add( ETournamentPools.F_Heel,  new TournamentPool( this, EMonsterRanks.A,       ETournamentPools.F_Heel,    "F - Heel", 5, 72, 76, _configuration._confABD_tournament_rank_b, 9999, 7 ) );
        tournamentPools.Add( ETournamentPools.F_Elder, new TournamentPool( this, EMonsterRanks.A,       ETournamentPools.F_Elder,   "F - Elder", 3, 77, 79, _configuration._confABD_tournament_rank_b, 9999, 8 ) );

        tournamentPools.Add( ETournamentPools.S_FIMBA, new TournamentPool(this, EMonsterRanks.S,        ETournamentPools.S_FIMBA,   "S FIMBA", 4, 80, 83, _configuration._confABD_tournament_rank_a + 200, _configuration._confABD_tournament_rank_m4, 8 ) );
        tournamentPools.Add( ETournamentPools.A_FIMBA, new TournamentPool( this, EMonsterRanks.A,       ETournamentPools.A_FIMBA,   "A FIMBA", 4, 84, 87, _configuration._confABD_tournament_rank_b + 200, _configuration._confABD_tournament_rank_a, 6 ) );
        tournamentPools.Add( ETournamentPools.B_FIMBA, new TournamentPool( this, EMonsterRanks.B,       ETournamentPools.B_FIMBA,   "B FIMBA", 4, 88, 91, _configuration._confABD_tournament_rank_c + 200, _configuration._confABD_tournament_rank_b, 5 ) );
        tournamentPools.Add( ETournamentPools.C_FIMBA, new TournamentPool( this, EMonsterRanks.C,       ETournamentPools.C_FIMBA,   "C FIMBA", 4, 92, 95, _configuration._confABD_tournament_rank_d + 100, _configuration._confABD_tournament_rank_c, 4 ) );
        tournamentPools.Add( ETournamentPools.D_FIMBA, new TournamentPool( this, EMonsterRanks.D,       ETournamentPools.D_FIMBA,   "D FIMBA", 4, 96, 99, _configuration._confABD_tournament_rank_e + 100, _configuration._confABD_tournament_rank_d, 2 ) );

        tournamentPools.Add( ETournamentPools.S_FIMBA2, new TournamentPool( this, EMonsterRanks.S,      ETournamentPools.S_FIMBA2,  "S FIMBA2", 4, 100, 103, _configuration._confABD_tournament_rank_a + 200, _configuration._confABD_tournament_rank_m4, 8 ) );
        tournamentPools.Add( ETournamentPools.A_FIMBA2, new TournamentPool( this, EMonsterRanks.A,      ETournamentPools.A_FIMBA2,  "A FIMBA2", 4, 104, 107, _configuration._confABD_tournament_rank_b + 200, _configuration._confABD_tournament_rank_a, 6 ) );
        tournamentPools.Add( ETournamentPools.B_FIMBA2, new TournamentPool( this, EMonsterRanks.B,      ETournamentPools.B_FIMBA2,  "B FIMBA2", 4, 108, 111, _configuration._confABD_tournament_rank_c + 200, _configuration._confABD_tournament_rank_b, 5 ) );
        tournamentPools.Add( ETournamentPools.C_FIMBA2, new TournamentPool( this, EMonsterRanks.C,      ETournamentPools.C_FIMBA2,  "C FIMBA2", 4, 112, 115, _configuration._confABD_tournament_rank_d + 100, _configuration._confABD_tournament_rank_c, 4 ) );
        tournamentPools.Add( ETournamentPools.D_FIMBA2, new TournamentPool( this, EMonsterRanks.D,      ETournamentPools.D_FIMBA2,  "D FIMBA2", 4, 116, 118, _configuration._confABD_tournament_rank_e + 100, _configuration._confABD_tournament_rank_d, 2 ) );
    }

    public void AddExistingMonster(TournamentMonster m, int id)
    {
        ETournamentPools pool = ETournamentPools.S;
        ABD_TournamentMonster abdm = new ABD_TournamentMonster(m);

        if ( id >= 1 && id <= 8 )           { pool = ETournamentPools.S; }
        else if ( id >= 9 && id <= 16 )     { pool = ETournamentPools.A; }
        else if ( id >= 17 && id <= 26 )    { pool = ETournamentPools.B; }
        else if ( id >= 27 && id <= 36 )    { pool = ETournamentPools.C; }
        else if ( id >= 37 && id <= 44 )    { pool = ETournamentPools.D; }
        else if ( id >= 45 && id <= 50 )    { pool = ETournamentPools.E; }

        else if ( id >= 51 && id <= 51 )    { pool = ETournamentPools.X_MOO; } 
        else if ( id >= 52 && id <= 53 )    { pool = ETournamentPools.L; }
        else if ( id >= 54 && id <= 61 )    { pool = ETournamentPools.M; }

        else if ( id >= 62 && id <= 64 )    { pool = ETournamentPools.A_Phoenix; }
        else if ( id >= 65 && id <= 65 )    { pool = ETournamentPools.B_Dragon; }
        else if ( id >= 66 && id <= 66 )    { pool = ETournamentPools.A_DEdge; }

        else if ( id >= 67 && id <= 71 )    { pool = ETournamentPools.F_Hero; }
        else if ( id >= 72 && id <= 76 )    { pool = ETournamentPools.F_Heel; }
        else if ( id >= 77 && id <= 79 )    { pool = ETournamentPools.F_Elder; }

        else if ( id >= 80 && id <= 83 )    { pool = ETournamentPools.S_FIMBA; }
        else if ( id >= 84 && id <= 87 )    { pool = ETournamentPools.A_FIMBA; }
        else if ( id >= 88 && id <= 91 )    { pool = ETournamentPools.B_FIMBA; }
        else if ( id >= 92 && id <= 95 )    { pool = ETournamentPools.C_FIMBA; }
        else if ( id >= 96 && id <= 99 )    { pool = ETournamentPools.D_FIMBA; }

        else if ( id >= 100 && id <= 103 )  { pool = ETournamentPools.S_FIMBA2; }
        else if ( id >= 104 && id <= 107 )  { pool = ETournamentPools.A_FIMBA2; }
        else if ( id >= 108 && id <= 111 )  { pool = ETournamentPools.B_FIMBA2; }
        else if ( id >= 112 && id <= 115 )  { pool = ETournamentPools.C_FIMBA2; }
        else if ( id >= 116 && id <= 119 )  { pool = ETournamentPools.D_FIMBA2; }

        abdm._monsterRank = tournamentPools[ pool ]._monsterRank;

        monsters.Add(abdm);
        tournamentPools[pool].MonsterAdd(abdm);
    }

    public void AddExistingMonster(ABD_TournamentMonster abdm) {
        monsters.Add( abdm );

        foreach ( TournamentPool pool in tournamentPools.Values ) {
            for ( var i = 0; i < 4; i++ ) {
                if ( (ETournamentPools) abdm._rawpools[ i ] == pool._tournamentPool ) {
                    pool.MonsterAdd( abdm );
                }
            }
        }
    }

    public void AdvanceWeek ( uint currentWeek, List<MonsterGenus> unlockedmonsters ) {
        _unlockedTournamentBreeds = unlockedmonsters;

        if ( !_initialized ) { _currentWeek = currentWeek; return; }
        if ( _firstweek ) { _currentWeek = currentWeek - 1; }

        while ( _currentWeek < currentWeek ) {
            _currentWeek++;

            if ( _currentWeek % 4 == 0 ) {
                AdvanceMonth();
            }

            if ( _currentWeek % 12 == 0 ) {
                AdvanceTournamentPromotions();
            }
        }

        _mod.DebugLog( 1, "Finished Advancing Weeks, Checking Pools", Color.Yellow );
        foreach ( TournamentPool pool in tournamentPools.Values ) {
            while ( pool.monsters.Count < pool._minimumSize ) {
                pool.GenerateNewValidMonster(_unlockedTournamentBreeds);
            }

            // Shuffle Monsters - TODO: This should happen weekly.
            ABD_TournamentMonster[] ml = pool.monsters.ToArray();
            Random.Shared.Shuffle( ml );
            pool.monsters = new List<ABD_TournamentMonster>( ml );
        }

        _firstweek = false;
    }
    public void AdvanceMonth()
    {
        _mod.DebugLog( 1, "Advancing month from TournamentData", Color.Blue );
        for (var i = monsters.Count - 1 ; i >= 0 ; i--) {
            var m = monsters[i];

            m.AdvanceMonth();
            if ( !m.alive ) {
                _mod.DebugLog( 3, m.monster.name + " has died. Removing from pools.", Color.Blue );
                for ( var j = m.pools.Count() - 1; j >= 0; j-- ) {
                    m.pools[j].MonsterRemove( m );
                }
                monsters.Remove( m );
                _mod.DebugLog( 3, m.monster.name + " removed from master pool list.", Color.Blue );
            }

            // TODO CONFIG TECHNIQUE RATE
            if ( Random.Shared.Next() % 25 == 0 ) {
                m.LearnTechnique();
            }
        }
    }

    private void AdvanceTournamentPromotions() {
        tournamentPools[ ETournamentPools.M ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.L ] );
        tournamentPools[ ETournamentPools.S ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.M ] );
        tournamentPools[ ETournamentPools.A ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.S ] );
        tournamentPools[ ETournamentPools.B ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.A ] );
        tournamentPools[ ETournamentPools.C ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.B ] );
        tournamentPools[ ETournamentPools.D ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.C ] );
        tournamentPools[ ETournamentPools.E ].MonstersPromoteToNewPool( tournamentPools[ ETournamentPools.D ] );
    }

    public List<ABD_TournamentMonster>GetTournamentMembers( int start, int end ) {
        List<ABD_TournamentMonster> participants = new List<ABD_TournamentMonster>();

        tournamentPools[ ETournamentPools.S ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.A ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.B ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.C ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.D ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.E ].AddTournamentParticipants( participants );

        // Edge Case to Handle the ONE SLOT that is skipped For Moo.
        participants.Add( tournamentPools[ ETournamentPools.S ].monsters[ 0 ] );

        tournamentPools[ ETournamentPools.L ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.M ].AddTournamentParticipants( participants );

        tournamentPools[ ETournamentPools.A_Phoenix ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.B_Dragon ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.A_DEdge ].AddTournamentParticipants( participants );

        tournamentPools[ ETournamentPools.F_Hero ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.F_Heel ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.F_Elder ].AddTournamentParticipants( participants );

        tournamentPools[ ETournamentPools.S_FIMBA ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.A_FIMBA ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.B_FIMBA ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.C_FIMBA ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.D_FIMBA ].AddTournamentParticipants( participants );

        tournamentPools[ ETournamentPools.S_FIMBA2 ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.A_FIMBA2 ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.B_FIMBA2 ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.C_FIMBA2 ].AddTournamentParticipants( participants );
        tournamentPools[ ETournamentPools.D_FIMBA2 ].AddTournamentParticipants( participants );

        return participants;
    }

    public void ClearAllData() {
        _initialized = false;

        monsters.Clear();
        foreach( TournamentPool pool in tournamentPools.Values ) {
            pool.monsters.Clear();
        }
    }
}



