using UnityEngine;
using UnityEngine.InputSystem;

public class WarlockDemoMovement : MonoBehaviour
{
    public Animator animator;
    public Camera viewCamera;
    public bool autoDemo = true;
    public float speed;
    public string locomotion = "Idle";
    public Vector3 destination;
    bool hasDestination;
    float demoTime;
    readonly Vector3[] points = { new Vector3(-3,0,-2), new Vector3(3,0,-2), new Vector3(3,0,3), new Vector3(-3,0,3) };
    int waypoint;
    Vector3 cameraOffset = new Vector3(8,11,-8);
    void Start() { destination = points[0]; hasDestination = true; }
    public void MoveTo(Vector3 target) { autoDemo=false; destination=new Vector3(target.x,0,target.z); hasDestination=true; }
    void Update()
    {
        var keyboard=Keyboard.current;
        if(keyboard!=null && keyboard.spaceKey.wasPressedThisFrame) { autoDemo=!autoDemo; demoTime=0; hasDestination=false; }
        Vector2 input=Vector2.zero;
        if(keyboard!=null) {
            input.x=(keyboard.dKey.isPressed?1:0)-(keyboard.aKey.isPressed?1:0);
            input.y=(keyboard.wKey.isPressed?1:0)-(keyboard.sKey.isPressed?1:0);
        }
        if(Mouse.current!=null && Mouse.current.rightButton.wasPressedThisFrame) {
            Ray ray=viewCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance)) MoveTo(ray.GetPoint(distance));
        }
        Vector3 direction=Vector3.zero;
        bool walking=keyboard!=null && keyboard.leftShiftKey.isPressed;
        if(input.sqrMagnitude>.01f) {
            autoDemo=false; hasDestination=false;
            Vector3 forward=viewCamera.transform.forward; forward.y=0; forward.Normalize();
            Vector3 right=viewCamera.transform.right; right.y=0; right.Normalize();
            direction=(forward*input.y+right*input.x).normalized;
        } else {
            if(autoDemo) {
                demoTime+=Time.deltaTime;
                float phase=demoTime%14f;
                walking=phase<7;
                if(phase<2 || phase>12) hasDestination=false;
                else if(!hasDestination || Vector3.Distance(transform.position,destination)<.18f) { destination=points[waypoint++%points.Length]; hasDestination=true; }
            }
            if(hasDestination) { Vector3 delta=destination-transform.position; delta.y=0; if(delta.magnitude>.10f) direction=delta.normalized; else hasDestination=false; }
        }
        float targetSpeed=direction.sqrMagnitude>.01f?(walking?1.5f:3.8f):0;
        speed=Mathf.MoveTowards(speed,targetSpeed,Time.deltaTime*18);
        if(direction.sqrMagnitude>.01f) {
            transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),720*Time.deltaTime);
            Vector3 step=direction*speed*Time.deltaTime;
            if(hasDestination && step.magnitude>Vector3.Distance(transform.position,destination)) step=destination-transform.position;
            transform.position+=step;
            transform.position=new Vector3(Mathf.Clamp(transform.position.x,-8.5f,8.5f),0,Mathf.Clamp(transform.position.z,-8.5f,8.5f));
        }
        string next=targetSpeed<.1f?"Idle":walking?"Walk":"Run";
        if(next!=locomotion) { animator.CrossFadeInFixedTime(next,.15f); locomotion=next; }
    }
    void LateUpdate() {
        viewCamera.transform.position=Vector3.Lerp(viewCamera.transform.position,transform.position+cameraOffset,1-Mathf.Exp(-7*Time.deltaTime));
        viewCamera.transform.rotation=Quaternion.LookRotation(-cameraOffset);
    }
}
