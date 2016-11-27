using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class MainInput : MonoBehaviour
    {
        GameOfLife GOL { get { return GameOfLife.GOL; } }


        void Update()
        {
            if (GameOfLife.EditMode)
            {
                EditUpdate();
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                GOL.Next();
                GOL.UpdateCubes();
            }
            else if (Input.GetKeyDown(KeyCode.U))
            {
                GOL.MarkAllForUpdate();
                GOL.UpdateCubes();
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
                        var u = hit.point + hit.normal / 1.5f;
                        if (GOL.IsInWorld(u))
                        {
                            GOL.SetBlock(u, true);
                            GOL.UpdateChunk(u);
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
                        if (GOL.IsInWorld(v))
                        {
                            GOL.SetBlock(v, false);
                            GOL.UpdateChunk(v);
                        }
                    }
                }
            }
        }
    }
}
