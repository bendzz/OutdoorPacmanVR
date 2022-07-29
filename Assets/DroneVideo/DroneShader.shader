Shader "Unlit/DroneShader"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D droneVideo;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex); 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 game = tex2D(_MainTex, i.uv);
                fixed4 drone = tex2D(droneVideo, i.uv);
               
                fixed4 droneOffset = tex2D(droneVideo, i.uv - float2(.001, .001));
                
                //droneOffset.rb = droneOffset.rb - droneOffset.g;
                //droneOffset.g = 0;

                //drone.rb = drone.rb - drone.g;
                //drone.g = 0;
                //drone.r = 0;


                fixed4 col = drone;
            //col -= tex2D(droneVideo, i.uv - float2(.001, .001));
                col -= droneOffset;
            col *= 10; 

                col.a = 1;
                col.g *= 0;
                col.r *= 0.2;

                //col = col - .2;

                col.g = col.b * .1;
                //col.g = 0;
                //col.b = 0;
                


                float4 darkOffset = .7;
                fixed4 dark = drone + darkOffset;
                dark = saturate(dark);
                dark -= darkOffset;
                dark -= .3;
                //game.bg += dark.r * .5;

                //game *= .3;
                game *= .7;
                game += col;

                // apply fog
                //return drone;
                return game;
            }
            ENDCG
        }
    }
}
