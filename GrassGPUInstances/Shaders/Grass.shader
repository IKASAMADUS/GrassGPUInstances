Shader "TA Shaders/Grass"
{
    Properties
    {
        [Header(Main)]
        [HideInInspector][Toggle] _UseBaseMap("UseBaseMap", float) = 0
        [HideInInspector][MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor][HideInInspector] _BaseColor("Firstly Color", Color) = (1,1,1,1)
        [HideInInspector]_GroundColor ("GroundColor", Color) = (1,1,1,1)
        [HideInInspector]_SecondColor ("Secondary Color", Color) = (1,1,1,1)
        [HideInInspector]_SecondColorOffset("Offset", Range(-1,1)) = 0
		[HideInInspector]_SecondColorFade("Fade", Range( -1 , 1)) = 0.5
        [HideInInspector]_WorldScale("World Scale", float) = 1
        [HideInInspector]_TranslucencyInt("Translucency Intensity", Range(0,1)) = 0.5
        [HideInInspector]_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.6

        [Space(10)]
        [Header(Wind Paramers)]
        [HideInInspector]_Wind("Wind(x,y,z,str)",Vector) = (1,0,1,2)
        [HideInInspector]_WindNoiseStrength("WindNoiseStr",Range(0,20)) = 4.8
        
        [HideInInspector][Toggle]_StormToggle("StormToggle",float) = 0
        [HideInInspector]_StormParams("Storm(Begin,Keep,End,Slient)",Vector) = (1,100,40,100)
        [HideInInspector]_StormStrength("StormStrength",Range(0,40)) = 20
        [Space(10)]
        [Header(Grass Paramers)]
        [HideInInspector][Toggle] _GrowDirToggle ("GrowDirection", float) = 1
        [HideInInspector]_Height("Height Noise",float) = 0.5
    }

    SubShader
    {

        Tags
        {
            "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" "Queue"="Geometry"
        }
        LOD 300
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        //CBUFFER_START(UnityPerMaterial)
        //MainColor
        half _UseBaseMap;
        half4 _BaseColor;
        half4 _GroundColor;
        float4 _BaseMap_ST;
        //Matrix
        float4x4 _TerrianLocalToWorld;
        //Wind Paramers
        float4 _Wind;
        float _WindNoiseStrength;
        float4 _StormParams;
        float _StormStrength;

        half _GrowDirToggle;
        half _Cutoff;
        
        half4 _SecondColor;
        half _WorldScale;
        half _SecondColorOffset;
        half _SecondColorFade;
        half _TranslucencyInt;
        float _Height;
        half _StormToggle;
        //CBUFFER_END
        // UNITY_INSTANCING_BUFFER_START(Props)
        // UNITY_DEFINE_INSTANCED_PROP(float4x4, _GrassInfos)
        // UNITY_INSTANCING_BUFFER_END(Props)
        // ------------------------------------------------------------------
        half _Fade;
        float3 _GrassQuadSize;
        #define StormFront _StormParams.x
        #define StormMiddle _StormParams.y
        #define StormEnd _StormParams.z
        #define StormSlient _StormParams.w
        
        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL
        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass
        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite On
            ZTest On
            Cull Off

            HLSLPROGRAM
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
        		float4 tangentOS : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 color : COLOR;
            	float4 shadowCoord : TEXCOORD3;
            	float3 tangentWS : TEXCOORD4;
            	float3 bitangentWS : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #pragma vertex PassVertex
            #pragma fragment PassFragment

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                struct GrassInfo{
                    float4x4 localToTerrian;
                    float4 texParams;
                };
                StructuredBuffer<GrassInfo> _GrassInfos;
            #endif

            void setup()
            {
            }

            float2 unity_gradientNoise_dir(float2 p)
            {
                p = p % 289;
                float x = (34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float unity_gradientNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(unity_gradientNoise_dir(ip), fp);
                float d01 = dot(unity_gradientNoise_dir(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(unity_gradientNoise_dir(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(unity_gradientNoise_dir(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                return lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x);
            }

            ///根据风力，计算顶点的世界坐标偏移
            ///positionWS - 顶点的世界坐标
            ///grassUpWS - 草的生长方向
            ///windDir - 是风的方向，应该为单位向量
            ///windStrength - 风力强度,范围(0~1)
            ///vertexLocalHeight - 顶点在草面片空间中的高度
            float3 applyWind(float3 positionWS, float3 grassUpWS, float3 windDir, float windStrength,
                             float vertexLocalHeight, int instanceID)
            {
                //根据风力，计算草弯曲角度，从0到90度
                float rad = windStrength * PI * 0.9 / 2;


                //得到wind与grassUpWS的正交向量
                windDir = normalize(windDir - dot(windDir, grassUpWS) * grassUpWS);

                float x, y; //弯曲后,x为单位球在wind方向计量，y为grassUp方向计量
                sincos(rad, x, y);

                //offset表示grassUpWS这个位置的顶点，在风力作用下，会偏移到windedPos位置
                float3 windedPos = x * windDir + y * grassUpWS;

                //加上世界偏移
                return (windedPos - grassUpWS) * vertexLocalHeight;
            }

            float applyStorm(float3 positionWS,float3 windDir,float windStrength){
                //首先，计算世界坐标在风向上的投影距离，乘以一个时间time,就可以让这个值随着时间移动
            
                float stormInterval = StormFront + StormMiddle + StormEnd + StormSlient;
            
                float disInWindDir = dot(positionWS - (windDir * _Time.y) * (windStrength + _StormStrength),windDir);
            
                //范围为0 ~ stormInterval
                float offsetInInterval = stormInterval - (disInWindDir % stormInterval) - step(disInWindDir,0) * stormInterval;
            
                float x = 0;
                if(offsetInInterval < StormFront){
                    //前部,x从0到1
                    x = offsetInInterval * rcp(StormFront);
                }else if(offsetInInterval < StormFront + StormMiddle){
                    //中部
                    x = 1;
                }
                else if(offsetInInterval < StormFront + StormMiddle + StormEnd){
                    //尾部,x从1到0
                    x = (StormFront + StormMiddle + StormEnd - offsetInInterval) / StormEnd;
                }
            
                //基础风力 + 强力风力
                return windStrength + _StormStrength * x;               
            }
            half3 ApplySingleDirectLight(Varyings input, half3 albedo, float positionOSY)
            {
                Light light = GetMainLight(input.shadowCoord,input.positionWS,0);
            	half3 normalWS = normalize( input.normalWS);
            	half3 tangentWS = normalize( input.tangentWS);
            	half3 bitangentWS = normalize( input.bitangentWS);
            	half3 lightWS = normalize(light.direction);
            	half3 viewWS = normalize(GetWorldSpaceViewDir(input.positionWS));
            	half3 halfDir = normalize(lightWS + viewWS);
            	half3 tanToWorld0 = half3(tangentWS.x,bitangentWS.x,normalWS.x);
            	half3 tanToWorld1 = half3(tangentWS.y,bitangentWS.y,normalWS.y);
            	half3 tanToWorld2 = half3(tangentWS.z,bitangentWS.z,normalWS.z);
            	half3 tanNormal = half3(0,0,1);
            	half3 currentNormal = normalize( float3(dot(tanToWorld0,tanNormal),dot(tanToWorld1,tanNormal),dot(tanToWorld2,tanNormal)));
                half NoL = saturate(dot(normalWS, lightWS));
                half NoV = saturate(dot(normalWS, viewWS));
                //direct diffuse 
                half directDiffuse = dot(currentNormal, lightWS) * 0.5 + 0.5;
                half3 direct = albedo * directDiffuse;
                //direct specular
                float directSpecular = saturate(dot(currentNormal,halfDir));
                //pow(directSpecular,8)
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= directSpecular;
                directSpecular *= 0.1 * positionOSY;

                half lightAtten = light.shadowAttenuation * light.distanceAttenuation;
                half shadow = MainLightRealtimeShadow(input.shadowCoord);
                half3 lightColor = light.color.rgb * lightAtten;

                float TranslucencyMask = 1 - NoV * 1.0 -0.2;
            	float3 Translucency = saturate(TranslucencyMask * ((NoL + 1) * lightAtten) * light.color * albedo * 0.25) * _TranslucencyInt;
                
                half3 result = ( direct + directSpecular + Translucency) * lightColor ;
                result *= shadow;
                return result;
            }

            Varyings PassVertex(Attributes input, uint InstanceID : SV_InstanceID)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float2 uv = TRANSFORM_TEX(input.uv, _BaseMap);
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;
                uint instanceID = InstanceID;
                
                positionOS.xy = positionOS.xy * _GrassQuadSize.xy;
                float localVertexHeight = positionOS.y;

                float3 grassUpDir = float3(0, 1, 0);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassInfo grassInfo = _GrassInfos[instanceID];

                    //将顶点和法线从Quad本地空间转换到Terrian本地空间
                    positionOS = mul(grassInfo.localToTerrian,float4(positionOS,1)).xyz;
                    normalOS = mul(grassInfo.localToTerrian,float4(normalOS,0)).xyz;
                    grassUpDir = mul(grassInfo.localToTerrian,float4(grassUpDir,0)).xyz;

                    //UV偏移缩放
                    uv = uv * grassInfo.texParams.xy + grassInfo.texParams.zw;

                #endif

                float4 positionWS = mul(_TerrianLocalToWorld, float4(positionOS, 1));
                positionWS /= positionWS.w;
                float3 pos = positionWS.xyz;
                float height = unity_gradientNoise(positionWS.xz * (_Height * positionWS.y));
                positionWS.y += height;
                float2 posXZ;
                float2 dir = saturate(positionWS.xz * -_Fade + _GrassQuadSize.z - lerp(0, _GrassQuadSize.z * 2, _Fade));
                float2 invertDir = saturate(positionWS.zx * -_Fade + _GrassQuadSize.z - lerp(0, _GrassQuadSize.z * 2, _Fade));
                posXZ = _GrowDirToggle == 1 ? invertDir : dir;
                positionWS.y *= posXZ.y;
                grassUpDir = normalize(mul(_TerrianLocalToWorld, float4(grassUpDir, 0)).xyz);

                float time = _Time.y;

                float3 windDir = normalize(_Wind.xyz);

                float windScale = _Wind.w;


                //定时生成一波大风，带来麦浪的感觉
                float windStrength = _StormToggle == 1 ? applyStorm(pos.xyz,windDir,windScale) : windScale;
                
                float2 noiseUV = frac((positionWS.xz - time) / 30);
                float noiseValue = unity_gradientNoise(noiseUV * 30);
                noiseValue = sin(noiseValue * windStrength);
                
                windStrength += noiseValue * _WindNoiseStrength;
                
                windStrength = saturate(windStrength / 30);
                float3 wind = applyWind(positionWS.xyz, grassUpDir.xyz, windDir.xyz, windStrength, localVertexHeight,
                                        instanceID);
                positionWS.xyz += wind;
                output.uv = uv;

                output.uv = _GrowDirToggle == 1
                                ? float2(output.uv.x *= posXZ.y, output.uv.y)
                                : float2(output.uv.x, output.uv.y *= posXZ.y);

                output.positionWS = positionWS.xyz;
                output.positionCS = mul(UNITY_MATRIX_VP, positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				output.tangentWS = TransformObjectToWorldDir(input.tangentOS.xyz);
            	output.bitangentWS = cross(output.normalWS,output.tangentWS) * input.tangentOS.w * GetOddNegativeScale();
                output.shadowCoord = TransformWorldToShadowCoord(positionWS.xyz);
                
                half colorMask = unity_gradientNoise(positionWS.xz * _WorldScale * 0.5 + 0.5) ;
                float SecondColorMask = saturate( (colorMask + (_SecondColorOffset)) * (_SecondColorFade * 2));
            	half3 noiseColor = lerp( _BaseColor.rgb,_SecondColor.rgb,SecondColorMask);
                
                half3 albedo = lerp(_GroundColor.rgb, noiseColor, input.positionOS.y);

                half3 ambient = SampleSH(half3(0,1,0)) * albedo;
				output.color = 0;
                //main direct light
                half3 lightingResult = ApplySingleDirectLight(output, albedo, positionOS.y);
                output.color += lightingResult + ambient;
				
                return output;
            }

            half4 PassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 sampleBase = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = _UseBaseMap == 1 ? input.color * sampleBase.rgb : input.color;
                float alpha = sampleBase.a;
            	clip(alpha - _Cutoff);
                return half4(albedo, 1);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}