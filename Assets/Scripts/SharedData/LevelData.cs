using System;
using System.Collections.Generic;
using Tactics.Battle;
using Tactics.View.Level;

namespace Tactics.SharedData
{
    public class LevelData
    {
        public int Width;
        public int Height;
        public List<EntityShell> Entities;
        public EntityShell[,] TilesEntities;
    }
}
