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
        public static int ThreadSize = 30;
        //Size of a chunk (<40)
        public static int ChunkSize = 39;

        public bool Busy;

        readonly Material RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial;
        bool[][,,] worlds;
        GameOfLifeRenderer[] gameOfLifeRenderers;
        int m_currentWorldIndex;
        int m_realWorldIndex;
        ManualResetEvent[] nextWaitHandles;
        ManualResetEvent[] cubesWaitHandles;
        ManualResetEvent[] cacheWaitHandles;
        bool cubesUpdateWaiting;
        bool cubesUpdateInProgress;
        bool nextInProgress;
        bool cacheInProgress;
        Stopwatch trianglesStopwatch;
        Stopwatch computationStopwatch;
        uint cache;

        public uint Cache { get { return cache; } set { cache = (uint)Mathf.Min(value, worlds.Length - 3); } }

        public int XSize { get { return worlds[0].GetLength(0); } }
        public int YSize { get { return worlds[0].GetLength(1); } }
        public int ZSize { get { return worlds[0].GetLength(2); } }

        int currentWorldIndex { get { return m_currentWorldIndex; } set { m_currentWorldIndex = mod(value, worlds.Length); Log.Cache(GetCacheSize()); } }
        int realWorldIndex { get { return m_realWorldIndex; } set { m_realWorldIndex = mod(value, worlds.Length); Log.Cache(GetCacheSize()); } }


        public GameOfLife(int XSize, int YSize, int ZSize, uint cacheSize,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
        {
            worlds = new bool[3 + cacheSize][,,];
            Cache = cacheSize;
            for (int i = 0; i < worlds.Length; i++)
            {
                worlds[i] = new bool[XSize, YSize, ZSize];
            }

            m_currentWorldIndex = 2;
            m_realWorldIndex = 2;

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

            currentWorldIndex = 2;
            realWorldIndex = 2;

            InitRenderers();
        }

        public bool[,,] GetCurrentWorld(int shift)
        {
            return worlds[mod(currentWorldIndex + shift, worlds.Length)];
        }

        public bool[,,] GetRealWorld(int shift)
        {
            return worlds[mod(realWorldIndex + shift, worlds.Length)];
        }

        public bool AlreadyComputed(int index)
        {
            index = mod(index, worlds.Length);
            if (currentWorldIndex <= realWorldIndex)
            {
                return currentWorldIndex <= index && index <= realWorldIndex;
            }
            else
            {
                return (index <= realWorldIndex) || (currentWorldIndex <= index);
            }
        }

        public int GetCacheSize()
        {
            if (currentWorldIndex <= realWorldIndex)
            {
                return realWorldIndex - currentWorldIndex;
            }
            else
            {
                return realWorldIndex + worlds.Length - currentWorldIndex;
            }
        }

        public void BeginSettingBlocks()
        {
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        GetCurrentWorld(-1)[x, y, z] = GetCurrentWorld(-2)[x, y, z];
                    }
                }
            }
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            GetCurrentWorld(-1)[x, y, z] = value;
        }

        public void ApplyBlocksChanges()
        {
            var waitHandles = CalculateNextWorld(GetCurrentWorld(-1), GetCurrentWorld(0));
            foreach (var w in waitHandles)
            {
                w.WaitOne();
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
                    currentWorldIndex++;
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

            if (cacheInProgress)
            {
                bool ended = true;
                foreach (var w in cacheWaitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    cacheInProgress = false;
                    realWorldIndex++;
                }
            }

            if (GetCacheSize() < Cache && !nextInProgress && !cacheInProgress && !cubesUpdateInProgress)
            {
                cacheWaitHandles = CalculateNextWorld(GetRealWorld(0), GetRealWorld(1));
                cacheInProgress = true;
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
                if (!AlreadyComputed(currentWorldIndex + 1))
                {
                    nextWaitHandles = CalculateNextWorld(GetCurrentWorld(0), GetCurrentWorld(1));
                    nextInProgress = true;
                    computationStopwatch = new Stopwatch();
                    computationStopwatch.Start();
                }
                else
                {
                    currentWorldIndex++;
                }
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
                            RedMaterial, WhiteMaterial, GreenMaterial, YellowMaterial);
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
                ThreadPool.QueueUserWorkItem(state => gameOfLifeRenderers[x].UpdateTriangles(waitHandles[x], GetCurrentWorld(0), GetCurrentWorld(-1), GetCurrentWorld(-2)));
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

        private ManualResetEvent[] CalculateNextWorld(bool[,,] currentWorld, bool[,,] nextWorld)
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
                        ThreadPool.QueueUserWorkItem(state => Thread(currentWorld, nextWorld,
                                                                    ThreadSize * x, Mathf.Min(XSize, ThreadSize * (x + 1)),
                                                                    ThreadSize * y, Mathf.Min(YSize, ThreadSize * (y + 1)),
                                                                    ThreadSize * z, Mathf.Min(ZSize, ThreadSize * (z + 1)),
                                                                    waitHandles[x + (XSize / ThreadSize + 1) * (y + (YSize / ThreadSize + 1) * z)]));
                    }
                }
            }
            return waitHandles;
        }


        private static void Thread(bool[,,] currentWorld, bool[,,] nextWorld,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            ManualResetEvent waitHandle)
        {
            var xSize = currentWorld.GetLength(0);
            var ySize = currentWorld.GetLength(1);
            var zSize = currentWorld.GetLength(2);
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int neighbors = 0;
                        #region ifs
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x - 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && currentWorld[x - 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x - 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x - 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z && z < zSize && currentWorld[x - 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x - 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x - 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && currentWorld[x - 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x - 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && currentWorld[x, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && currentWorld[x, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x + 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z && z < zSize && currentWorld[x + 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y - 1 && y - 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x + 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x + 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z && z < zSize && currentWorld[x + 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x + 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z - 1 && z - 1 < zSize && currentWorld[x + 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z && z < zSize && currentWorld[x + 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < xSize && 0 <= y + 1 && y + 1 < ySize && 0 <= z + 1 && z + 1 < zSize && currentWorld[x + 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        #endregion

                        nextWorld[x, y, z] = (Y < neighbors && neighbors < Z) || (W < neighbors && neighbors < X && currentWorld[x, y, z]);
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
