using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Assets.Scripts
{
    public class Root : MonoBehaviour
    {
        [SerializeField] private UI.BattleHUD hud;
        [SerializeField] private BattleManager battleManager;

        void Awake()
        {
            hud.Init(battleManager);
        }

    }
}
