using System.Drawing;
using MRDX.Base.Mod.Interfaces;
//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;

public enum EMonsterRanks
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

public enum ETournamentPools
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
    S_FIMBA2,
    A_FIMBA2,
    B_FIMBA2,
    C_FIMBA2,
    D_FIMBA2,
    X_MOO
}

public class TournamentData
{
    public static string[] _random_name_list =
    [
        "Cimasio", "Kyrades", "Ambroros", "Teodeus", "Lazan", "Pegetus", "Perseos", "Asandrou", "Agametrios",
        "Lazion", "Morphosyne", "Gelantinos", "Narkelous", "Taloclus", "Baltsalus", "Hypnaeon", "Atrol", "Alexede",
        "Baccinos", "Idastos", "Ophyroe", "Larissa", "Asperata",
        "Alnifolia", "Dentala", "Celsa", "Hempera", "Laurel", "Haldiphe", "Saffronea", "Quinn",
        "Poplarbush", "Snowdrop", "Funnyfluff", "Firo", "Limespice", "Herb", "Twinklespa", "Spring", "Shinyglade",
        "Almond", "Foggytree", "Pecan", "Jesterfeet", "Skylark", "Rainbow", "Snow", "Oakswamp", "Liri", "Briarpuff",
        "Extos", "Grimes", "Talis", "Anemia", "Tinder", "Neige", "Lec", "Chinook", "Graund", "Greidax", "Pigatt",
        "Kuanezz", "Nidar", "Danuzz", "Razodrug", "Krorodurr", "Galae",
        "Tepua", "Uvle", "Ujay", "Surlul", "Razsa", "Dezunu", "Urabu", "Sholgokoh", "Abedin", "Yetsi", "Jaedrey",
        "Leadeth", "Baudr", "Araldyng", "Gilparymr", "Sawah", "Mazraeh",
        "Aayuh", "Grimstriker", "Twistmight", "Pyregaze", "Heatmarch", "Omega", "Dalton", "Alpha", "Beta", "Gamma",
        "Zeta", "Phi", "Jaeger", "Dimbranch", "Raindust", "Hillbrace", "Storm", "Sohish", "Sunhol", "Ehtae", "Lilsof",
        "Ghostsign", "Snowlock", "Mystic", "Nevi", "Azahz", "Owen", "Denzel", "Robinson",
        "Blossom", "Yarn", "Skitter", "Mercy", "Firj", "Blubber", "Dribble", "Angel", "Tank", "Dottie", "Taugh",
        "Liberi", "Thespia", "Pirene", "Isonei", "Harrow", "Bakano", "Polo", "Okal", "Ochen", "Mhina", "Siaka", "Tamba",
        "Savane", "Boukary", "Traore", "Yaya", "Dia", "Dio", "Aaron", "Prizo", "Dimitri", "Dashaco",
        "Mathis", "Calamity", "Buve", "Hosho", "Zimba", "Tsun", "Mawere", "Rufaro", "Emoger", "Fida", "Thorns",
        "Saffron", "Teddago", "Skelyte", "Chilleni", "Slowhawk", "Jagola",
        "Zhar", "Dim", "Cleoz", "Rav", "Membut", "Dazam", "Groznur", "Aqrat", "Azzac", "Tergu", "Kirchon", "Nilla",
        "Ricryll", "Imnor", "Lanceruil", "Ballion",
        "Quosa", "Yesnorin", "Caina", "Holyn", "Athena", "Nandra", "Beratha", "Libgalyn", "Galin", "Heleto", "Faerona",
        "Fairest", "Chuckles", "Darkness",
        "Shiner", "Monkeytime", "Noper", "Willow", "Grassfall", "Misty", "Mantle", "Painscribe", "Plainwood", "Orbgold",
        "Ragespear", "Dawnward", "Clair", "Caffal", "Ronch", "Pola", "Moux", "Ranteau", "Nothier", "Peseul", "Astellon",
        "Glide", "Roughkiss", "Mosswisp", "Shadow", "Autumn",
        "Voidbane", "Voidsicle", "Kingsmith", "Kingly", "Peasant", "Pleasant", "Swellow", "Alexa", "Luitgard", "Ede",
        "Medou", "Branka", "Devotee", "Aura",
        "Outlaw", "Jade", "Nocturne", "Jarvis", "Beeps", "Faint", "Perkless", "Yill", "Quona", "Washerguard", "Eda",
        "Rosery", "Tapper", "Undergrow", "Ova",
        "Adamant", "Silverlock", "Dobby", "Finx", "Gar", "Hope", "Jewel", "Kattery", "Languish", "Zephyr", "Xilla",
        "Cedar", "Villa", "Branx", "Naught", "Midas",
        "Atronaph", "Argus", "Aideen", "Alias", "Adonay", "Anno", "Apollo", "Aydin", "Asakoa", "Aviri", "Adelynn",
        "Arsonwheel", "Angerstomp",
        "Bajor", "Beekler", "Bobbles", "Buu", "Brainstorm", "Bracer", "Basselt", "Boggycreek", "Boggart", "Bahamut",
        "Baretree", "Birchbellow",
        "Calaphyx", "Cawcaws", "Cix", "Cerrusio", "Creator", "Clipse", "Conjus", "Chanceux", "Ciorliath", "Clearwish",
        "David", "Dingus", "Dakadaka", "Dill", "Doodle", "Daydream", "Dreameater", "Diablo", "Diabolos", "Dunker",
        "Dragonfly",
        "Eater", "Eo", "Endofall", "Exuberance", "Etresse", "Elan", "Entun", "Earthtender",
        "Fargus", "Fillero", "Ferrus", "Faunus", "Feathers", "Fuzzball", "Foolcaller",
        "Gronkula", "Gimmles", "Golox", "Gargamel", "Gutterman", "Gale", "Gemlashes", "Gotusloop", "Goldenboy",
        "Hardness", "Herman", "Hillox", "Hundredyear", "Hurlante", "Hazel", "Hanzel", "Hatemonger",
        "Io", "Iodine", "Iaz", "Illomens", "Incarnate", "Isaias", "Islecrusher", "Iaull", "Itong", "Iilos",
        "Jax", "Jack", "Jillian", "Jellyjam", "Julius", "Jasmine", "Jazlynn",
        "Kawkaws", "Ki", "Kallus", "Keith", "Kevin", "Kitten", "Kedijah", "Keah", "Kammi",
        "Larrius", "Lengeru", "Ludwiz", "Longboy", "Llij", "Liyong", "Laylah",
        "Moparscape", "Mardok", "Mueller", "Mastodon", "Morphius", "Murph", "McNasty", "Mehret", "Mordheim", "Mors",
        "Magician", "Morefather",
        "Nilus", "Neo", "Nevarine", "Nix", "Nemo", "Nangara", "Neta", "Nontoun",
        "Ox", "Otherwilds", "Outerspace", "Oiler", "Officer", "Omexx", "Ocus", "Oddball",
        "Parrix", "Pickle", "Piledriver", "Puffpuff", "Peachclaw", "Peargrinder", "Paraema",
        "Quizler", "Qix", "Quark", "Qidus", "Quid",
        "Roachest", "Rizz", "Rue", "Ramman", "Rammuh", "Ragdoll", "Rose",
        "Savi", "Serenity", "Silverhand", "Sammy", "Soothsayer", "Swissmiss", "Sully", "Semere", "Spiris", "Splinter",
        "Sandytwist", "Seatsaidh",
        "Tav", "Traveller", "Taximon", "Truegold", "Tuskies", "Tinkertot", "Terminus", "Tamil",
        "Underway", "Uco", "Unibrow",
        "Valiant", "Violence", "Vorton", "Voodoo", "Veuve", "Vexee", "Volance",
        "Wabberjack", "Warbler", "Werebaby", "Wodyeith",
        "Xilla", "Xerces",
        "Yoyo", "Yanger", "Yucca", "Yew", "Yewflower",
        "Zoro", "Zewdi", "Zaben", "Zookeeper"
    ];

    public static Random GrowthRNG = new(Random.Shared.Next());
    public static Random LifespanRNG = new(Random.Shared.Next());

    private readonly Config _config;

    private uint _currentWeek;
    private bool _firstweek;

    private bool _initialized;
    public List<MonsterGenus> _unlockedTournamentBreeds = new();
    public List<TournamentMonster> monsters = new();

    public List<byte[]> startingMonsterTemplates = new();

    public Dictionary<ETournamentPools, TournamentPool> tournamentPools = new();

    public TournamentData(Config config)
    {
        _config = config;
        foreach (var pool in Enum.GetValues<ETournamentPools>())
            tournamentPools.Add(pool, new TournamentPool(config, pool));
    }

    public void AddExistingMonster(IBattleMonsterData m, int id)
    {
        var pool = TournamentPool.PoolFromId(id);
        var abdm = new TournamentMonster(_config, m)
        {
            Rank = tournamentPools[pool].Info.Rank
        };

        monsters.Add(abdm);
        tournamentPools[pool].MonsterAdd(abdm);
    }

    public void AddExistingMonster(TournamentMonster abdm)
    {
        monsters.Add(abdm);

        foreach (var pool in tournamentPools.Values)
        foreach (var monpool in abdm.pools)
            if ((ETournamentPools)abdm._rawpools[i] == pool.Pool)
                pool.MonsterAdd(abdm);
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

            if (_currentWeek % 12 == 0) AdvanceTournamentPromotions();
        }

        Logger.Debug("Finished Advancing Weeks, Checking Pools", Color.Yellow);
        foreach (var pool in tournamentPools.Values)
        {
            while (pool.Monsters.Count < pool._minimumSize) pool.GenerateNewValidMonster(_unlockedTournamentBreeds);

            // Shuffle Monsters - TODO: This should happen weekly.
            var ml = pool.Monsters.ToArray();
            Random.Shared.Shuffle(ml);
            pool.Monsters = new List<TournamentMonster>(ml);
        }

        _firstweek = false;
    }

    public void AdvanceMonth()
    {
        Logger.Debug("Advancing month from TournamentData", Color.Blue);
        for (var i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];

            m.AdvanceMonth();
            if (!m.Alive)
            {
                Logger.Info(m.Name + " has died. Rest in peace.", Color.Blue);
                for (var j = m.pools.Count() - 1; j >= 0; j--) m.pools[j].MonsterRemove(m);
                monsters.Remove(m);
            }

            // TODO CONFIG TECHNIQUE RATE
            if (Random.Shared.Next() % 25 == 0) m.LearnTechnique();
        }
    }

    private void AdvanceTournamentPromotions()
    {
        tournamentPools[ETournamentPools.M].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.L]);
        tournamentPools[ETournamentPools.S].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.M]);
        tournamentPools[ETournamentPools.A].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.S]);
        tournamentPools[ETournamentPools.B].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.A]);
        tournamentPools[ETournamentPools.C].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.B]);
        tournamentPools[ETournamentPools.D].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.C]);
        tournamentPools[ETournamentPools.E].MonstersPromoteToNewPool(tournamentPools[ETournamentPools.D]);
    }

    public List<TournamentMonster> GetTournamentMembers(int start, int end)
    {
        var participants = new List<TournamentMonster>();

        tournamentPools[ETournamentPools.S].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.A].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.B].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.C].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.D].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.E].AddTournamentParticipants(participants);

        // Edge Case to Handle the ONE SLOT that is skipped For Moo.
        participants.Add(tournamentPools[ETournamentPools.S].Monsters[0]);

        tournamentPools[ETournamentPools.L].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.M].AddTournamentParticipants(participants);

        tournamentPools[ETournamentPools.A_Phoenix].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.B_Dragon].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.A_DEdge].AddTournamentParticipants(participants);

        tournamentPools[ETournamentPools.F_Hero].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.F_Heel].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.F_Elder].AddTournamentParticipants(participants);

        tournamentPools[ETournamentPools.S_FIMBA].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.A_FIMBA].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.B_FIMBA].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.C_FIMBA].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.D_FIMBA].AddTournamentParticipants(participants);

        tournamentPools[ETournamentPools.S_FIMBA2].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.A_FIMBA2].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.B_FIMBA2].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.C_FIMBA2].AddTournamentParticipants(participants);
        tournamentPools[ETournamentPools.D_FIMBA2].AddTournamentParticipants(participants);

        return participants;
    }

    public void ClearAllData()
    {
        _initialized = false;

        monsters.Clear();
        foreach (var pool in tournamentPools.Values) pool.Monsters.Clear();
    }
}