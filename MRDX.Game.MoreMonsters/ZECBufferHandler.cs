using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;

namespace MRDX.Game.MoreMonsters;

[Function( CallingConventions.Fastcall )]
public delegate int Build551b00 ( int obj, int pos );

[Function( CallingConventions.Fastcall )]
public delegate void LoadSet450970 ( uint param1, uint param2, int param3 );

[Function( CallingConventions.MicrosoftThiscall )]
public delegate void CachePageDirty ( IntPtr cache, IntPtr rect );

public class ZECBufferHandler {
    private const string Version = "fixes-3.2";
    private const long ImageBase = 0x400000;
    private const int DefaultBufferSize = 8 * 1024 * 1024;

    private readonly IReloadedHooks? _hooks;
    private readonly Action<string> _log;

    public bool RelocateRanch = true;
    public bool RelocateOpponent = true;
    public bool RelocateDisplayTown = true;
    public bool RelocateAnims = true;
    public bool RelocateAnimTown = false;
    public bool RelocateAnimRanch = false;
    public bool PreviewRedirect = true;
    public bool BigPacketRegion = true;
    public bool FloorRedirect = false;
    public bool MonsterParking = false;
    public bool DepthSafetyNet = true;
    public bool RelocatePlayerB = false;

    public bool MoveDisplayList = true;
    public bool WidenVramStride = true;
    public bool ExtendTpageX = true;
    public bool ExtendTexUpload = true;
    public bool TexStagingRedirect = true;
    public int TexStagingMb = 4;
    public uint DeathArenaBase = 0x00820000;
    public uint OpponentStaticHome = 0x02002000;
    public bool TpageBppGate = true;
    public uint BigRegionBase = 0x00790000;
    public bool ExtendedTextureRefresh = true;
    public bool EnlargeParsePool = true;
    public int ParsePoolMb = 32;
    public bool WidenVertexSlots = true;

    public ZECBufferHandler ( IReloadedHooks? hooks, Action<string> log ) {
        _hooks = hooks;
        _log = log;
    }

    private void Log ( string msg ) => _log( $"[MonsterBufferFixes] {msg}" );

    private readonly List<nuint> _buffers = new();
    private long _moduleBase;
    private long _relocDelta;

    public void Apply () {
        Log( $"==== MonsterBuffer {Version} ====" );

        var moduleBase = Process.GetCurrentProcess().MainModule!.BaseAddress.ToInt64();
        _moduleBase = moduleBase;
        _relocDelta = moduleBase - ImageBase;
        Log( $"MF2.exe base = 0x{moduleBase:X} (reloc delta 0x{_relocDelta:X})" );

        _relocBufBytes = DefaultBufferSize;

        foreach ( var b in Buffers ) {
            if ( !b.Enabled( this ) ) { Log( $"[{b.Name}] disabled." ); continue; }
            var buf = Relocate( b, moduleBase, _relocDelta );
            if ( buf != 0 ) {
                _buffers.Add( buf );
                _relocated.Add( b.Name );
                _relocBufs[ b.Name ] = (buf, (long) (uint) ( b.OldBase + _relocDelta ));
            }
        }

        if ( EnlargeParsePool ) ApplyParsePoolEnlarge();
        if ( WidenVertexSlots ) ApplyVertexSlotWiden();

        if ( PreviewRedirect ) SetupPreviewGuard( moduleBase );
        if ( BigPacketRegion ) ApplyBigRegion( moduleBase, _relocDelta );
        if ( FloorRedirect ) SetupFloorRedirect( moduleBase );
        else Log( "Town-floor redirect disabled." );

        ApplyDeathArena();

        if ( MoveDisplayList || WidenVramStride ) ApplyVramWiden();
        if ( _usesOldDisplayList != null && _newDisplayList == 0 )
            Log( $"[{_usesOldDisplayList}] ERROR: static home is in the old display list but " +
                "the display list did NOT move - the engine will overwrite it." );
        if ( ExtendTpageX ) ApplyTpageExtend();
        if ( ExtendedTextureRefresh && _tpageCave != 0 ) ApplyExtendedTextureRefresh();
        if ( ExtendTexUpload ) ApplyTexUploadExtend();
        if ( TexStagingRedirect ) ApplyTexStagingRedirect();

        if ( DepthSafetyNet ) ApplyDepthSafetyNet();
    }

    private sealed class ModelBuffer {
        public string Name = "";
        public uint OldBase;
        public (uint va, int off)[] Sites = Array.Empty<(uint, int)>();
        public Func<ZECBufferHandler, bool> Enabled = _ => true;
        public Func<ZECBufferHandler, uint> StaticBase = _ => 0;
    }

    private const uint LinkWindowLo = 0x0078150C;
    private const uint LinkWindowHi = 0x0478150C;
    private const uint OldDisplayListLo = 0x01F015D8;
    private const uint VramWideEnd = 0x020015D8;
    private const uint OldDisplayListHi = 0x021015D8;
    private string? _usesOldDisplayList;
    private readonly Dictionary<string, long> _relocSpan = new();

    private static readonly ModelBuffer[] Buffers =
    {
        new()
        {
            Name = "RANCH/player",
            OldBase = 0x00877510,
            Enabled = f => f.RelocateRanch,
            Sites = new (uint, int)[]
            {
                (0x004509B5, 0), (0x004509F4, 0), (0x00450AB9, 0), (0x00450B47, 0),
                (0x004A013E, 0), (0x004A1891, 0), (0x005147DE, 0), (0x00545A87, 0),
                (0x0064E4D8, 0)
            }
        },
        new()
        {
            Name = "PLAYER-B / VS preview (0x871510)",
            OldBase = 0x00871510,
            Enabled = f => f.RelocatePlayerB,
            Sites = new (uint, int)[]
            {
                (0x00450997, 0), (0x004509E3, 0), (0x00450AA6, 0), (0x00450B34, 0),
                (0x0049ED93, 0),
                (0x0064E4E4, 0)
            }
        },
        new()
        {
            Name = "OPPONENT/battle",
            OldBase = 0x00909510,
            Enabled = f => f.RelocateOpponent,
            StaticBase = f => f.OpponentStaticHome,
            Sites = new (uint, int)[]
            {
                (0x00450B86, 0x0), (0x0049F212, 0x0), (0x004A117B, 0x0), (0x004B09CB, 0x0),
                (0x004B0BA6, 0x0), (0x004B0BEF, 0x0), (0x004B0C44, 0x0), (0x004B0C90, 0x0),
                (0x004F84E2, 0x0), (0x004F8A8A, 0x0), (0x004FD30E, 0x0), (0x004FD625, 0x0),
                (0x00501190, 0x0), (0x0050123F, 0x0), (0x0054527E, 0x0), (0x0054531B, 0x0),
                (0x0054533E, 0x0), (0x00545384, 0x0), (0x0054590B, 0x0),
                (0x0064E40C, 0x0), (0x0064E4DC, 0x0), (0x0064E500, 0x0),
                (0x004B0BE6, 0x08), (0x004B0B8F, 0x0C), (0x004B0C3D, 0x10),
                (0x004B0C89, 0x14), (0x004B09C0, 0x18), (0x004D82A3, 0x600)
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
                (0x0064E4FC, 0)
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

    private nuint Relocate ( ModelBuffer b, long moduleBase, long relocDelta ) {
        var size = DefaultBufferSize;
        var oldBaseActual = (uint) ( b.OldBase + relocDelta );
        var mem = Memory.Instance;

        var handle = IntPtr.Zero;
        nuint newBuf;
        var sb = b.StaticBase( this );
        if ( sb != 0 ) {
            if ( sb < LinkWindowLo || sb >= LinkWindowHi ) { Log( $"[{b.Name}] static home 0x{sb:X} is outside the link window. Skipped." ); return 0; }
            if ( sb >= OldDisplayListLo && sb < OldDisplayListHi && !MoveDisplayList ) { Log( $"[{b.Name}] static home 0x{sb:X} needs MoveDisplayList. Skipped." ); return 0; }
            if ( sb >= OldDisplayListLo && sb < VramWideEnd && WidenVramStride ) { Log( $"[{b.Name}] static home 0x{sb:X} is where the widened VRAM grows. Skipped." ); return 0; }
            newBuf = (nuint) (long) (uint) ( sb + relocDelta );
            var inDl = sb >= OldDisplayListLo && sb < OldDisplayListHi;
            if ( inDl ) _usesOldDisplayList = b.Name;
            _relocSpan[ b.Name ] = inDl ? OldDisplayListHi - sb : size;
            Log( $"[{b.Name}] static home 0x{sb:X} ({( sb - LinkWindowLo ) / 1048576.0:F1} MB into the link window)." );
            goto patch;
        }

        handle = Marshal.AllocHGlobal( size );
        newBuf = (nuint) (long) handle;
        Marshal.Copy( new byte[ size ], 0, handle, size );
        _relocSpan[ b.Name ] = size;

        if ( newBuf > uint.MaxValue ) {
            Marshal.FreeHGlobal( handle );
            Log( $"[{b.Name}] allocated above 4 GB, a 32-bit immediate can't reach " +
                "it. Skipped." );
            return 0;
        }

    patch:
        var rebased = 0;
        foreach ( var (va, off) in b.Sites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var got );
            var expect = oldBaseActual + (uint) off;
            if ( got == expect ) {
                mem.SafeWrite( addr, BitConverter.GetBytes( (uint) newBuf + (uint) off ) );
                rebased++;
            }
            else
                Log( $"  [{b.Name}] @0x{va:X}: expected 0x{expect:X}, got 0x{got:X}. Skipped." );
        }

        if ( rebased == b.Sites.Length ) {
            Log( $"[{b.Name}] relocated {rebased}/{b.Sites.Length} refs -> 0x{newBuf:X} " +
                $"({( sb != 0 ? _relocSpan[ b.Name ] : size ) / 1024} KB)." );
            return newBuf;
        }

        Log( $"[{b.Name}] only {rebased}/{b.Sites.Length} refs repointed. Rolling back." );
        foreach ( var (va, off) in b.Sites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var cur );
            if ( cur == (uint) newBuf + (uint) off )
                mem.SafeWrite( addr, BitConverter.GetBytes( oldBaseActual + (uint) off ) );
        }
        if ( handle != IntPtr.Zero ) Marshal.FreeHGlobal( handle );
        return 0;
    }

    private const uint DeathArenaImmVa = 0x00506833;

    private void ApplyDeathArena () {
        if ( DeathArenaBase == 0 ) { Log( "[DeathArena] off - death scene keeps its 8 KB arena." ); return; }
        if ( DeathArenaBase < LinkWindowLo || DeathArenaBase >= LinkWindowHi ) { Log( $"[DeathArena] 0x{DeathArenaBase:X} is outside the link window. Skipped." ); return; }
        var mem = Memory.Instance;
        var addr = (nuint) ( _moduleBase + ( DeathArenaImmVa - ImageBase ) );
        var expect = (uint) ( 0x00907510 + _relocDelta );
        mem.Read<uint>( addr, out var got );
        if ( got != expect ) { Log( $"[DeathArena] @0x{DeathArenaImmVa:X}: expected 0x{expect:X}, got 0x{got:X}. Skipped." ); return; }
        mem.SafeWrite( addr, BitConverter.GetBytes( (uint) ( DeathArenaBase + _relocDelta ) ) );
        Log( $"[DeathArena] 0x907510 -> 0x{DeathArenaBase:X} (was 8 KB)." );
    }

    private IHook<LoadSet450970>? _loadSetHook;
    private nuint _previewScratch;
    private static readonly uint[] PreviewBSites = { 0x00450997, 0x004509E3 };

    private void SetupPreviewGuard ( long moduleBase ) {
        if ( _hooks == null ) { Log( "PreviewGuard: no hooks controller." ); return; }
        if ( _loadSetHook != null ) return;

        var scratch = Marshal.AllocHGlobal( DefaultBufferSize );
        if ( (nuint) (long) scratch > uint.MaxValue ) {
            Marshal.FreeHGlobal( scratch );
            Log( "PreviewGuard: scratch above 4 GB, disabled." );
            return;
        }
        Marshal.Copy( new byte[ DefaultBufferSize ], 0, scratch, DefaultBufferSize );
        _previewScratch = (nuint) (long) scratch;
        _buffers.Add( _previewScratch );

        _relocBufs[ "PREVIEW scratch" ] =
            (_previewScratch, (long) (uint) ( 0x00871510 + ( moduleBase - ImageBase ) ));

        _loadSetHook = _hooks.CreateHook<LoadSet450970>( LoadSetDetour,
            moduleBase + 0x50970 ).Activate();
        Log( $"PreviewGuard active: preview B-staging -> 0x{_previewScratch:X}." );
    }

    private void LoadSetDetour ( uint param1, uint param2, int param3 ) {
        try { LoadSetDetourInner( param1, param2, param3 ); }
        catch ( Exception ex ) {
            try { Log( $"PreviewGuard exception: {ex.Message}" ); } catch { }
            try { _loadSetHook!.OriginalFunction( param1, param2, param3 ); } catch { }
        }
    }

    private void LoadSetDetourInner ( uint param1, uint param2, int param3 ) {
        if ( param3 != 0x8d9510 || _previewScratch == 0 ) {
            _loadSetHook!.OriginalFunction( param1, param2, param3 );
            return;
        }
        var mem = Memory.Instance;
        Span<uint> saved = stackalloc uint[ PreviewBSites.Length ];
        for ( var i = 0; i < PreviewBSites.Length; i++ ) {
            var addr = (nuint) ( _moduleBase + ( PreviewBSites[ i ] - ImageBase ) );
            mem.Read<uint>( addr, out var cur );
            saved[ i ] = cur;
            mem.SafeWrite( addr, BitConverter.GetBytes( (uint) _previewScratch ) );
        }
        try { _loadSetHook!.OriginalFunction( param1, param2, param3 ); }
        finally {
            for ( var i = 0; i < PreviewBSites.Length; i++ )
                mem.SafeWrite( (nuint) ( _moduleBase + ( PreviewBSites[ i ] - ImageBase ) ),
                    BitConverter.GetBytes( saved[ i ] ) );
        }
    }

    private const uint MonRegionOld = 0x008AE510;
    private static readonly uint[] MonRegionSites =
    {
        0x0048BE70, 0x004CFB83, 0x004D00F9, 0x004D83D6, 0x004D97C0,
        0x004D995F, 0x004E0629, 0x004E45A8, 0x004E4628, 0x004F85A7,
        0x004FD472, 0x00501322, 0x00509492, 0x0051D56F, 0x00520C61,
        0x0052AAF1, 0x00536C5F, 0x0064E450,
    };

    private long _bigRegionLive;

    private void ApplyBigRegion ( long moduleBase, long relocDelta ) {
        var mem = Memory.Instance;
        var expect = (uint) ( MonRegionOld + relocDelta );
        var newBase = BigRegionBase;
        if ( newBase < 0x781510 || newBase >= 0x1081000 ) {
            Log( $"[BigRegion] base 0x{newBase:X} out of range, skipped." );
            return;
        }
        var repl = (uint) ( newBase + relocDelta );

        var done = 0;
        foreach ( var va in MonRegionSites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var got );
            if ( got == expect ) {
                mem.SafeWrite( addr, BitConverter.GetBytes( repl ) );
                done++;
            }
            else
                Log( $"[BigRegion] @0x{va:X}: expected 0x{expect:X}, got 0x{got:X}. Skipped." );
        }

        if ( done == MonRegionSites.Length ) {
            _bigRegionLive = repl;
            var span = 0x00868510L - newBase;
            Log( $"[BigRegion] monster packet region -> 0x{repl:X}; {span / 1024} KB " +
                $"to the next tenant (~{span / 80} prims, vanilla was 66 KB / ~840)." );
            return;
        }

        Log( $"[BigRegion] only {done}/{MonRegionSites.Length} sites. Rolling back." );
        foreach ( var va in MonRegionSites ) {
            var addr = (nuint) ( moduleBase + ( va - ImageBase ) );
            mem.Read<uint>( addr, out var got );
            if ( got == repl ) mem.SafeWrite( addr, BitConverter.GetBytes( expect ) );
        }
    }

    private const uint BuildVa = 0x00551B00;
    private const uint FloorGeomVa = 0x0092F540;
    private const uint ArenaTopVa = 0x008DC510;
    private const uint BattleArenaVa = 0x008BED10;

    private IHook<Build551b00>? _buildHook;
    private long _floorGeom;
    private long _arenaTop;
    private int _redirectLogCount;
    private readonly HashSet<string> _relocated = new();
    private readonly Dictionary<string, (nuint buf, long home)> _relocBufs = new();
    private System.Threading.Timer? _mirrorTimer;

    private static readonly (string name, int span)[] MirrorSlots =
    {
        ("ANIM town (0x8DC510)",              0xA000),
        ("ANIM ranch (0x8DE510)",             0x8000),
        ("DISPLAY town/field/HoF (0x869510)", 0xE000)
    };

    private readonly Dictionary<string, (byte[] probe, int tick)> _mirrorFresh = new();
    private readonly Dictionary<string, byte[]> _mirrorShadow = new();
    private volatile bool _mirrorPaused;
    private volatile bool _battleLayout;
    private int _mirrorPauseTick;

    private void MirrorTick ( object? _ ) {
        try {
            if ( !_sceneTown && !_battleLayout ) return;

            if ( _mirrorPaused ) {
                if ( !_sceneTown ) return;
                if ( Environment.TickCount - _mirrorPauseTick < 2000 ) return;
                _mirrorPaused = false;
                _mirrorShadow.Clear();
            }

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

            order.Sort( ( a, b ) => a.tick.CompareTo( b.tick ) );
            foreach ( var e in order ) {
                if ( _mirrorShadow.TryGetValue( e.name, out var shadow ) ) {
                    unsafe {
                        var p = (byte*) e.home;
                        for ( var i = 0; i < shadow.Length; i++ )
                            if ( p[ i ] != shadow[ i ] ) {
                                _mirrorPaused = true;
                                _battleLayout = false;
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
        catch { }
    }

    private void SetupFloorRedirect ( long moduleBase ) {
        if ( _hooks == null ) { Log( "FloorRedirect: no hooks controller." ); return; }
        _floorGeom = moduleBase + ( FloorGeomVa - ImageBase );
        _arenaTop = moduleBase + ( ArenaTopVa - ImageBase );

        if ( _relocBufs.Count > 0 && _mirrorTimer == null ) {
            _mirrorTimer = new System.Threading.Timer( MirrorTick, null, 500, 10 );
            Log( "Static-home mirror timer active (10 ms)." );
        }

        var bAddr = moduleBase + ( BuildVa - ImageBase );
        _buildHook = _hooks.CreateHook<Build551b00>( Build551b00Detour, bAddr ).Activate();
        Log( $"Town-floor redirect active: build hook @0x{bAddr:X}, " +
            $"floor geom @0x{_floorGeom:X}, arena top 0x{_arenaTop:X}." );
    }

    private long _monPark, _monParkEnd, _monParkCur;
    private long _heapParkCur;
    private long _heapRanchCur;
    private long _heapOppCur;
    private readonly Dictionary<long, (long dest, int cap)> _heapParkMap = new();
    private int _relocBufBytes;
    private bool _monParking;
    private long _monLastEnd;
    private bool _sceneTown = true;
    private long _monBoundary;
    private long _lastBuildEnd;
    private long _arenaMax;
    private bool _floorSceneParked;
    private int _monParkLogs;

    private int Build551b00Detour ( int obj, int pos ) {
        var end = _buildHook!.OriginalFunction( obj, pos );
        try {
            if ( obj != 0 && end > pos && MonsterParking ) {
                foreach ( var kv in _relocBufs ) {
                    var b = (long) kv.Value.buf;
                    var span = _relocSpan.TryGetValue( kv.Key, out var sp ) ? sp : _relocBufBytes;
                    if ( pos < b || pos >= b + span ) continue;
                    var hsize = (int) ( end - pos );

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
                            var d2 = _arenaTop - (long) (uint) ArenaTopVa;
                            var zLo = 0x00909510 + d2 + 0x2400;
                            var rEnd = 0x00931510 + d2 - 0x400;
                            if ( _heapRanchCur == 0 ) _heapRanchCur = zLo;
                            if ( _relocated.Contains( "OPPONENT/battle" ) &&
                                _heapRanchCur + hsize <= rEnd ) {
                                hdest = _heapRanchCur;
                                _heapRanchCur += ( hsize + 0xF ) & ~0xF;
                            }
                            else {
                                if ( _heapOppCur == 0 ) _heapOppCur = 0x00909510 + d2 + 0x400;
                                var oEnd = 0x00931510 + d2 - 0x400;
                                if ( _relocated.Contains( "OPPONENT/battle" ) &&
                                    _heapOppCur + hsize <= oEnd ) {
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
                    }
                    else if ( _monParkLogs < 24 ) {
                        _monParkLogs++;
                        Log( $"HEAP PARK: all zones full, block 0x{pos:X} size={hsize} " +
                            "left in place." );
                    }
                    return end;
                }
            }

            if ( obj != 0 && end > pos ) {
                var arenaBase0 = _arenaTop - 0x1C000;
                var battleBase = arenaBase0 - ( 0x8C0510 - BattleArenaVa );
                if ( _monBoundary == 0 ) _monBoundary = arenaBase0;

                if ( pos != _lastBuildEnd ) {
                    if ( pos == battleBase ) {
                        _sceneTown = false; _monBoundary = battleBase; _monParking = false;
                        _battleLayout = true;
                        var dj = _arenaTop - (long) (uint) ArenaTopVa;
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

                var crossing = pos < _monBoundary && end > _monBoundary;
                if ( crossing ) {
                    var d = _arenaTop - (long) (uint) ArenaTopVa;
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
                            Log( "MON PARK: turn on RelocateOpponent for the safe zone." );
                        }
                    }
                    _monParkLogs = 0;
                }
                else if ( _monParking && !( pos == _monLastEnd && pos >= _monBoundary ) ) {
                    _monParking = false;
                }

                if ( _monParking ) {
                    var msize = end - pos;
                    var mdest = 0;
                    if ( _monParkCur + msize <= _monParkEnd ) {
                        mdest = (int) _monParkCur;
                        _monParkCur += ( msize + 0xF ) & ~0xF;
                    }
                    if ( mdest != 0 ) {
                        unsafe { Buffer.MemoryCopy( (void*) pos, (void*) mdest, msize, msize ); }
                        unsafe { *(int*) ( obj + 0xc ) = mdest; }
                    }
                    else if ( _monParkLogs < 24 ) {
                        _monParkLogs++;
                        Log( $"MON PARK: zones full, block 0x{pos:X} size={msize} left in place." );
                    }
                    _monLastEnd = end;
                }
            }

            var arenaBase = _arenaTop - 0x1C000;
            var scanBase = arenaBase - ( 0x8C0510 - BattleArenaVa );
            if ( pos == arenaBase || pos == scanBase ) {
                _arenaMax = 0;
                _floorSceneParked = false;
                _heapParkCur = _arenaTop - 0x100;
                _heapRanchCur = 0;
                _heapOppCur = 0;
                _heapParkMap.Clear();
                if ( pos == arenaBase ) {
                    _mirrorShadow.Clear();
                    _mirrorPaused = false;
                }
            }
            if ( pos >= scanBase && pos < _arenaTop && end > _arenaMax ) _arenaMax = end;

            if ( obj != 0 ) {
                Memory.Instance.Read<int>( (nuint) obj + 8, out var geom );
                if ( geom == (int) _floorGeom ) {
                    var size = end - pos;
                    if ( size > 0 && size <= 0x20000 ) {
                        var dest = (int) ( _arenaTop - size - 0x200 );
                        var fits = (uint) dest > (uint) ( pos + size )
                                && (uint) dest + (uint) size <= (uint) _arenaTop;
                        if ( fits ) _floorSceneParked = true;
                        else dest = 0;

                        if ( dest != 0 ) {
                            unsafe { Buffer.MemoryCopy( (void*) pos, (void*) dest, size, size ); }
                            unsafe { *(int*) ( obj + 0xc ) = dest; }
                            if ( _redirectLogCount < 6 ) {
                                _redirectLogCount++;
                                Log( $"Floor redirect #{_redirectLogCount}: 0x{pos:X} -> " +
                                    $"0x{dest:X} size={size}." );
                            }
                        }
                        else if ( _redirectLogCount < 6 ) {
                            _redirectLogCount++;
                            Log( $"Floor redirect #{_redirectLogCount} skipped, no room." );
                        }
                    }
                }
            }
        }
        catch { }
        return end;
    }

    private const uint DepthArrVa = 0x01681580;

    private VehDelegate? _veh;
    private uint _depthArrBase;
    private int _clampCount, _loggedClamps;

    private delegate int VehDelegate ( IntPtr exceptionInfo );

    public int DepthClampCount => _clampCount;

    private void ApplyDepthSafetyNet () {
        if ( _veh != null ) return;
        _depthArrBase = (uint) ( DepthArrVa + _relocDelta );
        _veh = VehHandler;
        var stub = BuildVehFilter( Marshal.GetFunctionPointerForDelegate( _veh ) );
        var h = stub != IntPtr.Zero ? AddVectoredExceptionHandler( 1, stub ) : IntPtr.Zero;
        Log( h != IntPtr.Zero
            ? $"[DepthNet] armed around 0x{_depthArrBase:X}."
            : "[DepthNet] AddVectoredExceptionHandler failed." );
    }

    private int VehHandler ( IntPtr ep ) {
        try {
            if ( ep == IntPtr.Zero ) return 0;
            var rec = Marshal.ReadIntPtr( ep );
            if ( rec == IntPtr.Zero ) return 0;
            if ( (uint) Marshal.ReadInt32( rec, 0x00 ) != 0xC0000005 ) return 0;
            if ( Marshal.ReadInt32( rec, 0x10 ) < 2 ) return 0;
            if ( Marshal.ReadInt32( rec, 0x14 ) == 8 ) return 0;

            var ctx = Marshal.ReadIntPtr( ep, 4 );
            if ( ctx == IntPtr.Zero ) return 0;
            var eip = (long) (uint) Marshal.ReadInt32( ctx, 0xB8 );
            if ( eip == 0 ) return 0;

            var o0 = Marshal.ReadByte( (IntPtr) eip );
            var o1 = Marshal.ReadByte( (IntPtr) eip, 1 );
            var isStore = o0 == 0x66 && o1 == 0x89;
            var isLoad = o0 == 0x0F && o1 == 0xB7;
            var disp = (uint) Marshal.ReadInt32( (IntPtr) eip, 4 );
            if ( !( isStore || isLoad ) || disp != _depthArrBase ) return 0;

            var sib = Marshal.ReadByte( (IntPtr) eip, 3 );
            var idxReg = ( sib >> 3 ) & 7;
            int[] ctxOfs = { 0xB0, 0xAC, 0xA8, 0xA4, -1, 0xB4, 0xA0, 0x9C };
            var ofs = ctxOfs[ idxReg ];
            if ( ofs <= 0 ) return 0;

            var cur = (uint) Marshal.ReadInt32( ctx, ofs );
            var clamped = 0x80000u | ( cur & 0x3FFFu );
            Marshal.WriteInt32( ctx, ofs, (int) clamped );
            _clampCount++;
            if ( _loggedClamps < 8 ) {
                _loggedClamps++;
                Log( $"[DepthNet] clamped depth {( isStore ? "store" : "load" )} " +
                    $"@0x{eip:X}: index 0x{cur:X} -> 0x{clamped:X}." );
            }
            return -1;
        }
        catch { return 0; }
    }

    private const uint DisplayListVa = 0x01F015D8;
    private static readonly uint[] DisplayListSites =
        { 0x00487BDA, 0x00487BED, 0x00487C2D, 0x0054D478, 0x0054D494 };

    private static readonly uint[] VramStrideShl =
        { 0x0054D12A, 0x0054D251, 0x0054D316, 0x0054D372,
          0x0054FAEC, 0x0054FCE1, 0x005501B5, 0x00551093 };

    private static readonly uint[] VramRowAdd =
        { 0x0054D2F9, 0x0054D35E, 0x0054D3F7, 0x0054D630, 0x0054FB39, 0x005501F5 };

    private const uint VramRowLoopCmpVa = 0x0054D636;

    private nuint _newDisplayList;
    private bool _vramWidened;

    public bool VramWidened => _vramWidened;

    private void ApplyVramWiden () {
        var mem = Memory.Instance;

        if ( MoveDisplayList && _newDisplayList == 0 ) {
            const int dlSize = 2 * 1024 * 1024;
            var h = Marshal.AllocHGlobal( dlSize );
            var buf = (nuint) (long) h;
            if ( buf > uint.MaxValue ) {
                Marshal.FreeHGlobal( h );
                Log( "[Vram] display list alloc above 4 GB, skipped." );
                return;
            }
            Marshal.Copy( new byte[ dlSize ], 0, h, dlSize );

            var expect = (uint) ( DisplayListVa + _relocDelta );
            var done = new List<uint>();
            foreach ( var va in DisplayListSites ) {
                var addr = (nuint) ( _moduleBase + ( va - ImageBase ) );
                mem.Read<uint>( addr, out var got );
                if ( got != expect ) {
                    Log( $"[Vram] display list @0x{va:X}: expected 0x{expect:X}, " +
                        $"got 0x{got:X}. Rolling back." );
                    foreach ( var r in done )
                        mem.SafeWrite( (nuint) ( _moduleBase + ( r - ImageBase ) ),
                            BitConverter.GetBytes( expect ) );
                    Marshal.FreeHGlobal( h );
                    return;
                }
                mem.SafeWrite( addr, BitConverter.GetBytes( (uint) buf ) );
                done.Add( va );
            }
            _newDisplayList = buf;
            _buffers.Add( buf );
            Log( $"[Vram] display list moved to 0x{buf:X}." );
        }

        if ( !WidenVramStride ) return;

        if ( _newDisplayList == 0 ) {
            Log( "[Vram] stride widen skipped, display list has not been moved." );
            return;
        }

        var undo = new List<(nuint addr, byte[] old)>();

        bool Patch ( nuint addr, byte[] expect, byte[] want, string what ) {
            var cur = new byte[ expect.Length ];
            for ( var i = 0; i < expect.Length; i++ ) {
                mem.Read<byte>( addr + (nuint) i, out var v );
                cur[ i ] = v;
            }
            if ( !cur.AsSpan().SequenceEqual( expect ) ) {
                Log( $"[Vram] {what} @0x{(long) addr:X}: expected " +
                    $"{BitConverter.ToString( expect )}, got {BitConverter.ToString( cur )}." );
                return false;
            }
            undo.Add( (addr, cur) );
            mem.SafeWrite( addr, want );
            return true;
        }

        var ok = true;

        foreach ( var va in VramStrideShl ) {
            var addr = (nuint) ( _moduleBase + ( va - ImageBase ) + 2 );
            ok &= Patch( addr, new byte[] { 0x0A }, new byte[] { 0x0B }, "stride shl" );
            if ( !ok ) break;
        }

        if ( ok )
            foreach ( var va in VramRowAdd ) {
                mem.Read<byte>( (nuint) ( _moduleBase + ( va - ImageBase ) ), out var op );
                var immOfs = op == 0x05 ? 1 : 2;
                var addr = (nuint) ( _moduleBase + ( va - ImageBase ) + immOfs );
                ok &= Patch( addr, BitConverter.GetBytes( 0x800u ),
                    BitConverter.GetBytes( 0x1000u ), "row step" );
                if ( !ok ) break;
            }

        if ( ok ) {
            var addr = (nuint) ( _moduleBase + ( VramRowLoopCmpVa - ImageBase ) );
            ok &= Patch( addr,
                BitConverter.GetBytes( (uint) ( 0x01E035D8 + _relocDelta ) ),
                BitConverter.GetBytes( (uint) ( 0x01E055D8 + _relocDelta ) ),
                "row loop bound" );
        }

        if ( !ok ) {
            foreach ( var (addr, old) in undo ) mem.SafeWrite( addr, old );
            Log( "[Vram] stride widen rolled back, VRAM unchanged." );
            return;
        }

        _vramWidened = true;
        Log( "[Vram] VRAM widened to 2048x512." );
    }

    private const uint TpageDecodeVa = 0x0054D224;
    private static readonly byte[] TpageOriginal =
    {
        0x0F, 0xB6, 0x45, 0x08,
        0x8B, 0xC8,
        0xC1, 0xE8, 0x04,
        0x83, 0xE1, 0x0F,
        0xC1, 0xE0, 0x08,
        0xC1, 0xE1, 0x06,
    };

    private const uint TpageClutVa = 0x0054D0E1;
    private static readonly byte[] TpageClutOriginal =
        { 0x8B, 0xDE, 0x66, 0xC1, 0xEE, 0x06 };

    private nuint _tpageCave;

    private void ApplyTpageExtend () {
        if ( _tpageCave != 0 ) return;
        var mem = Memory.Instance;
        var site = (nuint) ( _moduleBase + ( TpageDecodeVa - ImageBase ) );

        var cur = new byte[ TpageOriginal.Length ];
        for ( var i = 0; i < cur.Length; i++ ) {
            mem.Read<byte>( site + (nuint) i, out var v );
            cur[ i ] = v;
        }
        if ( !cur.AsSpan().SequenceEqual( TpageOriginal ) ) {
            Log( $"[Tpage] decode block @0x{TpageDecodeVa:X} does not match, skipped." );
            return;
        }

        const uint MEM_COMMIT = 0x1000, MEM_RESERVE = 0x2000, PAGE_EXEC_RW = 0x40;
        var cave = VirtualAlloc( IntPtr.Zero, (UIntPtr) 0x1000,
            MEM_COMMIT | MEM_RESERVE, PAGE_EXEC_RW );
        if ( cave == IntPtr.Zero ) { Log( "[Tpage] cave alloc failed." ); return; }

        var stub = new List<byte>
        {
            0x0F, 0xB6, 0x45, 0x08,
            0x8B, 0xC8,
            0x83, 0xE1, 0x0F,
        };
        if ( TpageBppGate )
            stub.AddRange( new byte[] { 0x80, 0xFB, 0x10,
                                       0x74, 0x0C } );
        stub.AddRange( new byte[]
        {
            0xF7, 0x45, 0x0C, 0x00, 0x80, 0x00, 0x00,
            0x74, 0x03,
            0x83, 0xC9, 0x10,
            0xC1, 0xE1, 0x06,
        } );
        stub.AddRange( new byte[]
        {
            0x0F, 0xB6, 0x45, 0x08,
            0xC1, 0xE8, 0x04,
            0x83, 0xE0, 0x01,
            0xC1, 0xE0, 0x08,
        } );

        var backTo = (long) site + TpageOriginal.Length;
        stub.Add( 0xE9 );
        stub.AddRange( BitConverter.GetBytes(
            (int) ( backTo - ( (long) cave + stub.Count + 4 ) ) ) );
        Marshal.Copy( stub.ToArray(), 0, cave, stub.Count );

        var patch = new byte[ TpageOriginal.Length ];
        for ( var i = 0; i < patch.Length; i++ ) patch[ i ] = 0x90;
        patch[ 0 ] = 0xE9;
        BitConverter.GetBytes( (int) ( (long) cave - ( (long) site + 5 ) ) ).CopyTo( patch, 1 );
        mem.SafeWrite( site, patch );
        _tpageCave = (nuint) (long) cave;

        var cSite = (nuint) ( _moduleBase + ( TpageClutVa - ImageBase ) );
        mem.ReadRaw( cSite, out var cGot, TpageClutOriginal.Length );
        if ( cGot == null || !cGot.AsSpan().SequenceEqual( TpageClutOriginal ) ) {
            Log( $"[Tpage] CLUT block @0x{TpageClutVa:X} does not match. Without this " +
                "the carrier bit corrupts every palette, so rolling back." );
            mem.SafeWrite( site, TpageOriginal );
            _tpageCave = 0;
            return;
        }

        var cCave = (long) cave + 0x80;
        var cStub = new List<byte> { 0x81, 0xE6, 0xFF, 0x7F, 0x00, 0x00 };
        cStub.AddRange( TpageClutOriginal );
        var cBack = (long) TpageClutVa + TpageClutOriginal.Length + _relocDelta;
        cStub.Add( 0xE9 );
        cStub.AddRange( BitConverter.GetBytes( (int) ( cBack - ( cCave + cStub.Count + 4 ) ) ) );
        Marshal.Copy( cStub.ToArray(), 0, (IntPtr) cCave, cStub.Count );

        var cPatch = new byte[ TpageClutOriginal.Length ];
        for ( var i = 0; i < cPatch.Length; i++ ) cPatch[ i ] = 0x90;
        cPatch[ 0 ] = 0xE9;
        BitConverter.GetBytes( (int) ( cCave - ( (long) cSite + 5 ) ) ).CopyTo( cPatch, 1 );
        mem.SafeWrite( cSite, cPatch );

        Log( $"[Tpage] X page index extended to 5 bits, cave at 0x{(long) cave:X}. " +
            "Primitives reach columns 1024-2047 by setting bit 0x8000 in the CLUT." );
    }

    private static readonly (uint va, byte[] expect, byte[] patch, string what)[]
        TexUploadSites =
    {
        (0x0048711A, new byte[] { 0xC1, 0xE1, 0x04 }, new byte[] { 0xC1, 0xE1, 0x05 },
            "shl ecx,4 -> 5"),
        (0x00487138, new byte[] { 0xB8, 0x1F, 0, 0, 0 }, new byte[] { 0xB8, 0x3F, 0, 0, 0 },
            "clamp ceiling 0x1F -> 0x3F"),
        (0x0048713D, new byte[] { 0x83, 0xF9, 0x1F }, new byte[] { 0x83, 0xF9, 0x3F },
            "cmp ecx,0x1F -> 0x3F"),
        (0x0048714B, new byte[] { 0x66, 0xC1, 0xE9, 0x04 }, new byte[] { 0x66, 0xC1, 0xE9, 0x05 },
            "shr cx,4 -> 5"),
        (0x0048714F, new byte[] { 0x83, 0xE2, 0x0F }, new byte[] { 0x83, 0xE2, 0x1F },
            "and edx,0xF -> 0x1F"),
        (0x00450158, new byte[] { 0xBA, 0xF0, 0xFF, 0xFF, 0xFF },
                     new byte[] { 0xBA, 0xE0, 0xFF, 0xFF, 0xFF },
            "upper half bias -0x10 -> -0x20"),
    };

    private void ApplyTexUploadExtend () {
        var mem = Memory.Instance;
        var done = new List<int>();
        for ( var i = 0; i < TexUploadSites.Length; i++ ) {
            var (va, expect, patch, what) = TexUploadSites[ i ];
            var addr = (nuint) ( _moduleBase + ( va - ImageBase ) );
            mem.ReadRaw( addr, out var got, expect.Length );
            if ( got == null || !got.AsSpan().SequenceEqual( expect ) ) {
                Log( $"[TexUpload] 0x{va:X} ({what}) does not match. Rolling back." );
                foreach ( var j in done )
                    mem.SafeWrite( (nuint) ( _moduleBase + ( TexUploadSites[ j ].va - ImageBase ) ),
                        TexUploadSites[ j ].expect );
                return;
            }
            mem.SafeWrite( addr, patch );
            done.Add( i );
        }
        Log( $"[TexUpload] TIM page index extended to 5 bits, {done.Count} sites. " +
            "TIMs can now be written to columns 1024-2047." );
    }

    private const uint TexStagingSiteVa = 0x00450C3D;
    private const uint TexStagingGlobalVa = 0x00769820;
    private nuint _texStaging;

    private void ApplyTexStagingRedirect () {
        if ( _texStaging != 0 ) return;
        var mem = Memory.Instance;
        var addr = (nuint) ( _moduleBase + ( TexStagingSiteVa - ImageBase ) );

        mem.ReadRaw( addr, out var got, 5 );
        var expect = new byte[ 5 ];
        expect[ 0 ] = 0xA1;
        BitConverter.GetBytes( (uint) ( TexStagingGlobalVa + _relocDelta ) ).CopyTo( expect, 1 );
        if ( got == null || got.Length != 5 || !got.AsSpan().SequenceEqual( expect ) ) {
            Log( $"[TexStaging] 0x{TexStagingSiteVa:X} does not match, aborting." );
            return;
        }

        var mb = Math.Clamp( TexStagingMb, 1, 64 );
        var size = mb * 1024 * 1024;
        var h = Marshal.AllocHGlobal( size );
        var buf = (nuint) (long) h;
        if ( buf > uint.MaxValue ) {
            Marshal.FreeHGlobal( h );
            Log( "[TexStaging] buffer allocated above 4 GB, skipped." );
            return;
        }
        Marshal.Copy( new byte[ size ], 0, h, size );

        var patch = new byte[ 5 ];
        patch[ 0 ] = 0xB8;
        BitConverter.GetBytes( (uint) buf ).CopyTo( patch, 1 );
        mem.SafeWrite( addr, patch );

        _texStaging = buf;
        _buffers.Add( buf );
        Log( $"[TexStaging] .tex now loads into 0x{buf:X} ({mb} MB). " +
            "The 133,120 byte limit is gone." );
    }

    [DllImport( "kernel32.dll", SetLastError = true )]
    private static extern IntPtr VirtualAlloc ( IntPtr lpAddress, UIntPtr dwSize,
        uint flAllocationType, uint flProtect );

    private IntPtr _vehStub;

    private IntPtr BuildVehFilter ( IntPtr managed ) {
        var main = Process.GetCurrentProcess().MainModule!;
        var lo = (uint) (long) main.BaseAddress;
        var hi = lo + (uint) main.ModuleMemorySize;
        var mem = VirtualAlloc( IntPtr.Zero, (UIntPtr) 0x1000, 0x3000, 0x40 );
        if ( mem == IntPtr.Zero ) return IntPtr.Zero;
        var code = new List<byte>();
        void B ( params byte[] x ) => code.AddRange( x );
        void D ( uint v ) => code.AddRange( BitConverter.GetBytes( v ) );
        B( 0x8B, 0x44, 0x24, 0x04, 0x8B, 0x08, 0x81, 0x39 ); D( 0xC0000005 );
        B( 0x75, 0x1E, 0x8B, 0x40, 0x04, 0x8B, 0x80, 0xB8, 0, 0, 0 );
        B( 0x3D ); D( lo ); B( 0x72, 0x0E );
        B( 0x3D ); D( hi ); B( 0x73, 0x07 );
        B( 0xB8 ); D( (uint) (long) managed ); B( 0xFF, 0xE0 );
        B( 0x31, 0xC0, 0xC2, 0x04, 0x00 );
        Marshal.Copy( code.ToArray(), 0, mem, code.Count );
        _vehStub = mem;
        return mem;
    }

    private const uint ParsePoolBaseGlobalVa = 0x02104E34;
    private static readonly uint[] ParsePoolSites = { 0x0054C196, 0x0054C1B0, 0x0054C1CA, 0x0054C1D4 };

    private void ApplyParsePoolEnlarge () {
        var mem = Memory.Instance;
        var mb = Math.Clamp( ParsePoolMb, 8, 256 );
        var size = (uint) mb * 1024u * 1024u;
        if ( size == 0x800000 ) return;

        mem.Read<uint>( (nuint) ( _moduleBase + ( ParsePoolBaseGlobalVa - ImageBase ) ), out var poolBase );
        if ( poolBase != 0 ) {
            Log( $"[ParsePool] pool already built at 0x{poolBase:X}, too late to resize. Skipped." );
            return;
        }

        var expect = new uint[] { 0x800000, 0x800000, 0x7FFFF0, 0x80000 };
        var write = new uint[] { size, size, size - 0x10, size / 0x10 };
        var done = new List<int>();
        for ( var i = 0; i < ParsePoolSites.Length; i++ ) {
            var addr = (nuint) ( _moduleBase + ( ParsePoolSites[ i ] - ImageBase ) );
            mem.Read<uint>( addr, out var got );
            if ( got != expect[ i ] ) {
                Log( $"[ParsePool] 0x{ParsePoolSites[ i ]:X}: expected 0x{expect[ i ]:X}, got 0x{got:X}. Rolling back." );
                foreach ( var j in done )
                    mem.SafeWrite( (nuint) ( _moduleBase + ( ParsePoolSites[ j ] - ImageBase ) ),
                        BitConverter.GetBytes( expect[ j ] ) );
                return;
            }
            mem.SafeWrite( addr, BitConverter.GetBytes( write[ i ] ) );
            done.Add( i );
        }
        Log( $"[ParsePool] shared parse pool {mb} MB (was 8)." );
    }

    private const uint ShlImm8Va = 0x0046277B;
    private const uint VtxLea1Va = 0x0046278A;
    private const uint VtxLea2Va = 0x004627A3;
    private const uint OldVtxRegionVa = 0x02168A10;

    private void ApplyVertexSlotWiden () {
        var mem = Memory.Instance;
        var size = 8 * 0x80000;
        var h = Marshal.AllocHGlobal( size );
        var newBase = (nuint) (long) h;
        if ( newBase > uint.MaxValue ) {
            Marshal.FreeHGlobal( h );
            Log( "[VertexSlots] region above 4 GB, skipped." );
            return;
        }
        Marshal.Copy( new byte[ size ], 0, h, size );

        var shlAddr = (nuint) ( _moduleBase + ( ShlImm8Va - ImageBase ) );
        var lea1Addr = (nuint) ( _moduleBase + ( VtxLea1Va - ImageBase ) );
        var lea2Addr = (nuint) ( _moduleBase + ( VtxLea2Va - ImageBase ) );
        var oldRegion = (uint) ( OldVtxRegionVa + _relocDelta );
        mem.Read<byte>( shlAddr, out var shl );
        mem.Read<uint>( lea1Addr, out var l1 );
        mem.Read<uint>( lea2Addr, out var l2 );
        if ( shl != 0x10 || l1 != oldRegion || l2 != oldRegion ) {
            Log( $"[VertexSlots] sites do not match (shl 0x{shl:X}, lea 0x{l1:X}/0x{l2:X}). Skipped." );
            Marshal.FreeHGlobal( h );
            return;
        }
        mem.SafeWrite( shlAddr, new byte[] { 0x13 } );
        mem.SafeWrite( lea1Addr, BitConverter.GetBytes( (uint) newBase ) );
        mem.SafeWrite( lea2Addr, BitConverter.GetBytes( (uint) newBase ) );
        _buffers.Add( newBase );
        Log( $"[VertexSlots] widened to 512 KB each at 0x{newBase:X}." );
    }

    private const uint ClutDirtyVa = 0x0056197D;
    private static readonly byte[] ClutDirtyOriginal =
    {
        0x0F, 0xB7, 0x58, 0xFD, 0xBF, 0x01, 0x00, 0x00, 0x00, 0x8B, 0xF3,
        0x66, 0xD3, 0xE7, 0x66, 0xC1, 0xEB, 0x06, 0x83, 0xE6, 0x3F,
    };
    private static readonly byte[] ClutDirtyPatched =
    {
        0x0F, 0xB7, 0x58, 0xFD, 0x6A, 0x01, 0x5F, 0x8B, 0xF3, 0x66, 0xD3,
        0xE7, 0xC1, 0xEB, 0x06, 0x80, 0xE7, 0x01, 0x83, 0xE6, 0x3F,
    };
    private const uint CachePageDirtyVa = 0x00561870;
    private const uint FreezeSaveVa = 0x0051D015;
    private const uint FreezeRestoreVa = 0x0051EEE0;
    private const uint FreezeSaveOld = 0x008DC510;
    private const int FreezeSaveBytes = 0x4000;

    private IHook<CachePageDirty>? _cachePageHook;

    private void ApplyExtendedTextureRefresh () {
        var mem = Memory.Instance;
        var done = new List<string>();

        var site = (nuint) ( _moduleBase + ( ClutDirtyVa - ImageBase ) );
        mem.ReadRaw( site, out var got, ClutDirtyOriginal.Length );
        if ( got != null && got.AsSpan().SequenceEqual( ClutDirtyOriginal ) ) {
            mem.SafeWrite( site, ClutDirtyPatched );
            done.Add( "palette changes" );
        }
        else Log( $"[ExtRefresh] CLUT check @0x{ClutDirtyVa:X} does not match, skipped." );

        if ( _hooks != null ) {
            mem.ReadRaw( (nuint) ( _moduleBase + ( CachePageDirtyVa - ImageBase ) ), out var pro, 3 );
            if ( pro != null && pro[ 0 ] == 0x55 && pro[ 1 ] == 0x8B && pro[ 2 ] == 0xEC ) {
                _cachePageHook = _hooks.CreateHook<CachePageDirty>( CachePageDirtyDetour,
                    _moduleBase + ( CachePageDirtyVa - ImageBase ) ).Activate();
                done.Add( "pixel uploads" );
            }
            else Log( $"[ExtRefresh] page check @0x{CachePageDirtyVa:X} does not match, skipped." );
        }

        var oldVal = (uint) ( FreezeSaveOld + _relocDelta );
        var a1 = (nuint) ( _moduleBase + ( FreezeSaveVa - ImageBase ) );
        var a2 = (nuint) ( _moduleBase + ( FreezeRestoreVa - ImageBase ) );
        mem.Read<uint>( a1, out var v1 );
        mem.Read<uint>( a2, out var v2 );
        if ( v1 == oldVal && v2 == oldVal ) {
            var buf = Marshal.AllocHGlobal( FreezeSaveBytes );
            if ( (ulong) (long) buf <= uint.MaxValue ) {
                Marshal.Copy( new byte[ FreezeSaveBytes ], 0, buf, FreezeSaveBytes );
                _buffers.Add( (nuint) (long) buf );
                var nb = BitConverter.GetBytes( (uint) (long) buf );
                mem.SafeWrite( a1, nb );
                mem.SafeWrite( a2, nb );
                done.Add( "freezer palette save" );
            }
            else Marshal.FreeHGlobal( buf );
        }

        if ( done.Count > 0 )
            Log( $"[ExtRefresh] extra-page textures refresh on {string.Join( ", ", done )}." );
    }

    private unsafe void CachePageDirtyDetour ( IntPtr cache, IntPtr rect ) {
        _cachePageHook!.OriginalFunction( cache, rect );
        try {
            var r = (short*) rect;
            if ( r == null || r[ 2 ] < 1024 ) return;
            int c0 = Math.Max( (int) r[ 0 ], 1024 ) >> 6, c1 = r[ 2 ] >> 6;
            int r0 = Math.Clamp( r[ 1 ] >> 8, 0, 1 ), r1 = Math.Clamp( r[ 3 ] >> 8, 0, 1 );
            var e = (byte*) cache + 0x80;
            for ( var i = 0; i < 256; i++, e += 0x18 ) {
                if ( e[ 0x14 ] == 0 || ( *(ushort*) ( e + 0x10 ) & 0x8000 ) == 0 ) continue;
                int tb = e[ 0x12 ];
                int col = ( tb & 0xF ) + 16, row = ( tb >> 4 ) & 1;
                int span = Math.Max( 1, e[ 0x13 ] / 4 );
                if ( row >= r0 && row <= r1 && col <= c1 && col + span - 1 >= c0 )
                    e[ 0x15 ] = 1;
            }
        }
        catch { }
    }

    [DllImport( "kernel32.dll" )]
    private static extern IntPtr AddVectoredExceptionHandler ( uint first, IntPtr handler );
}
