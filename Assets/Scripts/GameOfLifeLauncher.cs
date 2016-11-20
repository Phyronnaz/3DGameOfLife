using UnityEngine;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public int Size = 10;

        public Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;

        public float RefreshRate = 0.5f;

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
                        gameOfLife.SetBlock(x, y, z, Random.value > 0.7);
                    }
                }
            }
            gameOfLife.ApplyBlocksChanges();
            gameOfLife.UpdateCubes();
            InvokeRepeating("GameOfLifeUpdate", 0, RefreshRate);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                gameOfLife.Next();
                gameOfLife.UpdateCubes();
            }
        }

        void GameOfLifeUpdate()
        {
            gameOfLife.Update();
        }
    }
}