using System;

namespace DDCImprover.Core
{
    public enum ImproverStatus
    {
        Idle,
        PreProcessing,
        GeneratingDD,
        PostProcessing,
        Completed,
        ProcessingError,
        LoadError,
        DDCError
    }
}
