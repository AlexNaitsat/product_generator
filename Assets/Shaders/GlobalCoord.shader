// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
Shader "Unlit/GlobalCoord"
{
   Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        //_min_coord("MinimumLocalCoordinates",float) =  10000 
        //_max_coord("MaxLocalCoordinates",float) = -10000 
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
            // make fog work //disabled it 
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION; //in local object space
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION; //each float is in [-1,1] range
                float4 global_coord : COLOR; 
             };

            sampler2D _MainTex;
            float4 _MainTex_ST;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//coordinates camera space
                o.global_coord = mul ( unity_ObjectToWorld, v.vertex );

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = i.global_coord/2; //[-1,1] coord. range to [0,1] color range  
                return col;
            }
            ENDCG
        }
    }
}
