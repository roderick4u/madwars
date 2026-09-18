import bpy
def show():
 scene=bpy.context.scene
 scene.frame_set(1)
 for window in bpy.context.window_manager.windows:
  for area in window.screen.areas:
   if area.type=='VIEW_3D':
    area.spaces.active.region_3d.view_perspective='CAMERA'
    area.spaces.active.overlay.show_overlays=False
    with bpy.context.temp_override(window=window,area=area):
     if not window.screen.is_animation_playing:bpy.ops.screen.animation_play()
    return None
 return 1
bpy.app.timers.register(show,first_interval=3)
