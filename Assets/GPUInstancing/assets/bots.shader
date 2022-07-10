Shader "Custom/bots"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            Cull off

            Pass
            {
            CGPROGRAM
            // Physically based Standard lighting model, and enable shadows on all light types
            //#pragma surface surf Standard fullforwardshadows

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            // Use shader model 3.0 target, to get nicer looking lighting
            //#pragma target 3.0

            #include "CSDefs.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0
        StructuredBuffer<Submesh> submeshes;
        StructuredBuffer<float3> meshVertices;
        StructuredBuffer<int> meshTriangles;
        StructuredBuffer<bot> bots;

        int submeshI;
        int instanceCount;
        int botOffset;

        sampler2D _MainTex;

        //struct Input
        //{
        //    float2 uv_MainTex;
        //};

        //half _Glossiness;
        //half _Metallic;
        //fixed4 _Color;

        //// Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        //// See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        //// #pragma instancing_options assumeuniformscaling
        //UNITY_INSTANCING_BUFFER_START(Props)
        //    // put more per-instance properties here
        //UNITY_INSTANCING_BUFFER_END(Props)

        //void surf (Input IN, inout SurfaceOutputStandard o)
        //{
        //    // Albedo comes from a texture tinted by color
        //    fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
        //    o.Albedo = c.rgb;
        //    // Metallic and smoothness come from slider variables
        //    o.Metallic = _Metallic;
        //    o.Smoothness = _Glossiness;
        //    o.Alpha = c.a;
        //}
         


        //sampler2D _MainTex;
        //float4 _MainTex_ST;

        struct vertsOutput
        {
            float4 info : TEXCOORD0;
        };
        vertsOutput vert(uint id : SV_VertexID) {
            vertsOutput o = (vertsOutput)0;
            o.info.x = id;
            return o;
        }


        struct geomOutput
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : TEXCOORD2;	// color. TODO rename
            //float4 parentPos : TEXCOORD3;	//debug
            //float4 info : COLOR;
            //UNITY_FOG_COORDS(1)
            float4 info : TEXCOORD1;
        };

        [maxvertexcount(4)]
        //void geom(point input p[1], inout TriangleStream<geomOutput> triStream)
        void geom(point vertsOutput p[1], inout TriangleStream<geomOutput> triStream)
        {
            int id = p[0].info.x;

            //StructuredBuffer<Submesh> submeshes;
            //StructuredBuffer<float3> meshVertices;
            //StructuredBuffer<int> meshTriangles;
            //StructuredBuffer<bot> bots;

            //int submesh;

            Submesh sub = submeshes[submeshI];

            //int vertOffset = (int)(id / sub.verticesCount) - verticesStart;

            //int tri = ((id * 3) + sub.trianglesStart) % sub.trianglesCount;
            int tri = (id * 3) % sub.trianglesCount + sub.trianglesStart;


            //int botID = (id * 3) / sub.trianglesCount + (instanceCount * (submeshI - 1));   // TODO this instanceCount won't work
            int botID = (id * 3) / sub.trianglesCount + botOffset;
            
            bot bo = bots[botID];

            //float3 normal = getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart], meshVertices[meshTriangles[tri + 1] + sub.verticesStart], meshVertices[meshTriangles[tri + 2] + sub.verticesStart]);
            float3 normal = -getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 1] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 2] + sub.verticesStart].xzy);

            for (int v = 0; v < 3; v++) {
                int vert = meshTriangles[tri + v] + sub.verticesStart;

                geomOutput gout = (geomOutput)0;
                //gout.pos = float4(meshVertices[vert], 0) * .1 + float4(bo.pos,1); //+ float4(botID * 2, submeshI*1.5, 0, 0);
                //gout.pos = float4(meshVertices[vert], 0) * 1 + float4(bo.pos,1); //+ float4(botID * 2, submeshI*1.5, 0, 0);
                gout.pos = float4(meshVertices[vert].xzy, 0) * 100 + float4(bo.pos,1); //+ float4(botID * 2, submeshI*1.5, 0, 0);
                //gout.pos = float4(meshVertices[vert], 0) * .1 + float4(id,0,0,1); //+ float4(botID * 2, submeshI*1.5, 0, 0);
                //gout.pos = float4(meshVertices[vert], 1) +float4(botID * 2, submeshI*1.5, 0, 0);
                gout.pos = mul(UNITY_MATRIX_VP, gout.pos);
                //gout.info = float4(botID, bo.info.xyz);
                gout.info.xyz = bo.info;
                
                //gout.color = float4(1, 0, 0, 1);
                //gout.color.xyz = normal;
                float4 color = float4(1, 0, .5, 1);
                float light = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
                gout.color.xyz = color.xyz * light;

                triStream.Append(gout);
            }
            //triStream.RestartStrip();
        }

        fixed4 frag(geomOutput i) : SV_Target
        {
            fixed4 col = i.color;//float4(1,0,1,1);

            //col = float4(abs(35 - i.info.x % 70) / 35, 1, i.info.x / 19 % .5, 1);
            //col = float4(i.info.xyz, 1);

            return col;
        }

        ENDCG
            }
    }
    FallBack "Diffuse"
}
