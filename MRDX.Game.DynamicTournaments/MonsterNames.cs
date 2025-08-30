using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;
public class MonsterNames
{

    private static bool _initialized = false;
    public static List<MonsterNames> AllNames = new List<MonsterNames>();
    public static List<(MonsterGenus, List<string>)> MainLocks = new List<(MonsterGenus, List<string>)>();
    public static List<(MonsterGenus, List<string>)> SubLocks = new List<(MonsterGenus, List<string>)>();

    public string monsterName;

    public sbyte breedMainLock;
    public sbyte breedSubLock;

    public static void InitializeLists() {
        _initialized = true;

        for ( var i = 0; i < 43; i++ ) {
            MainLocks.Add( ((MonsterGenus) i, new List<string>()) );
            SubLocks.Add( ((MonsterGenus) i, new List<string>()) );
        }

    }

    public static string GetName( MonsterGenus main, MonsterGenus sub ) {

        var chosenMN = AllNames.Where( name => ( name.breedMainLock == (sbyte) main || name.breedMainLock == -1 ) &&
            ( name.breedSubLock == (sbyte) sub || name.breedSubLock == -1 ) );

        var c = Random.Shared.Next(chosenMN.Count());
        foreach ( MonsterNames mn in chosenMN ) {
            if ( c == 0 ) { return mn.monsterName; }
            c--;
        }

        return "Oopsies";

       /* if ( Random.Shared.Next(100) < 20) {
            var valid = true;
            var mList = MainLocks[ (int) main ].Item2;
            var sList = SubLocks[ (int) sub ].Item2;

            if ( mList.Count > 0 ) {
                var mainName = mList[ Random.Shared.Next( mList.Count ) ];


                if ( ( sList.Count > 0 && sList.Contains(mainName) ) || sList.Count == 0 ) {
                    return mainName;
                }
            }

            else if ( sList.Count > 0 ) {
                return sList[ Random.Shared.Next( sList.Count ) ];
            }
        }*/
    }
}