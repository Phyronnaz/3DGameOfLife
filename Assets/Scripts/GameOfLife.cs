using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class GameOfLife
    {
        //Stay alive
        public static int W = 1;
        public static int X = 4;
        //Became alive
        public static int Y = 2;
        public static int Z = 3;
        //Size of a thread
        public static int ChunkSize = 30;

        Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;
        bool[][,,] worlds = new bool[3][,,];
        GameOfLifeRenderer[,,] gameOfLifeRenderers;
        int currentWorldIndex;

        public int XSize { get { return worlds[0].GetLength(0); } }
        public int YSize { get { return worlds[0].GetLength(1); } }
        public int ZSize { get { return worlds[0].GetLength(2); } }


        public GameOfLife(int XSize, int YSize, int ZSize,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
        {
            worlds[0] = new bool[XSize, YSize, ZSize];
            worlds[1] = new bool[XSize, YSize, ZSize];
            worlds[2] = new bool[XSize, YSize, ZSize];
            RedMaterial = redMaterial;
            WhiteMaterial = whiteMaterial;
            GreenMaterial = greenMaterial;
            YellowMaterial = yellowMaterial;
        }


        public void SetWorld(bool[,,] world)
        {
            var size = world.GetLength(0);
            worlds[0] = new bool[size, size, size];
            worlds[1] = world;
            worlds[2] = new bool[size, size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        worlds[0][x, y, z] = false;
                    }
                }
            }

            CalculateNextWorld(worlds[1], worlds[2]);
            currentWorldIndex = 1;

            foreach (var g in gameOfLifeRenderers)
            {
                Object.Destroy(g.gameObject);
            }
        }

        public bool[,,] GetWorld(int index)
        {
            return worlds[(worlds.Length + currentWorldIndex + index) % worlds.Length];
        }

        public void Next()
        {
            CalculateNextWorld(GetWorld(0), GetWorld(1));
            currentWorldIndex++;
        }

        public void UpdateCubes()
        {
            foreach (var g in gameOfLifeRenderers)
            {
                g.UpdateCubes();
            }
        }

        public void SimpleUpdate()
        {
            foreach (var g in gameOfLifeRenderers)
            {
                g.UpdateWhiteCubes();
            }
        }


        private void InitRenderers()
        {
            gameOfLifeRenderers = new GameOfLifeRenderer[XSize / 38 + 1, YSize / 38 + 1, ZSize / 38 + 1];
            for (int i = 0; i < XSize / 38; i++)
            {
                for (int j = 0; j < YSize / 38; j++)
                {
                    for (int k = 0; k < ZSize / 38; k++)
                    {
                        gameOfLifeRenderers[i, j, k] = new GameOfLifeRenderer(this,
                            38 * i, 38 * (i + 1), 38 * j, 38 * (j + 1), 38 * k, 38 * (k + 1),
                            RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
                    }
                }
            }
            gameOfLifeRenderers[XSize / 38, YSize / 38, ZSize / 38] = new GameOfLifeRenderer(this,
                            XSize / 38, XSize, YSize / 38, YSize, ZSize / 38, ZSize,
                            RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
        }

        private void CalculateNextWorld(bool[,,] currentWorld, bool[,,] nextWorld)
        {
            for (int i = 0; i < XSize / ChunkSize; i++)
            {
                for (int j = 0; j < YSize / ChunkSize; j++)
                {
                    for (int k = 0; k < ZSize / ChunkSize; k++)
                    {
                        Thread(currentWorld, nextWorld, ChunkSize * i, ChunkSize * (i + 1), ChunkSize * j, ChunkSize * (j + 1), ChunkSize * k, ChunkSize * (k + 1));
                    }
                }
            }
            Thread(currentWorld, nextWorld, XSize / ChunkSize, XSize, YSize / ChunkSize, YSize, ZSize / ChunkSize, ZSize);
        }

        private static void Thread(bool[,,] currentWorld, bool[,,] nextWorld,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd)
        {
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int neighbors = currentWorld[x, y, z] ? -1 : 0;
                        for (int i = -1; i < 2; i++)
                        {
                            for (int j = -1; j < 2; j++)
                            {
                                for (int k = -1; k < 2; k++)
                                {
                                    if (0 <= x + i && x + i < xEnd &&
                                        0 <= y + j && y + j < yEnd &&
                                        0 <= z + k && z + k < zEnd)
                                    {
                                        if (currentWorld[x + i, y + j, z + k])
                                        {
                                            neighbors++;
                                        }
                                    }
                                }
                            }
                        }
                        nextWorld[x, y, z] = (Y < neighbors && neighbors < Z) || (W < neighbors && neighbors < X && currentWorld[x, y, z]);
                    }
                }
            }
        }
    }
}
