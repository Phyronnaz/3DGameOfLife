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

        public static bool EditMode;
        public static bool Use3D = true;

        public bool Busy;
        public bool ConstantUpdate;

        //Stay alive
        static int m_W = 2;
        static int m_X = 3;
        //Became alive
        static int m_Y = 3;
        static int m_Z = 3;

        static int m_ThreadSize;
        static int m_ChunkSize;

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

        public static int W { get { m_W = PlayerPrefs.GetInt("W", 3); return m_W; } set { PlayerPrefs.SetInt("W", value); m_W = value; } }
        public static int X { get { m_X = PlayerPrefs.GetInt("X", 3); return m_X; } set { PlayerPrefs.SetInt("X", value); m_X = value; } }
        public static int Y { get { m_Y = PlayerPrefs.GetInt("Y", 3); return m_Y; } set { PlayerPrefs.SetInt("Y", value); m_Y = value; } }
        public static int Z { get { m_Z = PlayerPrefs.GetInt("Z", 3); return m_Z; } set { PlayerPrefs.SetInt("Z", value); m_Z = value; } }

        public static int ThreadSize { get { m_ThreadSize = PlayerPrefs.GetInt("ThreadSize", 20); return m_ThreadSize; } set { PlayerPrefs.SetInt("ThreadSize", value); m_ThreadSize = value; } }
        public static int ChunkSize { get { m_ChunkSize = PlayerPrefs.GetInt("ChunkSize", 39); return m_ChunkSize; } set { var v = Mathf.Min(39, value); PlayerPrefs.SetInt("ChunkSize", v); m_ChunkSize = v; } }

        public bool[,,] World { get; private set; }
        private bool[,,] WorkingWorld { get; set; }
        private bool[,,] NeedUpdate { get; set; }

        public int XSize { get { return World.GetLength(0); } }
        public int YSize { get { return World.GetLength(1); } }
        public int ZSize { get { return World.GetLength(2); } }


        public GameOfLife(int XSize, int YSize, int ZSize, Material material)
        {
            World = new bool[XSize, YSize, ZSize];
            WorkingWorld = new bool[XSize, YSize, ZSize];
            NeedUpdate = new bool[XSize / ChunkSize + 1, YSize / ChunkSize + 1, ZSize / ChunkSize + 1];

            GOL = this;

            Material = material;

            InitRenderers();
        }


        public void SetSize(int size)
        {
            if (size != XSize && !Busy)
            {
                var new_world = new bool[size, size, size];
                if (size < XSize)
                {
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            for (int z = 0; z < size; z++)
                            {
                                new_world[x, y, z] = World[x, y, z];
                            }
                        }
                    }
                }
                else
                {
                    for (int x = 0; x < XSize; x++)
                    {
                        for (int y = 0; y < YSize; y++)
                        {
                            for (int z = 0; z < ZSize; z++)
                            {
                                new_world[x + (size - XSize) / 2, y + (size - YSize) / 2, z + (size - ZSize) / 2] = World[x, y, z];
                            }
                        }
                    }
                }

                World = new_world;
                WorkingWorld = new bool[size, size, size];
                NeedUpdate = new bool[XSize / m_ChunkSize + 1, YSize / m_ChunkSize + 1, ZSize / m_ChunkSize + 1];

                InitRenderers();
            }
        }

        public void SetWorld(bool[,,] world)
        {
            World = world;
            WorkingWorld = new bool[XSize, YSize, ZSize];
            NeedUpdate = new bool[XSize / m_ChunkSize + 1, YSize / m_ChunkSize + 1, ZSize / m_ChunkSize + 1];

            InitRenderers();
        }

        public void SetBlock(Vector3 v, bool value)
        {
            SetBlock(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z), value);
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            if (!Busy)
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
            if (!Busy)
            {
                gameOfLifeRenderers[x / m_ChunkSize, y / m_ChunkSize, z / m_ChunkSize].UpdateTriangles(World);
                gameOfLifeRenderers[x / m_ChunkSize, y / m_ChunkSize, z / m_ChunkSize].UpdateMeshes();
            }
        }

        public void MarkAllForUpdate()
        {
            for (int x = 0; x < XSize / m_ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / m_ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / m_ChunkSize + 1; z++)
                    {
                        NeedUpdate[x, y, z] = true;
                    }
                }
            }
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
            MarkAllForUpdate();
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
            MarkAllForUpdate();
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
            if (!Busy && ConstantUpdate)
            {
                Next();
                UpdateCubes();
            }
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
                    UnityEngine.Object.Destroy(g.gameObject);
                }
            }
            gameOfLifeRenderers = new GameOfLifeRenderer[XSize / m_ChunkSize + 1, YSize / m_ChunkSize + 1, ZSize / m_ChunkSize + 1];
            for (int x = 0; x < XSize / m_ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / m_ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / m_ChunkSize + 1; z++)
                    {
                        gameOfLifeRenderers[x, y, z] = new GameOfLifeRenderer(
                            m_ChunkSize * x, Mathf.Min(XSize, m_ChunkSize * (x + 1)),
                            m_ChunkSize * y, Mathf.Min(XSize, m_ChunkSize * (y + 1)),
                            m_ChunkSize * z, Mathf.Min(XSize, m_ChunkSize * (z + 1)),
                            Material);
                    }
                }
            }
        }

        private ManualResetEvent[,,] UpdateTriangles()
        {
            var waitHandles = new ManualResetEvent[gameOfLifeRenderers.GetLength(0), gameOfLifeRenderers.GetLength(1), gameOfLifeRenderers.GetLength(2)];

            for (int x = 0; x < XSize / m_ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / m_ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / m_ChunkSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        if (NeedUpdate[x, y, z])
                        {
                            waitHandles[i, j, k] = new ManualResetEvent(false);
                            ThreadPool.QueueUserWorkItem(state => gameOfLifeRenderers[i, j, k].UpdateTriangles(World, waitHandles[i, j, k]));
                        }
                        else
                        {
                            waitHandles[i, j, k] = new ManualResetEvent(true);
                        }
                    }
                }
            }
            return waitHandles;
        }

        private void UpdateMeshes()
        {
            for (int x = 0; x < XSize / m_ChunkSize + 1; x++)
            {
                for (int y = 0; y < YSize / m_ChunkSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / m_ChunkSize + 1; z++)
                    {
                        if (NeedUpdate[x, y, z])
                        {
                            gameOfLifeRenderers[x, y, z].UpdateMeshes();
                            NeedUpdate[x, y, z] = false;
                        }
                    }
                }
            }
        }

        private ManualResetEvent[,,] CalculateNextWorld()
        {
            var waitHandles = new ManualResetEvent[XSize / m_ThreadSize + 1, YSize / m_ThreadSize + 1, ZSize / m_ThreadSize + 1];
            var threadSize = m_ThreadSize;
            var chunkSize = m_ChunkSize;
            for (int x = 0; x < XSize / m_ThreadSize + 1; x++)
            {
                for (int y = 0; y < YSize / m_ThreadSize + 1; y++)
                {
                    for (int z = 0; z < ZSize / m_ThreadSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        waitHandles[i, j, k] = new ManualResetEvent(false);
                        if (Use3D)
                        {
                            ThreadPool.QueueUserWorkItem(state => Thread3D(World, WorkingWorld, NeedUpdate, chunkSize,
                                                                         threadSize * i, Mathf.Min(XSize, threadSize * (i + 1)),
                                                                         threadSize * j, Mathf.Min(YSize, threadSize * (j + 1)),
                                                                         threadSize * k, Mathf.Min(ZSize, threadSize * (k + 1)),
                                                                         waitHandles[i, j, k]));
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(state => Thread2D(World, WorkingWorld, NeedUpdate, chunkSize,
                                                                         threadSize * i, Mathf.Min(XSize, threadSize * (i + 1)),
                                                                         threadSize * j, Mathf.Min(YSize, threadSize * (j + 1)),
                                                                         threadSize * k, Mathf.Min(ZSize, threadSize * (k + 1)),
                                                                         waitHandles[i, j, k]));
                        }
                    }
                }
            }
            return waitHandles;
        }


        private static void Thread3D(bool[,,] world, bool[,,] workingWorld, bool[,,] needUpdate, int chunkSize,
                                     int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, ManualResetEvent waitHandle)
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

                        workingWorld[x, y, z] = (m_Y <= neighbors && neighbors <= m_Z) || (m_W <= neighbors && neighbors <= m_X && world[x, y, z]);

                        if (world[x, y, z] != workingWorld[x, y, z])
                        {
                            needUpdate[x / chunkSize, y / chunkSize, z / chunkSize] = true;
                        }
                    }
                }
            }
            waitHandle.Set();
        }

        private static void Thread2D(bool[,,] world, bool[,,] workingWorld, bool[,,] needUpdate, int chunkSize,
                                     int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, ManualResetEvent waitHandle)
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
                        if (y == ySize - 1)
                        {
                            int neighbors = 0;
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
                            if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z - 1 && z - 1 < zSize && world[x, y, z - 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x && x < xSize && 0 <= y && y < ySize && 0 <= z + 1 && z + 1 < zSize && world[x, y, z + 1])
                            {
                                neighbors++;
                            }

                            workingWorld[x, y, z] = (m_Y <= neighbors && neighbors <= m_Z) || (m_W <= neighbors && neighbors <= m_X && world[x, y, z]);
                        }
                        else
                        {
                            workingWorld[x, y, z] = world[x, y + 1, z];
                        }
                        if (world[x, y, z] != workingWorld[x, y, z])
                        {
                            needUpdate[x / chunkSize, y / chunkSize, z / chunkSize] = true;
                        }
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
