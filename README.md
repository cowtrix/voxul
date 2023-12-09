# 𝕧𝕠𝕩𝕦𝕝 - a Voxel Framework & Editor Tool

*voxul* is a voxel system and editor tool for [Unity 3D.](https://unity.com/)

![island-1](https://user-images.githubusercontent.com/5094696/149266031-c3606c4f-bbb0-4726-8692-45b26ac7ef25.png)

This project consists of both a runtime framework for handling and rendering voxel data, and an editor tool for painting and modifying voxel data from within Unity.

## Feature Showcase

![Bench_FaceMerging](https://user-images.githubusercontent.com/5094696/149459399-85eebd4f-729c-4093-988e-853b86874395.gif)![DifferentSurfaceMaterials](https://user-images.githubusercontent.com/5094696/149460110-ebd04cf3-7407-4164-8f15-58fc6ff1d13f.gif)
![VoxelPainterUI](https://user-images.githubusercontent.com/5094696/149496255-ca06bae6-c1ca-4a9e-ab6d-8f86a4a5462e.png)
![DifferentPlaneModes](https://user-images.githubusercontent.com/5094696/149496273-9b6c8d77-05a4-4ff3-9a3c-b4d0f59cf8ba.png)

## Games Using Voxul

### Cold Weld

[![ColdWeld](https://user-images.githubusercontent.com/5094696/149497037-9f3be769-4bc9-48f3-8332-a1018a01462b.png)](https://cow-trix.itch.io/cold-weld)

### Island

[![Island](https://user-images.githubusercontent.com/5094696/149497135-009c6447-b803-4ff0-aa48-59a4347633fa.png)](https://cow-trix.itch.io/island-1)

### [Dritch](https://cow-trix.itch.io/dritch)

![05](https://github.com/cowtrix/voxul/assets/5094696/08424b5e-d598-4be8-aca9-fbb70e1226ea)
![02](https://github.com/cowtrix/voxul/assets/5094696/27c7f0d0-98d2-4a3f-88a1-c14ef8c612b5)
![06](https://github.com/cowtrix/voxul/assets/5094696/f1516238-8161-4fbf-ab1f-753c210c4edd)
![03](https://github.com/cowtrix/voxul/assets/5094696/ed26fa01-dedd-4252-a92c-4e459169fd3c)

## Getting Started

### Requirements

- Unity 2020.3.2f1 or later
    - Your project must include both the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@11.0/manual/index.html) & [Shadergraph](https://unity.com/shader-graph)

Generally this project will be kept working on the [Unity LTS](https://unity3d.com/unity/qa/lts-releases) version. If you need to stay pinned to a specific release, or require support for an alpha release, you are strongly recommended to fork this project and integrate further commits yourself.

### Installation

For most users it is recommended to take the [latest release](https://github.com/cowtrix/voxul/releases), which is in a `.unitypackage`. You can either import this package by dragging it onto your Project view in Unity, or by using the `Assets > Import Package > Custom Package...` menu.

Alternatively, you can clone the repository directly into your project. If you are using git as your source control you can just have it as a submodule within your `Assets` folder. This is the best option if you want to work on the latest version of voxul at all times.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
