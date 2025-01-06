namespace CustomRP.Runtime.Volume
{
    enum PostFX_Pass
    {
        CopyWithPoint,
        CopyWithLinear,
    
        BloomVertical,
        BloomHorizontal,
        BloomCombinePass,
        BloomPrefilter,
    
        EdgeDectectWithNormalDepth,
        EdgeDectectWithSurfaceIdDepth,
        DrawEdgePixel
    }
}