using System;
using System.Collections.Generic;
using Tactics.Battle;
using Tactics.View.Level;

namespace Tactics.Helpers
{
    public class LevelData
    {
        public int Width;
        public int Height;
        public TileView[,] Tiles;
        public List<Entity> Entities;
        public Entity[,] TilesEntities;
    }
}
