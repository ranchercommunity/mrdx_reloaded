using System.Drawing;
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
        "Almond","Foggytree","Pecan","Jesterfeet","Skylark","Rainbow","Snow","Oakswamp","Liri", "Briarpuff",
        "Extos","Grimes","Talis","Anemia","Tinder","Neige","Lec","Chinook","Graund","Greidax","Pigatt",
        "Kuanezz","Nidar","Danuzz","Razodrug","Krorodurr","Galae",
        "Tepua","Uvle","Ujay","Surlul","Razsa","Dezunu","Urabu","Sholgokoh","Abedin","Yetsi","Jaedrey",
        "Leadeth","Baudr","Araldyng","Gilparymr","Sawah","Mazraeh",
        "Aayuh","Grimstriker","Twistmight","Pyregaze","Heatmarch","Omega","Dalton","Alpha","Beta","Gamma","Zeta","Phi","Jaeger",
        "Dimbranch","Raindust","Hillbrace","Storm","Sohish","Sunhol","Ehtae","Lilsof","Ghostsign","Snowlock","Mystic","Nevi","Azahz","Owen","Denzel","Robinson",
        "Blossom","Yarn","Skitter","Mercy","Firj","Blubber","Dribble","Angel","Tank","Dottie","Taugh","Liberi",
        "Thespia","Pirene","Isonei","Harrow","Bakano","Polo","Okal","Ochen","Mhina","Siaka","Tamba","Savane","Boukary","Traore","Yaya","Dia","Dio","Aaron","Prizo","Dimitri","Dashaco",
        "Mathis","Calamity","Buve","Hosho","Zimba","Tsun","Mawere","Rufaro","Emoger","Fida","Thorns","Saffron","Teddago","Skelyte","Chilleni","Slowhawk","Jagola",
        "Zhar","Dim","Cleoz","Rav","Membut","Dazam","Groznur","Aqrat","Azzac","Tergu","Kirchon","Nilla","Ricryll","Imnor","Lanceruil","Ballion",
        "Quosa","Yesnorin","Caina","Holyn","Athena","Nandra","Beratha","Libgalyn","Galin","Heleto","Faerona","Fairest","Chuckles","Darkness",
        "Shiner","Monkeytime","Noper","Willow","Grassfall","Misty","Mantle","Painscribe","Plainwood","Orbgold","Ragespear","Dawnward",
        "Clair","Caffal","Ronch","Pola","Moux","Ranteau","Nothier","Peseul","Astellon","Glide","Roughkiss","Mosswisp","Shadow","Autumn",
        "Voidbane","Voidsicle","Kingsmith","Kingly","Peasant","Pleasant","Swellow","Alexa","Luitgard","Ede","Medou","Branka","Devotee","Aura",
        "Outlaw","Jade","Nocturne","Jarvis","Beeps","Faint","Perkless","Yill","Quona","Washerguard","Eda","Rosery","Tapper","Undergrow","Ova",
        "Adamant","Silverlock","Dobby","Finx","Gar","Hope","Jewel","Kattery","Languish","Zephyr","Xilla","Cedar","Villa","Branx","Naught","Midas",
        "Atronaph", "Argus", "Aideen", "Alias", "Adonay", "Anno", "Apollo", "Aydin", "Asakoa", "Aviri", "Adelynn", "Arsonwheel", "Angerstomp",
        "Bajor", "Beekler", "Bobbles", "Buu", "Brainstorm", "Bracer", "Basselt", "Boggycreek", "Boggart", "Bahamut", "Baretree", "Birchbellow",
        "Calaphyx", "Cawcaws", "Cix", "Cerrusio", "Creator", "Clipse", "Conjus", "Chanceux", "Ciorliath", "Clearwish", 
        "David", "Dingus", "Dakadaka", "Dill", "Doodle", "Daydream", "Dreameater", "Diablo", "Diabolos", "Dunker", "Dragonfly", 
        "Eater", "Eo", "Endofall", "Exuberance", "Etresse", "Elan", "Entun", "Earthtender",
        "Fargus", "Fillero", "Ferrus", "Faunus", "Feathers", "Fuzzball", "Foolcaller",
        "Gronkula", "Gimmles", "Golox", "Gargamel", "Gutterman", "Gale", "Gemlashes", "Gotusloop", "Goldenboy",
        "Hardness", "Herman", "Hillox", "Hundredyear", "Hurlante", "Hazel", "Hanzel", "Hatemonger",
        "Io", "Iodine", "Iaz", "Illomens", "Incarnate", "Isaias", "Islecrusher", "Iaull", "Itong", "Iilos",
        "Jax", "Jack", "Jillian", "Jellyjam", "Julius", "Jasmine", "Jazlynn",
        "Kawkaws", "Ki", "Kallus", "Keith", "Kevin", "Kitten", "Kedijah", "Keah", "Kammi",
        "Larrius", "Lengeru", "Ludwiz", "Longboy", "Llij", "Liyong", "Laylah",
        "Moparscape", "Mardok", "Mueller", "Mastodon", "Morphius", "Murph", "McNasty", "Mehret", "Mordheim", "Mors", "Magician", "Morefather",
        "Nilus", "Neo", "Nevarine", "Nix", "Nemo", "Nangara", "Neta", "Nontoun",
        "Ox", "Otherwilds", "Outerspace", "Oiler", "Officer", "Omexx", "Ocus", "Oddball",
        "Parrix", "Pickle", "Piledriver", "Puffpuff", "Peachclaw", "Peargrinder", "Paraema", 
        "Quizler", "Qix", "Quark", "Qidus", "Quid",
        "Roachest", "Rizz", "Rue", "Ramman", "Rammuh", "Ragdoll", "Rose",
        "Savi", "Serenity", "Silverhand", "Sammy", "Soothsayer", "Swissmiss", "Sully", "Semere", "Spiris", "Splinter", "Sandytwist", "Seatsaidh",
        "Tav", "Traveller", "Taximon", "Truegold", "Tuskies", "Tinkertot", "Terminus", "Tamil",
        "Underway", "Uco", "Unibrow",
        "Valiant", "Violence", "Vorton", "Voodoo", "Veuve", "Vexee", "Volance", 
        "Wabberjack", "Warbler", "Werebaby", "Wodyeith",
        "Xilla", "Xerces", 
        "Yoyo", "Yanger", "Yucca", "Yew", "Yewflower", 
        "Zoro", "Zewdi", "Zaben", "Zookeeper"];

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

        tournamentPools.Add( ETournamentPools.L, new TournamentPool( this, EMonsterRanks.L, ETournamentPools.L, "Legend", 2, 52, 53,     _configuration._confABD_tournament_rank_m4,        9999, 9 ) );
        tournamentPools.Add( ETournamentPools.M, new TournamentPool( this, EMonsterRanks.M, ETournamentPools.M, "Major 4", 8, 54, 61,    _configuration._confABD_tournament_rank_s,         _configuration._confABD_tournament_rank_m4 - 1, 7 ) );
        tournamentPools.Add( ETournamentPools.S, new TournamentPool( this, EMonsterRanks.S, ETournamentPools.S, "S Rank", 8, 1, 8,       _configuration._confABD_tournament_rank_a,         _configuration._confABD_tournament_rank_s - 1, 6));
        tournamentPools.Add( ETournamentPools.A, new TournamentPool( this, EMonsterRanks.A, ETournamentPools.A, "A Rank", 8, 9, 16,      _configuration._confABD_tournament_rank_b,         _configuration._confABD_tournament_rank_a - 1, 5));
        tournamentPools.Add( ETournamentPools.B, new TournamentPool( this, EMonsterRanks.B, ETournamentPools.B, "B Rank", 10, 17, 26,    _configuration._confABD_tournament_rank_c,         _configuration._confABD_tournament_rank_b - 1, 4));
        tournamentPools.Add( ETournamentPools.C, new TournamentPool( this, EMonsterRanks.C, ETournamentPools.C, "C Rank", 10, 27, 36,    _configuration._confABD_tournament_rank_d,         _configuration._confABD_tournament_rank_c - 1, 2));
        tournamentPools.Add( ETournamentPools.D, new TournamentPool( this, EMonsterRanks.D, ETournamentPools.D, "D Rank", 8, 37, 44,     _configuration._confABD_tournament_rank_e,         _configuration._confABD_tournament_rank_d - 1, 1));
        tournamentPools.Add( ETournamentPools.E, new TournamentPool( this, EMonsterRanks.E, ETournamentPools.E, "E Rank", 6, 45, 50,     _configuration._confABD_tournament_rank_z,         _configuration._confABD_tournament_rank_e - 1, 0));

        tournamentPools.Add( ETournamentPools.X_MOO, new TournamentPool( this, EMonsterRanks.L, ETournamentPools.X_MOO, "L - Moo", 6, 51, 51, _configuration._confABD_tournament_rank_m4 + 250, 9999, 9 ) );

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

        pool = id switch
        {
            >= 1 and <= 8 => ETournamentPools.S,
            >= 9 and <= 16 => ETournamentPools.A,
            >= 17 and <= 26 => ETournamentPools.B,
            >= 27 and <= 36 => ETournamentPools.C,
            >= 37 and <= 44 => ETournamentPools.D,
            >= 45 and <= 50 => ETournamentPools.E,
            >= 51 and <= 51 => ETournamentPools.X_MOO,
            >= 52 and <= 53 => ETournamentPools.L,
            >= 54 and <= 61 => ETournamentPools.M,
            >= 62 and <= 64 => ETournamentPools.A_Phoenix,
            >= 65 and <= 65 => ETournamentPools.B_Dragon,
            >= 66 and <= 66 => ETournamentPools.A_DEdge,
            >= 67 and <= 71 => ETournamentPools.F_Hero,
            >= 72 and <= 76 => ETournamentPools.F_Heel,
            >= 77 and <= 79 => ETournamentPools.F_Elder,
            >= 80 and <= 83 => ETournamentPools.S_FIMBA,
            >= 84 and <= 87 => ETournamentPools.A_FIMBA,
            >= 88 and <= 91 => ETournamentPools.B_FIMBA,
            >= 92 and <= 95 => ETournamentPools.C_FIMBA,
            >= 96 and <= 99 => ETournamentPools.D_FIMBA,
            >= 100 and <= 103 => ETournamentPools.S_FIMBA2,
            >= 104 and <= 107 => ETournamentPools.A_FIMBA2,
            >= 108 and <= 111 => ETournamentPools.B_FIMBA2,
            >= 112 and <= 115 => ETournamentPools.C_FIMBA2,
            >= 116 and <= 119 => ETournamentPools.D_FIMBA2,
            _ => pool
        };

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
        if ( _firstweek && currentWeek != 0 ) { _currentWeek = currentWeek - 1; }
        else if ( currentWeek > int.MaxValue - 4 ) { _currentWeek = 0; currentWeek = 0; }

        Logger.Debug("Advancing Weeks in TD: " + _currentWeek + " trying to get to " + currentWeek );
        while ( _currentWeek < currentWeek ) {
            _currentWeek++;

            if ( _currentWeek % 4 == 0 ) {
                AdvanceMonth();
            }

            if ( _currentWeek % 12 == 0 ) {
                AdvanceTournamentPromotions();
            }
        }

        Logger.Debug("Finished Advancing Weeks, Checking Pools", Color.Yellow );
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
        Logger.Debug("Advancing month from TournamentData", Color.Blue );
        for (var i = monsters.Count - 1 ; i >= 0 ; i--) {
            var m = monsters[i];

            m.AdvanceMonth();
            if ( !m.alive ) {
                Logger.Info(m.monster.name + " has died. Rest in peace.", Color.Blue );
                for ( var j = m.pools.Count() - 1; j >= 0; j-- ) {
                    m.pools[j].MonsterRemove( m );
                }
                monsters.Remove( m );
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



