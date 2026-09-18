using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SetupFireballParticles
{
 const string Dir="Assets/WarlockDemo/Fireball/";
 public static string Build(){
  var shader=Shader.Find("Warlock/Fireball Fragments");if(!shader||ShaderUtil.ShaderHasError(shader))throw new System.Exception("Fragment shader failed");
  Store(new Material(shader){name="Fireball hard-edged fragments"},"FireballFragments.mat");
  var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var cubeMesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(cube);Store(cubeMesh,"FireballCube.asset");
  var corners=new[]{new Vector3(1,1,1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(1,-1,-1)};
  var order=new[]{0,2,1,0,1,3,0,3,2,1,2,3};var v=new Vector3[12];var indices=new int[12];
  for(int i=0;i<12;i++){v[i]=corners[order[i]]*.45f;indices[i]=i;}
  var tetra=new Mesh{name="Four-faced spark"};tetra.vertices=v;tetra.triangles=indices;tetra.RecalculateNormals();tetra.RecalculateBounds();Store(tetra,"FireballTetrahedron.asset");
  var root=PrefabUtility.LoadPrefabContents(Dir+"Fireball.prefab");
  try{
   foreach(string name in new[]{"Angular Trail","Spinning Sparks"}){var old=root.transform.Find(name);if(old)Object.DestroyImmediate(old.gameObject);}
   Add(root,"Angular Trail","FireballCube.asset",false);
   Add(root,"Spinning Sparks","FireballTetrahedron.asset",true);
   PrefabUtility.SaveAsPrefabAsset(root,Dir+"Fireball.prefab");
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  return "Saved mesh particles: cube trail max 40, tetrahedron sparks max 24; world space, linear shrink, random XYZ spin, opaque material.";
 }
 static void Add(GameObject root,string name,string meshName,bool spark){
  var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(0,0,spark?-.13f:-.28f);go.transform.localRotation=Quaternion.Euler(0,180,0);
  var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  var main=ps.main;main.loop=true;main.duration=2;main.playOnAwake=true;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=spark?24:40;
  main.startLifetime=new ParticleSystem.MinMaxCurve(spark?.25f:.30f,spark?.52f:.58f);
  main.startSpeed=new ParticleSystem.MinMaxCurve(spark?1.4f:.8f,spark?2.2f:1.4f);
  main.startSize=new ParticleSystem.MinMaxCurve(spark?.035f:.095f,spark?.075f:.17f);
  main.startRotation3D=true;main.startRotationX=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startRotationY=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);main.startRotationZ=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
  main.startColor=new ParticleSystem.MinMaxGradient(new Color(1,.24f,.012f),new Color(1,.83f,.10f));main.gravityModifier=spark?.065f:0;
  var emission=ps.emission;emission.rateOverTime=spark?22:48;
  var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=spark?22:9;shape.radius=spark?.12f:.075f;
  var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
  var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.separateAxes=true;rotation.x=new ParticleSystem.MinMaxCurve(-13,13);rotation.y=new ParticleSystem.MinMaxCurve(-17,17);rotation.z=new ParticleSystem.MinMaxCurve(-15,15);
  var colors=ps.colorOverLifetime;colors.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(new Color(1,.55f,.25f),.45f),new GradientColorKey(new Color(.8f,.12f,.025f),1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,1)});colors.color=gradient;
  var renderer=go.GetComponent<ParticleSystemRenderer>();renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Dir+meshName);renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Dir+"FireballFragments.mat");renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
  ps.useAutoRandomSeed=true;
 }
 static void Store(Object o,string name){var old=AssetDatabase.LoadAssetAtPath<Object>(Dir+name);if(old)EditorUtility.CopySerialized(o,old);else AssetDatabase.CreateAsset(o,Dir+name);}
}
