﻿using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using SharedData;
using Tactics.Battle;
using Tactics.Helpers;
using Tactics.SharedData;
using Tactics.View;
using Tactics.View.Level;
using UnityEngine;

namespace Tactics.View.Level
{
    public class LevelView
    {
        public LevelData LevelData;

        public Vector2Int GridSize { get; private set; }

        private Sprite[] tileSprites;
        private GameObject tilePrefab;

        private Sprite[] entitySprites;
        private Entity entityPrefab;

        private Transform levelContainer;
        private Transform tilesContainer;
        private Transform entitiesContainer;

        private float quakeAnimationCooldown;
        private AudioComponent audio;

        private BattleManager battleManager;
        private GridNavigator gridNavigator;

        private float stepDuration = .2f;

        private TileView[,] Tiles;

        public LevelView()
        {
            tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
            tilePrefab = Resources.Load<GameObject>("Prefabs/Tile");

            entitySprites = Resources.LoadAll<Sprite>("Sprites/Entities");
            entityPrefab = Resources.Load<Entity>("Prefabs/Entity");

            levelContainer = GameObject.Find("Level").transform;
            tilesContainer = levelContainer.transform.Find("Tiles");
            entitiesContainer = levelContainer.transform.Find("Entities");

            audio = GameObject.Find("Audio").GetComponent<AudioComponent>();
        }

        public void Init(BattleManager battleManager, GridNavigator gridNavigator, LevelData levelData, string[] rows)
        {
            this.gridNavigator = gridNavigator;
            this.battleManager = battleManager;
            battleManager.OnPlayerTurnEnded += OnPlayerTurnEnded;

            int width = levelData.Width;
            int height = levelData.Height;
            GridSize = new Vector2Int(width, height);

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

            // Entities
            Sprite entitySprite;
            for (int y = 0; y < height; y++)
            {
                var row = rows[4 + height + y];

                for (int x = 0; x < width; x++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    switch (row[x])
                    {
                        case 'e':
                            entitySprite = entitySprites[UnityEngine.Random.Range(0, 5)];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Enemy);
                            break;

                        case 'p':
                            entitySprite = entitySprites[UnityEngine.Random.Range(5, 10)];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Player);
                            break;

                        case '#':
                            entitySprite = tileSprites[49];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Obstacle, EntityFaction.Neutral);
                            break;
                    }
                }
            }

            CenterCamera(height);
        }

        public List<Entity> GetEntities()
        {
            return LevelData.Entities;
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

        private void InstantiateEntity(Vector2Int gridPosition, Sprite sprite, EntityType type, EntityFaction faction)
        {
            Entity newEntity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entitiesContainer);
            newEntity.name = type.ToString();
            newEntity.Init(gridPosition, battleManager, gridNavigator, sprite, type, faction, this);
            if (type == EntityType.Character)
            {
                string pathToConfig = "Configs/" + "DefaultCharacterConfig";
                var config = Resources.Load<CharacterConfig>(pathToConfig);
                newEntity.AddCharacterParams(config, stepDuration);
                newEntity.OnMovementFinished += OnEntityMoved;
                newEntity.OnDestroyed += OnEntityDestroyed;
                newEntity.OnSelected += OnEntitySelected;
                newEntity.OnAttack += OnEntityAttack;
            }
            LevelData.Entities.Add(newEntity);
            LevelData.TilesEntities[gridPosition.x, gridPosition.y] = newEntity;
        }

        private void OnPlayerTurnEnded()
        {
            HideAllAttackTargetSelections();
            HideAllBreadCrumbs();
        }

        private void OnEntityAttack()
        {
            HideAllBreadCrumbs();
            HideAllAttackTargetSelections();
        }

        private void OnEntitySelected(Entity selectedEntity, bool isSelected)
        {
            foreach (var entity in GetEntities())
            {
                if (entity != selectedEntity)
                {
                    entity.EntityView.Deselect();
                }
            }
        }

        public void SetBreadCrumbVisible(int x, int y, bool isVisible, float delay = 0)
        {
            Tiles[x, y].SetBreadCrumbVisible(isVisible, delay);
        }

        private void HideAllAttackTargetSelections()
        {
            foreach (var entity in GetEntities())
            {
                entity.EntityView.HideTargetVisuals();
            }
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

        public void PlayQuakeAnimation(int x, int y, int radius)
        {
            if (Time.realtimeSinceStartup < quakeAnimationCooldown)
            {
                return;
            }

            var calculatedTotalDurationOfAnimation = ((radius * 0.25f) * 0.5f) + 0.75f;
            quakeAnimationCooldown = Time.realtimeSinceStartup + calculatedTotalDurationOfAnimation;

            var center = new Vector2(x, y);
            var current = Vector2.zero;

            for (int y2 = y - radius; y2 <= y + radius; y2++)
            {
                for (int x2 = x - radius; x2 <= x + radius; x2++)
                {
                    if (x2 < 0 || x2 >= LevelData.Width ||
                        y2 < 0 || y2 >= LevelData.Height)
                    {
                        continue;
                    }

                    current.x = x2;
                    current.y = y2;

                    var distance = Vector2.Distance(current, center);

                    if (distance <= radius)
                    {
                        var tile = Tiles[x2, y2].transform;
                        var originalY = tile.position.y;

                        var delay = (distance * 0.25f) * 0.5f;

                        var sequence = DOTween.Sequence();
                        sequence.PrependInterval(delay);
                        sequence.Append(tile.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
                        sequence.Append(tile.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
                        sequence.Append(tile.DOLocalMoveY(originalY, 0.25f));

                        for (int i = 0; i < LevelData.Entities.Count; i++)
                        {
                            var entity = LevelData.Entities[i];
                            if (entity.GridPosition == current)
                            {
                                var sequence2 = DOTween.Sequence();
                                sequence2.PrependInterval(delay);
                                var entityTransform = entity.gameObject.transform;
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY + 0.1f, 0.25f).SetEase(Ease.OutBack));
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY - 0.1f, 0.25f).SetEase(Ease.OutBack));
                                sequence2.Append(entityTransform.DOLocalMoveY(originalY, 0.25f));
                            }
                        }
                    }
                }
            }

            audio.PlayQuake();
        }

        private void OnEntityDestroyed(Entity entity)
        {
            LevelData.Entities.Remove(entity);
            LevelData.TilesEntities[entity.GridPosition.x, entity.GridPosition.y] = null;
        }

        private void OnEntityMoved(Entity entity, Vector2Int oldPosition, Vector2Int newPosition)
        {
            HideAllBreadCrumbs();
            LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
            LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
        }
    }
}
