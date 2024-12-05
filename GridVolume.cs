// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci | https://github.com/matt-mert
// ========================================================================================

using System;
using System.Collections;
using System.Collections.Generic;

namespace GridSystem
{
    /// <summary>
    /// Base interface for 3d volume-based grids
    /// </summary>
    public interface IGridVolume : IEnumerable<IGridUnit>
    {
        int Width { get; }
        int Height { get; }
        int Depth { get; }
        int Count { get; }
        int Dimension { get; }
        IGridUnit this[int i, int j, int k] { get; }
        IGridUnit this[(int, int, int) coords] { get; }
        IGridSurface GetSurface(GridAxis normal, int index);
        IGridUnit GetUnit(int i, int j, int k);
        IGridUnit GetUnit((int, int, int) coords);
        IGridUnit[] GetUnits();
        IGridUnit[] GetUnits((int, int, int) from, (int, int, int) to);
        void CreateVolume<T>(Func<T> getter) where T : class, IGridObject;
        void RemoveVolume<T>(Action<T> collector) where T : class, IGridObject;
        void ResizeVolume(int width, int height, int depth);
        void TrimVolume((int, int, int) from, (int, int, int) to);
        void DisposeUnits();
        void AddSurface(GridAxis normal);
        void InsertSurface(GridAxis normal, int index);
        void RemoveSurface(GridAxis normal, int index);
    }

    /// <summary>
    /// Volume-based grid that contains surfaces, lines, and units on three axes
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridVolume<T> : IGridVolume where T : class, IGridObject
    {
        public int Dimension => 3;
        public int Width => _surfaces[GridAxis.x].Count;
        public int Height => _surfaces[GridAxis.y].Count;
        public int Depth => _surfaces[GridAxis.z].Count;
        public int Count => Width * Height * Depth;

        private readonly Dictionary<GridAxis, List<GridSurface<T>>> _surfaces;
        private readonly Dictionary<GridAxis, Dictionary<(int, int, int), GridLine<T>>> _lines;
        private readonly Dictionary<(int, int, int), GridUnit<T>> _units;
        private Func<T> _getter;

        public GridVolume(int width, int height, int depth)
        {
            _units = new Dictionary<(int, int, int), GridUnit<T>>();
            _surfaces = new Dictionary<GridAxis, List<GridSurface<T>>>();
            _lines = new Dictionary<GridAxis, Dictionary<(int, int, int), GridLine<T>>>
            {
                [GridAxis.x] = new(),
                [GridAxis.y] = new(),
                [GridAxis.z] = new()
            };

            GenerateVolume(width, height, depth);
        }

        IGridUnit IGridVolume.this[int i, int j, int k] => _units[(i, j, k)];

        IGridUnit IGridVolume.this[(int, int, int) coords] => _units[(coords.Item1, coords.Item2, coords.Item3)];

        public GridUnit<T> this[int i, int j, int k] => _units[(i, j, k)];

        public GridUnit<T> this[(int, int, int) coords] => _units[(coords.Item1, coords.Item2, coords.Item3)];

        public IEnumerator<IGridUnit> GetEnumerator() => _units.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IGridSurface IGridVolume.GetSurface(GridAxis normal, int index) => _surfaces[normal][index];

        public GridSurface<T> GetSurface(GridAxis normal, int index) => _surfaces[normal][index];

        IGridUnit IGridVolume.GetUnit(int i, int j, int k) => _units[(i, j, k)];

        IGridUnit IGridVolume.GetUnit((int, int, int) coords) => _units[coords];

        public GridUnit<T> GetUnit(int i, int j, int k) => _units[(i, j, k)];

        public GridUnit<T> GetUnit((int, int, int) coords) => _units[coords];

        IGridUnit[] IGridVolume.GetUnits()
        {
            var result = new IGridUnit[Count];
            var counter = 0;
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++) for (var k = 0; k < Depth; k++)
            {
                result[counter] = _units[(i, j, k)];
                counter++;
            }

            return result;
        }

        public GridUnit<T>[] GetUnits()
        {
            var result = new GridUnit<T>[Count];
            var counter = 0;
            foreach (var pair in _units)
            {
                result[counter] = pair.Value;
                counter++;
            }

            return result;
        }

        IGridUnit[] IGridVolume.GetUnits((int, int, int) from, (int, int, int) to)
        {
            var result = new IGridUnit[Count];
            var counter = 0;
            for (var i = from.Item1; i < to.Item1; i++)
            for (var j = from.Item2; j < to.Item2; j++)
            for (var k = from.Item3; k < to.Item3; k++)
            {
                result[counter] = _units[(i, j, k)];
                counter++;
            }

            return result;
        }

        public GridUnit<T>[] GetUnits((int, int, int) from, (int, int, int) to)
        {
            var result = new GridUnit<T>[Count];
            var counter = 0;
            for (var i = from.Item1; i < to.Item1; i++)
            for (var j = from.Item2; j < to.Item2; j++)
            for (var k = from.Item3; k < to.Item3; k++)
            {
                result[counter] = _units[(i, j, k)];
                counter++;
            }

            return result;
        }

        void IGridVolume.CreateVolume<TObj>(Func<TObj> getter)
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            for (var k = 0; k < Depth; k++)
            {
                _units[(i, j, k)].CreateUnit(_getter);
            }
        }

        public void CreateVolume(Func<T> getter)
        {
            _getter = getter;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            for (var k = 0; k < Depth; k++)
            {
                _units[(i, j, k)].CreateUnit(getter);
            }
        }

        void IGridVolume.RemoveVolume<TObj>(Action<TObj> collector)
        {
            var method = collector as Action<T>;
            var methodIsNull = method == null;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            for (var k = 0; k < Depth; k++)
            {
                var unit = _units[(i, j, k)];
                unit.DisposeUnit();
                if (!methodIsNull) method.Invoke(unit.Object);
            }

            _surfaces.Clear();
            _lines.Clear();
            _units.Clear();
        }

        public void RemoveVolume(Action<T> collector)
        {
            var methodIsNull = collector == null;
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++) for (var k = 0; k < Depth; k++)
            {
                var unit = _units[(i, j, k)];
                unit.DisposeUnit();
                if (!methodIsNull) collector.Invoke(unit.Object);
            }

            _surfaces.Clear();
            _lines.Clear();
            _units.Clear();
        }

        public void ResizeVolume(int width, int height, int depth)
        {
            for (var i = Width - 1; i >= width; i--)
            {
                RemoveSurface(GridAxis.x, i);
            }

            for (var i = Height - 1; i >= height; i--)
            {
                RemoveSurface(GridAxis.y, i);
            }

            for (var i = Depth - 1; i >= depth; i--)
            {
                RemoveSurface(GridAxis.z, i);
            }
        }

        public void TrimVolume((int, int, int) from, (int, int, int) to)
        {
            for (var i = to.Item1 - 1; i >= from.Item1; i--)
            {
                RemoveSurface(GridAxis.x, i);
            }

            for (var i = to.Item2 - 1; i >= from.Item2; i--)
            {
                RemoveSurface(GridAxis.y, i);
            }

            for (var i = to.Item3 - 1; i >= from.Item3; i--)
            {
                RemoveSurface(GridAxis.z, i);
            }
        }

        public void DisposeUnits()
        {
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            for (var k = 0; k < Depth; k++)
            {
                _units[(i, j, k)].DisposeUnit();
            }
        }

        public void AddSurface(GridAxis normal)
        {
            var width = GridUtilities.GetWidth(normal, Width, Height, Depth);
            var height = GridUtilities.GetHeight(normal, Width, Height, Depth);
            var depth = GridUtilities.GetDepth(normal, Width, Height, Depth);

            AddUnits(normal, width, height, depth);
            AddLines(normal, width, height, depth);
            AddSurface(normal, width, height, depth);
        }

        public void InsertSurface(GridAxis normal, int index)
        {
            var width = GridUtilities.GetWidth(normal, Width, Height, Depth);
            var height = GridUtilities.GetHeight(normal, Width, Height, Depth);
            var depth = GridUtilities.GetDepth(normal, Width, Height, Depth);

            InsertUnits(index, normal, width, height, depth);
            InsertLines(index, normal, width, height, depth);
            InsertSurface(index, normal, width, height);
        }

        public void RemoveSurface(GridAxis normal, int index)
        {
            var width = GridUtilities.GetWidth(normal, Width, Height, Depth);
            var height = GridUtilities.GetHeight(normal, Width, Height, Depth);
            var depth = GridUtilities.GetDepth(normal, Width, Height, Depth);

            RemoveUnits(index, normal, width, height, depth);
            RemoveLines(index, normal, width, height, depth);
            RemoveSurface(index, normal);
        }

        private void GenerateVolume(int x, int y, int z)
        {
            GenerateUnits(x, y, z);
            GenerateLines(x, y, z);
            GenerateSurfaces(x, y, z);
        }

        private void GenerateSurfaces(int x, int y, int z)
        {
            for (var normalIndex = 0; normalIndex < 3; normalIndex++)
            {
                var normal = (GridAxis)normalIndex;
                var index = GridUtilities.GetDepth(normal, x, y, z);
                var width = GridUtilities.GetWidth(normal, x, y, z);
                var height = GridUtilities.GetHeight(normal, x, y, z);
                var widthAxis = normal.ToWidthAxis();
                var heightAxis = normal.ToHeightAxis();

                _surfaces[normal] = new List<GridSurface<T>>(width * height);

                for (var i = 0; i < index; i++)
                {
                    var surface = new GridSurface<T>(this, width, height, normal);
                    surface._lines.Add(widthAxis, new List<GridLine<T>>(height));

                    for (var j = 0; j < height; j++)
                    {
                        var vector = GridUtilities.ConvertToVector(normal, 0, j, i);
                        surface._lines[widthAxis].Add(_lines[widthAxis][vector]);
                    }

                    surface._lines.Add(heightAxis, new List<GridLine<T>>(width));

                    for (var j = 0; j < width; j++)
                    {
                        var vector = GridUtilities.ConvertToVector(normal, j, 0, i);
                        surface._lines[heightAxis].Add(_lines[heightAxis][vector]);
                    }

                    for (var j = 0; j < height; j++)
                    for (var k = 0; k < width; k++)
                    {
                        var vector = GridUtilities.ConvertToVector(normal, k, j, i);
                        surface._units.Add(vector, _units[vector]);
                    }

                    _surfaces[normal].Add(surface);
                }
            }
        }

        private void GenerateLines(int x, int y, int z)
        {
            for (var i = 0; i < x; i++)
            for (var j = 0; j < y; j++)
            for (var k = 0; k < z; k++)
            {
                if (i == 0)
                {
                    var line = new GridLine<T>(this, x, GridAxis.x);
                    for (var index = 0; index < x; index++)
                        line._units.Add(_units[(index, j, k)]);
                    _lines[GridAxis.x].Add((i, j, k), line);
                }

                if (j == 0)
                {
                    var line = new GridLine<T>(this, y, GridAxis.y);
                    for (var index = 0; index < y; index++)
                        line._units.Add(_units[(i, index, k)]);
                    _lines[GridAxis.y].Add((i, j, k), line);
                }

                if (k == 0)
                {
                    var line = new GridLine<T>(this, z, GridAxis.z);
                    for (var index = 0; index < z; index++)
                        line._units.Add(_units[(i, j, index)]);
                    _lines[GridAxis.z].Add((i, j, k), line);
                }
            }
        }

        private void GenerateUnits(int x, int y, int z)
        {
            for (var i = 0; i < x; i++)
            for (var j = 0; j < y; j++)
            for (var k = 0; k < z; k++)
            {
                var unit = new GridUnit<T>(this, (i, j, k));
                unit.CreateUnit(_getter);
                _units.Add((i, j, k), unit);
            }
        }

        private void AddSurface(GridAxis normal, int width, int height, int depth)
        {
            var widthAxis = normal.ToWidthAxis();
            var heightAxis = normal.ToHeightAxis();

            var surface = new GridSurface<T>(this, width, height, normal);
            surface._lines.Add(widthAxis, new List<GridLine<T>>(height));

            for (var i = 0; i < height; i++)
            {
                var vector = GridUtilities.ConvertToVector(normal, 0, i, depth);
                surface._lines[widthAxis].Add(_lines[widthAxis][vector]);
            }

            surface._lines.Add(heightAxis, new List<GridLine<T>>(width));

            for (var i = 0; i < width; i++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, 0, depth);
                surface._lines[heightAxis].Add(_lines[heightAxis][vector]);
            }

            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, depth);
                surface._units.Add(vector, _units[vector]);
            }

            _surfaces[normal].Add(surface);
        }

        private void AddLines(GridAxis normal, int width, int height, int depth)
        {
            var widthAxis = normal.ToWidthAxis();
            var heightAxis = normal.ToHeightAxis();

            for (var i = 0; i < width; i++)
            {
                var line = new GridLine<T>(this, height, heightAxis);
                for (var j = 0; j < height; j++)
                {
                    var unitVector = GridUtilities.ConvertToVector(normal, i, j, depth);
                    line._units.Add(_units[unitVector]);
                }

                var vector = GridUtilities.ConvertToVector(normal, i, 0, depth);
                _lines[heightAxis].Add(vector, line);
            }

            for (var i = 0; i < height; i++)
            {
                var line = new GridLine<T>(this, width, widthAxis);
                for (var j = 0; j < width; j++)
                {
                    var unitVector = GridUtilities.ConvertToVector(normal, j, i, depth);
                    line._units.Add(_units[unitVector]);
                }

                var vector = GridUtilities.ConvertToVector(normal, 0, i, depth);
                _lines[widthAxis].Add(vector, line);
            }
        }

        private void AddUnits(GridAxis normal, int width, int height, int depth)
        {
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, depth);
                var newUnit = new GridUnit<T>(this, vector);
                newUnit.CreateUnit(_getter);
                _units.Add(vector, newUnit);
            }
        }

        private void InsertSurface(int index, GridAxis normal, int width, int height)
        {
            var widthAxis = normal.ToWidthAxis();
            var heightAxis = normal.ToHeightAxis();

            var newSurface = new GridSurface<T>(this, width, height, normal);
            newSurface._lines.Add(widthAxis, new List<GridLine<T>>(height));

            for (var i = 0; i < height; i++)
            {
                var vector = GridUtilities.ConvertToVector(normal, 0, i, index);
                newSurface._lines[widthAxis].Add(_lines[widthAxis][vector]);
            }

            newSurface._lines.Add(heightAxis, new List<GridLine<T>>(width));

            for (var i = 0; i < width; i++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, 0, index);
                newSurface._lines[heightAxis].Add(_lines[heightAxis][vector]);
            }

            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, index);
                newSurface._units.Add(vector, _units[vector]);
            }

            _surfaces[normal].Insert(index, newSurface);
        }

        private void InsertLines(int index, GridAxis normal, int width, int height, int depth)
        {
            var widthAxis = normal.ToWidthAxis();
            var heightAxis = normal.ToHeightAxis();

            for (var i = depth - 1; i >= index; i--)
            {
                for (var j = 0; j < width; j++)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, j, 0, i);
                    var existingLine = _lines[heightAxis][oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, j, 0, i + 1);
                    _lines[heightAxis][newVector] = existingLine;
                }

                for (var j = 0; j < height; j++)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, 0, j, i);
                    var existingLine = _lines[widthAxis][oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, 0, j, i + 1);
                    _lines[widthAxis][newVector] = existingLine;
                }
            }
            
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, 0);
                var line = _lines[normal][vector];
                line._units.Insert(index, _units[vector]);
            }

            for (var i = 0; i < width; i++)
            {
                var newLine = new GridLine<T>(this, height, heightAxis);
                for (var j = 0; j < height; j++)
                {
                    var unitVector = GridUtilities.ConvertToVector(normal, i, j, index);
                    newLine._units.Add(_units[unitVector]);
                }

                var lineVector = GridUtilities.ConvertToVector(normal, i, 0, index);
                _lines[heightAxis][lineVector] = newLine;
            }

            for (var i = 0; i < height; i++)
            {
                var newLine = new GridLine<T>(this, width, widthAxis);
                for (var j = 0; j < width; j++)
                {
                    var unitVector = GridUtilities.ConvertToVector(normal, j, i, index);
                    newLine._units.Add(_units[unitVector]);
                }

                var lineVector = GridUtilities.ConvertToVector(normal, 0, i, index);
                _lines[widthAxis][lineVector] = newLine;
            }
        }

        private void InsertUnits(int index, GridAxis normal, int width, int height, int depth)
        {
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            for (var k = depth - 1; k >= index; k--)
            {
                var oldVector = GridUtilities.ConvertToVector(normal, i, j, k);
                var existingUnit = _units[oldVector];
                var newVector = GridUtilities.ConvertToVector(normal, i, j, k + 1);
                _units[newVector] = existingUnit;
                _units.Remove(oldVector);
                existingUnit.SetCoords(newVector);
                existingUnit.OnShift();
            }

            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, index);
                var newUnit = new GridUnit<T>(this, vector);
                newUnit.CreateUnit(_getter);
                _units.Add(vector, newUnit);
            }
        }

        private void RemoveSurface(int index, GridAxis normal)
        {
            _surfaces[normal].RemoveAt(index);
        }

        private void RemoveLines(int index, GridAxis normal, int width, int height, int depth)
        {
            var widthAxis = normal.ToWidthAxis();
            var heightAxis = normal.ToHeightAxis();

            for (var i = 0; i < width; i++)
            {
                var lineVector = GridUtilities.ConvertToVector(normal, i, 0, index);
                _lines[heightAxis].Remove(lineVector);
            }

            for (var i = 0; i < height; i++)
            {
                var lineVector = GridUtilities.ConvertToVector(normal, 0, i, index);
                _lines[widthAxis].Remove(lineVector);
            }

            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, 0);
                var line = _lines[normal][vector];
                line._units.RemoveAt(index);
            }

            if (index + 1 >= depth)
                return;

            for (var i = index + 1; i < depth; i++)
            {
                for (var j = 0; j < width; j++)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, j, 0, i);
                    var existingLine = _lines[heightAxis][oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, j, 0, i - 1);
                    _lines[heightAxis][newVector] = existingLine;
                }

                for (var j = 0; j < height; j++)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, 0, j, i);
                    var existingLine = _lines[widthAxis][oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, 0, j, i - 1);
                    _lines[widthAxis][newVector] = existingLine;
                }
            }
        }

        private void RemoveUnits(int index, GridAxis normal, int width, int height, int depth)
        {
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, index);
                var unit = _units[vector];
                _units.Remove(vector);
                unit.DisposeUnit();
            }

            if (index + 1 >= depth)
                return;

            for (var i = index + 1; i < depth; i++)
            for (var j = 0; j < width; j++)
            for (var k = 0; k < height; k++)
            {
                var oldVector = GridUtilities.ConvertToVector(normal, j, k, i);
                var existingUnit = _units[oldVector];
                var newVector = GridUtilities.ConvertToVector(normal, j, k, i - 1);
                _units[newVector] = existingUnit;
                _units.Remove(oldVector);
                existingUnit.SetCoords(newVector);
                existingUnit.OnShift();
            }
        }
    }
}
