using UnityEngine;

// Local two-bone arm solve; the existing torso and free-arm animation remain active.
[DefaultExecutionOrder(80)]
public class WarlockStaffGrip : MonoBehaviour
{
 public Transform upperArm,forearm,hand,chest,staff;
 public Animator animator;
 public Vector3 gripInHand;
 public Quaternion handBasis;
 public float staffScale=.9f;
 void LateUpdate(){ApplyPose();}
 public void ApplyPose(){
  if(!hand||!staff||!upperArm||!forearm)return;
  var cast=GetComponent<WarlockFireballCast>();
  if(cast && cast.IsCasting){
   staff.position=hand.TransformPoint(gripInHand);
   staff.rotation=hand.rotation*Quaternion.Inverse(handBasis);
   staff.localScale=Vector3.one*staffScale;
   return;
  }
  var state=animator.GetCurrentAnimatorStateInfo(0);
  Vector3 reference=ReferenceHand(state);
  float activity=Activity(state), follow=Follow(state);
  if(animator.IsInTransition(0)){
   var next=animator.GetNextAnimatorStateInfo(0);
   float blend=Mathf.Clamp01(animator.GetAnimatorTransitionInfo(0).normalizedTime);
   reference=Vector3.Lerp(reference,ReferenceHand(next),blend);
   activity=Mathf.Lerp(activity,Activity(next),blend);
   follow=Mathf.Lerp(follow,Follow(next),blend);
  }
  // Read the Animator's right-hand pose before IK, keeping its actual timing and transitions.
  Vector3 animatedHand=transform.InverseTransformVector(hand.position-chest.position);
  Vector3 delta=animatedHand-reference;
  Vector3 swing=new Vector3(Mathf.Clamp(delta.x*.3f,-.012f,.012f),
   Mathf.Clamp(delta.y*.12f,-.016f,.016f),Mathf.Clamp(delta.z*follow,-.045f,.045f));
  Vector3 chestLocal=transform.InverseTransformPoint(chest.position);
  Vector3 target=transform.TransformPoint(new Vector3(.29f,chestLocal.y-.035f,.10f)+swing);
  Vector3 a=upperArm.position;float l1=Vector3.Distance(a,forearm.position),l2=Vector3.Distance(forearm.position,hand.position);
  Vector3 axis=target-a;float distance=Mathf.Clamp(axis.magnitude,.02f,l1+l2-.002f);axis.Normalize();
  float along=(l1*l1-l2*l2+distance*distance)/(2*distance);
  Vector3 pole=transform.TransformDirection(new Vector3(.3f,-1,-.25f));pole=(pole-axis*Vector3.Dot(pole,axis)).normalized;
  Vector3 elbow=a+axis*along+pole*Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
  upperArm.rotation=Quaternion.FromToRotation(forearm.position-a,elbow-a)*upperArm.rotation;
  forearm.rotation=Quaternion.FromToRotation(hand.position-forearm.position,target-forearm.position)*forearm.rotation;
  Quaternion tilt=Quaternion.Euler(-7-activity*7-swing.z*55,0,-5-swing.x*50);
  hand.rotation=transform.rotation*tilt*handBasis;
  staff.position=hand.TransformPoint(gripInHand);
  staff.rotation=transform.rotation*tilt;
  staff.localScale=Vector3.one*staffScale;
 }
 static float Activity(AnimatorStateInfo state){return state.IsName("Run")?1:state.IsName("Walk")?.4f:0;}
 static float Follow(AnimatorStateInfo state){return state.IsName("Run")?.15f:state.IsName("Walk")?.32f:.25f;}
 // Cycle averages, in character space relative to the chest, from the current source clips.
 static Vector3 ReferenceHand(AnimatorStateInfo state){
  if(state.IsName("Run"))return new Vector3(.2765f,-.1407f,.1175f);
  if(state.IsName("Walk"))return new Vector3(.2978f,-.2732f,.0201f);
  return new Vector3(.2941f,-.2761f,.0464f);
 }
}
