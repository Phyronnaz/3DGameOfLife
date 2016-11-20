using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    class GameOfLifeRenderer
    {
        public GameObject gameObject;

        public int XSize { get { return XEnd - XStart; } }
        public int YSize { get { return YEnd - YStart; } }
        public int ZSize { get { return ZEnd - ZStart; } }

        int XStart, XEnd, YStart, YEnd, ZStart, ZEnd;
        Mesh redMesh, whiteMesh, greenMesh, yellowMesh;
        GameOfLife gameOfLife;


        public GameOfLifeRenderer(
            GameOfLife gameOfLife,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
        {
            this.gameOfLife = gameOfLife;
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
        }

        public void UpdateCubes()
        {
            UpdateRedCubes();
            UpdateWhiteCubes();
            UpdateGreenCubes();
            UpdateYellowCubes();
        }

        public void UpdateRedCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        if (gameOfLife.GetWorld(-2)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(-1)[x + XStart, y + YStart, z + ZStart] && !gameOfLife.GetWorld(0)[x + XStart, y + YStart, z + ZStart])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + XSize * (y + YSize * z))] =
                                    x + Triangles[3 * i] + (XSize + 1) * (y + Triangles[3 * i + 1] + (YSize + 1) * (z + Triangles[3 * i + 2]));
                            }

                        }
                    }
                }
            }
            redMesh.SetTriangles(triangles, 0, false);
            redMesh.UploadMeshData(false);
        }
        public void UpdateWhiteCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        if (gameOfLife.GetWorld(-2)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(-1)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(0)[x + XStart, y + YStart, z + ZStart])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + XSize * (y + YSize * z))] =
                                     x + Triangles[3 * i] + (XSize + 1) * (y + Triangles[3 * i + 1] + (YSize + 1) * (z + Triangles[3 * i + 2]));
                            }

                        }
                    }
                }
            }
            whiteMesh.SetTriangles(triangles, 0, false);
            whiteMesh.UploadMeshData(false);
        }
        public void UpdateGreenCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        if (!gameOfLife.GetWorld(-2)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(-1)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(0)[x + XStart, y + YStart, z + ZStart])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + XSize * (y + YSize * z))] =
                                     x + Triangles[3 * i] + (XSize + 1) * (y + Triangles[3 * i + 1] + (YSize + 1) * (z + Triangles[3 * i + 2]));
                            }

                        }
                    }
                }
            }
            greenMesh.SetTriangles(triangles, 0, false);
            greenMesh.UploadMeshData(false);
        }
        public void UpdateYellowCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = 0; x < XSize; x++)
            {
                for (int y = 0; y < YSize; y++)
                {
                    for (int z = 0; z < ZSize; z++)
                    {
                        if (!gameOfLife.GetWorld(-2)[x + XStart, y + YStart, z + ZStart] && gameOfLife.GetWorld(-1)[x + XStart, y + YStart, z + ZStart] && !gameOfLife.GetWorld(0)[x + XStart, y + YStart, z + ZStart])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + XSize * (y + YSize * z))] =
                                     x + Triangles[3 * i] + (XSize + 1) * (y + Triangles[3 * i + 1] + (YSize + 1) * (z + Triangles[3 * i + 2]));
                            }

                        }
                    }
                }
            }
            yellowMesh.SetTriangles(triangles, 0, false);
            yellowMesh.UploadMeshData(false);
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
