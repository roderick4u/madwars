using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
public static class SetupFireball {
 const string Dir="Assets/WarlockDemo/Fireball/";
 public static string Build(){
  // A simplified sphere with seven staggered rings stretched into a trailing point.
  float[] z={-.46f,-.32f,-.18f,-.055f,.065f,.16f,.22f};
  float[] radius={.042f,.083f,.136f,.188f,.195f,.153f,.080f};
  var points=new List<Vector3>{new Vector3(0,0,-.61f)};
  const int sides=10;
  for(int r=0;r<z.Length;r++)for(int c=0;c<sides;c++){
   float a=(c+(r%2)*.5f)*Mathf.PI*2/sides;
   float asym=1+.055f*Mathf.Sin(c*2.4f+r);
   points.Add(new Vector3(Mathf.Cos(a)*radius[r]*asym,Mathf.Sin(a)*radius[r]*asym,z[r]));
  }
  int nose=points.Count;points.Add(new Vector3(0,0,.25f));var tris=new List<int>();
  for(int c=0;c<sides;c++)tris.AddRange(new[]{0,1+(c+1)%sides,1+c});
  for(int r=0;r<z.Length-1;r++)for(int c=0;c<sides;c++){int a=1+r*sides+c,b=1+r*sides+(c+1)%sides;tris.AddRange(new[]{a,b,a+sides,b,b+sides,a+sides});}
  for(int c=0;c<sides;c++)tris.AddRange(new[]{nose,1+6*sides+c,1+6*sides+(c+1)%sides});
  // Split triangle corners: no smoothing and no welded-normal artifacts on import.
  var vertices=new Vector3[tris.Count];var indices=new int[tris.Count];
  for(int i=0;i<tris.Count;i++){vertices[i]=points[tris[i]];indices[i]=i;}
  var mesh=new Mesh{name="Fireball Flat 140 triangles"};mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.bounds=new Bounds(new Vector3(0,0,-.18f),new Vector3(.56f,.56f,1.02f));Store(mesh,"FireballMesh.asset");
  var shader=Shader.Find("Warlock/Fireball Toon");if(!shader||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Fireball shader compilation failed");
  var mat=new Material(shader){name="Fireball Toon Orange"};mat.enableInstancing=true;Store(mat,"FireballToon.mat");
  var prop=new GameObject("Fireball");prop.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+"FireballMesh.asset");var renderer=prop.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"FireballToon.mat");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  var prefab=PrefabUtility.SaveAsPrefabAsset(prop,Dir+"Fireball.prefab");Object.DestroyImmediate(prop);
  var old=GameObject.Find("Fireball Preview");if(old)Object.DestroyImmediate(old);
  var demo=(GameObject)PrefabUtility.InstantiatePrefab(prefab);demo.name="Fireball Preview";var preview=demo.AddComponent<FireballPreview>();preview.follow=GameObject.Find("Warlock Player").transform;demo.transform.position=preview.follow.TransformPoint(preview.offset);demo.transform.rotation=preview.follow.rotation;
  EditorSceneManager.MarkSceneDirty(demo.scene);EditorSceneManager.SaveScene(demo.scene);AssetDatabase.SaveAssets();return "Fireball prefab: 140 triangles, 420 flat corners, 72 unique positions, 1 material, GPU animation, local +Z flight / -Z tail.";
 }
 static void Store(Object o,string name){var old=AssetDatabase.LoadAssetAtPath<Object>(Dir+name);if(old)EditorUtility.CopySerialized(o,old);else AssetDatabase.CreateAsset(o,Dir+name);}
}
