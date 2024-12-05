// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci | https://github.com/matt-mert
// ========================================================================================

namespace GridSystem
{
    public static class GridUtilities
    {
        internal static int GetWidth(GridAxis normal, int x, int y, int z)
        {
            return normal switch
            {
                GridAxis.x => z,
                GridAxis.y => x,
                GridAxis.z => x,
                _ => default
            };
        }

        internal static int GetHeight(GridAxis normal, int x, int y, int z)
        {
            return normal switch
            {
                GridAxis.x => y,
                GridAxis.y => z,
                GridAxis.z => y,
                _ => default
            };
        }

        internal static int GetDepth(GridAxis normal, int x, int y, int z)
        {
            return normal switch
            {
                GridAxis.x => x,
                GridAxis.y => y,
                GridAxis.z => z,
                _ => default
            };
        }

        internal static int GetLength(GridAxis axis, GridAxis normal, int width, int height)
        {
            return normal.ToWidthAxis() == axis ? width : height;
        }

        internal static int GetOther(GridAxis axis, GridAxis normal, int width, int height)
        {
            return normal.ToWidthAxis() == axis ? height : width;
        }

        internal static GridAxis GetOtherAxis(GridAxis axis, GridAxis normal)
        {
            var widthAxis = normal.ToWidthAxis();
            return widthAxis == axis ? normal.ToHeightAxis() : widthAxis;
        }

        internal static (int, int, int) ConvertToVector(GridAxis normal, int width, int height, int depth)
        {
            return normal switch
            {
                GridAxis.x => (depth, height, width),
                GridAxis.y => (width, depth, height),
                GridAxis.z => (width, height, depth),
                _ => default
            };
        }

        internal static (int, int, int) ConvertToVector(GridAxis axis, GridAxis normal, int length, int other)
        {
            var isWidth = normal.ToWidthAxis() == axis;
            return normal switch
            {
                GridAxis.x => isWidth ? (0, other, length) : (0, length, other),
                GridAxis.y => isWidth ? (length, 0, other) : (other, 0, length),
                GridAxis.z => isWidth ? (length, other, 0) : (other, length, 0),
                _ => default
            };
        }
    }
}
