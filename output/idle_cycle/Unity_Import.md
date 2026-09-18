# Warlock idle

Clip: Idle_Breathing. Four-second in-place loop, 30 fps, 120 frame intervals plus the repeated endpoint. Same 27-bone skeleton, 5,097 triangles and shared material as the locomotion assets.

Import Warlock_Idle.fbx with its textures. Use Rig: Generic, Root node: Root. Enable Loop Time, retain the full exported clip, and disable Animator Apply Root Motion. Rename the imported animation Idle_Breathing if the importer adds a prefix.

Includes subtle breathing, a small weight shift, relaxed arm motion and slight cape movement. Both feet stay planted. No cloth simulation is needed.

The Blender file retains the preceding walk and run actions; the FBX contains only idle. Unity runtime transitions and device performance have not been tested.

The ORM texture stores occlusion in R, roughness in G and metallic in B. Repack it before assigning Unity metallic/smoothness slots. Mark the normal PNG as a Normal map.
