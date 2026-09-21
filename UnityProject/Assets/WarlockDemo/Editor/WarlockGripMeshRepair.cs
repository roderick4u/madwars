using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class WarlockGripMeshRepair {
 [MenuItem("Tools/Warlock/Repair Staff Grip")]
 public static void Apply(){ Debug.Log(Repair()); }
 public static string Repair(){
  if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
  var actor=GameObject.Find("Warlock Player");var rig=actor.GetComponent<WarlockStaffGrip>();rig.ApplyPose();
  var body=actor.GetComponentsInChildren<SkinnedMeshRenderer>().First(r=>r.bones.Any(b=>b&&b.name=="Hand.R"));
  int hand=Array.FindIndex(body.bones,b=>b&&b.name=="Hand.R");
  var source=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/WarlockDemo/WarlockBodyWithoutCape.asset");
  var vertices=source.vertices;var weights=source.boneWeights;var parents=Enumerable.Range(0,vertices.Length).ToArray();var weld=new Dictionary<Vector3Int,int>();
  for(int i=0;i<vertices.Length;i++){var key=Key(vertices[i]);if(weld.TryGetValue(key,out int other))Join(parents,i,other);else weld.Add(key,i);}
  var tris=source.triangles;for(int i=0;i<tris.Length;i+=3){Join(parents,tris[i],tris[i+1]);Join(parents,tris[i],tris[i+2]);}
  var toStaff=rig.staff.worldToLocalMatrix*rig.hand.localToWorldMatrix*source.bindposes[hand];var toMesh=toStaff.inverse;
  var staffVertices=rig.staff.GetComponent<MeshFilter>().sharedMesh.vertices;
  var reports=new List<string>();int count=0;
  foreach(var g in Enumerable.Range(0,vertices.Length).GroupBy(i=>Root(parents,i))){
   var ids=g.ToArray();if(!ids.All(i=>weights[i].boneIndex0==hand&&weights[i].weight0>.95f))continue;
   var ps=ids.Select(i=>vertices[i]*100f).ToArray();
   if(ps.Min(p=>p.x)<.695f||ps.Max(p=>p.x)<.740f||ps.Max(p=>p.x)>.820f||ps.Min(p=>p.z)<.910f||ps.Max(p=>p.z)>1.020f)continue;
   var unique=ids.GroupBy(i=>Key(vertices[i])).Select(k=>vertices[k.First()]).OrderBy(p=>p.x).ToArray();
   if(unique.Length!=24)throw new Exception("Unexpected finger topology");
   var old=new Vector3[4];var ringMap=new Dictionary<Vector3Int,int>();
   for(int k=0;k<24;k++){old[k/6]+=toStaff.MultiplyPoint3x4(unique[k])/6f;ringMap.Add(Key(unique[k]),k/6);}
   var slice=staffVertices.Where(p=>Mathf.Abs(p.y-old[0].y)<.018f).ToArray();
   if(slice.Length<6)throw new Exception("No handle slice near knuckle");
   var center=new Vector3((slice.Min(p=>p.x)+slice.Max(p=>p.x))*.5f,old[0].y,(slice.Min(p=>p.z)+slice.Max(p=>p.z))*.5f);
   float shaftRadius=Mathf.Max(slice.Max(p=>p.x)-slice.Min(p=>p.x),slice.Max(p=>p.z)-slice.Min(p=>p.z))*.5f;
   Vector3 radial=old[0]-center;radial.y=0;
   float angle=Mathf.Atan2(radial.z,radial.x);
   Vector3 first=old[1]-center;first.y=0;
   float direction=Mathf.Sign(Mathf.DeltaAngle(angle*Mathf.Rad2Deg,Mathf.Atan2(first.z,first.x)*Mathf.Rad2Deg));
   if(direction==0)direction=1;
   var next=new Vector3[4];next[0]=old[0];
   float[] sweep={0,50,100,155};float[] thick={.010f,.0095f,.008f,.0055f};
   for(int k=1;k<4;k++){float a=angle+direction*sweep[k]*Mathf.Deg2Rad;float radius=(shaftRadius+.0015f)/Mathf.Cos(27.5f*Mathf.Deg2Rad)+thick[k]/rig.staffScale;next[k]=center+new Vector3(Mathf.Cos(a)*radius,old[k].y-old[0].y,Mathf.Sin(a)*radius);}
   foreach(int i in ids){int ring=ringMap[Key(vertices[i])];if(ring==0)continue;var p=toStaff.MultiplyPoint3x4(vertices[i]);int lo=ring-1,hi=Mathf.Min(3,ring+1);Vector3 tangent=(next[hi]-next[lo]).normalized;var q=Quaternion.FromToRotation(old[hi]-old[lo],tangent);var offset=Vector3.ProjectOnPlane(q*(p-old[ring]),tangent).normalized*(thick[ring]/rig.staffScale);vertices[i]=toMesh.MultiplyPoint3x4(next[ring]+offset);}
   reports.Add("finger "+count+" knuckle="+old[0]+" shaft="+center+" radius="+shaftRadius+" direction="+direction);count++;
  }
  if(count!=4)throw new Exception("Expected four fingers, got "+count);
  var mesh=UnityEngine.Object.Instantiate(source);mesh.name="WarlockHoldingStaff";mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
  const string path="Assets/WarlockDemo/Staff/WarlockHoldingStaffClosed.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);Undo.RecordObject(body,"Finish staff grip");
  if(existing){Undo.RegisterCompleteObjectUndo(existing,"Finish staff grip mesh");EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
  body.sharedMesh=null;body.sharedMesh=existing;existing.vertices=vertices;existing.RecalculateNormals();existing.RecalculateTangents();existing.UploadMeshData(false);EditorUtility.SetDirty(body);EditorUtility.SetDirty(existing);EditorSceneManager.MarkSceneDirty(actor.scene);SceneView.RepaintAll();
  SceneView.lastActiveSceneView.LookAt(rig.hand.position,Quaternion.Euler(15,160,0),.28f);
  return string.Join("\n",reports);
 }
 static Vector3Int Key(Vector3 p){return new Vector3Int(Mathf.RoundToInt(p.x*1e7f),Mathf.RoundToInt(p.y*1e7f),Mathf.RoundToInt(p.z*1e7f));}
 static int Root(int[] p,int i){while(p[i]!=i){p[i]=p[p[i]];i=p[i];}return i;}
 static void Join(int[] p,int a,int b){p[Root(p,b)]=Root(p,a);}
}
