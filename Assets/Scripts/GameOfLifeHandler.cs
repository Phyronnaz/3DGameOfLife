using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;

namespace Assets.Scripts
{
    public class GameOfLifeHandler : MonoBehaviour
    {
        public static bool ConstantUpdate;
        public static float UpdateTime = 1;
        public static Image WorkingImage;

        public Material Material;

        GameOfLife gameOfLife;
        float lastRefreshTime;

        void Awake()
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
            if (ConstantUpdate && Time.time - lastRefreshTime > UpdateTime)
            {
                lastRefreshTime = Time.time;
                gameOfLife.ScheduleNext();
                gameOfLife.ScheduleCubesUpdate();
            }
            gameOfLife.Update();

            WorkingImage.color = gameOfLife.Working ? Color.red : Color.green;
        }

        void OnApplicationQuit()
        {
            gameOfLife.Quit();
            gameOfLife = null;
        }
    }
}