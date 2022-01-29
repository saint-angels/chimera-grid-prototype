using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tactics.Battle;
using Tactics.Helpers;

namespace Tactics
{
    public class Root : MonoBehaviour
    {
        [SerializeField] private Tactics.BattleHUD hud;
        [SerializeField] private BattleManager battleManager;
        [SerializeField] private InputSystem input;

        void Start()
        {
            hud.Init(battleManager);
            battleManager.Init(hud, input);
        }

    }
}
