Shader "Unlit/Lambert"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 positionOS : POSITION;
                float2 texcoord : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct v2f
            {
                float2 texcoord : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.normalWS = UnityObjectToWorldNormal(v.normalOS) ;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 tex = tex2D(_MainTex, i.texcoord);
                //Light myLight = GetMainLight;
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float DiffuseCol = (dot(lightDir,i.normalWS))*0.5 + 0.5;

                return tex * DiffuseCol;
            }
            ENDCG
        }
    }
}
