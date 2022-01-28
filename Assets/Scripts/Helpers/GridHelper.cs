using UnityEngine;

namespace Helpers
{
	public static class GridHelper
	{
		public static readonly Vector2 TileSize = new Vector2(0.5f, 0.25f);

        public static Vector2 ToWorldCoordinates(Vector2Int position)
        {
            return ToWorldCoordinates(position.x, position.y);
        }

        public static Vector2 ToWorldCoordinates(int x, int y)
		{
			var tileX = (x * TileSize.x) - (y * TileSize.x);
			var tileY = -(x * TileSize.y) - (y * TileSize.y);
			return new Vector2(tileX, tileY);
		}

		public static Vector2Int ToGridCoordinatesFloor(float worldX, float worldY)
		{
			worldY -= TileSize.y;
			float tileX = (worldX / TileSize.x - worldY / TileSize.y) / 2f;
            float tileY = (-worldX / TileSize.x - worldY / TileSize.y) / 2f;
			return new Vector2Int(Mathf.FloorToInt(tileX), Mathf.FloorToInt(tileY));
		}

		public static int GetSortingOrder(int x, int y)
		{
			var sortingOrder = Mathf.Abs(-x - y);
			return sortingOrder;
		}

		public static Vector2Int MouseToGridCoordinates()
		{
			var mousePosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.nearClipPlane);
			var mousePositionInWorldSpace = Camera.main.ScreenToWorldPoint(mousePosition);
			return ToGridCoordinatesRound(mousePositionInWorldSpace.x, mousePositionInWorldSpace.y);
		}

        public static Vector2Int ToGridCoordinatesRound(float worldX, float worldY)
        {
            worldY -= TileSize.y;
            var tileX = (worldX / TileSize.x - worldY / TileSize.y) / 2f;
            var tileY = (-worldX / TileSize.x - worldY / TileSize.y) / 2f;
            return new Vector2Int(Mathf.RoundToInt(tileX), Mathf.RoundToInt(tileY));
        }
    }
}