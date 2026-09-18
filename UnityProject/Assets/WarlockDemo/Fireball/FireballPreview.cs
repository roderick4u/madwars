using UnityEngine;

// Presentation only; remove this component when using the prefab as a projectile.
public class FireballPreview : MonoBehaviour
{
 public Transform follow;
 public Vector3 offset=new Vector3(-.7f,1.05f,.55f);
 public float flightDistance=2.6f;
 public float flightSpeed=3.5f;
 float traveled;
 Vector3 origin,direction;
 bool started;
 TrailRenderer[] trails;
 ParticleSystem[] particles;
 void OnEnable(){started=false;trails=GetComponentsInChildren<TrailRenderer>();particles=GetComponentsInChildren<ParticleSystem>();}
 void LateUpdate(){
  if(!follow)return;
  traveled+=Time.deltaTime*flightSpeed;
  if(!started||traveled>=flightDistance){
   started=true;traveled=0;origin=follow.TransformPoint(offset);direction=follow.forward;
   transform.position=origin;transform.rotation=Quaternion.LookRotation(direction);
   foreach(var trail in trails)trail.Clear();
   foreach(var particle in particles)particle.Clear();
  }
  transform.position=origin+direction*traveled;
 }
}
