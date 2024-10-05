// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci  / https://github.com/matt-mert
// ========================================================================================
// The grid system does not depend Unity Engine.
// ========================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace GridSystem
{
    /// <summary>
    /// Axis convention used for the system
    /// </summary>
    public enum GridAxis
    {
        x,
        y,
        z
    }
    
    /// <summary>
    /// Base interface for objects to be placed int the grid units
    /// </summary>
    public interface IGridElement
    {
        /// <summary>
        /// Unit is set with reflection
        /// </summary>
        public IGridUnit Unit { get; }
        void OnCreate();
        void OnDispose();
        void OnShift();
    }

    /// <summary>
    /// Base interface for 3d volume-based grids
    /// </summary>
    public interface IGridVolume
    {
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int Count { get; }
        public int Dimension { get; }
        IGridUnit this[int i, int j, int k] { get; }
        IEnumerator<IGridSurface> GetEnumerator();
        IGridSurface GetSurface(GridAxis axis, int index);
        IGridUnit GetUnit(int i, int j, int k);
        void CreateVolume<T>(Func<T> getter) where T : IGridElement;
        void DisposeVolume();
        void AddSurface(GridAxis normal);
        void InsertSurface(GridAxis normal, int index);
        void RemoveSurface(GridAxis normal, int index);
    }

    /// <summary>
    /// Base interface for 2d surface-based grids
    /// </summary>
    public interface IGridSurface
    {
        public IGridVolume Volume { get; }
        public GridAxis Normal { get; }
        public int Width { get; }
        public int Height { get; }
        public int Count { get; }
        public int Dimension { get; }
        IGridUnit this[int i, int j] { get; }
        IEnumerator<IGridLine> GetEnumerator();
        IGridLine GetLine(GridAxis axis, int index);
        IGridUnit GetUnit(int i, int j);
        void CreateSurface<T>(Func<T> getter) where T : IGridElement;
        void DisposeSurface();
        void AddLine(GridAxis axis);
        void InsertLine(GridAxis axis, int index);
        void RemoveLine(GridAxis axis, int index);
    }

    /// <summary>
    /// Base interface for 1d line-based grids
    /// </summary>
    public interface IGridLine
    {
        public IGridVolume Volume { get; }
        public IGridSurface Surface { get; }
        public GridAxis Axis { get; }
        public int Count { get; }
        public int Length { get; }
        public int Dimension { get; }
        IGridUnit this[int i] { get; }
        IEnumerator<IGridUnit> GetEnumerator();
        IGridUnit GetUnit(int i);
        void CreateLine<T>(Func<T> getter) where T : IGridElement;
        void DisposeLine();
        void AddUnit();
        void InsertUnit(int index);
        void RemoveUnit(int index);
    }

    /// <summary>
    /// Base interface for units in the grid
    /// </summary>
    public interface IGridUnit
    {
        public IGridElement Element { get; }
        public IGridVolume Volume { get; }
        public IGridSurface Surface { get; }
        public IGridLine Line { get; }
        public (int, int, int) Coords { get; }
        public int Dimension { get; }
        void CreateUnit<T>(Func<T> getter) where T : IGridElement;
        void DisposeUnit();
    }

    /// <summary>
    /// Volume-based grid that contains surfaces, lines, and units on three axes
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridVolume<T> : IEnumerable<IGridSurface>, IGridVolume where T : class, IGridElement
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

        public IGridUnit this[int i, int j, int k] => _units[(i, j, k)];

        public IEnumerator<IGridSurface> GetEnumerator() => _surfaces[GridAxis.z].GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IGridSurface GetSurface(GridAxis normal, int i) => _surfaces[normal][i];

        public IGridUnit GetUnit(int i, int j, int k) => this[i, j, k];

        public void CreateVolume<TElement>(Func<TElement> getter) where TElement : IGridElement
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++) for (var k = 0; k < Depth; k++)
            {
                this[i, j, k].CreateUnit(getter);
            }
        }

        public void DisposeVolume()
        {
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++) for (var k = 0; k < Depth; k++)
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
                    for (var j = 0; j < height; j++) for (var k = 0; k < width; k++)
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
            for (var i = 0; i < x; i++) for (var j = 0; j < y; j++) for (var k = 0; k < z; k++)
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
            for (var i = 0; i < x; i++) for (var j = 0; j < y; j++) for (var k = 0; k < z; k++)
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
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
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
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
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
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
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
            
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
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
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
            {
                for (var k = depth - 1; k >= index; k--)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, i, j, k);
                    var existingUnit = _units[oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, i, j, k + 1);
                    _units[newVector] = existingUnit;
                    _units.Remove(oldVector);
                    existingUnit._coords = newVector;
                    existingUnit.OnShift();
                }
            }
            
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
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
            
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, 0);
                var line = _lines[normal][vector];
                line._units.RemoveAt(index);
            }

            if (index + 1 >= depth)
            {
                return;
            }
            
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
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(normal, i, j, index);
                var unit = _units[vector];
                _units.Remove(vector);
                unit.DisposeUnit();
            }

            if (index + 1 >= depth)
            {
                return;
            }

            for (var i = index + 1; i < depth; i++)
            {
                for (var j = 0; j < width; j++) for (var k = 0; k < height; k++)
                {
                    var oldVector = GridUtilities.ConvertToVector(normal, j, k, i);
                    var existingUnit = _units[oldVector];
                    var newVector = GridUtilities.ConvertToVector(normal, j, k, i - 1);
                    _units[newVector] = existingUnit;
                    _units.Remove(oldVector);
                    existingUnit._coords = newVector;
                    existingUnit.OnShift();
                }
            }
        }
    }

    /// <summary>
    /// Surface-based grid that contains lines and units on two axes
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridSurface<T> : IEnumerable<IGridLine>, IGridSurface where T : class, IGridElement
    {
        public IGridVolume Volume => _volume;
        public GridAxis Normal { get; }
        public int Dimension { get; }
        public int Width => _lines[Normal.ToHeightAxis()].Count;
        public int Height => _lines[Normal.ToWidthAxis()].Count;
        public int Count => Width * Height;
        
        internal readonly Dictionary<GridAxis, List<GridLine<T>>> _lines;
        internal readonly Dictionary<(int, int, int), GridUnit<T>> _units;
        private readonly GridVolume<T> _volume;
        private Func<T> _getter;
        
        public GridSurface(int width, int height, GridAxis normal)
        {
            Normal = normal;
            Dimension = 2;
            _volume = null;
            _units = new Dictionary<(int, int, int), GridUnit<T>>(width * height);
            _lines = new Dictionary<GridAxis, List<GridLine<T>>>
            {
                { normal.ToWidthAxis(), new List<GridLine<T>>(height) },
                { normal.ToHeightAxis(), new List<GridLine<T>>(width) }
            };

            GenerateSurface(width, height);
        }

        internal GridSurface(GridVolume<T> volume, int width, int height, GridAxis normal)
        {
            Normal = normal;
            Dimension = 3;
            _volume = volume;
            _units = new Dictionary<(int, int, int), GridUnit<T>>(width * height);
            _lines = new Dictionary<GridAxis, List<GridLine<T>>>(width * height);
        }

        public IGridUnit this[int i, int j] => _lines[Normal.ToHeightAxis()][i]._units[j];

        public IEnumerator<IGridLine> GetEnumerator() => _lines[Normal.ToHeightAxis()].GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IGridLine GetLine(GridAxis axis, int index) => _lines[axis][index];

        public IGridUnit GetUnit(int i, int j) => this[i, j];

        public void CreateSurface<TElement>(Func<TElement> getter) where TElement : IGridElement
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++)
            {
                this[i, j].CreateUnit(getter);
            }
        }

        public void DisposeSurface()
        {
            for (var i = 0; i < Width; i++) for (var j = 0; j < Height; j++)
            {
                this[i, j].DisposeUnit();
            }
        }
        
        public void AddLine(GridAxis axis)
        {
            var length = GridUtilities.GetLength(axis, Normal, Width, Height);
            var other = GridUtilities.GetOther(axis, Normal, Width, Height);
            
            AddUnits(axis, length, other);
            AddLine(axis, length, other);
        }

        public void InsertLine(GridAxis axis, int index)
        {
            var length = GridUtilities.GetLength(axis, Normal, Width, Height);
            var other = GridUtilities.GetOther(axis, Normal, Width, Height);
            
            InsertUnits(index, axis, length, other);
            InsertLine(index, axis, length);
        }

        public void RemoveLine(GridAxis axis, int index)
        {
            var length = GridUtilities.GetLength(axis, Normal, Width, Height);
            var other = GridUtilities.GetOther(axis, Normal, Width, Height);
            
            RemoveUnits(index, axis, length, other);
            RemoveLine(index, axis, length);
        }

        private void GenerateSurface(int width, int height)
        {
            GenerateUnits(width, height);
            GenerateLines(width, height);
        }

        private void GenerateLines(int width, int height)
        {
            var widthAxis = Normal.ToWidthAxis();
            var heightAxis = Normal.ToHeightAxis();
            
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
            {
                if (i == 0)
                {
                    var line = new GridLine<T>(this, width, widthAxis);
                    for (var k = 0; k < width; k++)
                    {
                        var vector = GridUtilities.ConvertToVector(Normal, k, j, 0);
                        line._units.Add(_units[vector]);
                    }
                    _lines[widthAxis].Add(line);
                }
                if (j == 0)
                {
                    var line = new GridLine<T>(this, height, heightAxis);
                    for (var k = 0; k < height; k++)
                    {
                        var vector = GridUtilities.ConvertToVector(Normal, i, k, 0);
                        line._units.Add(_units[vector]);
                    }
                    _lines[heightAxis].Add(line);
                }
            }
        }

        private void GenerateUnits(int width, int height)
        {
            for (var i = 0; i < width; i++) for (var j = 0; j < height; j++)
            {
                var vector = GridUtilities.ConvertToVector(Normal, i, j, 0);
                var unit = new GridUnit<T>(this, vector);
                unit.CreateUnit(_getter);
                _units.Add(vector, unit);
            }
        }

        private void AddLine(GridAxis axis, int length, int other)
        {
            var line = new GridLine<T>(this, length, axis);
            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, other);
                line._units.Add(_units[vector]);
            }

            _lines[axis].Add(line);
        }

        private void AddUnits(GridAxis axis, int length, int other)
        {
            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, other);
                var newUnit = new GridUnit<T>(this, vector);
                newUnit.CreateUnit(_getter);
                _units.Add(vector, newUnit);
            }
        }

        private void InsertLine(int index, GridAxis axis, int length)
        {
            var otherAxis = GridUtilities.GetOtherAxis(axis, Normal);
            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, index);
                var otherLine = _lines[otherAxis][i];
                otherLine._units.Insert(index, _units[vector]);
            }
            
            var newLine = new GridLine<T>(this, length, axis);
            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, index);
                newLine._units.Add(_units[vector]);
            }
            
            _lines[axis].Insert(index, newLine);
        }

        private void InsertUnits(int index, GridAxis axis, int length, int other)
        {
            for (var i = 0; i < length; i++)
            {
                for (var j = other - 1; j >= index; j--)
                {
                    var oldVector = GridUtilities.ConvertToVector(axis, Normal, i, j);
                    var existingUnit = _units[oldVector];
                    var newVector = GridUtilities.ConvertToVector(axis, Normal, i, j + 1);
                    _units[newVector] = existingUnit;
                    _units.Remove(oldVector);
                    existingUnit._coords = newVector;
                    existingUnit.OnShift();
                }
            }

            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, index);
                var newUnit = new GridUnit<T>(this, vector);
                newUnit.CreateUnit(_getter);
                _units.Add(vector, newUnit);
            }
        }

        private void RemoveLine(int index, GridAxis axis, int length)
        {
            var otherAxis = GridUtilities.GetOtherAxis(axis, Normal);
            for (var i = 0; i < length; i++)
            {
                var otherLine = _lines[otherAxis][i];
                otherLine._units.RemoveAt(index);
            }
            
            _lines[axis].RemoveAt(index);
        }

        private void RemoveUnits(int index, GridAxis axis, int length, int other)
        {
            for (var i = 0; i < length; i++)
            {
                var vector = GridUtilities.ConvertToVector(axis, Normal, i, index);
                var unit = _units[vector];
                _units.Remove(vector);
                unit.DisposeUnit();
            }

            if (index + 1 >= other)
            {
                return;
            }

            for (var i = index + 1; i < other; i++)
            {
                for (var j = 0; j < length; j++)
                {
                    var oldVector = GridUtilities.ConvertToVector(axis, Normal, j, i);
                    var existingUnit = _units[oldVector];
                    var newVector = GridUtilities.ConvertToVector(axis, Normal, j, i - 1);
                    _units[newVector] = existingUnit;
                    _units.Remove(oldVector);
                    existingUnit._coords = newVector;
                    existingUnit.OnShift();
                }
            }
        }
    }

    /// <summary>
    /// Line-based grid that contains units on a single axis
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridLine<T> : IEnumerable<IGridUnit>, IGridLine where T : class, IGridElement
    {
        public IGridVolume Volume => _volume;
        public IGridSurface Surface => _surface;
        public GridAxis Axis { get; }
        public int Dimension { get; }
        public int Count => _units.Count;
        public int Length => _units.Count;
        
        internal readonly List<GridUnit<T>> _units;
        private readonly GridVolume<T> _volume;
        private readonly GridSurface<T> _surface;
        private Func<T> _getter;
        
        public GridLine(int length, GridAxis axis)
        {
            Axis = axis;
            Dimension = 1;
            _volume = null;
            _surface = null;
            _units = new List<GridUnit<T>>(length);
            
            GenerateLine(length);
        }

        internal GridLine(GridVolume<T> volume, int length, GridAxis axis)
        {
            Axis = axis;
            Dimension = 3;
            _volume = volume;
            _surface = null;
            _units = new List<GridUnit<T>>(length);
        }

        internal GridLine(GridSurface<T> surface, int length, GridAxis axis)
        {
            Axis = axis;
            Dimension = 2;
            _volume = null;
            _surface = surface;
            _units = new List<GridUnit<T>>(length);
        }
        
        public IGridUnit this[int i] => _units[i];

        public IEnumerator<IGridUnit> GetEnumerator() => _units.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IGridUnit GetUnit(int index) => this[index];

        public void CreateLine<TElement>(Func<TElement> getter) where TElement : IGridElement
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Length; i++)
            {
                this[i].CreateUnit(getter);
            }
        }

        public void DisposeLine()
        {
            for (var i = 0; i < Length; i++)
            {
                _units[i].DisposeUnit();
            }
        }

        public void AddUnit()
        {
            var coords = Axis.ToCoords(Length);
            var unit = new GridUnit<T>(this, coords);
            unit.CreateUnit(_getter);
            _units.Add(unit);
        }

        public void InsertUnit(int index)
        {
            var coords = Axis.ToCoords(index);
            var unit = new GridUnit<T>(this, coords);
            unit.CreateUnit(_getter);
            _units.Insert(index, unit);
            
            for (var i = index; i < Length + 1; i++)
            {
                _units[i]._coords = Axis.ToCoords(i);
                _units[i].OnShift();
            }
        }

        public void RemoveUnit(int index)
        {
            var unit = _units[index];
            _units.Remove(unit);
            unit.DisposeUnit();

            for (var i = index; i < Length - 1; i++)
            {
                _units[i]._coords = Axis.ToCoords(i);
                _units[i].OnShift();
            }
        }

        private void GenerateLine(int length)
        {
            for (var i = 0; i < length; i++)
            {
                var coords = Axis.ToCoords(i);
                var unit = new GridUnit<T>(this, coords);
                unit.CreateUnit(_getter);
                _units.Add(unit);
            }
        }
    }

    /// <summary>
    /// Units to be placed in lines, surfaces, or volumes that carry objects
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridUnit<T> : IGridUnit where T : class, IGridElement
    {
        public IGridElement Element => _value;
        public IGridVolume Volume => _volume;
        public IGridSurface Surface => _surface;
        public IGridLine Line => _line;
        public (int, int, int) Coords => _coords;
        public int Dimension { get; }

        internal (int, int, int) _coords;
        private readonly GridVolume<T> _volume;
        private readonly GridSurface<T> _surface;
        private readonly GridLine<T> _line;
        private Func<T> _getter;
        private bool _hasValue;
        private T _value;
        
        public GridUnit()
        {
            _volume = null;
            _surface = null;
            _line = null;
            _coords = (0, 0, 0);
            Dimension = 0;
        }

        internal GridUnit(GridVolume<T> volume, (int, int, int) coords)
        {
            _volume = volume;
            _surface = null;
            _line = null;
            _coords = coords;
            Dimension = 3;
        }
        
        internal GridUnit(GridSurface<T> surface, (int, int, int) coords)
        {
            _volume = null;
            _surface = surface;
            _line = null;
            _coords = coords;
            Dimension = 2;
        }

        internal GridUnit(GridLine<T> line, (int, int, int) coords)
        {
            _volume = null;
            _surface = null;
            _line = line;
            _coords = coords;
            Dimension = 1;
        }

        public void CreateUnit<TElement>(Func<TElement> getter) where TElement : IGridElement
        {
            _getter = getter as Func<T>;
            OnCreate();
        }

        public void DisposeUnit()
        {
            OnDispose();
        }

        private void OnCreate()
        {
            if (_getter == null) return;
            
            _value = _getter.Invoke();
            var field = _value.GetType().GetField("<Unit>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null) return;
            field.SetValue(_value, this);
            _hasValue = true;
            _value.OnCreate();
        }

        private void OnDispose()
        {
            if (_hasValue) _value.OnDispose();
        }

        internal void OnShift()
        {
            if (_hasValue) _value.OnShift();
        }
    }
    
    internal static class GridUtilities
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

    internal static class GridExtensions
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
    }
}
