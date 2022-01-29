﻿using SharedData;
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
        public event Action<Entity> OnCharacterClicked = (character) => { };
        public event Action<Vector2Int> OnEmptyTileClicked = (coordinates) => { };
        public event Action OnOutOfBoundsClick = () => { };

        private LevelView levelService;

        public void Init(LevelView levelService)
        {
            this.levelService = levelService;
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2Int clickedCoordinates = GridHelper.MouseToGridCoordinates();
                //print("Clicked on " + clickedCoordinates);

                bool isPointOnLevelGrid = levelService.IsPointOnLevelGrid(clickedCoordinates.x, clickedCoordinates.y);
                if (isPointOnLevelGrid)
                {
                    Entity clickedEntity = levelService.GetEntityAtPosition(clickedCoordinates.x, clickedCoordinates.y);
                    if (clickedEntity != null)
                    {
                        if (clickedEntity.Type == EntityType.Character)
                        {
                            OnCharacterClicked(clickedEntity);
                        }
                        else
                        {
                            print("Non-character entity click");
                        }
                    }
                    else
                    {
                        OnEmptyTileClicked(clickedCoordinates);
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
