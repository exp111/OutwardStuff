SL Guide: https://sinai-dev.github.io/OSLDocs/#/Guides/ItemVisuals?id=custom-visual-prefabs
Install Unity 2018.4.8 (https://unity3d.com/get-unity/download/archive)
### ThunderKit
ThunderKit to easily create asset bundles (https://github.com/PassivePicasso/ThunderKit):
- add `"com.passivepicasso.thunderkit":"https://github.com/PassivePicasso/ThunderKit.git",` to the `manifest.json` in `<ProjectFolder>\Packages`
- also add `"com.unity.scriptablebuildpipeline": "1.18.0",
 "com.unity.test-framework.performance": "1.0.9-preview"` ?
 - in unity add `Manifest` with a `AssetBundleDefinition` (By clicking "Add Manifest Datum") and configure
 - then add that to a `Pipeline` with a `StageAssetBundles` job

ScriptableBuildPipeline versions: https://docs.unity3d.com/2018.4/Documentation/Manual/com.unity.scriptablebuildpipeline.html

### Creating the AssetBundle
https://sinai-dev.github.io/OSLDocs/#/Guides/ItemVisuals?id=creating-the-prefab
- Create game object from model
- Add texture if necessary
- Needs a MeshRenderer/SkinnedMeshRenderer
- Add a BoxCollider for world items, leave it for equipped items
- Move game object back to Assets to make it a prefab

Put assetbundle into `SideLoader\AssetBundles\`

### SLPack
https://sinai-dev.github.io/OSLDocs/#/Basics/SLPacks
Path like ``Outward\BepInEx\plugins\{Name}\SideLoader\``

### Item
https://sinai-dev.github.io/OSLDocs/#/Guides/Items
Generate Template from item

In `ItemVisuals`
Unset `ResourcesPrefabPath` (do we need this)
Set `Prefab_SLPack` to your SLPack name (`MyPack`), is this necessary?
Set `Prefab_AssetBundle` to `mybundle` (if `MyPack/AssetBundles/mybundle`)
Set `Prefab_Name` to GameObject name
Maybe do the same for `SpecialItemVisuals` if you want a equipped version
Probably also add a texture or material
