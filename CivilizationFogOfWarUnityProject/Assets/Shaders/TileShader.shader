Shader "Lexdev/CaseStudies/CivilizationMap"
{
    Properties
    {
        //Hex texture
        [NoScaleOffset]_MainTex ("Color Texture", 2D) = "white" {}
        [NoScaleOffset]_MapTex ("Map Texture", 2D) = "white" {}

        //Perlin noise texture
        [NoScaleOffset]_Noise ("Noise", 2D) = "black" {}

        //Cutoff value that determines at which visibility value the hex tile becomes visible
        _Cutoff("Map Cutoff", float) = 0.4

        //Mak color
        _MapColor("Map Color", Color) = (1,1,1,1)
        _MapEdgeColor("Map Edge Color", Color) = (1,1,1,1)

        //Map background color
        [NoScaleOffset]_MapBackground("Map Background Texture", 2D) = "white" {}
    }
    SubShader
    {
        CGPROGRAM

        //Our surface function is called surf and we are using the standard lighting
        #pragma surface surf Standard

        //Global variables

        //The generated mask from the compute shader
        sampler2D _Mask;
        //The size of our hex grid in units
        float _MapSize;

        //Input struct with additional data we need
        struct Input
        {
            float2 uv_MainTex;
            float2 uv_MapTex;
            float3 worldPos;
        };

        //Property variables
        sampler2D _MainTex;
        sampler2D _MapTex;

        sampler2D _Noise;

        float _Cutoff;

        float4 _MapColor;
        float4 _MapEdgeColor;
        sampler2D _MapBackground;

        //Surface function, we are only interested in setting the albedo we can leave the rest as default
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            //Sample the visibility texture
            float maskVal = tex2D(_Mask, IN.worldPos.xz / _MapSize).r;

            //Sample the tile textures
            float4 tile = tex2D(_MainTex, IN.uv_MainTex);
            float4 tileMap = tex2D(_MapTex, IN.uv_MapTex);

            //Sample the map background texture
            float4 mapBackground = tex2D(_MapBackground, IN.worldPos.xz / _MapSize);

            //Sample the noise texture
            float noise = tex2D(_Noise, IN.worldPos.xz / _MapSize).r;

            //Add noise to the blend area to make the edges of the map more random
            float maskNoise = clamp(maskVal - pow(1.0f - maskVal, 0.01f) * noise, 0, 1);

            //Render the map if the calculated value is smaller than our cutoff
            if(maskNoise < _Cutoff)
                tile = lerp(_MapColor * tileMap * mapBackground, _MapEdgeColor, maskNoise / _Cutoff);

            //Assign the color
            o.Albedo = tile.rgb;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
