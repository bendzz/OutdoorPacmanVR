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
            //Tags { "RenderType" = "Opaque" }
            Tags { "RenderType" = "transparent " }
            //Blend One One // additive blending 
            LOD 200

            Cull off
            //ZWRITE OFF

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

        float4 ghostPositions[4];

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
            float4 color : COLOR;
            //float4 parentPos : TEXCOORD3;	//debug
            //UNITY_FOG_COORDS(1)
            float4 info : TEXCOORD1;
        };

        [maxvertexcount(4)]
        //void geom(point input p[1], inout TriangleStream<geomOutput> triStream)
        void geom(point vertsOutput p[1], inout TriangleStream<geomOutput> triStream)
        {
            int id = p[0].info.x;


            Submesh sub = submeshes[submeshI];

            int tri = (id * 3) % (uint)sub.trianglesCount + sub.trianglesStart;

            int botID = (id * 3) / (uint)sub.trianglesCount + botOffset;

            bot bo = bots[botID];

            float3 vecs[3] = { meshVertices[meshTriangles[tri + 0] + sub.verticesStart].xzy, 
                meshVertices[meshTriangles[tri + 1] + sub.verticesStart].xzy,meshVertices[meshTriangles[tri + 2] + sub.verticesStart].xzy };

            for (int v = 0; v < 3; v++) {
                vecs[v] += float4(bo.pos, 1);   // add object position
            }

            float4 color = float4(1, 1, 1, 1);

            // pacman coloring
            if (submeshI == 0)
                //color = float4(0.129, 0.129, 1, 1);
                color = float4(0.129, 0.129, 1, 1) * .9 + .1;
            else
                color = float4(1, 0.725, 0.686, 1);
            color *= 1.4;

            // Pacman game specific warping effect
            for (v = 0; v < 3; v++) {
                for (int g = 0; g < 3; g++) {
                    float3 offset = lerp(bo.pos, vecs[v], .7) - ghostPositions[g];    // makes it half pushing objects half warping them
                    float range = 4;
                    float power = saturate((range - length(offset)) / range);
                    power = pow(power, 2) * 1;
                    vecs[v] = lerp(vecs[v], bo.pos, power * .7);    // shrink the affected objects slightly
                    vecs[v] += normalize(offset) * power;
                }
            }

            //float3 normal = getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart], meshVertices[meshTriangles[tri + 1] + sub.verticesStart], meshVertices[meshTriangles[tri + 2] + sub.verticesStart]);
            //float3 normal = -getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 1] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 2] + sub.verticesStart].xzy);
            float3 normal = -getNormal(vecs[0], vecs[1], vecs[2]);

            float light = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
            light = light * .8 + .2;
            for (v = 0; v < 3; v++) {
                geomOutput gout = (geomOutput)0;
                gout.pos = float4(vecs[v], 1);
                gout.pos = mul(UNITY_MATRIX_VP, gout.pos);
                gout.info.xyz = bo.info;

                gout.color.xyz = color.xyz * light;

                triStream.Append(gout);
            }
            //triStream.RestartStrip();
        }

        fixed4 frag(geomOutput i) : SV_Target
        {
            fixed4 col = i.color;

            return col;
        }

        ENDCG
            }
    }
    FallBack "Diffuse"
}
