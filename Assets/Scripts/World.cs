using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    public static class World
    {
        public static readonly int ChunkSize = 39;
        public static Dictionary<Position, Chunk> Chunks = new Dictionary<Position, Chunk>();
        public static List<Chunk> ChunksList = new List<Chunk>();

        public static bool GetBlock(Position position)
        {
            Chunk chunk;
            if (!Chunks.TryGetValue(position / ChunkSize, out chunk))
            {
                chunk = new Chunk(ChunkSize, position);
                Chunks.Add(position / ChunkSize, chunk);
            }
            return chunk.GetBlock(position % ChunkSize);
        }

        public static void SetBlock(Position position, bool value)
        {
            Chunk chunk;
            if (!Chunks.TryGetValue(position / ChunkSize, out chunk))
            {
                chunk = new Chunk(ChunkSize, position);
                Chunks.Add(position / ChunkSize, chunk);
            }
            chunk.SetBlock(position % ChunkSize, value);
        }

        public static void Randomize(float density)
        {
            foreach(var chunk in chunks)
            {
                SetBlock(x, y, z, UnityEngine.Random.value < density);
            }
        }

        public static void Reset()
        {
            for (int x = 0; x < GameOfLife.GOL.Size; x++)
            {
                for (int y = 0; y < GameOfLife.GOL.Size; y++)
                {
                    for (int z = 0; z < GameOfLife.GOL.Size; z++)
                    {
                        GameOfLife.GOL.SetBlock(x, y, z, false);
                    }
                }
            }
            GameOfLife.GOL.ScheduleCubesUpdate();
        }

        public static void SingleBlock()
        {
            Reset();
            if (GameOfLife.Use3D)
            {
                GameOfLife.GOL.SetBlock(GameOfLife.GOL.Size / 2, GameOfLife.GOL.Size / 2, GameOfLife.GOL.Size / 2, true);
            }
            else
            {
                GameOfLife.GOL.SetBlock(GameOfLife.GOL.Size / 2, GameOfLife.GOL.Size - 1, GameOfLife.GOL.Size / 2, true);
            }
            GameOfLife.GOL.ScheduleCubesUpdate();
        }

        public static void SetSize(int size)
        {
            if (size != GameOfLife.GOL.Size)
            {
                GameOfLife.GOL.WaitForThreads();
                var new_world = new bool[size, size, size];
                if (size < GameOfLife.GOL.Size)
                {
                    for (int x = 0; x < size; x++)
                    {
                        for (int y = 0; y < size; y++)
                        {
                            for (int z = 0; z < size; z++)
                            {
                                //new_world[x, y, z] = GameOfLife.GOL.World[x, y, z];
                            }
                        }
                    }
                }
                else
                {
                    var k = (size - GameOfLife.GOL.Size) / 2;
                    for (int x = 0; x < GameOfLife.GOL.Size; x++)
                    {
                        for (int y = 0; y < GameOfLife.GOL.Size; y++)
                        {
                            for (int z = 0; z < GameOfLife.GOL.Size; z++)
                            {
                                new_world[x + k, y + k, z + k] = GameOfLife.GOL.World[x, y, z];
                            }
                        }
                    }
                }

                GameOfLife.GOL.SetWorld(new_world);
                GameOfLife.GOL.MarkAllForUpdate();
                GameOfLife.GOL.ScheduleCubesUpdate();
            }
        }

        public static void SetEditMode(bool editMode)
        {
            GameOfLife.EditMode = editMode;
            if (editMode)
            {
                GameOfLife.GOL.UpdateCollisions();
            }
        }
    }
}
