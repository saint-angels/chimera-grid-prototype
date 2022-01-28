using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tactics.Battle;

namespace Tactics
{
    public class Root : MonoBehaviour
    {
        [SerializeField] private Tactics.BattleHUD hud;
        [SerializeField] private BattleManager battleManager;

        void Awake()
        {
            hud.Init(battleManager);
        }

    }
}
