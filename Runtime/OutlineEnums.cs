namespace Natteens.Outline
{
    public enum OutlineSelectionMode
    {
        Targets,
        Layers,
        TargetsAndLayers
    }

    public enum OutlineColorMode
    {
        Fixed,
        Adaptive
    }

    public enum OutlineDebugMode
    {
        None,
        Selection,
        ObjectIds,
        Silhouette,
        InternalDetail,
        DepthVisibility
    }
}
