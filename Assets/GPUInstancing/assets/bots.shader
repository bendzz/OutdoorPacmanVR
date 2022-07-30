// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

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

            //Cull off
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

        int AndroidInvertScreenUVs;

        float4 ghostPositions[4];

        float3 mapScale;
        float3 mapOffset;
        float3 mapCenter;


        sampler2D _MainTex;


        // pacman specific
        // Used to make a transparent hole in the walls for video footage
        float4 pacmanPos;
        float4 camPos;  // spectator camera
        float holeRadius;   // in world units

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

            float3 normals : NORMAL0;

            //pacman
            float4 lightPos0 : float4; 
            float4 lightPos1 : float41; 
            float4 lightPos2 : float42; 
            float4 lightPos3 : float43; 
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
                //color = float4(0.129, 0.129, 1, 1) * .5 + .1;
                color = float4(0, 0, 1, 1);
                //color = float4(0, 0, .7, 1);
            else
                //color = float4(1, 0.725, 0.686, 1);
                color = float4(1, 0.6, 0.1, 1);
                //color = float4(1, 0.1, 0.9, 1);
            //color *= 1.2;
            color *= .5;



            //if ((id) % 10 != 0) return;  // testing 

            // pacman lights
            float4 gcol = float4(1, 0, 0, 1);
            float4 lightPos[4];
            //for (int g = 0; g < 4; g++) {
            for (int g = 3; g >= 0; g--) {
                lightPos[g] = ghostPositions[g];    // reflection positions

                // maze color tinting
                if (g == 0)
                    gcol = float4(1, 0, 0, 1);
                if (g == 1)
                    gcol = float4(1, .4, .7, 1);
                if (g == 2)
                    gcol = float4(0, 1, .7, 1);
                if (g == 3)
                    gcol = float4(1, .7, 0, 1);
                //gcol -= .5;
                //gcol *= 2;

                float radius = 20;
                float strength = saturate((radius - length(vecs[0] - ghostPositions[g])) / radius);
                //strength = saturate((strength - (vecs[0].y - g*.5)) * .5);
                //color = lerp(color, gcol, pow(strength, 3) * .5);
                color = lerp(color, saturate(gcol - (1 - strength)), pow(strength, 3) * 1);
                //color = lerp(color, float4(0, 0, 0, 1), saturate((strength - .86) / .09));
                if (submeshI == 0)
                    color = lerp(color, float4(-1, -1, -1, 1), saturate((strength - .89) / .15));
                else
                    color = lerp(color, float4(1, 1, 1, 1), saturate((strength - .8) / .2));
            }

            // Pacman game specific warping effect
            for (v = 0; v < 3; v++) {
                for (int g = 0; g < 4; g++) {
                    float3 spot = ghostPositions[g] + float3(.1, 0, .1);
                    float lerpStr = .8; 
                    if (submeshI > 0)
                        lerpStr = .2;
                    float3 offset = lerp(bo.pos, vecs[v], lerpStr) - spot;    // makes it half pushing objects half warping them
                    float range = 6;
                    float power = saturate((range - length(offset)) / range);
                    power = pow(power, 2) * 1;
                    if (submeshI > 0)
                        power *= power;
                    //power = clamp(power, 0, .5);
                    //vecs[v] = lerp(vecs[v], bo.pos, power * .7);    // shrink the affected objects slightly
                    float multiplier = .7;
                    if (submeshI > 0)
                        multiplier = 1.3;
                    vecs[v] += normalize(offset) * float3(1,1.5,1) * multiplier * power;
                }
            }

            // draw hole in walls so you can see the player
            if (camPos.x != 0) {
                for (v = 0; v < 3; v++) {
                    if (submeshI != 0)
                        continue;
                    // Draw a line between pacman and camera, find distance from vector to that line
                    //float3 direction = pacmanPos - camPos;
                    //float3 nearestLinePoint = dot(direction, (vecs[v] - camPos)) * normalize(direction) + camPos;

                    float3 direction = pacmanPos - camPos;
                    float spotOnLine = dot(normalize(direction), (vecs[v] - camPos));
                    if (spotOnLine > length(direction))
                        spotOnLine = length(direction);

                    float3 nearestLinePoint = normalize(direction) * spotOnLine + camPos;

                    //float distance = length(vecs[v] - nearestLinePoint);
                    //float distance = length(vecs[v] - nearestLinePoint);
                    float distance = length(vecs[v].xz - nearestLinePoint.xz);

                    //float maxHeight = 1 - saturate(holeRadius - length(vecs[v] - pacmanPos) / holeRadius);
                    //float maxHeight = 1 - saturate(holeRadius - distance / holeRadius);
                    //float maxHeight =  (distance - holeRadius) + (nearestLinePoint.y - holeRadius);
                    float maxHeight =  (distance - holeRadius);
                    if (maxHeight < 0)
                        maxHeight = 0;
                    //float maxHeight = -distance;
                    //if (maxHeight < 1)
                    //    vecs[v].y = lerp(0, vecs[v].y, maxHeight);

                    if (vecs[v].y > maxHeight)
                        vecs[v].y = maxHeight;
                    //vecs[v].y = maxHeight;
                }
            }

            //float4 pacmanPos;
            //float4 camPos;  // spectator camera
            //float holeRadius;   // in world units

            // map scaling
            for (int v = 0; v < 3; v++) {
                vecs[v] += mapOffset;
                vecs[v] -= mapCenter;
                vecs[v] = vecs[v] * mapScale;
                vecs[v] += mapCenter;
            }
            
            //color.a = saturate(vecs[0].y + .4);
            color.a = saturate(vecs[0].y + 7);


            //float3 normal = getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart], meshVertices[meshTriangles[tri + 1] + sub.verticesStart], meshVertices[meshTriangles[tri + 2] + sub.verticesStart]);
            //float3 normal = -getNormal(meshVertices[meshTriangles[tri + 0] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 1] + sub.verticesStart].xzy, meshVertices[meshTriangles[tri + 2] + sub.verticesStart].xzy);
            float3 normal = -getNormal(vecs[0], vecs[1], vecs[2]);

            if (bo.alive == -1)
                color = float4(.02, .02, .02, 0);

            float light = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
            float white = saturate(light - .7);
            //light = light * .9 + .04;
            light = light * 1.5 + .04;
            color.xyz = color.xyz * light;

            // white capped walls
            //color.xyz += white * 1.5;
            color.bg += white * 1.5;

            // Only show the most powerful ghost light in the frag shader, since these lights are SUPER expensive
            // TODO clean up this code and, is there a prettier way to do this? The sharp edges are a bit ugly. Maybe 2 lights?
            float minDis = 100000;
            int nearestLight = 5;
            float4 nearestLightPos = (float4)0;
            float4 vertPos = UnityObjectToClipPos((vecs[0].xyz + vecs[1].xyz + vecs[2].xyz) / 3);
            vertPos.xy = vertPos.xy / vertPos.w;
            for (int l = 3; l >= 0; l--) {
                float4 tempLightPos = UnityObjectToClipPos(ghostPositions[l].xyz);
                float4 tempLightPos1 = (float4)0;
                tempLightPos1.xy = tempLightPos.xy / tempLightPos.w;
                //float dis = length(vertPos - tempLightPos);
                float dis = length(vertPos - tempLightPos1);
                if (dis < minDis) {
                    minDis = dis;
                    nearestLight = l;
                    nearestLightPos = tempLightPos;
                }
            }
            nearestLightPos.xy = .5 + (nearestLightPos.xy / nearestLightPos.w) / 2;
            float4 LightCol = float4(1, 0, 0, 1);
            if (nearestLight == 1) {
                LightCol = float4(1, .4, .7, 1);
            }
            if (nearestLight == 2)
            {
                LightCol = float4(0, 1, 1, 1);
            }
            if (nearestLight == 3)
            {
                LightCol = float4(.7, .4, 0, 1);
            }


            for (v = 2; v >= 0; v--) { 
                geomOutput gout = (geomOutput)0;
                gout.pos = float4(vecs[v], 1);
                gout.pos = mul(UNITY_MATRIX_VP, gout.pos);
                gout.info.xyz = bo.info;

                //gout.color.xyz = color.xyz;
                gout.color = color;
                //gout.color.a = color.a;

                gout.normals = normal;

                // pacmanLights

                if (bo.alive != -1) {
                    //gout.lightPos0 = UnityObjectToClipPos(ghostPositions[0].xyz);
                    //gout.lightPos1 = UnityObjectToClipPos(ghostPositions[1].xyz);
                    //gout.lightPos2 = UnityObjectToClipPos(ghostPositions[2].xyz);
                    //gout.lightPos3 = UnityObjectToClipPos(ghostPositions[3].xyz);
                    gout.lightPos0 = nearestLightPos;
                    gout.lightPos3 = LightCol;
                }

                triStream.Append(gout);
            }
            //triStream.RestartStrip();
        }

        fixed4 frag(geomOutput i) : SV_Target
        {
            fixed4 col = i.color;
        //col.a = 1;


        // show ghost lights though walls. Very expensive, especially when it used to do all 4.
        float radius = .05;

        float2 screenUV = (i.pos.xy / _ScreenParams.xy);
        if (AndroidInvertScreenUVs == 0)
            screenUV = float2(screenUV.x, 1 - screenUV.y);    // NOTE! Needs to be enabled on PC but not on Quest 2!

        //float4 light = i.lightPos0;

        //float4 result = (float4)0;
        //float4 gcol = float4(1, 0, 0, 1);
        //for (int g = 0; g < 4; g++) {
        //    if (g == 1) {
        //        light = i.lightPos1;
        //        gcol = float4(1, .4, .7, 1);
        //    }
        //    if (g == 2) 
        //    {
        //        light = i.lightPos2;
        //        gcol = float4(0, 1, 1, 1);
        //    }
        //    if (g == 3) 
        //    {
        //        light = i.lightPos3;
        //        gcol = float4(.7, .4, 0, 1);
        //    }
        //    float2 source = (.5 + (light.xy / light.w) / 2);

        float4 gcol = i.lightPos3;
        float2 source = i.lightPos0.xy;
        float4 result = gcol * saturate((radius - length(source - screenUV)) / radius);
        //float4 result = float4(1,1,0,1) * saturate((radius - length(source - screenUV)) / radius);
        //}
        col += result;



            return col;
        }

        ENDCG
            }
    }
    FallBack "Diffuse"
}
