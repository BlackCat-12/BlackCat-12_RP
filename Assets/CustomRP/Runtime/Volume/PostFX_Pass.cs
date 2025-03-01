namespace CustomRP.Runtime.Volume
{
    public enum PostFX_Pass
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
        DrawEdgePixel,
        
        ColorGradingNone,
        ColorGradingACES,
        ColorGradingNeutral,
        ColorGradingReinhard,
        Final
    }
}