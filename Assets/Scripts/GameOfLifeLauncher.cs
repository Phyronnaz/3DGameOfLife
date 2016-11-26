using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class GameOfLifeLauncher : MonoBehaviour
    {
        public int Size = 10;

        public Material Material;

        public float RefreshRate = 0.5f;

        GameOfLife gameOfLife;

        bool editMode = false;

        void Start()
        {
            gameOfLife = new GameOfLife(Size, Size, Size, Material);
            
            gameOfLife.UpdateCubes();
            InvokeRepeating("GameOfLifeUpdate", 0, RefreshRate);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                gameOfLife.Next();
                gameOfLife.UpdateCubes();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                editMode = !editMode;
            }
            if (Input.GetKeyDown(KeyCode.U))
            {
                gameOfLife.UpdateCubes();
            }
            if (editMode)
            {
                EditMode();
            }
        }

        void EditMode()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift)))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, 1000000))
                    {
                        var v = hit.point + hit.normal / 2;
                        if (gameOfLife.IsInWorld(v))
                        {
                            gameOfLife.SetBlock(v, true);
                            gameOfLife.UpdateChunk(v);
                        }
                        else
                        {
                            Log.LogWarning("Edge of the map!");
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftShift)))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit))
                    {
                        var v = hit.point - hit.normal / 2;
                        if (gameOfLife.IsInWorld(v))
                        {
                            gameOfLife.SetBlock(v, false);
                            gameOfLife.UpdateChunk(v);
                        }
                    }
                }
            }
        }
        void GameOfLifeUpdate()
        {
            gameOfLife.Update();
        }

        void OnApplicationQuit()
        {
            gameOfLife.Quit();
            gameOfLife = null;
        }
    }
}