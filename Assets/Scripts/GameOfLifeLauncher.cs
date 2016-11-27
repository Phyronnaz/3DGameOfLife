using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Threading;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public Material Material;


        GameOfLife gameOfLife;

        void Start()
        {
            gameOfLife = new GameOfLife(50, 50, 50, Material);
            gameOfLife.Randomize(0.01f);
            gameOfLife.UpdateCubes();
        }

        void Update()
        {
            gameOfLife.Update();
        }

        void OnApplicationQuit()
        {
            gameOfLife.Quit();
            gameOfLife = null;
        }
    }
}