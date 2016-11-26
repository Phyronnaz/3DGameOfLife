using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Assets.Scripts
{
    public static class IO
    {
        public static void SaveToFile(string path, bool[,,] world)
        {
            using (StreamWriter file = new StreamWriter(path, false))
            {
                file.WriteLine(world.GetLength(0));
                file.WriteLine(world.GetLength(1));
                file.WriteLine(world.GetLength(2));
                for (var i = 0; i < world.GetLength(0); i++)
                {
                    for (var j = 0; j < world.GetLength(1); j++)
                    {
                        StringBuilder s = new StringBuilder(world.GetLength(2));
                        for (var k = 0; k < world.GetLength(2); k++)
                        {
                            s.Append(world[i, j, k] ? '1' : '0');
                        }
                        file.WriteLine(s);
                    }
                }
            }
        }

        public static bool[,,] LoadFromFile(string path)
        {
            var s = File.ReadAllLines(path);
            var xsize = int.Parse(s[0]);
            var ysize = int.Parse(s[1]);
            var zsize = int.Parse(s[2]);
            var world = new bool[xsize, ysize, zsize];
            for (var i = 0; i < world.GetLength(0); i++)
            {
                for (var j = 0; j < world.GetLength(1); j++)
                {
                    for (var k = 0; k < world.GetLength(2); k++)
                    {
                        world[i, j, k] = s[i * xsize + j + 3][k] == '1';
                    }
                }
            }
            return world;
        }

        public static string LoadDialog()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.InitialDirectory = "C:\\";
            dialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }
        public static string SaveDialog()
        {
            SaveFileDialog dialog = new SaveFileDialog();

            dialog.InitialDirectory = "C:\\";
            dialog.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            else
            {
                return "";
            }
        }

        public static void Load()
        {
            var s = LoadDialog();
            if (s == "")
            {
                Log.LogWarning("World not loaded");
            }
            else
            {
                try
                {
                    var world = LoadFromFile(s);
                    GameOfLife.GOL.SetWorld(world);
                }
                catch (Exception ex)
                {
                    var error = "Error: Could not read file from disk. Original error: " + ex.Message;
                    Log.LogError(error);
                    MessageBox.Show(error);
                }
            }
        }

        public static void Save()
        {
            var s = SaveDialog();
            if (s == "")
            {
                Log.LogWarning("World not saved");
            }
            else
            {
                try
                {
                    SaveToFile(s, GameOfLife.GOL.World);
                }
                catch (Exception ex)
                {
                    var error = "Error: Could not write file to disk. Original error: " + ex.Message;
                    Log.LogError(error);
                    MessageBox.Show(error);
                }
            }
        }
    }
}
