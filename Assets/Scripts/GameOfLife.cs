using UnityEngine;
using System.Threading;
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
        public static int ThreadSize = 30;
        //Size of a chunk (<40)
        public static int ChunkSize = 39;

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

            InitRenderers();
        }


        public void SetWorld(bool[,,] world)
        {
            var size = world.GetLength(0);
            worlds[0] = new bool[size, size, size];
            worlds[1] = world;
            worlds[2] = new bool[size, size, size];

            currentWorldIndex = 1;
            Next();

            InitRenderers();
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            GetWorld(-1)[x, y, z] = value;
        }

        public void ApplyBlocksChanges()
        {
            CalculateNextWorld(GetWorld(-1), GetWorld(0));
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
            if (gameOfLifeRenderers != null)
            {
                foreach (var g in gameOfLifeRenderers)
                {
                    Object.Destroy(g.gameObject);
                }
            }
            gameOfLifeRenderers = new GameOfLifeRenderer[XSize / ChunkSize + 1, YSize / ChunkSize + 1, ZSize / ChunkSize + 1];
            for (int i = 0; i < XSize / ChunkSize + 1; i++)
            {
                for (int j = 0; j < YSize / ChunkSize + 1; j++)
                {
                    for (int k = 0; k < ZSize / ChunkSize + 1; k++)
                    {
                        gameOfLifeRenderers[i, j, k] = new GameOfLifeRenderer(this,
                            ChunkSize * i, Mathf.Min(XSize, ChunkSize * (i + 1)),
                            ChunkSize * j, Mathf.Min(XSize, ChunkSize * (j + 1)),
                            ChunkSize * k, Mathf.Min(XSize, ChunkSize * (k + 1)),
                            RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
                    }
                }
            }
        }

        private void CalculateNextWorld(bool[,,] currentWorld, bool[,,] nextWorld)
        {
            var waitHandles = new AutoResetEvent[(XSize / ThreadSize + 1) * (YSize / ThreadSize + 1) * (ZSize / ThreadSize + 1)];

            for (int i = 0; i < XSize / ThreadSize + 1; i++)
            {
                for (int j = 0; j < YSize / ThreadSize + 1; j++)
                {
                    for (int k = 0; k < ZSize / ThreadSize + 1; k++)
                    {
                        var x = i;
                        var y = j;
                        var z = k;
                        waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)] = new AutoResetEvent(false);
                        ThreadPool.QueueUserWorkItem(state => Thread(currentWorld, nextWorld,
                                                                    ThreadSize * x, Mathf.Min(XSize, ThreadSize * (x + 1)),
                                                                    ThreadSize * y, Mathf.Min(YSize, ThreadSize * (y + 1)),
                                                                    ThreadSize * z, Mathf.Min(ZSize, ThreadSize * (z + 1)),
                                                                    waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)]));
                    }
                }
            }
            foreach (var w in waitHandles)
            {
                w.WaitOne();
            }
        }

        private static void Thread(bool[,,] currentWorld, bool[,,] nextWorld,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            AutoResetEvent waitHandle)
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
            waitHandle.Set();
        }
    }
}
