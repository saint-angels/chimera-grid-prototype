using System;
using System.Collections.Generic;
using System.Linq;
using Tactics;
using SharedData;
using UnityEngine;
using Tactics.Helpers;
using Tactics.Helpers.Promises;
using Tactics.View.Level;
using Tactics.SharedData;
using Tactics.Helpers.StatefulEvent;

namespace Tactics.Battle
{
    public class BattleManager : MonoBehaviour
    {
        enum TurnState
        {
            UserIdle,
            UserCharSelected,
            ActionInProgress,
        }

        public Action<bool> OnBattleOver = (userWon) => { };
        public Action OnPlayerTurnEnded = () => { };
        public Action<Entity, Vector2Int, Vector2Int> OnCharacterMoved = (unit, oldPos, newPos) => { };
        public Action<Entity, bool> OnEntitySelected = (entity, isSelected) => { };
        public Action<List<Entity>, List<Entity>> OnUserCharacterActionsUpdate = (movable, attacking) => { };
        public Action<Entity, Entity, int> OnUnitAttack = (unit, actionType, damage) => { };
        public Action OnUnitMoveStarted = () => { };

        [SerializeField] private Transform entityContainer = null;

        private LevelView levelView;

        private Entity entityPrefab;
        private Entity selectedCharacter;

        private TurnState turnState;

        private LevelData LevelData;
        private List<Entity> MovableUserChars = new List<Entity>();
        private List<Entity> AttackingUserChars = new List<Entity>();

        public void Init(InputSystem inputSystem, GridNavigator gridNavigator)
        {
            MovableUserChars = new List<Entity>();
            AttackingUserChars = new List<Entity>();
            entityPrefab = Resources.Load<Entity>("Prefabs/Entity");
            var levelText = Resources.Load<TextAsset>($"Levels/Level1").text;
            string[] rows = levelText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int width = int.Parse(rows[0]);
            int height = int.Parse(rows[1]);

            LevelData = new LevelData
            {
                Width = width,
                Height = height,
                Entities = new List<Entity>(),
                TilesEntities = new Entity[width, height]
            };

            levelView = new LevelView();
            levelView.Init(this, gridNavigator, LevelData, rows);

            // Entities
            var entitySprites = Resources.LoadAll<Sprite>("Sprites/Entities");
            var tileSprites = Resources.LoadAll<Sprite>("Sprites/Tileset");
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
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Enemy, gridNavigator);
                            break;

                        case 'p':
                            entitySprite = entitySprites[UnityEngine.Random.Range(5, 10)];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Character, EntityFaction.Player, gridNavigator);
                            break;

                        case '#':
                            entitySprite = tileSprites[49];
                            InstantiateEntity(gridPosition, entitySprite, EntityType.Obstacle, EntityFaction.Neutral, gridNavigator);
                            break;
                    }
                }
            }

            StartPlayerTurn();

            void InstantiateEntity(Vector2Int gridPosition,
                                           Sprite sprite,
                                           EntityType type,
                                           EntityFaction faction,
                                           GridNavigator gridNavigator)
            {
                Entity newEntity = GameObject.Instantiate(entityPrefab, Vector3.zero, Quaternion.identity, entityContainer);
                newEntity.name = type.ToString();
                newEntity.Init(gridPosition, this, gridNavigator, sprite, type, faction, levelView);
                if (type == EntityType.Character)
                {
                    string pathToConfig = "Configs/" + "DefaultCharacterConfig";
                    var config = Resources.Load<CharacterConfig>(pathToConfig);
                    newEntity.AddCharacterParams(config);
                    newEntity.OnMoveStarted += () => OnUnitMoveStarted();
                    newEntity.OnMoved += (entity, oldPosition, newPosition) =>
                    {
                        LevelData.TilesEntities[oldPosition.x, oldPosition.y] = null;
                        LevelData.TilesEntities[newPosition.x, newPosition.y] = entity;
                        OnCharacterMoved(entity, oldPosition, newPosition);
                    };
                    newEntity.OnDestroyed += (entity) =>
                    {
                        LevelData.Entities.Remove(entity);
                        LevelData.TilesEntities[entity.GridPosition.x, entity.GridPosition.y] = null;
                    };
                    newEntity.OnSelected += (selectedEntity) =>
                    {
                        OnEntitySelected(selectedEntity, true);
                    };
                    newEntity.OnAttack += (entity, target, damage) => OnUnitAttack(entity, target, damage);
                }
                LevelData.Entities.Add(newEntity);
                LevelData.TilesEntities[gridPosition.x, gridPosition.y] = newEntity;
            }
        }


        public List<Entity> GetCharacters(EntityFaction? filterFaction = null)
        {
            return LevelData.Entities
                .Where(e =>
                    {
                        bool factionCheck = true;
                        if (filterFaction.HasValue)
                        {
                            factionCheck = filterFaction.Value == e.Faction;
                        }
                        bool typeCheck = e.Type == EntityType.Character;
                        return factionCheck && typeCheck;
                    })
                .ToList();
        }


        public List<Entity> GetAllEntities()
        {
            return LevelData.Entities;
        }

        //There could be only 1 entity at each tile at a time.
        public Entity GetEntityAtPosition(int x, int y)
        {
            if (IsPointOnLevelGrid(x, y))
            {
                return LevelData.TilesEntities[x, y];
            }
            else
            {
                return null;
            }
        }

        public List<Entity> GetEntitiesInRange(Entity attacker, EntityFaction targetFaction)
        {
            int range = attacker.AttackRange;
            Vector2Int position = attacker.GridPosition;

            List<Entity> entitiesList = new List<Entity>();

            //Check x axis
            for (int xOffset = -range; xOffset <= range; xOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x + xOffset, position.y);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            //Check y axis
            for (int yOffset = -range; yOffset <= range; yOffset++)
            {
                Vector2Int offsetPosition = new Vector2Int(position.x, position.y + yOffset);
                Entity entity = GetEntityAtPosition(offsetPosition.x, offsetPosition.y);
                if (entity != null && entity != attacker && entity.Faction == targetFaction)
                {
                    entitiesList.Add(entity);
                }
            }

            return entitiesList;
        }

        public Entity GetClosestCharacter(Vector2Int targetPosition, EntityFaction faction)
        {
            return LevelData.Entities
                        .Where(p => p.Type == EntityType.Character && p.Faction == faction)
                        .OrderBy((entity) => Vector2Int.Distance(targetPosition, entity.GridPosition))
                        .FirstOrDefault();
        }

        public bool IsPointOnLevelGrid(int x, int y)
        {
            bool outOfGridBounds = x >= LevelData.Width || x < 0 || y >= LevelData.Height || y < 0;
            return outOfGridBounds == false;
        }

        private void SetState(TurnState newTurnState)
        {
            turnState = newTurnState;
        }

        private void StartPlayerTurn()
        {
            MovableUserChars.Clear();
            AttackingUserChars.Clear();
            MovableUserChars.AddRange(GetCharacters(EntityFaction.Player));
            AttackingUserChars.AddRange(MovableUserChars);
            OnUserCharacterActionsUpdate(MovableUserChars, AttackingUserChars);

            SetState(TurnState.UserIdle);
        }

        private IPromise PlayEnemyTurn()
        {
            List<IPromise> enemyTurnPromises = new List<IPromise>();
            var enemies = GetCharacters(EntityFaction.Enemy);
            foreach (var enemy in enemies)
            {
                enemyTurnPromises.Add(enemy.MakeAITurn());
            }
            CheckForGameOver();

            return Deferred.All(enemyTurnPromises);
        }

        public void EndTurn()
        {
            OnPlayerTurnEnded();
            SetState(TurnState.ActionInProgress);
            PlayEnemyTurn().Done(() => StartPlayerTurn());
        }

        public void ClickCharacter(Entity clickedCharacter)
        {
            switch (turnState)
            {
                case TurnState.UserIdle:
                    if (clickedCharacter.Faction == EntityFaction.Player)
                    {
                        SelectUserCharacter(clickedCharacter);
                    }
                    break;
                case TurnState.UserCharSelected:
                    switch (clickedCharacter.Faction)
                    {
                        case EntityFaction.Player:
                            SelectUserCharacter(clickedCharacter);
                            break;
                        case EntityFaction.Enemy:
                            bool characterCanAttack = AttackingUserChars.Contains(selectedCharacter);
                            if (characterCanAttack && selectedCharacter.CanAttack(clickedCharacter))
                            {
                                AttackingUserChars.Remove(selectedCharacter);
                                OnUserCharacterActionsUpdate(MovableUserChars, AttackingUserChars);
                                selectedCharacter.Attack(clickedCharacter);
                                bool canMove = MovableUserChars.Contains(selectedCharacter);
                                if (canMove)
                                {
                                    SelectUserCharacter(selectedCharacter);
                                }
                                CheckForGameOver();
                            }
                            break;
                    }
                    break;
            }
        }

        public void HandleEmptyTileClick(Vector2Int gridPosition)
        {
            switch (turnState)
            {
                case TurnState.UserIdle:
                    break;
                case TurnState.UserCharSelected:
                    if (selectedCharacter.CanMove(gridPosition))
                    {
                        SetState(TurnState.ActionInProgress);
                        var movingCharacter = selectedCharacter;

                        movingCharacter.Move(gridPosition)
                            .Done(() =>
                            {
                                MovableUserChars.Remove(movingCharacter);
                                OnUserCharacterActionsUpdate(MovableUserChars, AttackingUserChars);
                                bool canAttack = AttackingUserChars.Contains(movingCharacter);
                                if (canAttack)
                                {
                                    SelectUserCharacter(movingCharacter);
                                }
                            });
                    }
                    break;
            }
        }

        private void CheckForGameOver()
        {
            var enemyCharacters = GetCharacters(EntityFaction.Enemy);
            var playerCharacters = GetCharacters(EntityFaction.Player);

            if (enemyCharacters.Count == 0)
            {
                OnBattleOver(true);
            }
            else if (playerCharacters.Count == 0)
            {
                OnBattleOver(false);
            }
        }

        private void SelectUserCharacter(Entity selectedCharacter)
        {
            this.selectedCharacter = selectedCharacter;

            bool movementAllowed = MovableUserChars.Contains(selectedCharacter);
            bool attackAllowed = AttackingUserChars.Contains(selectedCharacter);
            selectedCharacter.Select(movementAllowed, attackAllowed);
            SetState(TurnState.UserCharSelected);
        }
    }
}
