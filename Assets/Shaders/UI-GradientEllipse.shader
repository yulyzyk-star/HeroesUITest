Shader "UI/GradientEllipse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        
        [Header(Ellipse Color)]
        _EllipseColor ("Ellipse Color", Color) = (1, 1, 1, 1)
        _EllipseOpacity ("Ellipse Opacity", Range(0, 1)) = 0.5
        [KeywordEnum(Replace, Multiply, Screen, Overlay)] _BlendMode ("Blend Mode", Float) = 1
        
        [Header(Background Gradient)]
        _GradientTopColor ("Gradient Top Color", Color) = (0.2, 0.4, 0.8, 1)
        _GradientBottomColor ("Gradient Bottom Color", Color) = (0.8, 0.2, 0.4, 1)
        [Toggle] _HorizontalGradient ("Horizontal Gradient", Float) = 0
        
        [Header(Ellipse Settings)]
        _Center ("Center (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius (X, Y)", Vector) = (0.5, 0.5, 0, 0)
        _Softness ("Edge Softness", Range(0, 1)) = 0.1
        
        [Header(Gradient Curve)]
        _Power ("Gradient Power", Range(0.1, 5)) = 1.0
        
        // UI Masking
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _BLENDMODE_REPLACE _BLENDMODE_MULTIPLY _BLENDMODE_SCREEN _BLENDMODE_OVERLAY
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _EllipseColor;
            float _EllipseOpacity;
            fixed4 _GradientTopColor;
            fixed4 _GradientBottomColor;
            float _HorizontalGradient;
            float4 _Center;
            float4 _Radius;
            float _Softness;
            float _Power;
            float4 _ClipRect;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Получаем текстуру
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // Вычисляем фоновый градиент
                float gradientT = _HorizontalGradient > 0.5 ? i.uv.x : i.uv.y;
                fixed4 backgroundColor = lerp(_GradientBottomColor, _GradientTopColor, gradientT);
                
                // Вычисляем расстояние от центра эллипса
                float2 center = _Center.xy;
                float2 radius = max(_Radius.xy, 0.001); // Защита от деления на 0
                
                // Нормализованное расстояние (эллипс)
                float2 diff = (i.uv - center) / radius;
                float dist = length(diff);
                
                // Применяем power для управления кривой градиента
                dist = pow(saturate(dist), _Power);
                
                // Мягкий край (1 = внутри эллипса, 0 = снаружи)
                float ellipseMask = 1.0 - smoothstep(1.0 - _Softness, 1.0 + _Softness, dist);
                
                // Сила влияния эллипса (opacity * mask)
                float ellipseInfluence = ellipseMask * _EllipseOpacity;
                
                // Вычисляем цвет в зависимости от режима смешивания
                fixed4 blendedColor;
                
                #if _BLENDMODE_REPLACE
                    // Просто заменяем цвет
                    blendedColor = _EllipseColor;
                #elif _BLENDMODE_MULTIPLY
                    // Умножение - затемняет, сохраняя градиент
                    blendedColor = backgroundColor * _EllipseColor;
                #elif _BLENDMODE_SCREEN
                    // Screen - осветляет
                    blendedColor = 1.0 - (1.0 - backgroundColor) * (1.0 - _EllipseColor);
                #elif _BLENDMODE_OVERLAY
                    // Overlay - комбинация multiply и screen
                    fixed4 overlay;
                    overlay.r = backgroundColor.r < 0.5 ? 2.0 * backgroundColor.r * _EllipseColor.r : 1.0 - 2.0 * (1.0 - backgroundColor.r) * (1.0 - _EllipseColor.r);
                    overlay.g = backgroundColor.g < 0.5 ? 2.0 * backgroundColor.g * _EllipseColor.g : 1.0 - 2.0 * (1.0 - backgroundColor.g) * (1.0 - _EllipseColor.g);
                    overlay.b = backgroundColor.b < 0.5 ? 2.0 * backgroundColor.b * _EllipseColor.b : 1.0 - 2.0 * (1.0 - backgroundColor.b) * (1.0 - _EllipseColor.b);
                    overlay.a = _EllipseColor.a;
                    blendedColor = overlay;
                #else
                    blendedColor = _EllipseColor;
                #endif
                
                // Смешиваем с учётом маски эллипса
                fixed4 finalColor = lerp(backgroundColor, blendedColor, ellipseInfluence);
                
                // Применяем текстуру и vertex color
                finalColor = texColor * finalColor * i.color;
                
                // UI Clipping
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                
                return finalColor;
            }
            ENDCG
        }
    }
}

