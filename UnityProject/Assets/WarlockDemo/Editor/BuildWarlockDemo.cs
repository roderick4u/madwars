using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.Linq;
using System.IO;

public static class BuildWarlockDemo
{
    const string Folder="Assets/WarlockDemo";
    static Material Mat(string name,Color color) {
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) { m=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m,path); }
        m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",.22f); return m;
    }
    static GameObject Cube(string name,Vector3 position,Vector3 size,Material material) {
        var ob=GameObject.CreatePrimitive(PrimitiveType.Cube); ob.name=name; ob.transform.position=position; ob.transform.localScale=size; ob.GetComponent<Renderer>().sharedMaterial=material; return ob;
    }
    public static string Build() {
        Directory.CreateDirectory(Folder+"/Scenes");
        foreach(string name in new[]{"Idle","Walk","Run"}) {
            string path=Folder+"/Warlock_"+name+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation=true; importer.animationCompression=ModelImporterAnimationCompression.Off;
            var clips=importer.defaultClipAnimations;
            foreach(var clip in clips) { clip.name=name; clip.loopTime=true; clip.loopPose=true; }
            importer.clipAnimations=clips; importer.SaveAndReimport();
        }
        var skin=Mat("Warlock_PurpleGold",Color.white);
        skin.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Head_BaseColor.png"));
        var ti=(TextureImporter)AssetImporter.GetAtPath(Folder+"/Head_Normal.png"); ti.textureType=TextureImporterType.NormalMap; ti.SaveAndReimport();
        skin.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Head_Normal.png")); skin.EnableKeyword("_NORMALMAP"); skin.SetFloat("_BumpScale",.2f);
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(original.isDirty) EditorSceneManager.SaveScene(original,Folder+"/Scenes/PreviousSceneBackup.unity",true);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var ground=Mat("ArenaSlate",new Color(.12f,.19f,.22f)); var grid=Mat("Grid",new Color(.18f,.27f,.29f)); var rim=Mat("Rim",new Color(.075f,.10f,.14f)); var accent=Mat("ArcaneTeal",new Color(.08f,.6f,.46f));
        var plane=GameObject.CreatePrimitive(PrimitiveType.Plane); plane.name="Flat arena 20m"; plane.transform.localScale=new Vector3(2,1,2); plane.GetComponent<Renderer>().sharedMaterial=ground;
        for(int i=-9;i<=9;i+=2) {
            Cube("Grid X",new Vector3(i,.006f,0),new Vector3(.025f,.01f,18),grid);
            Cube("Grid Z",new Vector3(0,.006f,i),new Vector3(18,.01f,.025f),grid);
        }
        Cube("North border",new Vector3(0,.12f,9.5f),new Vector3(19,.24f,.3f),rim);
        Cube("South border",new Vector3(0,.12f,-9.5f),new Vector3(19,.24f,.3f),rim);
        Cube("West border",new Vector3(-9.5f,.12f,0),new Vector3(.3f,.24f,19),rim);
        Cube("East border",new Vector3(9.5f,.12f,0),new Vector3(.3f,.24f,19),rim);
        foreach(float x in new[]{-7f,7f}) foreach(float z in new[]{-7f,7f}) {
            Cube("Arena marker",new Vector3(x,.45f,z),new Vector3(.65f,.9f,.65f),rim);
            var gem=GameObject.CreatePrimitive(PrimitiveType.Sphere); gem.name="Arcane beacon"; gem.transform.position=new Vector3(x,1.05f,z); gem.transform.localScale=Vector3.one*.32f; gem.GetComponent<Renderer>().sharedMaterial=accent;
        }
        var actor=new GameObject("Warlock Player");
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Warlock_Idle.fbx")); model.name="Warlock Visual"; model.transform.SetParent(actor.transform,false);
        foreach(var r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial=skin;
        var animator=model.GetComponent<Animator>(); if(!animator) animator=model.AddComponent<Animator>(); animator.applyRootMotion=false;
        string controllerPath=Folder+"/WarlockLocomotion.controller";
        var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        foreach(string name in new[]{"Idle","Walk","Run"}) {
            var clip=AssetDatabase.LoadAllAssetsAtPath(Folder+"/Warlock_"+name+".fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview__"));
            var state=controller.layers[0].stateMachine.AddState(name); state.motion=clip;
            if(name=="Idle") controller.layers[0].stateMachine.defaultState=state;
        }
        animator.runtimeAnimatorController=controller;
        var cameraObj=new GameObject("MOBA Camera"); var camera=cameraObj.AddComponent<Camera>(); cameraObj.tag="MainCamera"; cameraObj.transform.position=new Vector3(9,13,-9); cameraObj.transform.LookAt(new Vector3(0,0,0)); camera.orthographic=true; camera.orthographicSize=7.2f; camera.nearClipPlane=.1f; camera.farClipPlane=80; camera.backgroundColor=new Color(.04f,.055f,.08f); camera.clearFlags=CameraClearFlags.SolidColor; cameraObj.AddComponent<AudioListener>();
        var mover=actor.AddComponent<WarlockDemoMovement>(); mover.animator=animator; mover.viewCamera=camera;
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=1.8f; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(50,-35,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.52f,.56f,.66f);
        Selection.activeGameObject=actor;
        EditorSceneManager.SaveScene(scene,Folder+"/Scenes/WarlockArena.unity"); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,.7f,0),Quaternion.Euler(45,135,0),9);
        return "Created WarlockArena: Idle/Walk/Run, WASD, right-click move, Shift walk, Space auto-demo.";
    }
}
