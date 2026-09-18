# Warlock walk cycle

Import `Warlock_Walk.fbx` into Unity with the PNG textures in the same folder.

- Rig: **Generic**, Root node: **Root**. This preserves the authored cape and robe bones. Humanoid retargeting has not been configured or tested.
- Animation: enable **Import Animation** and **Loop Time**. Rename the imported clip to `Walk_InPlace` if the FBX importer adds a prefix.
- Keep the full exported clip: 32 frame intervals at 30 fps, about 1.067 seconds. The final key repeats the first pose.
- This is an **in-place** cycle: disable Apply Root Motion on the Animator and move the character with your game controller.
- Use the base-color PNG with your Unity render pipeline's lit shader. Mark the normal texture as a Normal map. The ORM texture stores occlusion in R, roughness in G, metallic in B; it is not a Unity metallic/smoothness map and needs repacking for that slot.
- 5,097 triangles, one skinned mesh, one material. Up to two bone influences per vertex. The fingers are weighted to the hand bones and do not have separate animation controls.

Validated in Blender and by reimporting the FBX. Not yet tested inside your Unity project or on a mobile device. Clothing motion is bone animation, not cloth simulation.

Unity documentation: https://docs.unity.com/en-us/engine/6000.7/manual/assets-and-media/asset-types/models/importing/fbximporter-rig

Loop import settings: https://docs.unity.com/en-us/engine/6000.7/manual/assets-and-media/asset-types/models/importing/class-animation-clip
