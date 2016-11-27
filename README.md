# 3D Game Of Life

3D Game Of Life build in Unity.

## Features
* Cubes are rendered in chunks (max size 40x40x40)
* Triangles calculation and Game Of Life iterations are multi-threaded, which prevent Unity from freezing
* Tested in 500x500x500 with 16 GB of RAM
* Possibility to use Z axis as a trail for 2D Game Of Life
* Save/load worlds
* Edit mode similar to minecraft

## Issues
* Memory leak in C#: main arrays are not destroyed when changing size, so memory is never freed
* Camera control is buggy

## Controls
* H: Hide/Show UI
* U: Force update
* Return: Next iteration
* Space: Freeze camera
* Left Shift: allow to hold click in edit mode

## Download
Last build can be downloaded here: https://drive.google.com/open?id=0B0h3CZpUUUCwTzQ4dHY4UHR6V0E

## Screenshots
[Screenshot](/Screenshots/Screenshot%20(66).png?raw=true "Screenshot")
