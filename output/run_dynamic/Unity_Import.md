# Warlock Run

Use `Warlock_Run.fbx` with the included textures. The run uses the same 27-bone skeleton as the walk.

- Clip: `Run_Dynamic_InPlace`, 0.8 seconds, 30 fps, 24 frame intervals plus repeated endpoint.
- Rig: Generic; root bone: Root. Enable Loop Time and disable Animator Apply Root Motion.
- This is an in-place run; your game controller supplies forward movement.
- The Blender file keeps the earlier walk as an additional action. The FBX exports only the active run.
- 5,097 triangles, one material, one skinned mesh. Cape and robe movement is bone animation.
- The ORM texture stores occlusion in R, roughness in G and metallic in B; repack before using Unity metallic/smoothness slots.

Loop and export validated in Blender. Not yet tested in the Unity project or on a phone.
