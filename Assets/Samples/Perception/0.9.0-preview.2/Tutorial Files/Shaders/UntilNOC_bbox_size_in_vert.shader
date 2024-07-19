//here I tried to compute min and max local coordinates in vertex shader and then
//use it to normalize local coordinates in frag shader inorder to deal with scale differences of objects
//there are two problem
//1)  I cannot pas more than two POSITION fields to frag shader, so I need to save local coordinates as 
//    COLOR or TEXCOORD#
//2) major problem with parallel run of vertex->fragment pipelines, i.e., frag shaders do not wait until
// all vertex shaders are finishes, thus they will use wrong bbox size estimation


Shader "Unlit/UntilNOC"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _global_var_test("testVariable",float) = 1.0
        _min_coord("MinimumLocalCoordinates",float) =  10000 
        _max_coord("MaxLocalCoordinates",float) = -10000 
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
                //float4 local_coord : POSITION; //only single POSITION field is allowed for frag input!
                                                 //(SV_POSITION is another macro name for POSITION)
                //float4 local_coord : COLOR; //thus, I have to pass local coordinates as a field of other type
                                            //try also flota3 TEXCOORD1
                float4 local_coord : TEXCOORD1; //range is [0,1]
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float _min_coord;
            float _max_coord;
            float _global_var_test;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);//local coordintes to camera space
                // without it, the object is rendered like if  it was placed in different position

                //o.vertex =v.vertex;//pass local coordinates 

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);

                //added per vertex NOC  color for frag shader
                //o.color = v.vertex;
                o.local_coord = v.vertex;

                //min_local_coord=min(min_local_coord,v.vertex);
                //max_local_coord=max(max_local_coord,v.vertex);
                
                _min_coord = min(_min_coord, v.vertex[0]); //convert properly homogenious to cartesian!
                _max_coord = max(_max_coord, v.vertex[0]); //convert properly homogenious to cartesian!

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture - orignal texture without fog
                //fixed4 col = tex2D(_MainTex, i.uv);

                fixed4 col = i.local_coord;

                //fixed4 col = i.color; //without scale normalization
                //fixed4 col = (i.color - min_coord)/(max_coord-min_coord);
                
                //fixed4 col =  i.color* _global_var_test;
                return col;

                


                


                //// convert coordinates to color 
                //fixed4 col = (0,1,0);



                
                
                //return fixed4(0,1,0,1);

            }
            ENDCG
        }
    }
}
