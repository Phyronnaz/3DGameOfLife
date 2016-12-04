using System;
using System.Collections.Generic;
using System.Threading;

namespace Assets.Scripts
{
    public class Chunk
    {
        bool[,,] blocks;
        bool dirty;
        ChunkRenderer chunkRenderer;
        ManualResetEvent waitHandle;
        public bool IsWorking { get { return !waitHandle.WaitOne(0); } }

        public Chunk(int size, Position position)
        {
            blocks = new bool[size, size, size];
            chunkRenderer = new ChunkRenderer(position, size, Utility.Material);
            waitHandle = new ManualResetEvent(true);
        }


        public void SetDirty()
        {
            dirty = true;
        }

        public void UpdateTriangles()
        {
            if (!IsWorking)
            {
                waitHandle.Reset();
                chunkRenderer.UpdateTriangles(blocks, waitHandle);
            }
        }

        public void UpdateMesh()
        {
            chunkRenderer.UpdateMesh();
        }

        public void UpdateCollisions()
        {
            chunkRenderer.UpdateCollisions();
        }

        public void SetBlocks(bool[,,] blocks)
        {
            this.blocks = blocks;
        }
        public bool[,,] GetBlocks()
        {
            return blocks;
        }

        public void SetBlock(Position position, bool value)
        {
            blocks[position.x, position.y, position.z] = value;
        }
        public bool GetBlock(Position position)
        {
            return blocks[position.x, position.y, position.z];
        }
    }
}
