using System;
using System.Collections.Generic;
using System.Linq;
using Tactics;
using SharedData;
using UnityEngine;
using Tactics.Helpers;
using Tactics.Helpers.Promises;
using Tactics.View.Level;

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

        private LevelService levelService;

        private Entity selectedCharacter;

        private TurnState turnState;
        private List<Entity> movablePlayerCharacters = new List<Entity>();
        private List<Entity> attackingPlayerCharacters = new List<Entity>();

        public void Init(BattleHUD hud, InputSystem inputSystem)
        {

            var levelText = Resources.Load<TextAsset>($"Levels/Level2").text;
            string[] rows = levelText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int width = int.Parse(rows[0]);
            int height = int.Parse(rows[1]);

            var levelData = new LevelData
            {
                Width = width,
                Height = height,
                Tiles = new TileView[width, height],
                Entities = new List<Entity>(),
                TilesEntities = new Entity[width, height]
            };

            // Load the level
            levelService = new LevelService();
            GridNavigator gridNavigator = GetComponent<GridNavigator>() ?? gameObject.AddComponent<GridNavigator>();
            levelService.Init(this, gridNavigator, levelData, rows);

            hud.OnEndTurnClicked += OnEndTurnClicked;

            inputSystem.OnCharacterClicked += OnCharacterClicked;
            inputSystem.OnEmptyTileClicked += OnEmptyTileClicked;
            inputSystem.Init(levelService);

            StartPlayerTurn();
        }

        private void SetState(TurnState newTurnState)
        {
            turnState = newTurnState;
        }

        private void StartPlayerTurn()
        {
            movablePlayerCharacters.Clear();
            attackingPlayerCharacters.Clear();
            movablePlayerCharacters.AddRange(levelService.GetCharacters(EntityFaction.Player));
            attackingPlayerCharacters.AddRange(movablePlayerCharacters);

            SetState(TurnState.UserIdle);
        }

        private IPromise PlayEnemyTurn()
        {
            List<IPromise> enemyTurnPromises = new List<IPromise>();
            var enemies = levelService.GetCharacters(EntityFaction.Enemy);
            foreach (var enemy in enemies)
            {
                enemyTurnPromises.Add(enemy.MakeAITurn());
            }
            CheckForGameOver();

            return Deferred.All(enemyTurnPromises);
        }

        private void OnEndTurnClicked()
        {
            OnPlayerTurnEnded();
            SetState(TurnState.ActionInProgress);
            PlayEnemyTurn().Done(() => StartPlayerTurn());
        }

        private void OnCharacterClicked(Entity clickedCharacter)
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
                            bool characterCanAttack = attackingPlayerCharacters.Contains(selectedCharacter);
                            if (characterCanAttack && selectedCharacter.CanAttack(clickedCharacter))
                            {
                                attackingPlayerCharacters.Remove(selectedCharacter);
                                selectedCharacter.Attack(clickedCharacter);
                                bool canMove = movablePlayerCharacters.Contains(selectedCharacter);
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

        private void OnEmptyTileClicked(Vector2Int gridPosition)
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
                                movablePlayerCharacters.Remove(movingCharacter);
                                bool canAttack = attackingPlayerCharacters.Contains(movingCharacter);
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
            var enemyCharacters = levelService.GetCharacters(EntityFaction.Enemy);
            var playerCharacters = levelService.GetCharacters(EntityFaction.Player);

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

            //TODO: Let the entity itself contain the info if it can attack/move or not
            bool movementAllowed = movablePlayerCharacters.Contains(selectedCharacter);
            bool attackAllowed = attackingPlayerCharacters.Contains(selectedCharacter);
            selectedCharacter.Select(movementAllowed, attackAllowed);
            SetState(TurnState.UserCharSelected);
        }
    }
}
