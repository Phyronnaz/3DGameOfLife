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
        public static int W = 2;//Stay alive
        public static int X = 3;
        public static int Y = 3;//Become alive
        public static int Z = 3;
        public static int ThreadSize;
        public static int ChunkSize;

        private readonly Material Material;
        private readonly Stopwatch stopwatch;
        private readonly ManualResetEvent threadsCreated;
        private bool[,,] WorkingWorld;
        private bool[,,] DirtyChunks;
        private GameOfLifeRenderer[,,] gameOfLifeRenderers;
        private ManualResetEvent[,,] waitHandles;
        private ulong cubeUpdatePendingIndex;
        private ulong nextPendingIndex;

        public static GameOfLife GOL { get; private set; }

        public bool[,,] World { get; private set; }
        public bool CubesUpdatePending { get; private set; }
        public bool CubesUpdateInProgress { get; private set; }
        public bool NextPending { get; private set; }
        public bool NextInProgress { get; private set; }
        public bool Working { get { return NextInProgress || CubesUpdateInProgress; } }
        public int Size { get { return World.GetLength(0); } }



        public GameOfLife(int size, Material material)
        {
            World = new bool[size, size, size];
            WorkingWorld = new bool[size, size, size];

            threadsCreated = new ManualResetEvent(false);
            stopwatch = new Stopwatch();

            GOL = this;

            Material = material;

            InitRenderers();
        }



        public void SetWorld(bool[,,] world)
        {
            WaitForThreads();
            World = world;
            WorkingWorld = new bool[Size, Size, Size];

            InitRenderers();
        }

        public void SetBlock(int x, int y, int z, bool value)
        {
            if (Working)
            {
                Log.LogWarning("Not able to set block: Working");
            }
            else if (!IsInWorld(x, y, z))
            {
                Log.LogError(string.Format("Out of bound: ({0}, {1}, {2})", x, y, z));
            }
            else
            {
                World[x, y, z] = value;
                DirtyChunks[x / ChunkSize, y / ChunkSize, z / ChunkSize] = true;
            }
        }

        public bool IsInWorld(int x, int y, int z)
        {
            return 0 <= x && x < Size &&
                   0 <= y && y < Size &&
                   0 <= z && z < Size;
        }

        public void UpdateChunk(int x, int y, int z)
        {
            if (CubesUpdateInProgress)
            {
                Log.LogWarning("Not able to update chunk: Cubes update in progress");
            }
            else
            {
                gameOfLifeRenderers[x / ChunkSize, y / ChunkSize, z / ChunkSize].UpdateTriangles(World);
                gameOfLifeRenderers[x / ChunkSize, y / ChunkSize, z / ChunkSize].UpdateMesh();
                gameOfLifeRenderers[x / ChunkSize, y / ChunkSize, z / ChunkSize].UpdateCollisions();
            }
        }

        public void MarkAllForUpdate()
        {
            for (int x = 0; x < DirtyChunks.GetLength(0); x++)
            {
                for (int y = 0; y < DirtyChunks.GetLength(1); y++)
                {
                    for (int z = 0; z < DirtyChunks.GetLength(2); z++)
                    {
                        DirtyChunks[x, y, z] = true;
                    }
                }
            }
        }

        public void ScheduleNext()
        {
            if (!NextPending)
            {
                NextPending = true;
                nextPendingIndex = cubeUpdatePendingIndex + 1;
            }
        }

        public void ScheduleCubesUpdate()
        {
            if (!CubesUpdatePending)
            {
                CubesUpdatePending = true;
                cubeUpdatePendingIndex = nextPendingIndex + 1;
            }
        }

        public void Update()
        {
            if (threadsCreated.WaitOne(0) && Working)
            {
                bool ended = true;
                foreach (var w in waitHandles)
                {
                    ended = ended && w.WaitOne(0);
                }
                if (ended)
                {
                    if (NextInProgress)
                    {
                        EndNext();
                    }
                    if (CubesUpdateInProgress)
                    {
                        EndCubesUpdate();
                    }
                }
            }
            if (!Working)
            {
                if (NextPending && (!CubesUpdatePending || nextPendingIndex < cubeUpdatePendingIndex))
                {
                    StartNext();
                }
                else if (CubesUpdatePending && (!NextPending || cubeUpdatePendingIndex < nextPendingIndex))
                {
                    StartCubesUpdate();
                }
            }
        }

        public void Quit()
        {
            foreach (var renderer in gameOfLifeRenderers)
            {
                renderer.Quit();
            }
            GOL = null;
            World = null;
            WorkingWorld = null;
        }



        private void StartNext()
        {
            NextPending = false;
            NextInProgress = true;
            stopwatch.Reset();
            stopwatch.Start();
            threadsCreated.Reset();
            ThreadPool.QueueUserWorkItem(state => CalculateNextWorld());
        }

        private void StartCubesUpdate()
        {
            CubesUpdatePending = false;
            CubesUpdateInProgress = true;
            stopwatch.Reset();
            stopwatch.Start();
            threadsCreated.Reset();
            ThreadPool.QueueUserWorkItem(state => UpdateTriangles());
        }

        private void EndNext()
        {
            NextInProgress = false;
            var tmp = World;
            World = WorkingWorld;
            WorkingWorld = tmp;
            Log.LogComputationTime(stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
        }

        private void EndCubesUpdate()
        {
            CubesUpdateInProgress = false;
            UpdateMeshes();
            Log.LogTrianglesTime(stopwatch.ElapsedMilliseconds);
            stopwatch.Stop();
        }

        private void ClearWaitHandles()
        {
            if (waitHandles != null)
            {
                foreach (var w in waitHandles)
                {
                    w.Close();
                }
            }
        }

        private void InitRenderers()
        {
            if (gameOfLifeRenderers != null)
            {
                foreach (var renderer in gameOfLifeRenderers)
                {
                    renderer.Quit();
                }
            }
            DirtyChunks = new bool[Size / ChunkSize + 1, Size / ChunkSize + 1, Size / ChunkSize + 1];
            gameOfLifeRenderers = new GameOfLifeRenderer[Size / ChunkSize + 1, Size / ChunkSize + 1, Size / ChunkSize + 1];
            for (int x = 0; x < Size / ChunkSize + 1; x++)
            {
                for (int y = 0; y < Size / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < Size / ChunkSize + 1; z++)
                    {
                        gameOfLifeRenderers[x, y, z] = new GameOfLifeRenderer(
                            ChunkSize * x, Mathf.Min(Size, ChunkSize * (x + 1)),
                            ChunkSize * y, Mathf.Min(Size, ChunkSize * (y + 1)),
                            ChunkSize * z, Mathf.Min(Size, ChunkSize * (z + 1)),
                            Material);
                    }
                }
            }
        }

        private void UpdateTriangles()
        {
            ClearWaitHandles();
            waitHandles = new ManualResetEvent[gameOfLifeRenderers.GetLength(0), gameOfLifeRenderers.GetLength(1), gameOfLifeRenderers.GetLength(2)];

            for (int x = 0; x < Size / ChunkSize + 1; x++)
            {
                for (int y = 0; y < Size / ChunkSize + 1; y++)
                {
                    for (int z = 0; z < Size / ChunkSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        if (DirtyChunks[i, j, k])
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

            threadsCreated.Set();
        }

        public void UpdateMeshes()
        {
            for (int x = 0; x < DirtyChunks.GetLength(0); x++)
            {
                for (int y = 0; y < DirtyChunks.GetLength(1); y++)
                {
                    for (int z = 0; z < DirtyChunks.GetLength(2); z++)
                    {
                        if (DirtyChunks[x, y, z])
                        {
                            gameOfLifeRenderers[x, y, z].UpdateMesh();
                            if (EditMode)
                            {
                                gameOfLifeRenderers[x, y, z].UpdateCollisions();
                            }

                            DirtyChunks[x, y, z] = false;
                        }
                    }
                }
            }
        }

        public void UpdateCollisions()
        {
            foreach (var renderer in gameOfLifeRenderers)
            {
                renderer.UpdateCollisions();
            }
        }

        public void WaitForThreads()
        {
            foreach (var w in waitHandles)
            {
                w.WaitOne();
            }
        }

        private void CalculateNextWorld()
        {
            ClearWaitHandles();
            waitHandles = new ManualResetEvent[Size / ThreadSize + 1, Size / ThreadSize + 1, Size / ThreadSize + 1];
            for (int x = 0; x < Size / ThreadSize + 1; x++)
            {
                for (int y = 0; y < Size / ThreadSize + 1; y++)
                {
                    for (int z = 0; z < Size / ThreadSize + 1; z++)
                    {
                        var i = x;
                        var j = y;
                        var k = z;
                        waitHandles[i, j, k] = new ManualResetEvent(false);
                        if (Use3D)
                        {
                            ThreadPool.QueueUserWorkItem(state => Thread3D(World, WorkingWorld, DirtyChunks, ChunkSize,
                                                                         ThreadSize * i, Mathf.Min(Size, ThreadSize * (i + 1)),
                                                                         ThreadSize * j, Mathf.Min(Size, ThreadSize * (j + 1)),
                                                                         ThreadSize * k, Mathf.Min(Size, ThreadSize * (k + 1)),
                                                                         waitHandles[i, j, k]));
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(state => Thread2D(World, WorkingWorld, DirtyChunks, ChunkSize,
                                                                         ThreadSize * i, Mathf.Min(Size, ThreadSize * (i + 1)),
                                                                         ThreadSize * j, Mathf.Min(Size, ThreadSize * (j + 1)),
                                                                         ThreadSize * k, Mathf.Min(Size, ThreadSize * (k + 1)),
                                                                         waitHandles[i, j, k]));
                        }
                    }
                }
            }
            threadsCreated.Set();
        }



        private void Thread3D(bool[,,] world, bool[,,] workingWorld, bool[,,] needUpdate, int chunkSize,
                                     int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, ManualResetEvent waitHandle)
        {
            int size = world.GetLength(0);

            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int neighbors = 0;
                        #region ifs
                        if (0 <= x - 1 && x - 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z - 1 && z - 1 < size && world[x - 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z && z < size && world[x - 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z + 1 && z + 1 < size && world[x - 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x - 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z && z < size && world[x - 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x - 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z - 1 && z - 1 < size && world[x - 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z && z < size && world[x - 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x - 1 && x - 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z + 1 && z + 1 < size && world[x - 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y - 1 && y - 1 < size && 0 <= z - 1 && z - 1 < size && world[x, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y - 1 && y - 1 < size && 0 <= z && z < size && world[x, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y - 1 && y - 1 < size && 0 <= z + 1 && z + 1 < size && world[x, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y + 1 && y + 1 < size && 0 <= z - 1 && z - 1 < size && world[x, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y + 1 && y + 1 < size && 0 <= z && z < size && world[x, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x && x < size && 0 <= y + 1 && y + 1 < size && 0 <= z + 1 && z + 1 < size && world[x, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z - 1 && z - 1 < size && world[x + 1, y - 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z && z < size && world[x + 1, y - 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y - 1 && y - 1 < size && 0 <= z + 1 && z + 1 < size && world[x + 1, y - 1, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x + 1, y, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z && z < size && world[x + 1, y, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x + 1, y, z + 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z - 1 && z - 1 < size && world[x + 1, y + 1, z - 1])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z && z < size && world[x + 1, y + 1, z])
                        {
                            neighbors++;
                        }
                        if (0 <= x + 1 && x + 1 < size && 0 <= y + 1 && y + 1 < size && 0 <= z + 1 && z + 1 < size && world[x + 1, y + 1, z + 1])
                        {
                            neighbors++;
                        }
                        #endregion

                        workingWorld[x, y, z] = (Y <= neighbors && neighbors <= Z) || (W <= neighbors && neighbors <= X && world[x, y, z]);

                        if (world[x, y, z] != workingWorld[x, y, z])
                        {
                            needUpdate[x / chunkSize, y / chunkSize, z / chunkSize] = true;
                        }
                    }
                }
            }
            waitHandle.Set();
        }

        private void Thread2D(bool[,,] world, bool[,,] workingWorld, bool[,,] needUpdate, int chunkSize,
                                     int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, ManualResetEvent waitHandle)
        {
            int size = world.GetLength(0);
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    for (int z = zStart; z < zEnd; z++)
                    {
                        if (y == size - 1)
                        {
                            int neighbors = 0;
                            if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x - 1, y, z - 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z && z < size && world[x - 1, y, z])
                            {
                                neighbors++;
                            }
                            if (0 <= x - 1 && x - 1 < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x - 1, y, z + 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x + 1, y, z - 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z && z < size && world[x + 1, y, z])
                            {
                                neighbors++;
                            }
                            if (0 <= x + 1 && x + 1 < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x + 1, y, z + 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x && x < size && 0 <= y && y < size && 0 <= z - 1 && z - 1 < size && world[x, y, z - 1])
                            {
                                neighbors++;
                            }
                            if (0 <= x && x < size && 0 <= y && y < size && 0 <= z + 1 && z + 1 < size && world[x, y, z + 1])
                            {
                                neighbors++;
                            }

                            workingWorld[x, y, z] = (Y <= neighbors && neighbors <= Z) || (W <= neighbors && neighbors <= X && world[x, y, z]);
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
    }
}
