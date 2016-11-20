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
        readonly Mesh redMesh, whiteMesh, greenMesh, yellowMesh;
        readonly GameOfLife gameOfLife;

        int[] redTriangles, whiteTriangles, greenTriangles, yellowTriangles;
        int[] redTrianglesCache, whiteTrianglesCache, greenTrianglesCache, yellowTrianglesCache;


        public GameOfLifeRenderer(
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
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

            var redGO = new GameObject();
            var whiteGO = new GameObject();
            var greenGO = new GameObject();
            var yellowGO = new GameObject();

            redGO.name = "Red";
            whiteGO.name = "White";
            greenGO.name = "Green";
            yellowGO.name = "Yellow";

            redGO.transform.SetParent(gameObject.transform, false);
            whiteGO.transform.SetParent(gameObject.transform, false);
            greenGO.transform.SetParent(gameObject.transform, false);
            yellowGO.transform.SetParent(gameObject.transform, false);

            redGO.AddComponent<MeshRenderer>().material = redMaterial;
            whiteGO.AddComponent<MeshRenderer>().material = whiteMaterial;
            greenGO.AddComponent<MeshRenderer>().material = greenMaterial;
            yellowGO.AddComponent<MeshRenderer>().material = yellowMaterial;

            redMesh = redGO.AddComponent<MeshFilter>().mesh;
            whiteMesh = whiteGO.AddComponent<MeshFilter>().mesh;
            greenMesh = greenGO.AddComponent<MeshFilter>().mesh;
            yellowMesh = yellowGO.AddComponent<MeshFilter>().mesh;

            redMesh.MarkDynamic();
            whiteMesh.MarkDynamic();
            greenMesh.MarkDynamic();
            yellowMesh.MarkDynamic();

            redMesh.vertices = vertices;
            whiteMesh.vertices = (Vector3[])vertices.Clone();
            greenMesh.vertices = (Vector3[])vertices.Clone();
            yellowMesh.vertices = (Vector3[])vertices.Clone();

            redTriangles = new int[36 * XSize * YSize * ZSize];
            whiteTriangles = new int[36 * XSize * YSize * ZSize];
            greenTriangles = new int[36 * XSize * YSize * ZSize];
            yellowTriangles = new int[36 * XSize * YSize * ZSize];
        }

        public void UpdateTriangles(AutoResetEvent waitHandle, bool[,,] world0, bool[,,] world1, bool[,,] world2)
        {
            var redCount = 0;
            var whiteCount = 0;
            var greenCount = 0;
            var yellowCount = 0;

            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        var b1 = world2[x + XStart, y + YStart, z + ZStart];
                        var b2 = world1[x + XStart, y + YStart, z + ZStart];
                        var b3 = world0[x + XStart, y + YStart, z + ZStart];
                        for (int i = 0; i < Triangles.Length / 3; i++)
                        {
                            var verticesIndex = x + Triangles[3 * i] + (XSize + 1) * (y + Triangles[3 * i + 1] + (YSize + 1) * (z + Triangles[3 * i + 2]));

                            if (b1 && b2 && !b3) //Red
                            {
                                redTriangles[redCount] = verticesIndex;
                                redCount++;
                            }
                            else if (b1 && b2 && b3) //White
                            {
                                whiteTriangles[whiteCount] = verticesIndex;
                                whiteCount++;
                            }
                            else if (!b1 && b2 && b3) //Green
                            {
                                greenTriangles[greenCount] = verticesIndex;
                                greenCount++;
                            }
                            else if (!b1 && b2 && !b3) //Yellow
                            {
                                yellowTriangles[yellowCount] = verticesIndex;
                                yellowCount++;
                            }
                        }
                    }
                }
            }

            redTrianglesCache = new int[redCount];
            whiteTrianglesCache = new int[whiteCount];
            greenTrianglesCache = new int[greenCount];
            yellowTrianglesCache = new int[yellowCount];

            Array.Copy(redTriangles, 0, redTrianglesCache, 0, redCount);
            Array.Copy(whiteTriangles, 0, whiteTrianglesCache, 0, whiteCount);
            Array.Copy(greenTriangles, 0, greenTrianglesCache, 0, greenCount);
            Array.Copy(yellowTriangles, 0, yellowTrianglesCache, 0, yellowCount);

            waitHandle.Set();
        }

        public void UpdateMeshes()
        {
            redMesh.SetTriangles(redTrianglesCache, 0, false);
            whiteMesh.SetTriangles(whiteTrianglesCache, 0, false);
            greenMesh.SetTriangles(greenTrianglesCache, 0, false);
            yellowMesh.SetTriangles(yellowTrianglesCache, 0, false);

            redMesh.UploadMeshData(false);
            whiteMesh.UploadMeshData(false);
            greenMesh.UploadMeshData(false);
            yellowMesh.UploadMeshData(false);

            redTrianglesCache = new int[0];
            whiteTrianglesCache = new int[0];
            greenTrianglesCache = new int[0];
            yellowTrianglesCache = new int[0];
        }


        public static readonly int[] Triangles = new int[]{
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
