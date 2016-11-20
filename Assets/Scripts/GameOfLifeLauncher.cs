using UnityEngine;
using System.Collections;
using System.Diagnostics;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public int Size = 10;

        public Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;

        GameOfLife gameOfLife;

        void Start()
        {
            gameOfLife = new GameOfLife(Size, Size, Size, RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        gameOfLife.SetBlock(x, y, z, Random.value > 0.9);
                    }
                }
            }
            gameOfLife.ApplyBlocksChanges();
            gameOfLife.UpdateCubes();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                print("//////////////////////////////////////////");
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                gameOfLife.Next();
                print("Calcul time: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                stopwatch.Reset();
                stopwatch.Start();
                gameOfLife.UpdateCubes();
                print("Render time: " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
                stopwatch.Stop();
                print("//////////////////////////////////////////");
            }
        }
    }
}