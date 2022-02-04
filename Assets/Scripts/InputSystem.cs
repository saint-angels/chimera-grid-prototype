using SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using Tactics.Battle;
using Tactics.Helpers;
using Tactics.View.Level;
using UnityEngine;

namespace Tactics
{
    public class InputSystem : MonoBehaviour
    {
        public event Action OnOutOfBoundsClick = () => { };

        private BattleManager battleManager;

        public void Init(BattleManager battleManager)
        {
            this.battleManager = battleManager;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int clickedCoordinates = GridHelper.MouseToGridCoordinates();
                //print("Clicked on " + clickedCoordinates);

                bool isPointOnLevelGrid = battleManager.IsPointOnLevelGrid(clickedCoordinates.x, clickedCoordinates.y);
                if (isPointOnLevelGrid)
                {
                    Entity clickedEntity = battleManager.GetEntityAtPosition(clickedCoordinates.x, clickedCoordinates.y);
                    if (clickedEntity != null)
                    {
                        if (clickedEntity.Type == EntityType.Character)
                        {
                            battleManager.ClickCharacter(clickedEntity);
                        }
                        else
                        {
                            print("Non-character entity click");
                        }
                    }
                    else
                    {
                        battleManager.HandleEmptyTileClick(clickedCoordinates);
                    }
                }
                else
                {
                    OnOutOfBoundsClick();
                }
            }
        }
    }
}
