# Conway's Game Of Life
A modular and parameterized sandbox to experiment with Conway's Game of Life.

Created in Unity (6000.4.6f1). Does not use any external dependencies. Currently runs without parallel processing. This project will be used as a platform to learn and implement parallelized execution in future.

## Features
- Ability to create custom pattern using a 3x3 grid
- Pre-loaded with R-pentomino and Glider patterns (more can be added via ScriptableObjects)
- Ability to change the parameters of the four fundamental rule values
- Ability to implement a 3x3 or 5x5 Moore Neighborhood per cell when applying rules
- Ability to enter Edit Mode and "draw" alive cells using the mouse (in real-time)
- Infinite game world
- Ability to pause and play the simulation

## Planned Expansion
- Multithreading (or Burst whichever one makes more sense)
- WebGL version and deploy to website (at some point once it starts working)
- Deploy as a lightweight desktop application

## Packages Used
- 2D (Unity Technologies)
- Burst
- Scriptable Build Pipeline
- uGUI
- Visual Studio Editor
