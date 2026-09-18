Shader "Warlock/Fireball Toon"
{
 Properties {
  [HDR] _Hot("Hot core",Color)=(1,.82,.13,1)
  [HDR] _Mid("Flame orange",Color)=(1,.24,.015,1)
  [HDR] _Cool("Tail red",Color)=(.65,.035,.008,1)
  _Amplitude("Vertex fluctuation",Range(0,.05))=.016
  _Frequency("Noise scale",Range(1,20))=9
  _Speed("Fluctuation speed",Range(0,8))=3
  [Toggle(_EMISSION)] _Emission("Emission",Float)=1
  _EmissionIntensity("Emission HDR intensity",Range(1,8))=2.6
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Name "ToonFire" Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.0
   #pragma multi_compile_instancing
   #pragma shader_feature_local _EMISSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Hot,_Mid,_Cool;
   float _Amplitude,_Frequency,_Speed,_Emission,_EmissionIntensity;
   CBUFFER_END
   float Hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
   float Noise(float3 p){
    float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),
     lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+float3(1,1,1)),f.x),f.y),f.z);
   }
   struct Attributes {float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float axial:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO};
   Varyings Vert(Attributes IN){
    UNITY_SETUP_INSTANCE_ID(IN);Varyings OUT;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
    float3 p=IN.positionOS.xyz;
    float n=Noise(p*_Frequency+float3(0,_Time.y*_Speed,-_Time.y*_Speed*.7))*2-1;
    float tail=saturate(-p.z/.58);
    float3 radial=float3(p.xy,0)/max(length(p.xy),.001);
    p+=radial*n*_Amplitude*(1+tail*.65);
    p.x+=sin(_Time.y*_Speed*1.3+p.z*12)*_Amplitude*tail;
    p.y+=n*_Amplitude*tail;
    OUT.positionWS=TransformObjectToWorld(p);OUT.positionCS=TransformWorldToHClip(OUT.positionWS);OUT.axial=p.z;return OUT;
   }
   half4 Frag(Varyings IN):SV_Target{
    // Screen derivatives rebuild one normal for each deformed triangle: always flat shaded.
    float3 normal=normalize(cross(ddy(IN.positionWS),ddx(IN.positionWS)));
    float3 view=GetWorldSpaceNormalizeViewDir(IN.positionWS);
    if(dot(normal,view)<0)normal=-normal;
    Light key=GetMainLight();
    half light=saturate(dot(normal,normalize(key.direction+float3(0,.2,0)))*.5+.5);
    half heat=light*.65+saturate((IN.axial+.55)/.78)*.35;
    half3 color=heat>.76?_Hot.rgb:heat>.46?_Mid.rgb:_Cool.rgb;
    #if defined(_EMISSION)
    color*=lerp(1,_EmissionIntensity,heat>.76?1:heat>.46?.45:.08);
    #endif
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
