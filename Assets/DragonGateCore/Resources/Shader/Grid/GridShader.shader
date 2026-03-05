Shader "Custom/Grid"
{
    Properties
    {
        _MinorColor ("Minor Line Color", Color) = (1,1,1,0.4)
        _MajorColor ("Major Line Color", Color) = (1,1,1,0.8)
        _AxisColor ("Axis Color", Color) = (1,0.3,0.3,1)

        _CellSize ("Cell Size", Float) = 1
        _MajorInterval ("Major Interval", Float) = 10

        _MinorWidth ("Minor Line Width", Float) = 0.02
        _MajorWidth ("Major Line Width", Float) = 0.05

        _FadeStart ("Fade Start Distance", Float) = 30
        _FadeEnd ("Fade End Distance", Float) = 60
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _MinorColor;
            float4 _MajorColor;
            float4 _AxisColor;

            float _CellSize;
            float _MajorInterval;

            float _MinorWidth;
            float _MajorWidth;

            float _FadeStart;
            float _FadeEnd;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewPos = UnityWorldSpaceViewDir(o.worldPos);
                return o;
            }

            float gridLine(float2 uv, float width)
            {
                float2 f = frac(uv);
                float2 d = min(f, 1 - f);
                return step(d.x, width) + step(d.y, width);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 gridUV = i.worldPos.xz / _CellSize;

                // Minor grid
                float minor = saturate(gridLine(gridUV, _MinorWidth));

                // Major grid
                float2 majorUV = gridUV / _MajorInterval;
                float major = saturate(gridLine(majorUV, _MajorWidth));

                // Axis highlight (world origin)
                float axisX = step(abs(i.worldPos.x), _MajorWidth);
                float axisZ = step(abs(i.worldPos.z), _MajorWidth);
                float axis = max(axisX, axisZ);

                // Distance fade
                float distance = length(_WorldSpaceCameraPos - i.worldPos);
                float fade = saturate((_FadeEnd - distance) / (_FadeEnd - _FadeStart));

                float4 color = 0;

                color = lerp(color, _MinorColor, minor);
                color = lerp(color, _MajorColor, major);
                color = lerp(color, _AxisColor, axis);

                color.a *= fade;

                return color;
            }

            ENDHLSL
        }
    }
}