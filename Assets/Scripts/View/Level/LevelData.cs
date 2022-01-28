using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.Presentation.Entities;

namespace Helpers
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