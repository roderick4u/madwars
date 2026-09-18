using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
public static class SetupWarlockCape
{
 const string Folder="Assets/WarlockDemo/";
 public static string Build() {
  var actor=GameObject.Find("Warlock Player");
  var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(s=>s.name!="Warlock Cloth Cape");
  var old=actor.transform.Find("Warlock Cloth Cape"); if(old) Object.DestroyImmediate(old.gameObject);
  foreach(var t in actor.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Cape Collision")).ToArray()) Object.DestroyImmediate(t.gameObject);
  var source=body.sharedMesh; var weights=source.boneWeights;
  var capeBones=new HashSet<int>(Enumerable.Range(0,body.bones.Length).Where(i=>body.bones[i].name.StartsWith("Cape.")));
  System.Func<int,bool> cape=i=> (weights[i].weight0>.01f&&capeBones.Contains(weights[i].boneIndex0))||(weights[i].weight1>.01f&&capeBones.Contains(weights[i].boneIndex1))||(weights[i].weight2>.01f&&capeBones.Contains(weights[i].boneIndex2))||(weights[i].weight3>.01f&&capeBones.Contains(weights[i].boneIndex3));
  var triangles=source.triangles; var kept=new List<int>();
  for(int i=0;i<triangles.Length;i+=3) if(!cape(triangles[i])&&!cape(triangles[i+1])&&!cape(triangles[i+2])) kept.AddRange(new[]{triangles[i],triangles[i+1],triangles[i+2]});
  var used=kept.Distinct().OrderBy(i=>i).ToArray(); var map=used.Select((v,i)=>new{v,i}).ToDictionary(x=>x.v,x=>x.i);
  var clean=new Mesh{name="Warlock body without rigid cape"};
  clean.vertices=used.Select(i=>source.vertices[i]).ToArray(); clean.normals=used.Select(i=>source.normals[i]).ToArray(); clean.uv=used.Select(i=>source.uv[i]).ToArray(); clean.boneWeights=used.Select(i=>weights[i]).ToArray(); clean.bindposes=source.bindposes; clean.triangles=kept.Select(i=>map[i]).ToArray(); clean.RecalculateBounds(); clean.RecalculateTangents();
  Save(clean,"WarlockBodyWithoutCape.asset"); body.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"WarlockBodyWithoutCape.asset");
  var go=new GameObject("Warlock Cloth Cape"); go.transform.SetParent(actor.transform,false);
  var chest=body.bones.First(b=>b.name=="Chest");
  int cols=13,rows=17; var verts=new Vector3[cols*rows]; var uv=new Vector2[verts.Length]; var bw=new BoneWeight[verts.Length]; var indices=new List<int>();
  for(int r=0;r<rows;r++) for(int c=0;c<cols;c++) {float t=r/(float)(rows-1),u=c/(float)(cols-1),x=u*2-1; int i=r*cols+c;
   verts[i]=new Vector3(x*Mathf.Lerp(.145f,.285f,t),Mathf.Lerp(1.015f,.19f,t),-Mathf.Lerp(.18f,.23f,t)-.008f*Mathf.Sin(u*Mathf.PI*6)*t);
   uv[i]=new Vector2(u,1-t); bw[i]=new BoneWeight{boneIndex0=0,weight0=1};
   if(r<rows-1&&c<cols-1){indices.AddRange(new[]{i,i+cols,i+1,i+1,i+cols,i+cols+1});}
  }
  var mesh=new Mesh{name="Cape 384 triangles"}; mesh.vertices=verts;mesh.uv=uv;mesh.boneWeights=bw;mesh.bindposes=new[]{chest.worldToLocalMatrix*go.transform.localToWorldMatrix};mesh.triangles=indices.ToArray();mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();Save(mesh,"WarlockClothCape.asset");
  var tex=new Texture2D(256,256,TextureFormat.RGB24,false); for(int y=0;y<256;y++)for(int x=0;x<256;x++){bool edge=x<9||x>246||y<9;float shade=.94f+.06f*Mathf.Cos(x*.075f);tex.SetPixel(x,y,edge?new Color(.59f,.47f,.21f):new Color(.20f,.115f,.285f)*shade);} tex.Apply();System.IO.File.WriteAllBytes(Folder+"Cape_BaseColor.png",tex.EncodeToPNG());Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(Folder+"Cape_BaseColor.png");
  var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Cape purple and gold"};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Cape_BaseColor.png"));mat.SetFloat("_Cull",0);mat.SetFloat("_Smoothness",.16f);Save(mat,"WarlockClothCape.mat");
  var skin=go.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"WarlockClothCape.asset");skin.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"WarlockClothCape.mat");skin.bones=new[]{chest};skin.rootBone=chest;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(new Vector3(0,.65f,-.15f),new Vector3(1.4f,1.6f,1.4f));
  var colliders=new List<CapsuleCollider>();
  colliders.Add(Capsule(chest,actor.transform.TransformPoint(new Vector3(0,.58f,-.01f)),actor.transform.TransformPoint(new Vector3(0,.98f,-.01f)),.145f,"Torso"));
  foreach(string side in new[]{"L","R"}) {var upper=body.bones.First(b=>b.name=="UpperArm."+side);var lower=body.bones.First(b=>b.name=="LowerArm."+side);var hand=body.bones.First(b=>b.name=="Hand."+side);colliders.Add(Capsule(upper,upper.position,lower.position,.052f,"Upper "+side));colliders.Add(Capsule(lower,lower.position,hand.position,.044f,"Lower "+side));}
  foreach(string side in new[]{"L","R"}) {var thigh=body.bones.First(b=>b.name=="UpperLeg."+side);var knee=body.bones.First(b=>b.name=="LowerLeg."+side);var foot=body.bones.First(b=>b.name=="Foot."+side);colliders.Add(Capsule(thigh,thigh.position,knee.position,.075f,"Thigh "+side));colliders.Add(Capsule(knee,knee.position,foot.position,.065f,"Shin "+side));} var cloth=go.AddComponent<Cloth>();cloth.capsuleColliders=colliders.ToArray();cloth.useGravity=true;cloth.stretchingStiffness=1;cloth.bendingStiffness=.25f;cloth.damping=.45f;cloth.friction=.25f;cloth.worldVelocityScale=.08f;cloth.worldAccelerationScale=.025f;cloth.clothSolverFrequency=120;cloth.enableContinuousCollision=true;cloth.useTethers=true;
  var coef=new ClothSkinningCoefficient[verts.Length];for(int i=0;i<coef.Length;i++){float t=(i/cols)/(float)(rows-1);coef[i].maxDistance=t==0?0:Mathf.Lerp(.015f,.42f,t*t);coef[i].collisionSphereDistance=0;}cloth.coefficients=coef;
  EditorSceneManager.MarkSceneDirty(actor.scene);EditorSceneManager.SaveScene(actor.scene);AssetDatabase.SaveAssets();return "Body "+kept.Count/3+" tris; cape "+indices.Count/3+" tris, "+verts.Length+" simulated vertices; 9 capsule colliders.";
 }
 static CapsuleCollider Capsule(Transform parent,Vector3 a,Vector3 b,float radius,string label){var go=new GameObject("Cape Collision "+label);go.layer=2;go.transform.SetParent(parent,false);go.transform.position=(a+b)*.5f;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);go.transform.localScale=new Vector3(1/parent.lossyScale.x,1/parent.lossyScale.y,1/parent.lossyScale.z);var c=go.AddComponent<CapsuleCollider>();c.direction=1;c.radius=radius;c.height=Vector3.Distance(a,b)+radius*2;c.isTrigger=true;return c;}
 static void Save(Object obj,string name){var existing=AssetDatabase.LoadAssetAtPath<Object>(Folder+name);if(existing){EditorUtility.CopySerialized(obj,existing);Object.DestroyImmediate(obj);}else AssetDatabase.CreateAsset(obj,Folder+name);}
}
