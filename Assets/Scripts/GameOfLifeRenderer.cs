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
        readonly MeshCollider collider;

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
            collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            mesh = go.AddComponent<MeshFilter>().mesh;
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            triangles = new int[36 * XSize * YSize * ZSize];
        }

        public void Quit()
        {
            triangles = null;
            trianglesCache = null;
        }

        public void UpdateTriangles(bool[,,] world, ManualResetEvent waitHandle = null)
        {
            var count = 0;

            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        int i = x + XStart;
                        int j = y + YStart;
                        int k = z + ZStart;
                        if (world[i, j, k])
                        {
                            if (!world[i, j + 1, k])
                            {
                                for (int a = 0; a < CubeUp.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeUp[3 * a] + (XSize + 1) * (y + CubeUp[3 * a + 1] + (YSize + 1) * (z + CubeUp[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }

                            if (!world[i, j - 1, k])
                            {
                                for (int a = 0; a < CubeDown.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeDown[3 * a] + (XSize + 1) * (y + CubeDown[3 * a + 1] + (YSize + 1) * (z + CubeDown[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }


                            if (!world[i, j, k + 1])
                            {
                                for (int a = 0; a < CubeNorth.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeNorth[3 * a] + (XSize + 1) * (y + CubeNorth[3 * a + 1] + (YSize + 1) * (z + CubeNorth[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }

                            if (!world[i, j, k - 1])
                            {
                                for (int a = 0; a < CubeSouth.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeSouth[3 * a] + (XSize + 1) * (y + CubeSouth[3 * a + 1] + (YSize + 1) * (z + CubeSouth[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }


                            if (!world[i + 1, j, k])
                            {
                                for (int a = 0; a < CubeEast.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeEast[3 * a] + (XSize + 1) * (y + CubeEast[3 * a + 1] + (YSize + 1) * (z + CubeEast[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }


                            if (!world[i - 1, j, k])
                            {
                                for (int a = 0; a < CubeWest.Length / 3; a++)
                                {
                                    var verticesIndex = x + CubeWest[3 * a] + (XSize + 1) * (y + CubeWest[3 * a + 1] + (YSize + 1) * (z + CubeWest[3 * a + 2]));
                                    triangles[count] = verticesIndex;
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            trianglesCache = new int[count];

            Array.Copy(triangles, 0, trianglesCache, 0, count);

            if (waitHandle != null)
            {
                waitHandle.Set();
            }
        }

        public void UpdateMeshes()
        {
            mesh.SetTriangles(trianglesCache, 0, false);
            mesh.RecalculateNormals();
            mesh.UploadMeshData(false);
            collider.sharedMesh = mesh;
        }

        public static int[] CubeUp = new int[] { 0, 1, 1,
                                                 1, 1, 1,
                                                 1, 1, 0,

                                                 0, 1, 1,
                                                 1, 1, 0,
                                                 0, 1, 0};

        public static int[] CubeDown = new int[] { 0, 0, 0,
                                                   1, 0, 0,
                                                   1, 0, 1,

                                                   0, 0, 0,
                                                   1, 0, 1,
                                                   0, 0, 1};

        public static int[] CubeNorth = new int[] { 1, 0, 1,
                                                    1, 1, 1,
                                                    0, 1, 1,

                                                    1, 0, 1,
                                                    0, 1, 1,
                                                    0, 0, 1};

        public static int[] CubeEast = new int[] { 1, 0, 0,
                                                   1, 1, 0,
                                                   1, 1, 1,

                                                   1, 0, 0,
                                                   1, 1, 1,
                                                   1, 0, 1};


        public static int[] CubeSouth = new int[] { 0, 0, 0,
                                                    0, 1, 0,
                                                    1, 1, 0,

                                                    0, 0, 0,
                                                    1, 1, 0,
                                                    1, 0, 0};

        public static int[] CubeWest = new int[] { 0, 0, 1,
                                                   0, 1, 1,
                                                   0, 1, 0,

                                                   0, 0, 1,
                                                   0, 1, 0,
                                                   0, 0, 0};
    }
}
