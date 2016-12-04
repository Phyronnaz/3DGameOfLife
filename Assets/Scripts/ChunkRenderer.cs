using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class ChunkRenderer
    {
        readonly GameObject gameObject;
        readonly Mesh mesh;
        readonly MeshCollider collider;
        readonly int size;

        int[] trianglesCache;


        public ChunkRenderer(Vector3 position, int size, Material material)
        {
            this.size = size;

            var vertices = new Vector3[(size + 1) * (size + 1) * (size + 1)];
            var uv = new Vector2[(size + 1) * (size + 1) * (size + 1)];
            for (int x = 0; x < size + 1; x++)
            {
                for (int y = 0; y < size + 1; y++)
                {
                    for (int z = 0; z < size + 1; z++)
                    {
                        vertices[x + (size + 1) * (y + (size + 1) * z)] = new Vector3(x - 0.5f, y - 0.5f, z - 0.5f);
                        var i = x / size * 0.8f + 0.1f;
                        var j = y / size * 0.8f + 0.1f;
                        uv[x + (size + 1) * (y + (size + 1) * z)] = new Vector2(i, j);
                    }
                }
            }
            gameObject = new GameObject();
            gameObject.transform.position = position;
            gameObject.AddComponent<MeshRenderer>().material = material;
            collider = gameObject.AddComponent<MeshCollider>();
            mesh = gameObject.AddComponent<MeshFilter>().mesh;
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uv;
        }

        public void UpdateTriangles(bool[,,] blocks, ManualResetEvent waitHandle = null)
        {
            var count = 0;
            var triangles = new int[36 * size * size * size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        var b = blocks[x, y, z];
                        for (int i = 0; i < CubeTriangles.Length / 3; i++)
                        {
                            var verticesIndex = x + CubeTriangles[3 * i] + (size + 1) * (y + CubeTriangles[3 * i + 1] + (size + 1) * (z + CubeTriangles[3 * i + 2]));

                            if (b)
                            {
                                triangles[count] = verticesIndex;
                                count++;
                            }
                        }
                    }
                }
            }

            trianglesCache = new int[count];

            System.Array.Copy(triangles, 0, trianglesCache, 0, count);

            if (waitHandle != null)
            {
                waitHandle.Set();
            }
        }

        public void UpdateMesh()
        {
            mesh.SetTriangles(trianglesCache, 0, false);
            mesh.UploadMeshData(false);
            trianglesCache = null;
        }

        public void UpdateCollisions()
        {
            mesh.RecalculateNormals();
            collider.sharedMesh = mesh;
        }

        public void Destroy()
        {
            mesh.Clear();
            Object.Destroy(gameObject);
        }


        public static readonly int[] CubeTriangles = new int[]{
                                                            0,0,0,
                                                            0,0,1,
                                                            0,1,1,
                                                            1,1,0,
                                                            0,0,0,
                                                            0,1,0,
                                                            1,0,1,
                                                            0,0,0,
                                                            1,0,0,
                                                            1,1,0,
                                                            1,0,0,
                                                            0,0,0,
                                                            0,0,0,
                                                            0,1,1,
                                                            0,1,0,
                                                            1,0,1,
                                                            0,0,1,
                                                            0,0,0,
                                                            0,1,1,
                                                            0,0,1,
                                                            1,0,1,
                                                            1,1,1,
                                                            1,0,0,
                                                            1,1,0,
                                                            1,0,0,
                                                            1,1,1,
                                                            1,0,1,
                                                            1,1,1,
                                                            1,1,0,
                                                            0,1,0,
                                                            1,1,1,
                                                            0,1,0,
                                                            0,1,1,
                                                            1,1,1,
                                                            0,1,1,
                                                            1,0,1
        };
    }
}
