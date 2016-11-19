using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    class GameOfLifeRenderer : MonoBehaviour
    {
        int XStart, XEnd, YStart, YEnd, ZStart, ZEnd;

        GameOfLife gameOfLife;

        Mesh redMesh, whiteMesh, greenMesh, yellowMesh;

        int XSize { get { return XEnd - XStart; } }
        int YSize { get { return YEnd - YStart; } }
        int ZSize { get { return ZEnd - ZStart; } }


        public GameOfLifeRenderer(
            GameOfLife gameOfLife,
            int xStart, int xEnd, int yStart, int yEnd, int zStart, int zEnd,
            Material redMaterial, Material whiteMaterial, Material greenMaterial, Material yellowMaterial)
        {
            this.gameOfLife = gameOfLife;

            XStart = xStart;
            XEnd = xEnd;
            YStart = yStart;
            YEnd = yEnd;
            ZStart = zStart;
            ZEnd = zEnd;

            var vertices = new Vector3[64000];
            for (int x = 0; x < 40; x++)
            {
                for (int y = 0; y < 40; y++)
                {
                    for (int z = 0; z < 40; z++)
                    {
                        vertices[x + 40 * y + 40 * 40 * z] = new Vector3(x, y, z);
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

            redGO.transform.parent = transform;
            whiteGO.transform.parent = transform;
            greenGO.transform.parent = transform;
            yellowGO.transform.parent = transform;

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
            for (int x = XStart; x < XEnd; x++)
            {
                for (int y = YStart; y < YEnd; y++)
                {
                    for (int z = ZStart; z < ZEnd; z++)
                    {
                        if (gameOfLife.GetWorld(-2)[x, y, z] && gameOfLife.GetWorld(-1)[x, y, z] && !gameOfLife.GetWorld(0)[x, y, z])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + (x - XStart) * (y + (y - YStart) * z))] =
                                    x + Triangles[3 * i] +
                                    40 * (y + Triangles[3 * i + 1]) +
                                    40 * 40 * (z + Triangles[3 * i + 2]);
                            }

                        }
                    }
                }
            }
            redMesh.triangles = triangles;
            redMesh.UploadMeshData(false);
        }
        public void UpdateWhiteCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = XStart; x < XEnd; x++)
            {
                for (int y = YStart; y < YEnd; y++)
                {
                    for (int z = ZStart; z < ZEnd; z++)
                    {
                        if (gameOfLife.GetWorld(-2)[x, y, z] && gameOfLife.GetWorld(-1)[x, y, z] && gameOfLife.GetWorld(0)[x, y, z])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + (x - XStart) * (y + (y - YStart) * z))] =
                                    x + Triangles[3 * i] +
                                    40 * (y + Triangles[3 * i + 1]) +
                                    40 * 40 * (z + Triangles[3 * i + 2]);
                            }

                        }
                    }
                }
            }
            whiteMesh.triangles = triangles;
            whiteMesh.UploadMeshData(false);
        }
        public void UpdateGreenCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = XStart; x < XEnd; x++)
            {
                for (int y = YStart; y < YEnd; y++)
                {
                    for (int z = ZStart; z < ZEnd; z++)
                    {
                        if (!gameOfLife.GetWorld(-2)[x, y, z] && gameOfLife.GetWorld(-1)[x, y, z] && gameOfLife.GetWorld(0)[x, y, z])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + (x - XStart) * (y + (y - YStart) * z))] =
                                    x + Triangles[3 * i] +
                                    40 * (y + Triangles[3 * i + 1]) +
                                    40 * 40 * (z + Triangles[3 * i + 2]);
                            }

                        }
                    }
                }
            }
            greenMesh.triangles = triangles;
            greenMesh.UploadMeshData(false);
        }
        public void UpdateYellowCubes()
        {
            int[] triangles = new int[36 * XSize * YSize * ZSize];
            for (int x = XStart; x < XEnd; x++)
            {
                for (int y = YStart; y < YEnd; y++)
                {
                    for (int z = ZStart; z < ZEnd; z++)
                    {
                        if (!gameOfLife.GetWorld(-2)[x, y, z] && gameOfLife.GetWorld(-1)[x, y, z] && !gameOfLife.GetWorld(0)[x, y, z])
                        {
                            for (int i = 0; i < Triangles.Length / 3; i++)
                            {
                                triangles[i + Triangles.Length / 3 * (x + (x - XStart) * (y + (y - YStart) * z))] =
                                    x + Triangles[3 * i] +
                                    40 * (y + Triangles[3 * i + 1]) +
                                    40 * 40 * (z + Triangles[3 * i + 2]);
                            }

                        }
                    }
                }
            }
            yellowMesh.triangles = triangles;
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
