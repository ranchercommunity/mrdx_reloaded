using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRDX.Base.Mod;
using MRDX.Base.Mod.Interfaces;

namespace MRDX.Game.MoreMonsters
{
    public class MMBreed {

        public readonly MonsterGenus _genusNewMain;
        public readonly MonsterGenus _genusNewSub;

        public readonly MonsterGenus _genusBaseMain;
        public readonly MonsterGenus _genusBaseSub;

        // Filepath values are represented as [shortname\ssMain_ssSub].
        // Examples include 'kkro\kk_kf' and 'mggjr\mg_kb', used to precalculate the monster type specific filepaths for redirects.
        public readonly string _filepathBase;
        public readonly string _filepathNew;

        public List<IMonster> _monsterVariants;

        public MMBreed ( MonsterGenus newMain, MonsterGenus newSub, MonsterGenus baseMain, MonsterGenus baseSub ) {
            _genusNewMain = newMain;
            _genusNewSub = newSub;
            _genusBaseMain = baseMain;
            _genusBaseSub = baseSub;

            var baseMainInfo = IMonster.AllMonsters[ (int) _genusBaseMain ];
            var baseSubInfo = IMonster.AllMonsters[ (int) _genusBaseSub ];
            var newMainInfo = IMonster.AllMonsters[ (int) _genusNewMain ];
            var newSubInfo = IMonster.AllMonsters[ (int) _genusNewSub ];

            _filepathBase = baseMainInfo.ShortName + @"\" + baseMainInfo.ShortName[ ..2 ] + "_" + baseSubInfo.ShortName[ ..2 ];
            _filepathNew = newMainInfo.ShortName + @"\" + newMainInfo.ShortName[ ..2 ] + "_" + newSubInfo.ShortName[ ..2 ];

            _monsterVariants = new List<IMonster>();
        }

        public bool MatchNewBreed ( MonsterGenus main, MonsterGenus sub ) {
            return ( _genusNewMain == main && _genusNewSub == sub );
        }

        public bool MatchBaseBreed ( MonsterGenus main, MonsterGenus sub ) {
            return ( _genusBaseMain == main && _genusBaseSub == sub );
        }

        public void NewVariant ( ushort lifespan, short nature, LifeType growthpat,
            ushort slif, ushort spow, ushort sint, ushort sski, ushort sspe, ushort sdef,
            byte glif, byte gpow, byte gint, byte gski, byte gspe, byte gdef,
            byte arena, byte guts, int battlespec, int moves, ushort trainbonuses ) {

            MRDX.Base.Mod.MonsterCache monster = new MonsterCache();

            monster.GenusMain = _genusNewMain;
            monster.GenusSub = _genusNewSub;

            monster.Lifespan = lifespan;
            monster.InitalLifespan = lifespan;

            monster.NatureRaw = nature;
            monster.NatureBase = (sbyte) nature;

            monster.LifeType = growthpat;

            monster.Life = slif;
            monster.Power = spow;
            monster.Intelligence = sint;
            monster.Skill = sski;
            monster.Speed = sspe;
            monster.Defense = sdef;


            monster.GrowthRateLife = glif;
            monster.GrowthRatePower = gpow;
            monster.GrowthRateIntelligence = gint;
            monster.GrowthRateSkill = gski;
            monster.GrowthRateSpeed = gspe;
            monster.GrowthRateDefense = gdef;

            monster.ArenaSpeed = arena;
            monster.GutsRate = guts;

            monster.TrainBoost = trainbonuses;

            _monsterVariants.Add( monster );
        }
    }
    
}
