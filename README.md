# voxul
## A Unity 3D Voxel Painting Tool

![Banner](https://user-images.githubusercontent.com/5094696/120939811-b467b900-c711-11eb-895f-22773d26af42.png)

voxul is a voxel system and editor tool for [Unity 3D.](https://unity.com/) Use it to build voxel meshes, objects and levels.

## Requirements

- Unity 2020.3.2f1 or later
    - Your project must include both the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/index.html) & [Shadergraph](https://unity.com/shader-graph)

## Features

- Lots of useful utilities for building your voxel systems, like destroyable objects.
- Edit in-scene mesh objects, or save your meshes to their own `VoxelMesh` assets.
- Works efficiently and shares meshes with prefabs and multi-instanced objects.
- Choose from single threading, coroutines or dedicated tasks for rebake jobs.
- Advanced voxel mesh editor: add, remove, subdivide, select, copy/paste, mirror
- Automatic spritesheets and custom texturing for each voxel face.
- Paint surfaces seperately, controlling colour and texture
- Supports metallic, smoothness, transparency and emission on each voxel faces.
- Subdivision and resolution layering system that lets you mix and match different voxel grid resolutions.
- Import from MagicaVoxel .vox formats! (Experimental)

![select_delete](https://user-images.githubusercontent.com/5094696/120932524-1d3d3a00-c6ee-11eb-9400-3f863b56940a.gif)

![subdivide](https://user-images.githubusercontent.com/5094696/120932172-ab182580-c6ec-11eb-866c-f1f67644648a.gif)

![paintingSurfaces](https://user-images.githubusercontent.com/5094696/120931430-676fec80-c6e9-11eb-8b4d-e78bb99ba272.gif)

![VoxelText](https://user-images.githubusercontent.com/5094696/123529266-76ddc680-d6e6-11eb-8fde-90d6532e3a80.PNG)

- Voxel text from any font!

![VoxelSprite](https://user-images.githubusercontent.com/5094696/124587982-2b4abb80-de50-11eb-97fe-299020a11e27.PNG)

- Turn your sprites into voxel objects!
