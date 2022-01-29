using System;
using System.Collections.Generic;
using System.Linq;
using Tactics;
using SharedData;
using UnityEngine;
using Tactics.Helpers;
using Tactics.Helpers.Promises;

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
        private BattleHUD hud;
        private InputSystem inputSystem;

        private Entity selectedCharacter;

        private TurnState turnState;
        private List<Entity> movablePlayerCharacters = new List<Entity>();
        private List<Entity> attackingPlayerCharacters = new List<Entity>();

        private void Start()
        {
            // Load the level
            levelService = new LevelService();
            GridNavigator gridNavigator = GetComponent<GridNavigator>() ?? gameObject.AddComponent<GridNavigator>();
            levelService.Init("Level2", this, gridNavigator);

            hud = GameObject.Find("Canvas").GetComponent<BattleHUD>();
            hud.OnEndTurnClicked += OnEndTurnClicked;

            inputSystem = GetComponent<InputSystem>() ?? gameObject.AddComponent<InputSystem>();
            inputSystem.Init(levelService);
            inputSystem.OnCharacterClicked += OnCharacterClicked;
            inputSystem.OnEmptyTileClicked += OnEmptyTileClicked;

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
