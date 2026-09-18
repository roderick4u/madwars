using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public static class SetupFireballFinish {
 const string Dir="Assets/WarlockDemo/Fireball/";
 public static string Build(){
  var urp=UniversalRenderPipeline.asset;if(!urp)throw new System.Exception("URP required");
  var root=PrefabUtility.LoadPrefabContents(Dir+"Fireball.prefab");
  try{
   var material=AssetDatabase.LoadAssetAtPath<Material>(Dir+"FireballToon.mat");material.EnableKeyword("_EMISSION");material.SetFloat("_Emission",1);material.SetFloat("_EmissionIntensity",3.5f);EditorUtility.SetDirty(material);
   var trailMat=AssetDatabase.LoadAssetAtPath<Material>(Dir+"FireballTrail.mat");if(!trailMat){trailMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(trailMat,Dir+"FireballTrail.mat");}
   trailMat.SetColor("_BaseColor",new Color(1.5f,.30f,.008f,1));trailMat.SetFloat("_Surface",0);trailMat.SetFloat("_Cull",0);EditorUtility.SetDirty(trailMat);
   var old=root.transform.Find("Solid Speed Trail");if(old)Object.DestroyImmediate(old.gameObject);
   var go=new GameObject("Solid Speed Trail");go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(0,0,-.07f);
   var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMat;trail.time=.36f;trail.minVertexDistance=.065f;trail.widthMultiplier=.39f;trail.widthCurve=AnimationCurve.Linear(0,1,1,0);trail.numCornerVertices=0;trail.numCapVertices=0;trail.alignment=LineAlignment.View;trail.emitting=true;trail.autodestruct=false;trail.shadowCastingMode=ShadowCastingMode.Off;trail.receiveShadows=false;trail.generateLightingData=false;
   old=root.transform.Find("Fireball Warm Light");if(old)Object.DestroyImmediate(old.gameObject);
   go=new GameObject("Fireball Warm Light");go.transform.SetParent(root.transform,false);var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.48f,.08f);light.intensity=2.8f;light.range=2.4f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
   PrefabUtility.SaveAsPrefabAsset(root,Dir+"Fireball.prefab");
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Dir+"FireballBloom.asset");if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Dir+"FireballBloom.asset");}
  if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
  bloom.active=true;bloom.threshold.Override(1.0f);bloom.intensity.Override(.65f);bloom.scatter.Override(.5f);bloom.clamp.Override(5);bloom.highQualityFiltering.Override(false);bloom.maxIterations.Override(4);
  if(!profile.TryGet<Tonemapping>(out var tone)){tone=profile.Add<Tonemapping>();AssetDatabase.AddObjectToAsset(tone,profile);}tone.mode.Override(TonemappingMode.Neutral);
  EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(tone);EditorUtility.SetDirty(profile);
  var volumeGO=GameObject.Find("Fireball Bloom Volume");if(!volumeGO)volumeGO=new GameObject("Fireball Bloom Volume");volumeGO.layer=0;var vol=volumeGO.GetComponent<Volume>();if(!vol)vol=volumeGO.AddComponent<Volume>();vol.isGlobal=true;vol.enabled=true;vol.weight=1;vol.priority=5;vol.sharedProfile=profile;
  var cam=Camera.main;cam.allowHDR=true;var data=cam.GetComponent<UniversalAdditionalCameraData>();if(!data)data=cam.gameObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask|=1;data.renderType=CameraRenderType.Base;
  if(!urp.supportsHDR){urp.supportsHDR=true;EditorUtility.SetDirty(urp);}
  EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);EditorSceneManager.SaveScene(cam.gameObject.scene);AssetDatabase.SaveAssets();return "Solid taper .39m -> 0 / .25sec, warm point light, HDR emission 2.6, Bloom .38, Neutral tone; saved.";
 }
}
