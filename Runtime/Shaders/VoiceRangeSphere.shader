// Based off SkyProbe Fog by Silent licensed under MIT, see LICENSE_THIRD_PARTY.txt.
// git history has been kept from Silent's repository (make sure to follow renames).

// SkyProbe Fog by Silent
// version 3

// Based off Distance Fade Cube Volume
// by Neitri, free of charge, free to redistribute
// from https://github.com/netri/Neitri-Unity-Shaders

Shader "RP Menu/Voice Range Sphere"
{
    Properties
    {
        [Header(Base)]
        _Color("Color", Color) = (1,1,1,1)

        _InnerRadius("Inner Radius", Float) = 4.95
        _MiddleRadius("Middle Radius", Float) = 5
        _OuterRadius("Outer Radius", Float) = 8

        [Header(System)]
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode("Cull Mode", Float) = 1

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend("Src Factor", Float) = 5  // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlend("Dst Factor", Float) = 10 // OneMinusSrcAlpha
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Geometry+13"
            "RenderType" = "Custom"
            "ForceNoShadowCasting"="True"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
        }

        ZWrite Off
        ZTest Always
        Cull[_CullMode]
        Blend[_SrcBlend][_DstBlend]

        Pass
        {
            Lighting On
            Tags
            {
                "LightMode" = "Always"
            }
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            float4 _Color;
            float _InnerRadius;
            float _MiddleRadius;
            float _OuterRadius;

            sampler2D _CloudNoiseTexture; float4 _CloudNoiseTexture_TexelSize;
            UNITY_DECLARE_TEXCUBE(_ProbeTexture); float4 _ProbeTexture_HDR;

            struct appdata
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 vertex : POSITION;
            };

            struct v2f
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
                float4 pos : SV_POSITION;
                float4 depthTextureUv : TEXCOORD1;
                float4 rayFromCamera : TEXCOORD2;
                float4 worldPosition : TEXCOORD4;
                SHADOW_COORDS(3)
            };

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            // Dj Lukis.LT's oblique view frustum correction (VRChat mirrors use such view frustum)
            // https://github.com/lukis101/VRCUnityStuffs/blob/master/Shaders/DJL/Overlays/WorldPosOblique.shader
            inline float4 CalculateObliqueFrustumCorrection()
            {
                float x1 = -UNITY_MATRIX_P._31 / (UNITY_MATRIX_P._11 * UNITY_MATRIX_P._34);
                float x2 = -UNITY_MATRIX_P._32 / (UNITY_MATRIX_P._22 * UNITY_MATRIX_P._34);
                return float4(x1, x2, 0, UNITY_MATRIX_P._33 / UNITY_MATRIX_P._34 + x1 * UNITY_MATRIX_P._13 + x2 * UNITY_MATRIX_P._23);
            }
            inline float CorrectedLinearEyeDepth(float z, float correctionFactor)
            {
                return 1.f / (z / UNITY_MATRIX_P._34 + correctionFactor);
            }

            bool SceneZDefaultValue()
            {
                #if UNITY_REVERSED_Z
                    return 0.f;
                #else
                    return 1.f;
                #endif
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 worldPosition = mul(unity_ObjectToWorld, v.vertex);
                const fixed3 baseWorldPos = unity_ObjectToWorld._m03_m13_m23;
                o.pos = mul(UNITY_MATRIX_VP, worldPosition);
                o.depthTextureUv = ComputeGrabScreenPos(o.pos);
                // Warp ray by the base world position, so it's possible to have reoriented fog
                o.rayFromCamera.xyz = (worldPosition.xyz - _WorldSpaceCameraPos.xyz);
                o.rayFromCamera.w = dot(o.pos, CalculateObliqueFrustumCorrection()); // oblique frustrum correction factor
                o.worldPosition = worldPosition;
                //o.vertex2 = float4(UnityObjectToViewPos(v.pos), 1.0);
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                return o;
            }

            float3 worldPosFromDepth(float sceneZ, float3 camPos, float4 worldCoordinates)
            {
                sceneZ = DECODE_EYEDEPTH(sceneZ);

                // https://gamedev.stackexchange.com/questions/131978/shader-reconstructing-position-from-depth-in-vr-through-projection-matrix
                float3 viewDirection = (worldCoordinates.xyz - camPos) / (-mul(UNITY_MATRIX_V, worldCoordinates).z);

                return camPos + viewDirection * sceneZ;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float perspectiveDivide = 1.f / i.pos.w;
                float4 rayFromCamera = i.rayFromCamera * perspectiveDivide;

                // https://gamedev.stackexchange.com/questions/131978/shader-reconstructing-position-from-depth-in-vr-through-projection-matrix
                rayFromCamera.xyz = (i.worldPosition.xyz - _WorldSpaceCameraPos) / (-mul(UNITY_MATRIX_V, i.worldPosition).z);

                float2 depthTextureUv = i.depthTextureUv.xy * perspectiveDivide;
                const fixed3 baseWorldPos = unity_ObjectToWorld._m03_m13_m23;

                float sceneZ = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, depthTextureUv);
                bool isSkybox = (sceneZ == SceneZDefaultValue());
                clip(-isSkybox); // save the compute time of the rest of the shader

                // linearize depth and use it to calculate background world position
                float sceneDepth = CorrectedLinearEyeDepth(sceneZ, rayFromCamera.w*perspectiveDivide);
                float3 worldPosition = rayFromCamera.xyz * sceneDepth + _WorldSpaceCameraPos.xyz;
                worldPosition = worldPosFromDepth(sceneZ, _WorldSpaceCameraPos, i.worldPosition);

                float3 objectPosition = UNITY_MATRIX_M._m03_m13_m23;
                float dist = distance(objectPosition, worldPosition);

                clip(-(dist <= _InnerRadius || _OuterRadius <= dist));
                float opacity;
                if (dist <= _MiddleRadius)
                {
                    opacity = dist - _InnerRadius;
                    opacity = opacity / (_MiddleRadius - _InnerRadius) / 2;
                    opacity = sin(opacity * UNITY_PI);
                    opacity = opacity * opacity;
                }
                else
                {
                    opacity = dist - _MiddleRadius;
                    opacity = opacity / (_OuterRadius - _MiddleRadius) / 2;
                    opacity = 1 - sin(opacity * UNITY_PI);
                    opacity = pow(opacity, 4); // Should compile into just 2 multiplications.
                }
                float4 color = _Color;
                color.w *= opacity;
                return color;
            }

            ENDCG
        }
    }
}
