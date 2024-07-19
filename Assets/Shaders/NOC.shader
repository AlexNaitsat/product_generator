// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
Shader "Unlit/NOC"
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
            #define  HalfUnitCubeDiag 0.866025    
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
                //float4 local_coord : POSITION; //only single POSITION field is allowed for frag input!
                                                 //(SV_POSITION is another macro name for POSITION)
                float4 local_coord : COLOR; //thus, I have to pass local coordinates as a field of other type
                                            //try also flota3 TEXCOORD1
                //float4 local_coord : TEXCOORD1; //range is [0,1]
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _min_coord;
            float _max_coord;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//local coordintes to camera space
                // without it, the object is rendered like if  it was placed in different position

                //o.vertex =v.vertex;//pass local coordinates 

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);

                //An approximate normalization of NOC 
                float4 centerPos = mul ( unity_ObjectToWorld, float4(0,0,0,1) );
                float4 cornerPos = mul ( unity_ObjectToWorld, float4(1,1,1,1) );
                float4 diag      = cornerPos - centerPos;
                float bbox_scale  = length(diag)/HalfUnitCubeDiag;
                // bbox_scale is large => its a small object and thus it was scaled alot in the scene => its NOC need to be upscaled 
                float4 normalized_coord= v.vertex*bbox_scale;
                o.local_coord = (1+normalized_coord)/2; //converting [-1,1] coord range to [0,1] color range

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture - orignal texture without fog
                //fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 col = i.local_coord;
                return col;

            }
            ENDCG
        }
    }
}
