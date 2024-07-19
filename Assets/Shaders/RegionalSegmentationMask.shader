// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
Shader "Unlit/RegionalSegmentationMask"
{
   Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LeftPnt("_LeftPnt", float) = 5.0
        _RightPnt("_RightPnt", float) = 15.0
        _BottomPnt("_BottomPnt", float) = 5.0
        _TopPnt("_TopPnt", float) = 15.0
        _NearPnt("_NearPnt", float) = 5.0
        _FarPnt("_FarPnt", float) = 15.0

         // Vector proeprties doesnt work because its in [0,1] range
        //_BottomLeftNear("Bottom-Left-Near corner of the container box ", Vector) = (0,0,0) 
        //_TopRightFar("Top-Right-Far corner of the container box ", Vector) = (1,1,1)
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
                float4 mask_col : COLOR; 
             };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LeftPnt, _RightPnt, _BottomPnt, _TopPnt, _NearPnt,  _FarPnt;




            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//coordinates in camera space
                float4 coord = mul ( unity_ObjectToWorld, v.vertex ); //global coordinates 
                                                                    // v.vertex in [-1,1] range

                float3 interior_col = float3(1,0,0); // interior points will be red
                                                     // exterior points will be black
                // float3 mask = interior_col * (coord[0] >= _BottomLeftNear[0]  && coord[0] <= _TopRightFar[0]) 
                //                            * (coord[1] >= _BottomLeftNear[1]  && coord[1] <= _TopRightFar[1]) 
                //                            * (coord[2] >  =_BottomLeftNear[2] && coord[2] <= _TopRightFar[2]);

                
                float3 mask = interior_col *  (coord[0]  >= _LeftPnt   && coord[0]  <= _RightPnt) 
                                           *  (coord[1] >= _BottomPnt  && coord[1]  <= _TopPnt  ) 
                                           *  (coord[2] >= _NearPnt    && coord[2]  <= _FarPnt  );

                // float3 mask = interior_col * (coord[0] >= 5.0    && coord[0]  <= 15.0) 
                //                            * (coord[1] >= 5.0    && coord[1]  <= 15.0  ) 
                //                            * (coord[2] >= 5.0    && coord[2]  <= 15.0  );


                o.mask_col  = float4(mask, 1.0);
                

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return i.mask_col;
            }
            ENDCG
        }
    }
}
