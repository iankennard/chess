Shader "text" {

Properties
{
    _Color ("Tint Color (A = Opacity)", Color) = (1,1,1)
    _MainTex ("Texture  (A = Opacity)", 2D) = ""
}
 
SubShader 
{
    Tags {Queue = Transparent}
    ZWrite Off
    Colormask RGB
    Blend SrcAlpha OneMinusSrcAlpha
    
    Color [_Color]
    Pass
    {       
        SetTexture [_MainTex]
        {
            combine texture * primary
        }       
    }
}

}