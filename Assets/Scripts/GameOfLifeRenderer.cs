using System;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    class GameOfLifeRenderer
    {
        public readonly GameObject gameObject;

        public int XSize { get { return XEnd - XStart; } }
        public int YSize { get { return YEnd - YStart; } }
        public int ZSize { get { return ZEnd - ZStart; } }

        readonly int XStart, XEnd, YStart, YEnd, ZStart, ZEnd;
        readonly Mesh mesh;

        int[] triangles;
        int[] trianglesCache;


        public GameOfLifeRenderer(
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd, Material material)
        {
            gameObject = new GameObject();
            gameObject.transform.position = new Vector3(xStart, yStart, zStart);

            XStart = xStart;
            XEnd = xEnd;
            YStart = yStart;
            YEnd = yEnd;
            ZStart = zStart;
            ZEnd = zEnd;

            var vertices = new Vector3[(XSize + 1) * (YSize + 1) * (ZSize + 1)];
            for (int x = 0; x < XSize + 1; x++)
            {
                for (int y = 0; y < YSize + 1; y++)
                {
                    for (int z = 0; z < ZSize + 1; z++)
                    {
                        vertices[x + (XSize + 1) * (y + (YSize + 1) * z)] = new Vector3(x, y, z);
                    }
                }
            }

            var go = new GameObject();

            go.name = "GO";

            go.transform.SetParent(gameObject.transform, false);

            go.AddComponent<MeshRenderer>().material = material;

            mesh = go.AddComponent<MeshFilter>().mesh;

            mesh.MarkDynamic();

            mesh.vertices = vertices;

            triangles = new int[36 * XSize * YSize * ZSize];
        }

        public void UpdateTriangles(ManualResetEvent waitHandle, bool[,,] world)
        {
            var count = 0;

            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        var b = world[x + XStart, y + YStart, z + ZStart];
                        for (int i = 0; i < CubeTriangles.Length / 3; i++)
                        {
                            var verticesIndex = x + CubeTriangles[3 * i] + (XSize + 1) * (y + CubeTriangles[3 * i + 1] + (YSize + 1) * (z + CubeTriangles[3 * i + 2]));

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

            Array.Copy(triangles, 0, trianglesCache, 0, count);

            waitHandle.Set();
        }

        public void UpdateMeshes()
        {
            mesh.SetTriangles(trianglesCache, 0, false);

            mesh.UploadMeshData(false);
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
