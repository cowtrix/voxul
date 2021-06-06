# voxul
## A Unity 3D Voxel Painting Tool

voxul is a voxel system and editor tool for [Unity 3D.](https://unity.com/) It is distributed under the Apache License, Version 2.0 which means that it can be used freely. Feel free to fork this repository, log issues here, and submit pull requests.

## Requirements

- Unity 2020.3.2f1 or later
    - Your project must include both the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/index.html) & [Shadergraph](https://unity.com/shader-graph)

## Features

- Advanced voxel mesh editor: add, remove, subdivide, select, copy/paste, mirror

![select_delete](https://user-images.githubusercontent.com/5094696/120932524-1d3d3a00-c6ee-11eb-9400-3f863b56940a.gif)

- Subdivision and resolution layering system that lets you mix and match different voxel grid resolutions.

![subdivide](https://user-images.githubusercontent.com/5094696/120932172-ab182580-c6ec-11eb-866c-f1f67644648a.gif)

- Automatic spritesheets and custom texturing for each voxel face.
- Paint surfaces seperately, controlling colour and texture

![paintingSurfaces](https://user-images.githubusercontent.com/5094696/120931430-676fec80-c6e9-11eb-8b4d-e78bb99ba272.gif)

- Lots of useful utilities for building your voxel systems, like destroyable objects.
- Edit in-scene mesh objects, or save your meshes to their own `VoxelMesh` assets.
- Works efficiently and shares meshes with prefabs and multi-instanced objects.