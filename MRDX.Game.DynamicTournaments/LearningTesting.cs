using System.Diagnostics;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X86;
using Reloaded.Memory.Sources;
using CallingConventions = Reloaded.Hooks.Definitions.X86.CallingConventions;

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 ?? 66 89 15 ?? ?? ?? ?? 66 8B D0 8B 4E ?? 66 2B 51 ?? 66 89 15 ?? ?? ?? ?? 8B 4E ?? 5E 66 89 41 ?? 5D C2 04 00 33 C0 5E 5D C2 04 00")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Lif(nuint self, uint value);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 0E")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Pow(nuint self, uint value);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 10")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Def(nuint self, uint value);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 12")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Ski(nuint self, uint value);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 14")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Spd(nuint self, uint value);

[HookDef(BaseGame.Mr2, Region.Us,
    "55 8B EC 8B 45 ?? 56 8B F1 3D E7 03 00 00 77 ?? 8B 56 ?? 66 8B 52 16")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate uint LH_StatGain_Int(nuint self, uint value);


[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 81 EC 80 00 00 00")]
[Function(CallingConventions.Cdecl)]
public delegate void LH_TournamentComplete(int value);

[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 18 8B D1")]
[Function(CallingConventions.MicrosoftThiscall)]
public delegate void LH_DrillPerform_ChangeStats(nuint self, nuint value);

[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 81 EC D0 00 00 00 A1 ?? ?? ?? ?? 33 C5 89 45 ?? A1 ?? ?? ?? ?? 53")]
[Function(CallingConventions.Cdecl)]
public delegate void LH_TournamentLifeIndex(nuint self); // This function isn't right.

[HookDef(BaseGame.Mr2, Region.Us, "55 8B EC 83 EC 0C 53 56 57 8B 7D ?? 80 BF ?? ?? ?? ?? 01")]
[Function(CallingConventions.Cdecl)]
public delegate void LH_ErrantryStatGains(int value);


namespace MRDX.Game.DynamicTournaments
{
    internal class LearningTesting
    {
        private readonly nuint _address_currentweek;

        private readonly nuint _address_game;
        private readonly nuint _address_monsterdata;


        private uint _currentWeek;

        public bool _errantryEntered;

        private IHook<LH_ErrantryStatGains> _hook_errantryStatGains;
        private IHook<SetupCCtrlBattle>? _hookBattle;

        private IHook<UpdateGenericState>? _hookGenericUpdate;

        public byte _itemGiveHookCount;
        public bool _itemGivenSuccess;

        public bool _itemHandleMagicBananas;
        public byte _itemIdGiven = 0;
        public byte _itemOriginalIdGiven;
        private IHook<LH_DrillPerform_ChangeStats> _LHDPCS;

        private IHook<LH_TournamentComplete> _LHTournamentComplete;
        public byte[] _magicBanana_preStats = new byte[5];
        private IHook<LH_StatGain_Def>? _statGainDef;
        private IHook<LH_StatGain_Int>? _statGainInt;

        private IHook<LH_StatGain_Lif>? _statGainLif;
        private IHook<LH_StatGain_Pow>? _statGainPow;
        private IHook<LH_StatGain_Ski>? _statGainSki;
        private IHook<LH_StatGain_Spd>? _statGainSpd;

        public bool _tournamentEntered;
        public int _tournamentStatBonus = 5; // A Number between 0 and X, 0-X is added to the stat gains.

        public LearningTesting(IHooks hooks)
        {
            var thisProcess = Process.GetCurrentProcess();
            var module = thisProcess.MainModule!;
            var exeBaseAddress = module.BaseAddress.ToInt64();
            _address_game = (nuint)exeBaseAddress;
            _address_currentweek = _address_game + 0x379444;
            _address_monsterdata = _address_game + 0x37667C;


            hooks.AddHook<UpdateGenericState>(SetupGenericUpdateHook)
                .ContinueWith(result => _hookGenericUpdate = result.Result);

            hooks.AddHook<SetupCCtrlBattle>(SetupCCtrlBattleHook)
                .ContinueWith(result => _hookBattle = result.Result);


            hooks.AddHook<LH_StatGain_Lif>(SetupLHStatGainLifHook)
                .ContinueWith(result => _statGainLif = result.Result);

            hooks.AddHook<LH_StatGain_Pow>(SetupLHStatGainPowHook)
                .ContinueWith(result => _statGainPow = result.Result);

            hooks.AddHook<LH_StatGain_Def>(SetupLHStatGainDefHook)
                .ContinueWith(result => _statGainDef = result.Result);

            hooks.AddHook<LH_StatGain_Ski>(SetupLHStatGainSkiHook)
                .ContinueWith(result => _statGainSki = result.Result);

            hooks.AddHook<LH_StatGain_Spd>(SetupLHStatGainSpdHook)
                .ContinueWith(result => _statGainSpd = result.Result);

            hooks.AddHook<LH_StatGain_Int>(SetupLHStatGainIntHook)
                .ContinueWith(result => _statGainInt = result.Result);

            //_hooks.AddHook<LH_TournamentComplete>( SetupLHTournamentComplete ).ContinueWith( result => _LHTournamentComplete = result.Result.Activate() );

            /*_hooks.AddHook<LH_DrillPerform_ChangeStats>( SetupLHDPCS )
                .ContinueWith( result => _LHDPCS = result.Result.Activate() );*/

            hooks.AddHook<LH_ErrantryStatGains>(SetupHookErrantryStatGains)
                .ContinueWith(result => _hook_errantryStatGains = result.Result);
        }

        /// <summary>
        ///     Called regularly from many places.
        ///     This function keeps track of the current week, and if the week progresses forward, resets the tournamentEntered
        ///     flag.
        ///     The forward direction is required as for some reason, at the end of tournaments, the current date is rewound one
        ///     week.
        /// </summary>
        private void SetupGenericUpdateHook(nint parent)
        {
            _hookGenericUpdate!.OriginalFunction(parent);

            Memory.Instance.Read(_address_currentweek, out uint currentWeek);

            if (_currentWeek < currentWeek || _currentWeek == 0)
            {
                _currentWeek = currentWeek;
                _tournamentEntered = false;
                _errantryEntered = false;
            }

            if (_itemGiveHookCount < 6) _itemGiveHookCount++;
            if (_itemGiveHookCount >= 5)
            {
                //if ( _itemHandleMagicBananas ) { StaticBananas(); _itemGiveHookCount = 0; }
            }
        }

        /// <summary>
        ///     This function is called when a battle is intially setup.
        ///     At this point, it is guaranteed that the player is participating in a tournament and sets the tournamentEntered
        ///     flag.
        /// </summary>
        /// <param name="parent"></param>
        private void SetupCCtrlBattleHook(nuint parent)
        {
            Debug.WriteLine("Battle TEnt: " + _tournamentEntered);
            _hookBattle!.OriginalFunction(parent);

            _tournamentEntered = true;
        }

        // These functions all attempt to add an additional 0-TSB for all stat gains. Only called if the tournamentEntered flag is set.
        private uint SetupLHStatGainLifHook(nuint self, uint value)
        {
            Debug.WriteLine("Lif Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            var ret = _statGainLif!.OriginalFunction(self, value);
            return ret;
        }

        private uint SetupLHStatGainPowHook(nuint self, uint value)
        {
            Debug.WriteLine("Pow Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            var ret = _statGainPow!.OriginalFunction(self, value);
            return ret;
        }

        private uint SetupLHStatGainDefHook(nuint self, uint value)
        {
            Debug.WriteLine("Def Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            var ret = _statGainDef!.OriginalFunction(self, value);
            return ret;
        }

        private uint SetupLHStatGainSkiHook(nuint self, uint value)
        {
            Debug.WriteLine("Ski Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            /*if (_errantryEntered ) {
                value += 20;

                Memory.Instance.ReadRaw( _address_monsterdata + (nuint) ( 0x8 ), out byte[] life, 2 );
                int l = life[ 0 ] + (life[ 1 ] << 8);
                l += 20; life[ 0 ] = (byte) ( l & 0xff ); life[ 1 ] = (byte) ( ( l & 0xff00 ) >> 8 );
                Memory.Instance.WriteRaw( _address_monsterdata + (nuint) ( 0x8 ), life );
            }*/
            var ret = _statGainSki!.OriginalFunction(self, value);
            return ret;
        }

        private uint SetupLHStatGainSpdHook(nuint self, uint value)
        {
            Debug.WriteLine("Spd Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            var ret = _statGainSpd!.OriginalFunction(self, value);
            return ret;
        }

        private uint SetupLHStatGainIntHook(nuint self, uint value)
        {
            Debug.WriteLine("Int Gain Hook: " + self + ", Val: " + value + ", TEnt: " + _tournamentEntered);
            value = ApplyTournamentBonus(value);
            var ret = _statGainInt!.OriginalFunction(self, value);
            return ret;
        }


        private void SetupLHTournamentComplete(int value)
        {
            Debug.WriteLine("Tournament Complete Val: " + value);
            _LHTournamentComplete!.OriginalFunction(value);
        }

        private void SetupLHDPCS(nuint self, nuint value)
        {
            var nval = (nuint)(1 + Random.Shared.Next() % 3);
            Debug.WriteLine("Drill Stat?: " + value + " " + nval);

            _LHDPCS!.OriginalFunction(self, nval);
        }

        private void SetupHookErrantryStatGains(int value)
        {
            _errantryEntered = true;
            Debug.WriteLine("ESG: " + value);

            _hook_errantryStatGains!.OriginalFunction(value);
        }

        private void SetupHookItemUsed(int p1, uint p2, uint p3)
        {
            Debug.WriteLine("IU: " + p1 + ", " + p2 + ", " + p3);

            var addrItemId = (uint)p1 + 76;
            Memory.Instance.SafeRead(addrItemId, out _itemOriginalIdGiven);

            // TODO: TESTING PURPOSES ONLY INFINITE FEEDING!
            Memory.Instance.SafeWrite(_address_monsterdata + 0xf6, 0);

            Memory.Instance.SafeRead(_address_monsterdata + 0xf6,
                out _itemGivenSuccess); // Location of "Item already given this week" flag
            _itemGivenSuccess = !_itemGivenSuccess;

            if (_itemOriginalIdGiven == 7) Memory.Instance.SafeWrite(addrItemId, 28);

            if (_itemOriginalIdGiven == 28) // Magic Bananas
                if (_itemGivenSuccess)
                {
                    _itemHandleMagicBananas = true;

                    Memory.Instance.SafeRead(_address_monsterdata + 0x1f,
                        out _magicBanana_preStats[0]); // Monster Fatigue
                    Memory.Instance.SafeRead(_address_monsterdata + 0x23,
                        out _magicBanana_preStats[1]); // Monster Stress
                    Memory.Instance.SafeRead(_address_monsterdata + 0x24,
                        out _magicBanana_preStats[2]); // Monster Spoil
                    Memory.Instance.SafeRead(_address_monsterdata + 0x25, out _magicBanana_preStats[3]); // Monster Fear
                    Memory.Instance.SafeRead(_address_monsterdata + 0x26, out _magicBanana_preStats[4]); // Monster Form
                }

            /*Memory.Instance.SafeRead( (uint) p1, out uint locationa );
            Memory.Instance.SafeRead( (uint) p3, out uint locationx );
            Memory.Instance.SafeRead( (uint) p1 + 8, out uint locationb );
            Memory.Instance.SafeRead( (uint) p3 + 8, out uint locationy );

            Debug.WriteLine( locationa + " ||| " + locationb + " ||| " + locationx + " ||| " + locationy );

            Memory.Instance.SafeRead( (uint) locationa + 76, out byte ba );
            Memory.Instance.SafeRead( (uint) locationx + 76, out byte bx );
            Memory.Instance.SafeRead( (uint) locationb + 76, out byte bb );
            Memory.Instance.SafeRead( (uint) locationy + 76, out byte by );
            Memory.Instance.SafeRead( (uint) p1 + 76, out byte bz );

            Debug.WriteLine( ba + " ||| " + bx + " ||| " + bb + " ||| " + by + " ||| " + bz);*/
            //Memory.Instance.SafeRead( _address_game + 0x1f33a17c, out byte itemId );
            //Debug.WriteLine( "ITEM?! " + itemId );

            //_hook_itemUsed!.OriginalFunction( p1, p2, p3 );

            _itemGiveHookCount = 0;
        }

        private void StaticBananas()
        {
            if (!_itemHandleMagicBananas) return;


            Memory.Instance.SafeWrite(_address_monsterdata + 0x1f, _magicBanana_preStats[0]); // Monster Fatigue
            Memory.Instance.SafeWrite(_address_monsterdata + 0x23,
                Math.Clamp(_magicBanana_preStats[1] - 10, 0, 100)); // Monster Stress
            Memory.Instance.SafeWrite(_address_monsterdata + 0x24,
                Math.Clamp(_magicBanana_preStats[2] + 10, 0, 100)); // Monster Spoil
            Memory.Instance.SafeWrite(_address_monsterdata + 0x25,
                Math.Clamp(_magicBanana_preStats[3] + 10, 0, 100)); // Monster Fear
            //Memory.Instance.SafeWrite( _address_monsterdata + 0x26, Math.Clamp( _magicBanana_preStats[ 4 ] - 1, 0, 100 ) ); // Monster Form TODO: Form is weird and non-linear. Maths later
            Memory.Instance.SafeWrite(_address_monsterdata + 0x1f,
                _magicBanana_preStats[4]); // Monster Form - NOT REALISTIC

            _itemHandleMagicBananas = false;
        }

        private uint ApplyTournamentBonus(uint value)
        {
            if (!_tournamentEntered || _tournamentStatBonus <= 0) return value;
            value = (uint)(value + Random.Shared.Next() % _tournamentStatBonus);
            value = Math.Clamp(value, 1, 999);
            return value;
        }
    }
}

//47
//83 ff 03
//0f 8c 61 fe ff ff