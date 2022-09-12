using System;

#if UNITY_2021_1_OR_NEWER
namespace Voxul.Utilities.RectanglePacker
{
    /// <summary>
    /// Specifies hints that help optimize the rectangle packing algorithm. 
    /// </summary>
    [Flags]
    public enum PackingHints
    {
        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by area.</summary>
        TryByArea = 1,

        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by perimeter.</summary>
        TryByPerimeter = 2,

        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by bigger side.</summary>
        TryByBiggerSide = 4,

        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by width.</summary>
        TryByWidth = 8,

        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by height.</summary>
        TryByHeight = 16,

        /// <summary>Tells the rectangle packer to try inserting the rectangles ordered by a pathological multiplier.</summary>
        TryByPathologicalMultiplier = 32,

        /// <summary>Specifies to try all the possible hints, as to find the best packing configuration.</summary>
        FindBest = TryByArea | TryByPerimeter | TryByBiggerSide | TryByWidth | TryByHeight | TryByPathologicalMultiplier,

        /// <summary>Specifies hints to optimize for rectangles who have one side much bigger than the other.</summary>
        UnusualSizes = TryByPerimeter | TryByBiggerSide | TryByPathologicalMultiplier,

        /// <summary>Specifies hints to optimize for rectangles whose sides are relatively similar.</summary>
        MostlySquared = TryByArea | TryByBiggerSide | TryByWidth | TryByHeight,
    }

}
#endif