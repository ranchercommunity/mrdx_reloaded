using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.MoreMonsters;

[Function( CallingConventions.Fastcall )]
public delegate int Build551b00 ( int obj, int pos );

/*
 * This Module was developed by Zecster of the MR2 Community. Implementing as a portion of MM as with additional models,
 * this is effectively required and I do not want to split the mod into separate parts.
 * Descritpion: Relocates the monster model staging buffer so modded models can carry more geometry.
 */

public class ZECBufferHandler {
    private Mod _mod;
    private readonly IHooks _iHooks;
    private readonly IReloadedHooks? _hooks;
    private IMonster _monsterCurrent;

    // ---- Town-floor fix (Prospect 1: relocate-in-arena) ----------------
    // FUN_00551b00 builds each object's packet block into the shared town
    // packet arena and sets obj+0xc = block start. We hook it to catch the
    // floor's build (obj+8 == park.tmx geometry) and copy its clean block into
    // the FREE space at the TOP of the arena, redirecting obj+0xc there, so the
    // oversized monster's base-of-arena overflow never reaches the floor.
    private const uint BuildVa = 0x00551B00; // FUN_00551b00: packet-structure builder
    private const uint FloorGeomVa = 0x0092F540; // park.tmx geometry (the floor's source geom)
    private const uint ArenaTopVa = 0x008DC510; // next buffer = arena capacity end (~112KB)

    private const long ImageBase = 0x400000;
    private const int _defaultBufferSize = 8 * 1024 * 1024; // Size of the new relocated staging buffer.  Vanilla sizes range to a maximum of approximately 0.5 MB. 

    // ------------------------------------------------------------------
    //  INTEGRATION SURFACE
    // ------------------------------------------------------------------
    //  Construct once at mod load with the Reloaded hooks controller and a
    //  log callback, set the toggles you want (all default ON), then call
    //  Apply(). Everything else is self-contained.
    //
    //      var fixes = new MonsterBufferFixes(_hooks, msg => _logger.WriteLine(msg));
    //      fixes.Apply();
    // ------------------------------------------------------------------

    // Feature toggles. All are required for oversized custom monsters.
    public bool RelocateRanch = true;        // player model staging, 8 MB (also battle floor fix)
    public bool RelocatePlayerB = true;      // partner slot 0x871510 (VS-mode preview crash fix)
    public bool RelocateOpponent = true;     // opponent staging + the parking zones live in its home
    public bool RelocateDisplayTown = true;  // town/field/HoF display staging (ranch monster anims)
    public bool RelocateAnims = true;        // anim slot relocations (master switch)
    public bool RelocateAnimTown = true;     //   town half (0x8DC510)
    public bool RelocateAnimRanch = true;    //   ranch half (0x8DE510)
    public bool WidenVertexSlots = true;     // 64 KB -> 512 KB transformed-vertex slots
    public bool FloorRedirect = true;        // town floor packet-block rescue
    public bool MonsterParking = true;       // overflow tail + in-situ heap packet parking

    public ZECBufferHandler ( Mod mod, IReloadedHooks? hooks, IHooks iHooks ) {
        _mod = mod;
        _hooks = hooks;
        _iHooks = iHooks;

        Apply();
    }

    // Keep strong references so the buffers are never reclaimed.
    private readonly List<nuint> _buffers = new();

    private IHook<Build551b00>? _buildHook;   // FUN_00551b00, for the town-floor fix
    private long _moduleBase;
    // Town-floor fix state:
    private long _floorGeom;          // runtime addr of park.tmx geometry
    private long _arenaTop;           // arena capacity end (runtime)
    private int _redirectLogCount;    // floor-relocate log throttle
    private readonly HashSet<string> _relocated = new(); // succeeded buffers
    private readonly Dictionary<string, (nuint buf, long home)> _relocBufs = new();
    private int _mirrorTick;
    private System.Threading.Timer? _mirrorTimer;

    // Relocated slots whose STATIC homes are still read by engine code the
    // relocation site lists don't cover (naming keyboard, message text).
    // (relocation entry name, bytes to mirror = vanilla span to next buffer)
    private static readonly (string name, int span)[] MirrorSlots =
    {
        ("ANIM town (0x8DC510)",              0xA000),  // -> 0x8E6510 (spans ranch home)
        ("ANIM ranch (0x8DE510)",             0x8000),  // -> 0x8E6510
        ("DISPLAY town/field/HoF (0x869510)", 0xE000)   // -> 0x877510
        // NOTE: never mirror or park in the RANCH home [0x877510,0x87D510) -
        // the ranch scene keeps live message-text data there.
    };

    private readonly Dictionary<string, (byte[] probe, int tick)> _mirrorFresh = new();

    private readonly Dictionary<string, byte[]> _mirrorShadow = new();
    private volatile bool _mirrorPaused;
    private volatile bool _battleLayout; // battle layout, no foreign home-writes yet
    private int _mirrorPauseTick;

    private void MirrorTick ( object? _ ) {
        try {
            // Battle layout: mirror only while the homes are untouched
            // VS mode needs the mirror for its keyboard;
            // a real battle writes the homes itself and must not be overwritten.
            if ( !_sceneTown && !_battleLayout ) return;
            // Tripwire pause: expires after 2 s in town layout (it only
            // needs to cover load races); holds for a whole battle.
            if ( _mirrorPaused ) {
                if ( !_sceneTown ) return; // battle layout: pause holds (no expiry)
                if ( Environment.TickCount - _mirrorPauseTick < 2000 ) return;
                _mirrorPaused = false;
                _mirrorShadow.Clear(); // resync shadows to current state
            }
            // The town slot's naming table legitimately SPANS the ranch
            // slot's home; overlaps go to whichever slot's heap changed
            // most recently (freshness probe).
            var now = Environment.TickCount;
            var order = new List<(string name, int span, nuint buf, long home, int tick)>();
            foreach ( var (name, span) in MirrorSlots ) {
                if ( !_relocBufs.TryGetValue( name, out var s ) ) continue;
                var probe = new byte[ 96 ];
                Marshal.Copy( (nint) s.buf, probe, 0, 32 );
                Marshal.Copy( (nint) s.buf + 0x1000, probe, 32, 32 );
                Marshal.Copy( (nint) s.buf + 0x3000, probe, 64, 32 );
                if ( !_mirrorFresh.TryGetValue( name, out var f ) ||
                    !probe.AsSpan().SequenceEqual( f.probe ) )
                    _mirrorFresh[ name ] = (probe, now);
                order.Add( (name, span, s.buf, s.home, _mirrorFresh[ name ].tick) );
            }

            order.Sort( ( a, b ) => a.tick.CompareTo( b.tick ) ); // oldest first, freshest wins
            foreach ( var e in order ) {
                if ( _mirrorShadow.TryGetValue( e.name, out var shadow ) ) {
                    unsafe {
                        var p = (byte*) e.home;
                        for ( var i = 0; i < shadow.Length; i++ )
                            if ( p[ i ] != shadow[ i ] ) {
                                _mirrorPaused = true;
                                _battleLayout = false; // someone else owns the homes
                                _mirrorPauseTick = Environment.TickCount;
                                return;
                            }
                    }
                }

                unsafe { Buffer.MemoryCopy( (void*) e.buf, (void*) e.home, e.span, e.span ); }
            }

            foreach ( var e in order ) {
                var snap = new byte[ 64 ];
                Marshal.Copy( (nint) e.home, snap, 0, 64 );
                _mirrorShadow[ e.name ] = snap;
            }
        }
        catch { /* never crash the timer */ }
    }
    // ---- Buffer descriptor ---------------------------------------------

    private sealed class ModelBuffer {
        public string Name = "";
        public uint OldBase;
        public (uint va, int off)[] Sites = Array.Empty<(uint, int)>();
        public Func<ZECBufferHandler, bool> Enabled = _ => true;
    }

    // Each Sites entry is (siteVa, offsetIntoBuffer). The site currently holds
    // OldBase + Off (ASLR-relocated); we repoint it to newBuf + Off.
    // All site sets are the COMPLETE reloc-table refs in [base, base+0x100).

    private static readonly ModelBuffer[] Buffers =
    {
        new()
        {
            // Player monster staging (573 KB vanilla budget). Overflow
            // here is what broke the battle floor.
            Name = "RANCH/player",
            OldBase = 0x00877510,
            Enabled = f => f.RelocateRanch,
            Sites = new (uint, int)[]
            {
                (0x004509B5, 0), (0x004509F4, 0), (0x00450AB9, 0), (0x00450B47, 0),
                (0x004A013E, 0), (0x004A1891, 0), (0x005147DE, 0), (0x00545A87, 0),
                (0x0064E4D8, 0) // .rdata table
            }
        },
        new()
        {
            // Partner slot to RANCH (assembler FUN_00450A30 side=0 fills
            // both). Only 24 KB vanilla; the VS-mode save preview overruns
            // it with oversized monsters.
            Name = "PLAYER-B / VS preview (0x871510)",
            OldBase = 0x00871510,
            Enabled = f => f.RelocateRanch,
            Sites = new (uint, int)[]
            {
                (0x00450997, 0), (0x004509E3, 0), (0x00450AA6, 0), (0x00450B34, 0),
                (0x0049ED93, 0),
                (0x0064E4E4, 0) // .rdata table
            }
        },
        new()
        {
            Name = "OPPONENT/battle",
            OldBase = 0x00909510,
            Enabled = f => f.RelocateOpponent,
            Sites = new (uint, int)[]
            {
                (0x00450B86, 0x0), (0x0049F212, 0x0), (0x004A117B, 0x0), (0x004B09CB, 0x0),
                (0x004B0BA6, 0x0), (0x004B0BEF, 0x0), (0x004B0C44, 0x0), (0x004B0C90, 0x0),
                (0x004F84E2, 0x0), (0x004F8A8A, 0x0), (0x004FD30E, 0x0), (0x004FD625, 0x0),
                (0x00501190, 0x0), (0x0050123F, 0x0), (0x0054527E, 0x0), (0x0054531B, 0x0),
                (0x0054533E, 0x0), (0x00545384, 0x0), (0x0054590B, 0x0),
                (0x0064E40C, 0x0), (0x0064E4DC, 0x0), (0x0064E500, 0x0), // .rdata
                (0x004B0BE6, 0x08), (0x004B0B8F, 0x0C), (0x004B0C3D, 0x10),
                (0x004B0C89, 0x14), (0x004B09C0, 0x18), // header fields
                (0x004D82A3, 0x600) // Cocoon
            }
        },
        new()
        {
            Name = "DISPLAY town/field/HoF (0x869510)",
            OldBase = 0x00869510,
            Enabled = f => f.RelocateDisplayTown,
            Sites = new (uint, int)[]
            {
                (0x0045085A, 0), (0x0048BE69, 0), (0x004CFA7B, 0), (0x004CFB58, 0),
                (0x004D0037, 0), (0x004D0105, 0), (0x004D83CF, 0), (0x004D976F, 0),
                (0x004D97B9, 0), (0x004D9958, 0), (0x004E47C1, 0), (0x004F8496, 0),
                (0x004F85A0, 0), (0x004FD2C2, 0), (0x004FD46B, 0), (0x00501144, 0),
                (0x0050131B, 0), (0x005092F2, 0), (0x00509306, 0), (0x0050948C, 0),
                (0x0064E4FC, 0) // .rdata
            }
        },
        new()
        {
            Name = "ANIM town (0x8DC510)",
            OldBase = 0x008DC510,
            Enabled = f => f.RelocateAnims && f.RelocateAnimTown,
            Sites = new (uint, int)[]
            {
                (0x00512416, 0x0), (0x00517FC9, 0x0), (0x00517FF6, 0x0), (0x005197B3, 0x0),
                (0x0051C4AC, 0x0), (0x0051C4BB, 0x0), (0x0051C4CA, 0x0), (0x0051C4F4, 0x4),
                (0x0051C500, 0x0), (0x0051C508, 0x8), (0x0051C50E, 0x0), (0x0051C516, 0xC),
                (0x0051C51C, 0x0), (0x0051D015, 0x0), (0x0051D7D0, 0x0), (0x0051D7E4, 0x0),
                (0x0051D801, 0x0), (0x0051D810, 0x0), (0x0051EEE0, 0x0), (0x0051FD7D, 0x0),
                (0x005204D0, 0x0), (0x005204E2, 0x0), (0x0052073C, 0x0), (0x00520746, 0x0),
                (0x00520763, 0x0), (0x00520777, 0x0), (0x005207A0, 0x0), (0x005214E2, 0x0),
                (0x0052479D, 0x0), (0x00525BE7, 0x0), (0x00526C61, 0x0), (0x00529797, 0x0),
                (0x005299B1, 0x0), (0x00529BB7, 0x0), (0x00529BD2, 0x0), (0x00529BFA, 0x0),
                (0x00529C2C, 0x0), (0x00535EF7, 0x0), (0x00535F8E, 0x0), (0x00535FA6, 0x0),
                (0x00535FB5, 0x0), (0x0053605F, 0x0), (0x0053607F, 0x0), (0x00536093, 0x0),
                (0x00537169, 0x0), (0x00537494, 0x0), (0x00537AE9, 0x0), (0x00537AF8, 0x0),
                (0x00537B29, 0x0), (0x0053C42D, 0x0), (0x0053E803, 0x0), (0x0053E98F, 0x0),
                (0x0053E9B7, 0x0), (0x0053E9C6, 0x0), (0x0053F5DD, 0x0), (0x0053F5E3, 0x0),
                (0x0064E474, 0x0), (0x0064E4D0, 0x0),
            }
        },
        new()
        {
            Name = "ANIM ranch (0x8DE510)",
            OldBase = 0x008DE510,
            Enabled = f => f.RelocateAnims && f.RelocateAnimRanch,
            Sites = new (uint, int)[]
            {
                (0x004A1561, 0x0), (0x004CE864, 0x0), (0x004CE8E8, 0x0), (0x004D655E, 0x0),
                (0x004D6577, 0x0), (0x004D6582, 0x0), (0x004D65A0, 0x0), (0x004D65BF, 0x0),
                (0x004D65F8, 0x0), (0x004DC4BD, 0x0), (0x004DEF17, 0x0), (0x004DF054, 0x0),
                (0x004DF09C, 0x0), (0x004DF0D7, 0x0), (0x004DF0F2, 0x0), (0x004DF141, 0x0),
                (0x004DF153, 0x0), (0x004DF179, 0x0), (0x004DF1C4, 0x0), (0x004DF288, 0x0),
                (0x004DF29C, 0x0), (0x004DF320, 0x0), (0x004DF378, 0x0), (0x004DF3A4, 0x0),
                (0x004DF3CC, 0x0), (0x004DF3EF, 0x0), (0x004DF412, 0x0), (0x004DF42E, 0x0),
                (0x004DF447, 0x0), (0x004DF70F, 0x0), (0x004E2C1D, 0x0), (0x004E2D2F, 0x0),
                (0x004E2D55, 0x0), (0x004E2D72, 0x0), (0x004E2D81, 0x0), (0x004E396B, 0x0),
                (0x004E469D, 0x0), (0x004E8060, 0x0), (0x004EA217, 0x0), (0x004EF394, 0x0),
                (0x004EF48C, 0x0), (0x004EFCE3, 0x0), (0x004EFD62, 0x0), (0x004EFD84, 0x0),
                (0x004EFD9B, 0x0), (0x004EFDC3, 0x0), (0x0050B516, 0x0), (0x0050B6C0, 0x0),
                (0x0050B6ED, 0x0), (0x0050B6F8, 0x1B), (0x0050B71D, 0x1C),
                (0x0050BA43, 0x0), (0x0050BA95, 0x0), (0x0050BCD0, 0x0), (0x0050C040, 0x0),
                (0x0050CA62, 0x0), (0x0050CA83, 0x0), (0x0050D3A8, 0x0), (0x0050DED5, 0x0),
                (0x0050DF90, 0x0), (0x0050DFB6, 0x0), (0x0050DFF2, 0x0), (0x0050E004, 0x0),
                (0x0050E040, 0x0), (0x0050E062, 0x0), (0x0050F077, 0x0), (0x0050F07F, 0x0),
                (0x0050F41F, 0x0), (0x0050F426, 0x1B), (0x0050F486, 0x0),
                (0x0050F492, 0x1A), (0x0050F624, 0x0), (0x00512236, 0x0),
                (0x0051CFD6, 0x4), (0x0051CFDE, 0x0), (0x0051CFE6, 0x8), (0x0051CFEC, 0x0),
                (0x0051CFF4, 0xC), (0x0051CFFA, 0x0), (0x0051D81F, 0x0), (0x0054390A, 0x0),
                (0x0064E408, 0x0), (0x0064E478, 0x0), (0x0064E4A0, 0x0), (0x007538A8, 0x0),
            }
        },
    };

    // ---- Apply ---------------------------------------------------------

    private void Apply () {
        Log( $"==== MonsterBuffer ====" );

        var moduleBase = Process.GetCurrentProcess().MainModule!.BaseAddress.ToInt64();
        _moduleBase = moduleBase;
        var relocDelta = moduleBase - ImageBase;
        Log( $"MF2.exe base = 0x{moduleBase:X} (reloc delta 0x{relocDelta:X})" );

        _relocBufBytes = _defaultBufferSize; // fixed 8 MB per relocated buffer

        foreach ( var b in Buffers ) {
            if ( !b.Enabled( this ) ) {
                Log( $"[{b.Name}] disabled." );
                continue;
            }

            var buf = Relocate( b, moduleBase, relocDelta );
            if ( buf != 0 ) {
                _buffers.Add( buf );
                _relocated.Add( b.Name );
                _relocBufs[ b.Name ] = (buf, (long) (uint) ( b.OldBase + relocDelta ));
            }
        }

        if ( WidenVertexSlots )
            ApplyVertexSlotWiden( moduleBase, relocDelta );
        else
            Log( "Vertex-slot widen disabled." );

        if ( FloorRedirect )
            SetupFloorRedirect( moduleBase );
        else
            Log( "Town-floor redirect disabled." );
    }

    // Hook the packet builder for the floor redirect + parking, and start
    // the static-home mirror timer.
    private void SetupFloorRedirect ( long moduleBase ) {
        if ( _hooks == null ) { Log( "FloorRedirect: no hooks controller." ); return; }
        _floorGeom = moduleBase + ( FloorGeomVa - ImageBase );
        _arenaTop = moduleBase + ( ArenaTopVa - ImageBase );
        _imageEndVa = moduleBase + Process.GetCurrentProcess().MainModule!.ModuleMemorySize;

        if ( _relocBufs.Count > 0 && _mirrorTimer == null ) {
            // 10 ms: message dialogs lay out their first line within a frame
            // of the text file loading - a slow mirror loses that race.
            _mirrorTimer = new System.Threading.Timer( MirrorTick, null, 500, 10 );
            Log( "Static-home mirror timer active (10 ms)." );
        }

        var bAddr = moduleBase + ( BuildVa - ImageBase );
        _buildHook = _hooks.CreateHook<Build551b00>( Build551b00Detour, bAddr ).Activate();
        Log( $"Town-floor redirect active: build hook @0x{bAddr:X} (FUN_00551b00); " +
            $"floor geom @0x{_floorGeom:X}; arena top 0x{_arenaTop:X}." );
    }

    // ---- Monster overflow parking ---------------------------------------
    // The monster packet region [0x8AE510, 0x8C0510) is 73 KB; an oversized
    // monster's last part blocks cross into the scene arena, where scene
    // rebuilds clobber them (stream desync -> AV in the part-fill walkers).
    // Crossing blocks + their contiguous tail are copied to the OPPONENT
    // home and repointed via obj+0xC.
    private long _bwMax;
    private int _bwLogs;
    private long _monPark, _monParkEnd, _monParkCur;   // tail zone cursors
    private long _heapParkCur;   // in-situ heap re-park cursor (arena top, downward)
    private long _heapRanchCur;  // heap re-park overflow cursor (OPPONENT home)
    private long _heapOppCur;    // set when the OPPONENT home holds parked data
    private readonly Dictionary<long, (long dest, int cap)> _heapParkMap = new(); // rebuild dedup
    private int _relocBufBytes;  // relocated buffer size (for heap-build detection)
    private bool _monParking; // inside an overflow tail (engaged by a boundary-crossing block)
    private long _monLastEnd; // contiguity tracker: tail blocks abut exactly
    // Scene layout: whichever arena base a FRESH build lands on names the
    // live layout (battle blocks legitimately cross the town base, so the
    // boundary rule is per-layout).
    private bool _oppZoneDirty; // we parked something in the OPPONENT home
    private bool _sceneTown = true;
    private long _monBoundary;  // active layout's monster/scene boundary
    private long _lastBuildEnd; // bump-chain tracker for layout flips
    private long _arenaMax; // per-scene high-water of in-arena builds
    private bool _floorSceneParked; // this scene parked a floor copy at the arena top
    private bool _vtxSlotsWidened;
    private const uint BattleArenaVa = 0x008BED10;
    private long _imageEndVa;
    private int _monParkLogs;

    private int Build551b00Detour ( int obj, int pos ) {
        int end = _buildHook!.OriginalFunction( obj, pos );
        try {
            // IN-SITU HEAP RE-PARK: field scenes (errantry/training) build
            // packet blocks INSIDE their staging buffer. Relocated, those
            // packets land outside the depth-array span -> AV. Copy them
            // into the town arena (idle in field scenes) and repoint.
            if ( obj != 0 && end > pos && MonsterParking ) {
                foreach ( var kv in _relocBufs ) {
                    var b = (long) kv.Value.buf;
                    if ( pos < b || pos >= b + _relocBufBytes ) continue;
                    var hsize = (int) ( end - pos );
                    // Rebuilds re-emit the same source block: reuse its
                    // slot instead of leaking zone space.
                    long hdest = 0;
                    if ( _heapParkMap.TryGetValue( pos, out var prev ) && hsize <= prev.cap )
                        hdest = prev.dest;
                    if ( hdest == 0 ) {
                        if ( _heapParkCur == 0 ) _heapParkCur = _arenaTop - 0x100;
                        var floorLo = ( _arenaMax > 0 ? _arenaMax : _arenaTop - 0x1C000 ) + 0x400;
                        var hcand = ( _heapParkCur - hsize ) & ~0xFL;
                        if ( hcand >= floorLo && hsize <= 0x1C000 ) {
                            _heapParkCur = hcand;
                            hdest = hcand;
                        }
                        else {
                            // Overflow: OPPONENT home, past the tail slice.
                            var d2 = _arenaTop - (long) (uint) 0x008DC510;
                            var zLo = 0x00909510 + d2 + 0x2400;
                            var rEnd = 0x00931510 + d2 - 0x400;
                            if ( _heapRanchCur == 0 ) _heapRanchCur = zLo;
                            if ( _relocated.Contains( "OPPONENT/battle" ) && _heapRanchCur + hsize <= rEnd ) {
                                hdest = _heapRanchCur;
                                _heapRanchCur += ( hsize + 0xF ) & ~0xF;
                                _oppZoneDirty = true;
                            }
                            else {
                                // Overflow 2: orphaned OPPONENT battle home,
                                // 160 KB (battle's reader gets a zeroed home
                                // via the janitor, same as the RANCH zone).
                                if ( _heapOppCur == 0 ) _heapOppCur = 0x00909510 + d2 + 0x400;
                                var oEnd = 0x00931510 + d2 - 0x400;
                                if ( _relocated.Contains( "OPPONENT/battle" ) && _heapOppCur + hsize <= oEnd ) {
                                    hdest = _heapOppCur;
                                    _heapOppCur += ( hsize + 0xF ) & ~0xF;
                                }
                            }
                        }

                        if ( hdest != 0 ) _heapParkMap[ pos ] = (hdest, hsize);
                    }

                    if ( hdest != 0 ) {
                        unsafe { Buffer.MemoryCopy( (void*) pos, (void*) hdest, hsize, hsize ); }
                        unsafe { *(int*) ( obj + 0xc ) = (int) hdest; }
                        if ( _monParkLogs < 24 ) {
                            _monParkLogs++;
                            Log( $"HEAP PARK #{_monParkLogs}: pos=0x{pos:X} -> 0x{hdest:X} " +
                                $"size={hsize} [{kv.Key}]" );
                        }
                    }
                    else if ( _monParkLogs < 24 ) {
                        _monParkLogs++;
                        Log( $"HEAP PARK: all zones full, block 0x{pos:X} size={hsize} left in place." );
                    }

                    return end; // heap build handled; boundary logic n/a
                }
            }

            // Monster-overflow parking: geom in heap (relocated staging
            // buffer, i.e. outside the module image) + block reaching past
            // the town arena base = contested memory. Park it.
            if ( obj != 0 && end > pos ) {
                // The overflow signature is the BOUNDARY CROSSING itself:
                // no legitimate block straddles the arena base (scene statics
                // start AT it, the monster region ends BELOW it).
                var arenaBase0 = _arenaTop - 0x1C000;
                var battleBase = arenaBase0 - ( 0x8C0510 - BattleArenaVa );
                if ( _monBoundary == 0 ) _monBoundary = arenaBase0;
                // Layout flips only on a FRESH cursor at a scene base - a
                // block bump-landing on a base mid-chain is not a scene start.
                if ( pos != _lastBuildEnd ) {
                    if ( pos == battleBase ) {
                        _sceneTown = false; _monBoundary = battleBase; _monParking = false;
                        _battleLayout = true; // VS etc.: mirror until a foreign write
                        var dj = _arenaTop - (long) (uint) 0x008DC510;
                        if ( _relocated.Contains( "OPPONENT/battle" ) && _heapOppCur != 0 ) {
                            var lo = 0x00909510 + dj + 0x400;
                            var n = 0x00931510 + dj - 0x400 - lo;
                            unsafe {
                                var p = (byte*) lo;
                                for ( long i = 0; i < n; i++ ) p[ i ] = 0;
                            }
                        }
                    }
                    else if ( pos == arenaBase0 ) { _sceneTown = true; _monBoundary = arenaBase0; _monParking = false; }
                }

                _lastBuildEnd = end;
                // Battle layout parks too: its monster region ends at the
                // battle arena base (0x8BED10) and an oversized monster
                // crosses it exactly like the town case.
                var crossing = pos < _monBoundary && end > _monBoundary;
                if ( crossing ) {
                    // Tail zone = OPPONENT home front 8 KB. The battle path
                    // reads this home, so it is zeroed at battle entry (the
                    // "janitor" above); the parked tail is scene-lifetime
                    // data and is dead by then.
                    var d = _arenaTop - (long) (uint) 0x008DC510;
                    if ( _relocated.Contains( "OPPONENT/battle" ) ) {
                        _monPark = 0x00909510 + d + 0x400;
                        _monParkEnd = _monPark + 0x2000;
                        _monParkCur = _monPark;
                        _monParking = true;
                    }
                    else {
                        var reserve = _floorSceneParked ? 0x5800 : 0;
                        _monParkEnd = _arenaTop - 0x100 - reserve;
                        var hwm2 = _arenaMax > _monBoundary ? _arenaMax : _monBoundary;
                        _monPark = hwm2 + 0x400;
                        _monParkCur = _monPark;
                        _monParking = _monParkEnd - _monPark >= 0x1000;
                        if ( !_monParking && _monParkLogs < 24 ) {
                            _monParkLogs++;
                            Log( "MON PARK: enable 'Relocate battle opponent buffer' for the safe zone." );
                        }
                    }
                }
                else if ( _monParking && !( pos == _monLastEnd && pos >= _monBoundary ) ) {
                    _monParking = false; // discontinuity: tail over
                }

                if ( crossing ) _monParkLogs = 0; // fresh tail = fresh log budget
                if ( _monParking ) {
                    var msize = end - pos;
                    var dry = !MonsterParking;
                    var mdest = 0;
                    if ( !dry ) {
                        if ( _monParkCur + msize <= _monParkEnd ) {
                            mdest = (int) _monParkCur;
                            _monParkCur += ( msize + 0xF ) & ~0xF;
                            _oppZoneDirty = true;
                        }

                        if ( mdest != 0 ) {
                            unsafe { Buffer.MemoryCopy( (void*) pos, (void*) mdest, msize, msize ); }
                            unsafe { *(int*) ( obj + 0xc ) = mdest; }
                        }
                        else if ( _monParkLogs < 24 ) {
                            _monParkLogs++;
                            Log( $"MON PARK: zones full, block 0x{pos:X} size={msize} left in place." );
                        }
                    }

                    _monLastEnd = end; // original end: engine bump continues from here
                    if ( _monParkLogs < 24 ) {
                        _monParkLogs++;
                        Memory.Instance.Read<int>( (nuint) obj + 8, out var mg );
                        var pCnt = 0;
                        if ( mg != 0 ) Memory.Instance.Read<int>( (nuint) mg + 0x14, out pCnt );
                        Log( $"MON PARK{( dry ? " (dry)" : "" )} #{_monParkLogs}: pos=0x{pos:X}" +
                            $"{( dry ? "" : $" -> 0x{mdest:X}" )} size={msize} geom=0x{mg:X} prims={pCnt}" );
                    }
                }
            }

            // Build probe: logs every packet build (diagnostics), tracks
            // the per-scene arena watermark, and resets per-scene state on
            // arena-base builds (= scene rebuild markers).
            var arenaBase = _arenaTop - 0x1C000;
            if ( pos <= arenaBase && pos >= arenaBase - 0x40000 ) { _bwLogs = 0; _bwMax = 0; }
            var scanBase = arenaBase - ( 0x8C0510 - 0x8BED10 ); // battle arena base
            if ( pos == arenaBase || pos == scanBase ) {
                _arenaMax = 0;
                _floorSceneParked = false; // scene rebuild (either layout)
                _heapParkCur = _arenaTop - 0x100;
                _heapRanchCur = 0;
                _heapOppCur = 0;
                _heapParkMap.Clear();
                // NOTE: do NOT sweep the OPPONENT home here - town scenes
                // keep live data in it; the battle-entry janitor is the only
                // safe sweep.
                if ( pos == arenaBase ) {
                    _mirrorShadow.Clear(); // town/shrine rebuild: re-arm mirror
                    _mirrorPaused = false;
                }
            }
            if ( pos >= scanBase && pos < _arenaTop && end > _arenaMax ) _arenaMax = end;
            if ( _bwLogs < 40 ) {
                _bwLogs++;
                Memory.Instance.Read<int>( (nuint) obj + 8, out var g );
                var pOfs = 0; var pCnt = 0;
                if ( g != 0 ) {
                    Memory.Instance.Read<int>( (nuint) g + 0x10, out pOfs );
                    Memory.Instance.Read<int>( (nuint) g + 0x14, out pCnt );
                }

                Log( $"BUILD #{_bwLogs} pos=0x{pos:X} end=0x{end:X} size={end - pos} " +
                    $"geom=0x{g:X} prims={pCnt}" );
            }

            if ( obj != 0 ) {
                Memory.Instance.Read<int>( (nuint) obj + 8, out var geom );
                if ( geom == (int) _floorGeom ) {
                    int size = end - pos;
                    if ( size > 0 && size <= 0x20000 ) {
                        // Copy the floor's clean block to the arena top,
                        // out of the monster overflow's path.
                        int dest = (int) ( _arenaTop - size - 0x200 );
                        bool fits = (uint) dest > (uint) ( pos + size )
                                 && (uint) dest + (uint) size <= (uint) _arenaTop;
                        if ( fits ) _floorSceneParked = true;
                        if ( !fits ) dest = 0; // rare: skip -> vanilla behavior

                        if ( dest != 0 ) {
                            unsafe { Buffer.MemoryCopy( (void*) pos, (void*) dest, size, size ); }
                            unsafe { *(int*) ( obj + 0xc ) = dest; } // redirect the prim list
                            if ( _redirectLogCount < 6 ) {
                                _redirectLogCount++;
                                Memory.Instance.Read<uint>( (nuint) dest, out var td );
                                Log( $"Floor REDIRECT #{_redirectLogCount}: src=0x{pos:X} -> " +
                                    $"dest=0x{dest:X} size={size} tag0=0x{td:X8}." );
                            }
                        }
                        else if ( _redirectLogCount < 6 ) {
                            _redirectLogCount++;
                            Log( $"Floor REDIRECT #{_redirectLogCount} SKIPPED (no room)." );
                        }
                    }
                }
            }
        }
        catch { /* never break the build */ }
        return end;
    }

    // ---- Vertex-slot widen -------------------
    // 8 slots * 0x80000 = 4 MB region (~37000 verts/slot, was ~4600).
    private const uint ShlImm8Va = 0x0046277B; // imm8 of `shl esi,0x10`
    private const uint VtxLea1Va = 0x0046278A; // disp32 of lea eax,[esi+0x2168A10]
    private const uint VtxLea2Va = 0x004627A3; // disp32 of lea esi,[esi+0x2168A10]
    private const uint OldVtxRegionVa = 0x02168A10;
    private const byte OldSlotShift = 0x10;     // 64 KB slots
    private const byte NewSlotShift = 0x13;     // 512 KB slots

    private void ApplyVertexSlotWiden ( long moduleBase, long relocDelta ) {
        var mem = Memory.Instance;
        // 8 slots of (1 << NewSlotShift) bytes.
        var size = 8 * ( 1 << NewSlotShift );
        var handle = Marshal.AllocHGlobal( size );
        var newBase = (nuint) (long) handle;
        Marshal.Copy( new byte[ size ], 0, handle, size );
        if ( newBase > uint.MaxValue ) {
            Marshal.FreeHGlobal( handle );
            Log( "Vertex region above 4 GB -- can't reach with disp32. Skipped." );
            return;
        }

        var shlAddr = (nuint) ( moduleBase + ( ShlImm8Va - ImageBase ) );
        var lea1Addr = (nuint) ( moduleBase + ( VtxLea1Va - ImageBase ) );
        var lea2Addr = (nuint) ( moduleBase + ( VtxLea2Va - ImageBase ) );
        var oldRegionActual = (uint) ( OldVtxRegionVa + relocDelta );

        mem.Read<byte>( shlAddr, out var shlNow );
        mem.Read<uint>( lea1Addr, out var lea1Now );
        mem.Read<uint>( lea2Addr, out var lea2Now );

        if ( shlNow != OldSlotShift || lea1Now != oldRegionActual || lea2Now != oldRegionActual ) {
            Log( $"Vertex-slot sites don't match (shl=0x{shlNow:X} exp 0x{OldSlotShift:X}; " +
                $"lea1=0x{lea1Now:X}/lea2=0x{lea2Now:X} exp 0x{oldRegionActual:X}) -- " +
                "ABORTED, no change." );
            Marshal.FreeHGlobal( handle );
            return;
        }

        mem.SafeWrite( shlAddr, new[] { NewSlotShift } );
        mem.SafeWrite( lea1Addr, BitConverter.GetBytes( (uint) newBase ) );
        mem.SafeWrite( lea2Addr, BitConverter.GetBytes( (uint) newBase ) );
        _buffers.Add( newBase ); // keep alive
        _vtxSlotsWidened = true;
        Log( $"Vertex slots widened: 64 KB -> {( 1 << NewSlotShift ) / 1024} KB each, " +
            $"region @0x{newBase:X} (8 slots, {size / 1024} KB). Overworld floor fix." );
    }

    /// <summary>Relocate one staging buffer. Returns the new pointer or 0.</summary>
    private nuint Relocate ( ModelBuffer b, long moduleBase, long relocDelta ) {
        var size = _defaultBufferSize;
        var oldBaseActual = (uint) ( b.OldBase + relocDelta );
        var mem = Memory.Instance;

        var handle = Marshal.AllocHGlobal( size );
        var newBuf = (nuint) (long) handle;
        Marshal.Copy( new byte[ size ], 0, handle, size ); // zero-fill

        if ( newBuf > uint.MaxValue ) {
            Marshal.FreeHGlobal( handle );
            Log( $"[{b.Name}] allocation above 4 GB -- 32-bit immediate can't reach " +
                "it. Skipped." );
            return 0;
        }

        var rebased = 0;
        foreach ( var (va, off) in b.Sites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var got );
            var expect = oldBaseActual + (uint) off;
            if ( got == expect ) {
                mem.SafeWrite( addr, BitConverter.GetBytes( (uint) newBuf + (uint) off ) );
                rebased++;
            }
            else {
                Log( $"  [{b.Name}] @0x{va:X}: expected 0x{expect:X}, found 0x{got:X} " +
                    "-- SKIPPED" );
            }
        }

        if ( rebased == b.Sites.Length ) {
            Log( $"[{b.Name}] relocated {rebased}/{b.Sites.Length} refs -> 0x{newBuf:X} " +
                $"({size / 1024} KB)." );
            return newBuf;
        }

        // Partial -> roll back to stock so the game stays vanilla for this buffer.
        Log( $"[{b.Name}] only {rebased}/{b.Sites.Length} refs repointed -- ROLLING " +
            "BACK. No net change for this buffer." );
        foreach ( var (va, off) in b.Sites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var cur );
            if ( cur == (uint) newBuf + (uint) off )
                mem.SafeWrite( addr, BitConverter.GetBytes( oldBaseActual + (uint) off ) );
        }
        Marshal.FreeHGlobal( handle );
        return 0;
    }

    private void Log ( string msg ) {
        Logger.Trace( $"[MonsterBufferFixes] {msg}", Color.OrangeRed );
    }
}
