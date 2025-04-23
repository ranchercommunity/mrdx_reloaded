// using MRDX.Base.Mod.Interfaces;
//
// public class MonsterBreed
// {
//     public static List<MonsterBreed> AllBreeds = new();
//
//     public byte _rarity;
//
//     public List<IMonsterAttack> _techniques;
//
//     public MonsterGenus breed_id;
//     public string breed_identifier;
//
//     public string name;
//     public string name_short;
//     public MonsterGenus sub_id;
//
//     public MonsterBreed(string n, string nshort, byte bid, byte sid, string identifier, List<IMonsterAttack> techs)
//     {
//         name = n;
//         name_short = nshort;
//
//         breed_id = (MonsterGenus)bid;
//         sub_id = (MonsterGenus)sid;
//         breed_identifier = identifier;
//
//         _techniques = techs;
//
//         // Calculate Monster Rarity based upon breed and subbreeds.
//     }
//
//     // public static void SetupMonsterBreedList ( string gamePath ) {
//     //     for ( var i = 0; i < IMonster.AllMonsters.Count(); i++ ) {
//     //         MonsterInfo info = IMonster.AllMonsters[ i ];
//     //         MonsterInfo sinfo = IMonster.AllMonsters[ i ];
//     //         if ( !info.Name.StartsWith( "Unknown" ) ) {
//     //
//     //             string breedFolder = gamePath + "\\mf2\\data\\mon\\" + info.ShortName + "\\";
//     //             var textureFiles = Directory.EnumerateFiles( breedFolder, "??_??.tex" );
//     //             // var techniqueFile = breedFolder + info.ShortName.Substring( 0, 2 ) + "_" + info.ShortName.Substring( 0, 2 ) + "_wz.bin";
//     //
//     //             // Build a singular tech list. This will be the same for every breed until I do the right now and actually check errantry (no thanks :( )
//     //             // FileStream fs = File.OpenRead( techniqueFile );
//     //
//     //             Logger.Debug( info.Name );
//     //             // List<IMonsterAttack> techniqueList = IGame.ParseTechniqueFile( fs );
//     //
//     //             //Console.Write( "\nReading Techniques for " + info.Name + ": " );
//     //             
//     //             //byte [] techniqueList = ParseTechniqueFile( fs );
//     //
//     //             // Enumerate through each species tex file and generate the final breeds.
//     //             foreach ( string currentFile in textureFiles ) {
//     //                 var m_identifier = currentFile.Substring( currentFile.Length - 9, 2 );
//     //                 var sub_identifier = currentFile.Substring( currentFile.Length - 6, 2 );
//     //                 var sub_id = i;
//     //
//     //                 // Compares Sub Information to Known Monsters - Have to do some shenanigans to take care of the unknown ??? species in the database.
//     //                 for ( var j = 0; j < IMonster.AllMonsters.Length; j++ ) {
//     //                     sinfo = IMonster.AllMonsters[ j ];
//     //
//     //                     var infoCheck = sinfo.ShortName.Substring( 0, 2 );
//     //
//     //                     if ( sinfo.ShortName.EndsWith( "1" ) ) { infoCheck = "xx"; }
//     //                     else if ( sinfo.ShortName.EndsWith( "2" ) ) { infoCheck = "xy"; }
//     //                     else if ( sinfo.ShortName.EndsWith( "3" ) ) { infoCheck = "xz"; }
//     //                     else if ( sinfo.ShortName.EndsWith( "4" ) ) { infoCheck = "yx"; }
//     //                     else if ( sinfo.ShortName.EndsWith( "5" ) ) { infoCheck = "yy"; }
//     //                     else if ( sinfo.ShortName.EndsWith( "6" ) ) { infoCheck = "yz"; }
//     //
//     //                     if ( infoCheck == sub_identifier ) {
//     //                         sub_id = j;
//     //                         break;
//     //                     }
//     //                 }
//     //                 MonsterBreed newBreed = new MonsterBreed( info.Name + "/" + sinfo.Name, info.ShortName, (byte) i, (byte) sub_id, m_identifier + "_" + sub_identifier, techniqueList );
//     //                 MonsterBreed.AllBreeds.Add( newBreed );
//     //             }
//     //         }
//     //
//     //     }
//     // }
//
//     /// <summary>
//     ///     Reads from the provided FileStream and returns a byte list. 0 = Basic, 1 Hit, 2 Heavy, 3 Withering, 4 Sharp,
//     ///     5 Special, 6 Invalid
//     /// </summary>
//     private static byte[] ParseTechniqueFile(FileStream fs)
//     {
//         var techniqueList = new byte[24];
//         long tpos = 0;
//
//         for (var i = 0; i < 24; i++) techniqueList[i] = 6;
//
//         for (var i = 0; i < 24; i++)
//         {
//             fs.Position = i * 4;
//             tpos = fs.ReadByte();
//             tpos += (long)fs.ReadByte() * 256;
//
//             if (tpos != 0xFFFF)
//             {
//                 fs.Position = tpos + 0x10;
//                 techniqueList[(byte)((tpos - 0x60) / 0x20)] = (byte)fs.ReadByte();
//             }
//         }
//
//         fs.Close();
//         return techniqueList;
//     }
//
//     public static MonsterBreed GetBreedInfo(MonsterGenus main, MonsterGenus sub)
//     {
//         for (var i = 0; i < AllBreeds.Count; i++)
//         {
//             var mb = AllBreeds[i];
//             if (mb.breed_id == main && mb.sub_id == sub) return mb;
//         }
//
//         return AllBreeds[0];
//     }
// }

