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
        GameOfLifeRenderer[,,] gameOfLifeRenderers;
        ManualResetEvent[,,] nextWaitHandles;
        ManualResetEvent[,,] cubesWaitHandles;
        bool cubesUpdateWaiting;
        bool cubesUpdateInProgress;
        bool nextInProgress;
        Stopwatch trianglesStopwatch;
        Stopwatch computationStopwatch;


        public static GameOfLife GOL { get; private set; }

        public bool[,,] World { get; private set; }
        private bool[,,] WorkingWorld { get; set; }

        public int XSize { get { return World.GetLength(0); } }
        public int YSize { get { return World.GetLength(1); } }
        public int ZSize { get { return World.GetLength(2); } }


        public GameOfLife(int XSize, int YSize, int ZSize, Material material)
        {
            World = new bool[XSize, YSize, ZSize];
            WorkingWorld = new bool[XSize, YSize, ZSize];

            GOL = this;

            Material = material;

            InitRenderers();
        }



        public void SetWorld(bool[,,] world)
        {
            World = world;

            InitRenderers();
        }

        public void SetBlock(Vector3 v, bool value)
        {
            SetBlock(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z), value);
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            try
            {
                World[x, y, z] = value;
            }
            catch (IndexOutOfRangeException)
            {
                Log.LogError(string.Format("Out of bound: ({0}, {1}, {2})", x, y, z));
            }
        }


        public bool GetBlock(Vector3 v)
        {
            return GetBlock(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }

        public bool GetBlock(int x, int y, int z)
        {
            try
            {
                return World[x, y, z];
            }
            catch (IndexOutOfRangeException)
            {
                Log.LogError(string.Format("Out of bound: (%i, %i, %i)", x, y, z));
                return false;
            }
        }

        public bool IsInWorld(Vector3 v)
        {
            return IsInWorld(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }

        public bool IsInWorld(int x, int y, int z)
        {
            return 0 <= x && x < XSize &&
                   0 <= y && y < YSize &&
                   0 <= z && z < ZSize;
        }

        public void UpdateChunk(Vector3 v)
        {
            UpdateChunk(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }

        public void UpdateChunk(int x, int y, int z)
        {
            gameOfLifeRenderers[x / ChunkSize, y / ChunkSize, z / ChunkSize].UpdateTriangles(World);
            gameOfLifeRenderers[x / ChunkSize, y / ChunkSize, z / ChunkSize].UpdateMeshes();
        }

        public void Randomize(float density)
        {
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        SetBlock(x, y, z, UnityEngine.Random.value < density);
                    }
                }
            }
        }

        public void Reset()
        {
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        World[x, y, z] = false;
                    }
                }
            }
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
                    Log.LogComputationTime(computationStopwatch.ElapsedMilliseconds);
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
                    Log.LogTrianglesTime(trianglesStopwatch.ElapsedMilliseconds);
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
                Log.LogWarning("Not able to calculate next: Busy");
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
                Log.LogWarning("Not able to update cubes: Busy");
            }
            else
            {
                cubesUpdateWaiting = true;
            }
        }

        public void Quit()
        {
            foreach (var renderer in gameOfLifeRenderers)
            {
                renderer.Quit();
            }
            World = null;
            WorkingWorld = null;
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
            gameOfLifeRenderers = new GameOfLifeRenderer[XSize / ChunkSize + 1, YSize / ChunkSize + 1, ZSize / ChunkSize + 1];
            for (int x = 0; x < XSize / ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / ChunkSize + 1; z++)
                    {
                        gameOfLifeRenderers[x, y, z] = new GameOfLifeRenderer(
                            ChunkSize * x, Mathf.Min(XSize, ChunkSize * (x + 1)),
                            ChunkSize * y, Mathf.Min(XSize, ChunkSize * (y + 1)),
                            ChunkSize * z, Mathf.Min(XSize, ChunkSize * (z + 1)),
                            Material);
                    }
                }
            }
        }

        private ManualResetEvent[,,] UpdateTriangles()
        {
            var waitHandles = new ManualResetEvent[gameOfLifeRenderers.GetLength(0), gameOfLifeRenderers.GetLength(1), gameOfLifeRenderers.GetLength(2)];

            for (int x = 0; x < XSize / ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / ChunkSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        waitHandles[i, j, k] = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(state => gameOfLifeRenderers[i, j, k].UpdateTriangles(World, waitHandles[i, j, k]));
                    }
                }
            }
            return waitHandles;
        }

        private void UpdateMeshes()
        {
            foreach (var renderer in gameOfLifeRenderers)
            {
                renderer.UpdateMeshes();
            }
        }

        private ManualResetEvent[,,] CalculateNextWorld()
        {
            var waitHandles = new ManualResetEvent[XSize / ThreadSize + 1, YSize / ThreadSize + 1, ZSize / ThreadSize + 1];

            for (int x = 0; x < XSize / ThreadSize + 1; x++)
            {
                for (int y = 0; y < YSize / ThreadSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / ThreadSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        waitHandles[i, j, k] = new ManualResetEvent(false);
                        ThreadPool.QueueUserWorkItem(state => Thread(World, WorkingWorld,
                                                                     ThreadSize * i, Mathf.Min(XSize, ThreadSize * (i + 1)),
                                                                     ThreadSize * j, Mathf.Min(YSize, ThreadSize * (j + 1)),
                                                                     ThreadSize * k, Mathf.Min(ZSize, ThreadSize * (k + 1)),
                                                                     waitHandles[i, j, k]));
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
