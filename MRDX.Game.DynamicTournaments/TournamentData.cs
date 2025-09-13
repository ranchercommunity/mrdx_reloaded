using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public enum EMonsterRank
{
    L,
    M,
    S,
    A,
    B,
    C,
    D,
    E
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum EPool
{
    L,
    M,
    S,
    A,
    B,
    C,
    D,
    E,
    A_Phoenix,
    A_DEdge,
    B_Dragon,
    F_Hero,
    F_Heel,
    F_Elder,
    S_FIMBA,
    A_FIMBA,
    B_FIMBA,
    C_FIMBA,
    D_FIMBA,
    L_FIMBA,
}

public enum EMonsterRegion {
    IMA,
    FIMBA
}

public class TournamentData
{
    public static readonly Random GrowthRNG = new(Random.Shared.Next());
    public static readonly Random LifespanRNG = new(Random.Shared.Next());
    public static Dictionary<EMonsterRank, Range<int>>? RankBSTRanges;


    public readonly Config _config;
    public readonly string _gamePath;

    private readonly Dictionary<EPool, TournamentPool> TournamentPools = [];
    public readonly List<TournamentMonster> Monsters = [];
    private readonly List<EPool> ParticipantPoolMapping = [];

    private uint _currentWeek;
    private bool _firstweek;

    private bool _initialized;
    private List<MonsterGenus> _unlockedTournamentBreeds = [];

    public bool Initialized => _initialized;

    public TournamentData(string gamePath, Config config)
    {
        _config = config;
        _gamePath = gamePath;
        Logger.Trace("Creating new tourney pools");
        foreach (var pool in Enum.GetValues<EPool>())
            TournamentPools.Add(pool, new TournamentPool(this, config, pool));

        var addParticipants = (int c, EPool pool) => { for ( var i = 0; i < c; i++ ) { ParticipantPoolMapping.Add( pool ); }  };

        addParticipants( 8, EPool.S ); // 1-8
        addParticipants( 8, EPool.A ); // 9-16
        addParticipants( 10, EPool.B ); // 17-26
        addParticipants( 10, EPool.C ); // 27-36
        addParticipants( 8, EPool.D ); // 37-44
        addParticipants( 6, EPool.E ); // 45-50

        addParticipants( 1, EPool.L ); // 51 - Moo 
        addParticipants( 1, EPool.L ); // 52 - Most
        addParticipants( 1, EPool.L_FIMBA ); // 53 - Porotika

        addParticipants( 8, EPool.M ); // 54-61

        addParticipants( 3, EPool.A_Phoenix ); //62-64 Phoenix
        addParticipants( 1, EPool.B_Dragon ); // 65 Dragon
        addParticipants( 1, EPool.A_DEdge ); // 66 Durahan

        addParticipants( 5, EPool.F_Hero ); // 67 - 71 Hero
        addParticipants( 5, EPool.F_Heel ); // 72 - 76 Heel
        addParticipants( 3, EPool.F_Elder ); // 77-79 Elder

        addParticipants( 4, EPool.S_FIMBA ); 
        addParticipants( 4, EPool.A_FIMBA );
        addParticipants( 4, EPool.B_FIMBA );
        addParticipants( 4, EPool.C_FIMBA );
        addParticipants( 4, EPool.D_FIMBA );

        addParticipants( 4, EPool.S_FIMBA );
        addParticipants( 4, EPool.A_FIMBA );
        addParticipants( 4, EPool.B_FIMBA );
        addParticipants( 4, EPool.C_FIMBA );
        addParticipants( 4, EPool.D_FIMBA ); // 116-119

        RankBSTRanges = new Dictionary<EMonsterRank, Range<int>>() {
            { EMonsterRank.L, (config.RankM, 9999)},
            { EMonsterRank.M, (config.RankS, config.RankM)},
            { EMonsterRank.S, (config.RankA, config.RankS)},
            { EMonsterRank.A, (config.RankB, config.RankA)},
            { EMonsterRank.B, (config.RankC, config.RankB)},
            { EMonsterRank.C, (config.RankD, config.RankC)},
            { EMonsterRank.D, (config.RankE, config.RankD)},
            { EMonsterRank.E, (config.RankZ, config.RankE)}
        };
    }

    private TournamentMonster AddTaikaiFileMonster(IBattleMonsterData m, int id)
    {
        var dtpmonster = new TournamentMonster( _config, m );

        if ( id >= 80 ) { dtpmonster.Region = EMonsterRegion.FIMBA; }
        dtpmonster.PromoteTaikaiRank();

        Monsters.Add(dtpmonster);
        return dtpmonster;
    }

    public void AdvanceWeek(uint currentWeek, List<MonsterGenus> unlockedmonsters)
    {
        _unlockedTournamentBreeds = unlockedmonsters;

        if (!_initialized)
        {
            _currentWeek = currentWeek;
            return;
        }

        if (_firstweek && currentWeek != 0)
        {
            _currentWeek = currentWeek - 1;
        }
        else if (currentWeek > int.MaxValue - 4)
        {
            _currentWeek = 0;
            currentWeek = 0;
        }

        Logger.Debug("Advancing Weeks in TD: " + _currentWeek + " trying to get to " + currentWeek);
        while (_currentWeek < currentWeek)
        {
            _currentWeek++;

            if (_currentWeek % 4 == 0) AdvanceMonth();

            if (_currentWeek % 12 == 0) {
                PromoteMonsterRanks( EMonsterRegion.IMA );
                PromoteMonsterRanks( EMonsterRegion.FIMBA );
            }
        }


        Logger.Debug( "Finished advancing weeks, checking pools.", Color.Yellow );
        foreach (var pool in TournamentPools.Values) {
            var newCount = pool.ValidateTournamentReadiness( Monsters );
            for ( int i = 0; i < newCount; i++ ) {
                Monsters.Add(pool.GenerateNewValidMonster( _unlockedTournamentBreeds ) ); }
        }

        Utils.Shuffle(Random.Shared, Monsters);
        _firstweek = false;
    }

    public void LoadSavedTournamentData(List<byte[]> monstersRaw)
    {
        var monsters = new List<TournamentMonster>();
        foreach (var raw in monstersRaw)
            monsters.Add(new TournamentMonster(TournamentPools, raw));
        Logger.Info("Loaded Data for " + monsters.Count + " monsters.", Color.Orange);
        ClearAllData();
        foreach (var dtpmonster in monsters)
            Monsters.Add(dtpmonster);

        _initialized = true;
        _firstweek = true;
        Logger.Info("Initialization Complete", Color.Orange);
    }

    /// <summary>
    ///     Loads the taikai_en.flk file and generates the TournamentData from it. Is loaded at startup and when a new save
    ///     without save data is loaded.
    /// </summary>
    public void SetupTournamentParticipantsFromTaikai()
    {
        Logger.Info("Setting up toournament monster data from standard file.");
        ClearAllData();

        var tournamentMonsterFile = _gamePath + @"\mf2\data\taikai\taikai_en.flk";

        Logger.Trace($"Loading default tourney monster file ${tournamentMonsterFile}");
        var raw = File.ReadAllBytes(tournamentMonsterFile);
        
        var baseFilePos = 0xA8C + 60;
        for (var i = 1; i < 119; i++)
        {
            // 0 = Dummy Monster so skip. 119 in the standard file.
            var start = baseFilePos + i * 60;
            var end = start + 60;
            var tm = AddTaikaiFileMonster(IBattleMonsterData.FromBytes(raw[start..end]), i);
            Logger.Trace("Monster " + i + " Parsed: " + tm, Color.Lime);
        }

        _initialized = true;
        _firstweek = true;
    }

    public void WriteTournamentParticipantsToTaikai(string redirectPath)
    {
        var enemyFileRedirected = $@"{redirectPath}\taikai_en.flk";

        var monsters = GetTournamentMembers();
        var rawbytes = new List<byte>
        {
            Capacity = 60 * 119
        };
        foreach (var m in monsters)
            rawbytes.AddRange(m.Serialize());

        using var writer = new FileStream(enemyFileRedirected, FileMode.Open, FileAccess.Write);
        writer.Seek(0xA8C + 60, SeekOrigin.Begin);
        writer.Write(rawbytes.ToArray());
    }

    private void AdvanceMonth()
    {
        Logger.Debug("Advancing month from TournamentData", Color.Blue);
        for (var i = Monsters.Count - 1; i >= 0; i--)
        {
            var m = Monsters[i];

            m.AdvanceMonth();
            if (!m.Alive)
            {
                Logger.Info(m.Name + " has died. Rest in peace.", Color.Blue);
                Monsters.Remove(m);
                continue;
            }

            // TODO CONFIG TECHNIQUE RATE
            if (Random.Shared.Next(25) == 0) m.LearnTechnique();
        }
    }

    private void PromoteMonsterRanks(EMonsterRegion region) {
        Utils.Shuffle( Random.Shared, Monsters );

        List<TournamentMonster> monsters = Monsters.FindAll( m => m.Region == region );
        PromoteMonsterRank( monsters, EMonsterRank.M, EMonsterRank.L );
        PromoteMonsterRank( monsters, EMonsterRank.S, EMonsterRank.M );
        PromoteMonsterRank( monsters, EMonsterRank.A, EMonsterRank.S );
        PromoteMonsterRank( monsters, EMonsterRank.B, EMonsterRank.A );
        PromoteMonsterRank( monsters, EMonsterRank.C, EMonsterRank.B );
        PromoteMonsterRank( monsters, EMonsterRank.D, EMonsterRank.C );
        PromoteMonsterRank( monsters, EMonsterRank.E, EMonsterRank.D );
    }

    /// <summary>
    ///     This function promotes two sets of monsters.
    ///     1. A random monster from the rank, extremely weighted towards any monsters approaching the top of the soft stat
    ///     cap.
    ///     2. Any monsters remaining in the rank that are over the soft stat cap by a small amount.
    /// </summary>
    private void PromoteMonsterRank ( List<TournamentMonster> regionMonsters, EMonsterRank startRank, EMonsterRank endRank ) {
        Logger.Info( "Promoting monsters from " + startRank + " to " + endRank, Color.LightBlue );
        var stattotal = 0;
        // Find all monsters in this rank
        var monsters = regionMonsters.FindAll( m => m.Rank == startRank );
        if ( monsters.Count == 0 ) {
            Logger.Debug( $"Rank {startRank} has no monsters of that rank." );
            return;
        }

        var promoted = monsters[ 0 ];

        foreach ( var mon in monsters )
            stattotal += Math.Max( mon.StatTotal - ( RankBSTRanges[ startRank ].Max - 100 ), 1 );

        if ( stattotal > 50 ) {
            stattotal = Random.Shared.Next( stattotal );
            foreach ( var monster in monsters ) {
                var mvalue = Math.Max( monster.StatTotal - ( RankBSTRanges[ startRank ].Max - 100 ), 1 );

                stattotal -= mvalue;
                promoted = monster;
                if ( stattotal <= 0 ) break;
            }

            promoted.PromoteToRank( endRank );
            monsters.Remove( promoted );
        }

        else {
            Logger.Info( $"No monsters were promoted from {startRank} to {endRank} this month as none are worthy.", Color.LightBlue );
        }

        // Soft Cap Monster Promotions
        for ( var i = monsters.Count - 1; i >= 0; i-- )
            if ( monsters[ i ].StatTotal - 100 > RankBSTRanges[ startRank ].Max )
                monsters[ i ].PromoteToRank( endRank );
    }

    public List<TournamentMonster> GetTournamentMembers() {
        var participants = new List<TournamentMonster>();

        for ( var i = 0; i < ParticipantPoolMapping.Count; i++ ) {
            var tp = TournamentPools[ ParticipantPoolMapping[ i ] ];
            var monster = tp.ValidMonsters[ i % tp.ValidMonsters.Count ];
            participants.Add( monster ); // A failsafe for normal tournaments, intended for FIMBA where we restrict it to a single monster now!

            Logger.Trace( $"Pool {tp.TournamentRuleset.Name} Monster {i+1}: {monster.Name}, Rank {monster.Rank}, BST {monster.StatTotal}" );
        }
        return participants;
    }

    private void ClearAllData()
    {
        _initialized = false;
        Monsters.Clear();
        foreach (TournamentPool pool in TournamentPools.Values) {
            pool.ValidMonsters = new List<TournamentMonster>();
        }
    }
}