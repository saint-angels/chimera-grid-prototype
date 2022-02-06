using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SharedData;
using Tactics.Battle;
using Tactics.Helpers;
using Tactics.SharedData;
using UnityEngine;

namespace Tactics.View.Level
{
    public class LevelView
    {
        public LevelData LevelData;

        private Sprite[] tileSprites;
        private GameObject tilePrefab;

        private Transform levelContainer;
        private Transform tilesContainer;
        private Transform entitiesContainer;

        private float quakeAnimationCooldown;

        private TileView[,] Tiles;

        public LevelView(BattleManager battleManager)
        {
            tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
            tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

            levelContainer = GameObject.Find("Level").transform;
            tilesContainer = levelContainer.transform.Find("Tiles");

            battleManager.OnPlayerTurnEnded += () =>
            {
                HideAllBreadCrumbs();
            };

            battleManager.OnUnitAttack += (unit, target, dmg) =>
            {
                HideAllBreadCrumbs();
            };

            battleManager.OnCharacterMoved += (unit, oldPos, newPos) =>
            {
                HideAllBreadCrumbs();
            };

            battleManager.OnEntitySelected += (entity) =>
            {
                HideAllBreadCrumbs();
                foreach (var moveTargetPosition in entity.possibleMoveTargets)
                {
                    SetBreadCrumbVisible(moveTargetPosition.x, moveTargetPosition.y, true);
                }
            };

            battleManager.OnLevelInit += (levelData, rows) =>
            {
                int width = levelData.Width;
                int height = levelData.Height;

                Tiles = new TileView[width, height];

                this.LevelData = levelData;
                // Ground
                for (int y = 0; y < height; y++)
                {
                    var row = rows[3 + y];

                    for (int x = 0; x < width; x++)
                    {
                        var tile = int.Parse(row[x].ToString()) - 1;
                        InstantiateTile(x, y, tile);
                    }
                }

                CenterCamera(height);
            };
        }

        private void CenterCamera(int levelHeight)
        {
            var numberOfRowsBeforeAdjustmentIsNeeded = 9;
            var difference = Mathf.Max(0, levelHeight - numberOfRowsBeforeAdjustmentIsNeeded);

            var adjustment = -(0.33f * difference);
            adjustment = Mathf.RoundToInt(adjustment);
            Camera.main.transform.position = new Vector3(adjustment, adjustment, Camera.main.transform.position.z);
        }

        private void InstantiateTile(int x, int y, int index)
        {
            var tile = GameObject.Instantiate(tilePrefab, Vector3.zero, Quaternion.identity, tilesContainer);
            var renderer = tile.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = GridHelper.GetSortingOrder(x, y);
            renderer.sprite = tileSprites[index];
            renderer.color = (x ^ y) % 2 == 0 ? Color.white : new Color(0.95f, 0.95f, 0.95f);
            tile.name = $"Tile{x}_{y}";

            tile.transform.localPosition = GridHelper.ToWorldCoordinates(x, y);

            Tiles[x, y] = tile.GetComponent<TileView>();
        }

        public void SetBreadCrumbVisible(int x, int y, bool isVisible, float delay = 0)
        {
            Tiles[x, y].SetBreadCrumbVisible(isVisible, delay);
        }

        public void HideAllBreadCrumbs()
        {
            for (int y = 0; y < LevelData.Height; y++)
            {
                for (int x = 0; x < LevelData.Width; x++)
                {
                    Tiles[x, y].SetBreadCrumbVisible(false);
                }
            }
        }
    }
}
