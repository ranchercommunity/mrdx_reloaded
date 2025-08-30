using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Memory.Streams;

//using static MRDX.Base.Mod.Interfaces.TournamentData;
using Config = MRDX.Game.DynamicTournaments.Configuration.Config;

namespace MRDX.Game.DynamicTournaments;
public class MonsterNames
{

    private static bool _initialized = false;
    public static List<MonsterNames> AllNames = new List<MonsterNames>();

    public string monsterName;

    public sbyte breedMainLock;
    public sbyte breedSubLock;

    public static void InitializeMonsterNames( string tournamentFolder ) {

        var file = Path.Combine( tournamentFolder, $"MonsterNames.txt" );

        if ( File.Exists( file ) ) {
            try {
                var mdata = File.ReadAllLines( file );

                foreach ( var r in mdata ) {
                    var row = r.Split( "\t" );

                    AllNames.Add( new MonsterNames( row[ 0 ], sbyte.Parse( row[ 1 ] ), sbyte.Parse( row[ 2 ] ) ) );
                }
            } 
            catch { Logger.Error( "Monster Name file could not be loaded. DT will likely fail during monster generation.", Color.Red ); }
        }
        _initialized = true;
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

    public MonsterNames( string name, sbyte main, sbyte sub ) {
        monsterName = name;

        breedMainLock = main;
        breedSubLock = sub;

        AllNames.Add( this );
    }
}