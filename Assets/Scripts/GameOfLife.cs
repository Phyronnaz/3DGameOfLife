using UnityEngine;
using System;
using System.Threading;
using System.Diagnostics;
using System.Collections;
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
        public static int ThreadSize = 25;
        //Size of a chunk (<40)
        public static int ChunkSize = 25;

        public bool Busy;

        readonly Material Material;
        GameOfLifeRenderer[] gameOfLifeRenderers;
        ManualResetEvent[] nextWaitHandles;
        ManualResetEvent[] cubesWaitHandles;
        bool cubesUpdateWaiting;
        bool cubesUpdateInProgress;
        bool nextInProgress;
        Stopwatch trianglesStopwatch;
        Stopwatch computationStopwatch;


        public bool[,,] World { get; private set; }
        private bool[,,] WorkingWorld { get; set; }

        public int XSize { get { return World.GetLength(0); } }
        public int YSize { get { return World.GetLength(1); } }
        public int ZSize { get { return World.GetLength(2); } }


        public GameOfLife(int XSize, int YSize, int ZSize, uint cacheSize, Material material)
        {
            World = new bool[XSize, YSize, ZSize];
            WorkingWorld = new bool[XSize, YSize, ZSize];

            Material = material;

            InitRenderers();
        }



        public void SetWorld(bool[,,] world)
        {
            World = world;

            InitRenderers();
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            World[x, y, z] = value;
        }

        public void Update()
        {
            if (nextInProgress)
            {
                bool ended = true;
                foreach (var w in nextWaitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    nextInProgress = false;
                    var tmp = World;
                    World = WorkingWorld;
                    WorkingWorld = tmp;
                    Log.ComputationTime(computationStopwatch.ElapsedMilliseconds);
                    computationStopwatch.Stop();
                }
            }
            if (cubesUpdateWaiting && !nextInProgress)
            {
                cubesUpdateWaiting = false;
                cubesUpdateInProgress = true;
                cubesWaitHandles = UpdateTriangles();
                trianglesStopwatch = new Stopwatch();
                trianglesStopwatch.Start();
            }
            if (cubesUpdateInProgress)
            {
                bool ended = true;
                foreach (var w in cubesWaitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    cubesUpdateInProgress = false;
                    Log.TrianglesTime(trianglesStopwatch.ElapsedMilliseconds);
                    trianglesStopwatch.Stop();
                    UpdateMeshes();
                }
            }

            Busy = nextInProgress || cubesUpdateInProgress;
        }

        public void Next()
        {
            if (Busy)
            {
                Log.Warning("Not able to calculate next: Busy");
            }
            else
            {
                nextWaitHandles = CalculateNextWorld();
                nextInProgress = true;
                computationStopwatch = new Stopwatch();
                computationStopwatch.Start();
            }
        }

        public void UpdateCubes()
        {
            if (Busy)
            {
                Log.Warning("Not able to update cubes: Busy");
            }
            else
            {
                cubesUpdateWaiting = true;
            }
        }



        private void InitRenderers()
        {
            if (gameOfLifeRenderers != null)
            {
                foreach (var g in gameOfLifeRenderers)
                {
                    GameObject.Destroy(g.gameObject);
                }
            }
            gameOfLifeRenderers = new GameOfLifeRenderer[(XSize / ChunkSize + 1) * (YSize / ChunkSize + 1) * (ZSize / ChunkSize + 1)];
            for (int x = 0; x < XSize / ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / ChunkSize + 1; z++)
                    {
                        gameOfLifeRenderers[x + (XSize / ChunkSize + 1) * (y + (YSize / ChunkSize + 1) * z)] = new GameOfLifeRenderer(
                            ChunkSize * x, Mathf.Min(XSize, ChunkSize * (x + 1)),
                            ChunkSize * y, Mathf.Min(XSize, ChunkSize * (y + 1)),
                            ChunkSize * z, Mathf.Min(XSize, ChunkSize * (z + 1)),
                            Material);
                    }
                }
            }
        }

        private ManualResetEvent[] UpdateTriangles()
        {
            var waitHandles = new ManualResetEvent[gameOfLifeRenderers.Length];

            for (int i = 0; i < gameOfLifeRenderers.Length; i++)
            {
                var x = i;
                waitHandles[x] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(state => gameOfLifeRenderers[x].UpdateTriangles(waitHandles[x], World));
            }
            return waitHandles;
        }

        private void UpdateMeshes()
        {
            for (int i = 0; i < gameOfLifeRenderers.Length; i++)
            {
                gameOfLifeRenderers[i].UpdateMeshes();
            }
        }

        private ManualResetEvent[] CalculateNextWorld()
        {
            var waitHandles = new ManualResetEvent[(XSize / ThreadSize + 1) * (YSize / ThreadSize + 1) * (ZSize / ThreadSize + 1)];

            for (int i = 0; i < XSize / ThreadSize + 1; i++)
            {
                for (int j = 0; j < YSize / ThreadSize + 1; j++)
                {
                    for (int k = 0; k < ZSize / ThreadSize + 1; k++)
                    {
                        var x = i;
                        var y = j;
                        var z = k;
                        waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)] = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(state => Thread(World, WorkingWorld,
                                                                     ThreadSize * x, Mathf.Min(XSize, ThreadSize * (x + 1)),
                                                                     ThreadSize * y, Mathf.Min(YSize, ThreadSize * (y + 1)),
                                                                     ThreadSize * z, Mathf.Min(ZSize, ThreadSize * (z + 1)),
                                                                     waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)]));
                    }
                }
            }
            return waitHandles;
        }


        private static void Thread(bool[,,] world, bool[,,] workingWorld, int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, ManualResetEvent waitHandle)
        {
            int xSize = world.GetLength(0);
            int ySize = world.GetLength(1);
            int zSize = world.GetLength(2);
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int neighbors = 0;
                        #region ifs
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x - 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && world[x - 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x - 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && world[x - 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z && z < zSize && world[x - 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && world[x - 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x - 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && world[x - 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x - 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && world[x, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && world[x, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && world[x, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && world[x, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x + 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && world[x + 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x + 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && world[x + 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z && z < zSize && world[x + 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && world[x + 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && world[x + 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && world[x + 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && world[x + 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        #endregion

                        workingWorld[x, y, z] = (Y < neighbors && neighbors < Z) || (W < neighbors && neighbors < X && world[x, y, z]);
                    }
                }
            }
            waitHandle.Set();
        }

        private static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
