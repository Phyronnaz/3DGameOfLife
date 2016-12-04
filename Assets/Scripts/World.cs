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

        }

    }
}
