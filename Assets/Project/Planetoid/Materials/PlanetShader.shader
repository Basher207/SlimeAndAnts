Shader "Custom/PlanetShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _WaterLevel ("Water Level", float) = 0.3
        _FlatColor ("Flat Color", Color) = (0,1,0,1)
        _SteepColor ("Steep Color", Color) = (0.5,0.5,0.5,1)
        _WaterColor ("Water Color", Color) = (0,0,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 posWorld : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _WaterLevel;
            float4 _FlatColor;
            float4 _SteepColor;
            float4 _WaterColor;

            
#define PI 3.14159265
            
            float2 SpherePosToUV(float3 pos)
            {
                // Normalize the position vector
                float3 norm_pos = normalize(pos);
            
                // Convert the 3D vector to spherical coordinates (latitude and longitude)
                float longitude = atan2(norm_pos.z, norm_pos.x);
                float latitude = asin(norm_pos.y);
            
                // Normalize the spherical coordinates to the range [0, 1]
                float u = (longitude + PI) / (2.0 * PI);
                float v = (latitude + PI / 2.0) / PI;
            
                return float2(u, v);
            }
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = v.normal;//UnityObjectToWorldNormal(v.normal);
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            fixed4 frag (v2f i) : SV_Target {
               // normalize position
               float3 pos = normalize(i.posWorld.xyz);

               // Get UV coordinates from 3D position
               float2 uv = SpherePosToUV(pos);

               // Sample height at (u, v)
               float height = tex2Dlod(_MainTex, float4(uv, 0, 0)).r;

               // Sample heights slightly to the left/right and up/down to calculate slopes
               float heightLeft = tex2Dlod(_MainTex, float4(uv + float2(-0.01, 0), 0, 0)).r;
               float heightRight = tex2Dlod(_MainTex, float4(uv + float2(0.01, 0), 0, 0)).r;
               float heightUp = tex2Dlod(_MainTex, float4(uv + float2(0, -0.01), 0, 0)).r;
               float heightDown = tex2Dlod(_MainTex, float4(uv + float2(0, 0.01), 0, 0)).r;

               // Calculate slopes in the X and Y directions
               float dx = heightRight - heightLeft;
               float dy = heightDown - heightUp;

               // Calculate normal from slopes
               float3 norm = normalize(float3(dx, dy, 2.0));

               // water level
               if(height <= _WaterLevel) {
                   return _WaterColor;
               }
               else {
                   // lerp between flat and steep color based on slope
                   float slope = 1.0 - dot(norm, float3(0, 0, 1));
                   return lerp(_FlatColor, _SteepColor, clamp(slope*10000, 0, 1));
               }
            }
            
            // fixed4 frag (v2f i) : SV_Target {
            //     // normalize position and normal
            //     float3 pos = normalize(i.posWorld.xyz);
            //     float3 norm = normalize(i.normal);
            //
            //     // calculate the height based on distance from the center
            //     float height = length(i.posWorld.xyz);
            //
            //     // calculate the slope based on dot product of normal and position
            //     float slope = 1.0 - dot(-norm, pos);
            //
            //     // water level
            //     if(height <= _WaterLevel) {
            //         return _WaterColor;
            //     }
            //     else {
            //         // lerp between flat and steep color based on slope
            //         return lerp(_FlatColor, _SteepColor, clamp(slope, 0, 1));
            //     }
            // }
            ENDCG
        }
    }
}
