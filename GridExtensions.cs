// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci | https://github.com/matt-mert
// ========================================================================================

namespace GridSystem
{
    public static class GridExtensions
    {
        internal static GridAxis ToWidthAxis(this GridAxis normal)
        {
            return normal switch
            {
                GridAxis.x => GridAxis.z,
                GridAxis.y => GridAxis.x,
                GridAxis.z => GridAxis.x,
                _ => default
            };
        }

        internal static GridAxis ToHeightAxis(this GridAxis normal)
        {
            return normal switch
            {
                GridAxis.x => GridAxis.y,
                GridAxis.y => GridAxis.z,
                GridAxis.z => GridAxis.y,
                _ => default
            };
        }

        public static int GetWidth(this (int, int, int) coords, GridAxis normal)
        {
            return normal switch
            {
                GridAxis.x => coords.Item3,
                GridAxis.y => coords.Item1,
                GridAxis.z => coords.Item1,
                _ => default
            };
        }

        public static int GetHeight(this (int, int, int) coords, GridAxis normal)
        {
            return normal switch
            {
                GridAxis.x => coords.Item2,
                GridAxis.y => coords.Item3,
                GridAxis.z => coords.Item2,
                _ => default
            };
        }

        public static int GetDepth(this (int, int, int) coords, GridAxis normal)
        {
            return normal switch
            {
                GridAxis.x => coords.Item1,
                GridAxis.y => coords.Item2,
                GridAxis.z => coords.Item3,
                _ => default
            };
        }

        public static void SetWidth(this ref (int, int, int) coords, GridAxis normal, int value)
        {
            switch (normal)
            {
                case GridAxis.x:
                    coords.Item3 = value;
                    break;
                case GridAxis.y:
                    coords.Item1 = value;
                    break;
                case GridAxis.z:
                    coords.Item1 = value;
                    break;
            }
        }

        public static void SetHeight(this ref (int, int, int) coords, GridAxis normal, int value)
        {
            switch (normal)
            {
                case GridAxis.x:
                    coords.Item2 = value;
                    break;
                case GridAxis.y:
                    coords.Item3 = value;
                    break;
                case GridAxis.z:
                    coords.Item2 = value;
                    break;
            }
        }

        public static void SetDepth(this ref (int, int, int) coords, GridAxis normal, int value)
        {
            switch (normal)
            {
                case GridAxis.x:
                    coords.Item1 = value;
                    break;
                case GridAxis.y:
                    coords.Item2 = value;
                    break;
                case GridAxis.z:
                    coords.Item3 = value;
                    break;
            }
        }

        public static (int, int, int) ToCoords(this GridAxis axis)
        {
            return axis switch
            {
                GridAxis.x => (1, 0, 0),
                GridAxis.y => (0, 1, 0),
                GridAxis.z => (0, 0, 1),
                _ => default
            };
        }

        public static (int, int, int) ToCoords(this GridAxis axis, int i)
        {
            return axis switch
            {
                GridAxis.x => (i, 0, 0),
                GridAxis.y => (0, i, 0),
                GridAxis.z => (0, 0, i),
                _ => default
            };
        }

        public static IGridVolume GetVolume(this IGridObject obj)
        {
            return obj.GridUnit.Volume;
        }

        public static GridVolume<T> GetVolume<T>(this T obj) where T : class, IGridObject
        {
            return ((GridUnit<T>)obj.GridUnit).Volume;
        }

        public static IGridSurface GetSurface(this IGridObject obj)
        {
            return obj.GridUnit.Surface;
        }

        public static GridSurface<T> GetSurface<T>(this T obj) where T : class, IGridObject
        {
            return ((GridUnit<T>)obj.GridUnit).Surface;
        }

        public static IGridLine GetLine(this IGridObject obj)
        {
            return obj.GridUnit.Line;
        }

        public static GridLine<T> GetLine<T>(this T obj) where T : class, IGridObject
        {
            return ((GridUnit<T>)obj.GridUnit).Line;
        }

        public static IGridUnit GetUnit(this IGridObject obj)
        {
            return obj.GridUnit;
        }

        public static GridUnit<T> GetUnit<T>(this T obj) where T : class, IGridObject
        {
            return (GridUnit<T>)obj.GridUnit;
        }
    }
}
