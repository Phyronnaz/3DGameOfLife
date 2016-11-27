using System;
using UnityEngine;

namespace Assets.Scripts
{
    public static class GameOfLifeTools
    {

        public static int W
        {
            get
            {
                GameOfLife.W = PlayerPrefs.GetInt("W", 2);
                return GameOfLife.W;
            }
            set
            {
                PlayerPrefs.SetInt("W", value);
                GameOfLife.W = value;
            }
        }
        public static int X
        {
            get
            {
                GameOfLife.X = PlayerPrefs.GetInt("X", 3);
                return GameOfLife.X;
            }
            set
            {
                PlayerPrefs.SetInt("X", value);
                GameOfLife.X = value;
            }
        }
        public static int Y
        {
            get
            {
                GameOfLife.Y = PlayerPrefs.GetInt("Y", 3);
                return GameOfLife.Y;
            }
            set
            {
                PlayerPrefs.SetInt("Y", value);
                GameOfLife.Y = value;
            }
        }
        public static int Z
        {
            get
            {
                GameOfLife.Z = PlayerPrefs.GetInt("Z", 3);
                return GameOfLife.Z;
            }
            set
            {
                PlayerPrefs.SetInt("Z", value);
                GameOfLife.Z = value;
            }
        }

        public static int ThreadSize
        {
            get
            {
                GameOfLife.ThreadSize = PlayerPrefs.GetInt("ThreadSize", 20);
                return GameOfLife.ThreadSize;
            }
            set
            {
                PlayerPrefs.SetInt("ThreadSize", value);
                GameOfLife.ThreadSize = value;
            }
        }
        public static int ChunkSize
        {
            get
            {
                GameOfLife.ChunkSize = PlayerPrefs.GetInt("ChunkSize", 39);
                return GameOfLife.ChunkSize;
            }
            set
            {
                var v = Mathf.Min(39, value);
                PlayerPrefs.SetInt("ChunkSize", v);
                GameOfLife.ChunkSize = v;
            }
        }

        public static void InitProperties()
        {
            GameOfLife.W = PlayerPrefs.GetInt("W", 2);

            GameOfLife.X = PlayerPrefs.GetInt("X", 3);

            GameOfLife.Y = PlayerPrefs.GetInt("Y", 3);

            GameOfLife.Z = PlayerPrefs.GetInt("Z", 3);

            GameOfLife.ThreadSize = PlayerPrefs.GetInt("ThreadSize", 20);

            GameOfLife.ChunkSize = PlayerPrefs.GetInt("ChunkSize", 39);
        }


        public static void Randomize(float density)
        {
            for (int x = 0; x < GameOfLife.GOL.Size; x++)
            {
                for (int y = 0; y < GameOfLife.GOL.Size; y++)
                {
                    for (int z = 0; z < GameOfLife.GOL.Size; z++)
                    {
                        GameOfLife.GOL.SetBlock(x, y, z, UnityEngine.Random.value < density);
                    }
                }
            }
            GameOfLife.GOL.ScheduleCubesUpdate();
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
                                new_world[x, y, z] = GameOfLife.GOL.World[x, y, z];
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