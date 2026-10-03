// The CCTV look on a camera feed shown in UI (GDD §04.1, docs/LOOKDEV.md §2, P1.07): greyscale, contrast,
// grain that changes every frame, scan lines and a vignette. Grain and scan lines are per *feed* pixel —
// the feed's own resolution, _FeedSize — so an upscaled 320×180 feed still reads as 320×180.
// Parameters come from a CctvFilterProfile (CctvFeedView sets them).
Shader "LSW/UI/CctvFeed"
{
    Properties
    {
        [PerRendererData] _MainTex ("Feed", 2D) = "white" {}
        _FeedSize ("Feed size (px)", Vector) = (320, 180, 0, 0)
        _Contrast ("Contrast", Range(0.2, 3)) = 1.25
        _Brightness ("Brightness", Range(-0.5, 0.5)) = 0
        _Grain ("Grain", Range(0, 0.5)) = 0.08
        _ScanLines ("Scan lines", Range(0, 0.5)) = 0.12
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _GrainRate ("Grain frames per second", Float) = 24
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _FeedSize;
            float _Contrast;
            float _Brightness;
            float _Grain;
            float _ScanLines;
            float _Vignette;
            float _GrainRate;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pixel = floor(i.uv * _FeedSize.xy);
                float3 rgb = tex2D(_MainTex, (pixel + 0.5) / _FeedSize.xy).rgb;

                float grey = dot(rgb, float3(0.299, 0.587, 0.114));
                grey = (grey - 0.5) * _Contrast + 0.5 + _Brightness;

                float frame = floor(_Time.y * _GrainRate);
                grey += (hash12(pixel + frame * 17.0) - 0.5) * _Grain;
                grey *= 1.0 - _ScanLines * fmod(pixel.y, 2.0);

                float2 centred = i.uv * 2.0 - 1.0;
                grey *= 1.0 - _Vignette * saturate(dot(centred, centred) * 0.5);

                grey = saturate(grey);
                return fixed4(grey, grey, grey, i.color.a);
            }
            ENDCG
        }
    }
}
