using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
public static class SetupWarlockStaff {
 const string Dir="Assets/WarlockDemo/Staff/";
 public static string Build(){
  var actor=GameObject.Find("Warlock Player");var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.bones.Length>5);
  var hand=body.bones.First(b=>b.name=="Hand.R");int h=System.Array.IndexOf(body.bones,hand);
  var importer=(ModelImporter)AssetImporter.GetAtPath(Dir+"Warlock_Staff.fbx");importer.animationType=ModelImporterAnimationType.None;importer.importAnimation=false;importer.SaveAndReimport();
  var old=actor.transform.Find("Equipped Staff");if(old)Object.DestroyImmediate(old.gameObject);
  var root=new GameObject("Equipped Staff");root.transform.SetParent(actor.transform,false);
  var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"Warlock_Staff.fbx"));model.transform.SetParent(root.transform,false);
  // The FBX object transform includes its Blender grip origin; cancel that offset here.
  var prop=model.GetComponentInChildren<MeshFilter>();
  var meshCopy=Object.Instantiate(prop.sharedMesh);var vertices=meshCopy.vertices;
  var toRoot=root.transform.worldToLocalMatrix*prop.transform.localToWorldMatrix;
  for(int i=0;i<vertices.Length;i++) vertices[i]=toRoot.MultiplyPoint3x4(vertices[i])-new Vector3(0,.72f,0);
  meshCopy.vertices=vertices;meshCopy.RecalculateNormals();meshCopy.RecalculateBounds();
  Store(meshCopy,"StaffGripMesh.asset");Object.DestroyImmediate(model);
  root.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"StaffGripMesh.asset");var renderer=root.AddComponent<MeshRenderer>();
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name="Staff Wood PBR";mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Staff_BaseColor.png"));mat.SetFloat("_Smoothness",.18f);
  var normal=(TextureImporter)AssetImporter.GetAtPath(Dir+"Staff_Normal.png");normal.textureType=TextureImporterType.NormalMap;normal.SaveAndReimport();
  mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Staff_Normal.png"));mat.SetFloat("_BumpScale",.3f);mat.EnableKeyword("_NORMALMAP");mat.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Dir+"Staff_Emission.png"));mat.SetColor("_EmissionColor",Color.white*.8f);mat.EnableKeyword("_EMISSION");mat.SetFloat("_Cull",0);Store(mat,"StaffWood.mat");renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"StaffWood.mat");
  // Curl the existing low-poly fingers further around the handle. Preserve the original mesh asset.
  var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/WarlockDemo/WarlockBodyWithoutCape.asset");var gripMesh=Object.Instantiate(source);var v=gripMesh.vertices;var weights=gripMesh.boneWeights;
  for(int i=0;i<v.Length;i++){
   if(weights[i].boneIndex0!=h||weights[i].weight0<.95f)continue;
   var p=v[i]*100;
   if(p.x>.714f){p.x=.714f+(p.x-.714f)*.68f;p.z=.997f+(p.z-.997f)*1.5f;v[i]=p/100;}
  }
  gripMesh.vertices=v;gripMesh.RecalculateNormals();gripMesh.RecalculateBounds();Store(gripMesh,"WarlockHoldingStaff.asset");body.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"WarlockHoldingStaff.asset");
  var rig=actor.GetComponent<WarlockStaffGrip>();if(!rig)rig=actor.AddComponent<WarlockStaffGrip>();rig.upperArm=body.bones.First(b=>b.name=="UpperArm.R");rig.forearm=body.bones.First(b=>b.name=="LowerArm.R");rig.hand=hand;rig.chest=body.bones.First(b=>b.name=="Chest");rig.staff=root.transform;rig.animator=actor.GetComponentInChildren<Animator>();
  rig.gripInHand=source.bindposes[h].MultiplyPoint3x4(new Vector3(.00730f,.00016f,.00969f));
  rig.handBasis=Quaternion.LookRotation(Vector3.left,Vector3.up)*Quaternion.Inverse(source.bindposes[h].rotation);
  rig.ApplyPose();EditorSceneManager.MarkSceneDirty(actor.scene);EditorSceneManager.SaveScene(actor.scene);AssetDatabase.SaveAssets();return "Equipped staff; grip mesh and arm pose saved. Bounds "+root.GetComponent<MeshFilter>().sharedMesh.bounds;
 }
 static void Store(Object o,string name){var old=AssetDatabase.LoadAssetAtPath<Object>(Dir+name);if(old)EditorUtility.CopySerialized(o,old);else AssetDatabase.CreateAsset(o,Dir+name);}
}
