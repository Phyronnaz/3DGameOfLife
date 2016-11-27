using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public static bool ConstantUpdate;

        public Material Material;

        GameOfLife gameOfLife;

        void Start()
        {
#if UNITY_EDITOR
            Profiler.maxNumberOfSamplesPerFrame = 8000000;
#endif
            GameOfLifeTools.InitProperties();
            gameOfLife = new GameOfLife(50, Material);
            GameOfLifeTools.Randomize(0.01f);
        }

        void Update()
        {
            if (ConstantUpdate)
            {
                if (gameOfLife.CubesUpdateInProgress)
                {
                    gameOfLife.ScheduleNext();
                }
                gameOfLife.ScheduleCubesUpdate();
            }
            gameOfLife.Update();
        }

        void OnApplicationQuit()
        {
            gameOfLife.Quit();
            gameOfLife = null;
        }
    }
}