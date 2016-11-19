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
            var world = new bool[Size, Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        world[x, y, z] = Random.value > 0.6;
                    }
                }
            }
            gameOfLife = new GameOfLife(Size, Size, Size, RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
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
