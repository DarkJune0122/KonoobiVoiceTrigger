// Threshold shader 

// Object Declarations

sampler2D implicitInput : register(s0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 main(float2 uv : TEXCOORD) : COLOR
{
    float4 color = tex2D(implicitInput, uv);
    float mono = (0.2125 * color.r) + (0.7154 * color.g) + (0.0721 * color.b);
    return float4(mono, mono, mono, color.a);
}