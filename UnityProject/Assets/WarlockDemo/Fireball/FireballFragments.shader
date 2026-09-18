Shader "Warlock/Fireball Fragments"
{
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Cull Back ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma target 3.0
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half4 color:COLOR;};
   Varyings Vert(Attributes IN){Varyings o;o.positionWS=TransformObjectToWorld(IN.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);o.color=IN.color;return o;}
   half4 Frag(Varyings IN):SV_Target{
    float3 n=normalize(cross(ddy(IN.positionWS),ddx(IN.positionWS)));
    half band=abs(dot(n,normalize(float3(.4,.8,.3))))>.55?1:.78;
    return half4(IN.color.rgb*band,1);
   }
   ENDHLSL
  }
 }
}
