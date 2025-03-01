namespace CustomRP.Runtime.Volume
{
    enum PostFX_Pass
    {
        CopyWithPoint,
        CopyWithLinear,
    
        BloomVertical,
        BloomHorizontal,
        BloomPrefilter,
        BloomPrefilterFireflies,
        BloomCombinePass,
    
        EdgeDectectWithNormalDepth,
        EdgeDectectWithSurfaceIdDepth,
        DrawEdgePixel
    }
}