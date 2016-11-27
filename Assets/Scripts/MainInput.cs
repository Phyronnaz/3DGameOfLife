using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class MainInput : MonoBehaviour
    {
        public GameObject Canvas;

        void Update()
        {
            if (GameOfLife.EditMode)
            {
                EditUpdate();
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                GameOfLife.GOL.ScheduleNext();
                GameOfLife.GOL.ScheduleCubesUpdate();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                GameOfLife.GOL.MarkAllForUpdate();
                GameOfLife.GOL.ScheduleCubesUpdate();
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                Canvas.SetActive(!Canvas.activeSelf);
            }
        }

        void EditUpdate()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0) || (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftShift)))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit, 1000000))
                    {
                        var v = hit.point + hit.normal / 1.5f;
                        var x = Mathf.RoundToInt(v.x);
                        var y = Mathf.RoundToInt(v.y);
                        var z = Mathf.RoundToInt(v.z);
                        if (GameOfLife.GOL.IsInWorld(x, y, z))
                        {
                            GameOfLife.GOL.SetBlock(x, y, z, true);
                            GameOfLife.GOL.UpdateChunk(x, y, z);
                        }
                        else
                        {
                            Log.LogWarning("Out of the map!");
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1) || (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftShift)))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out hit))
                    {
                        var v = hit.point - hit.normal / 1.5f;
                        var x = Mathf.RoundToInt(v.x);
                        var y = Mathf.RoundToInt(v.y);
                        var z = Mathf.RoundToInt(v.z);
                        if (GameOfLife.GOL.IsInWorld(x, y, z))
                        {
                            GameOfLife.GOL.SetBlock(x, y, z, false);
                            GameOfLife.GOL.UpdateChunk(x, y, z);
                        }
                    }
                }
            }
        }
    }
}
