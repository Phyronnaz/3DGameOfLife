using UnityEngine;
using System.Collections;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public int Size = 10;

        public Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;

        GameOfLife gameOfLife;

        void Start()
        {
            Profiler.maxNumberOfSamplesPerFrame = 100000;
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
                gameOfLife.Next();
                gameOfLife.UpdateCubes();
            }
        }
    }
}