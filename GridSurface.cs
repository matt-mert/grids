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
    /// Base interface for 2d surface-based grids
    /// </summary>
    public interface IGridSurface : IEnumerable<IGridUnit>
    {
        IGridVolume Volume { get; }
        GridAxis Normal { get; }
        int Width { get; }
        int Height { get; }
        int Count { get; }
        int Dimension { get; }
        IGridUnit this[int i, int j] { get; }
        IGridUnit this[(int, int) coords] { get; }
        IGridLine GetLine(GridAxis axis, int index);
        IGridUnit GetUnit(int i, int j);
        IGridUnit GetUnit((int, int) coords);
        IGridUnit[] GetUnits();
        IGridUnit[] GetUnits((int, int) from, (int, int) to);
        void CreateSurface<T>(Func<T> getter) where T : class, IGridObject;
        void RemoveSurface<T>(Action<T> collector) where T : class, IGridObject;
        void ResizeSurface(int width, int height);
        void TrimSurface((int, int) from, (int, int) to);
        void DisposeUnits();
        void AddLine(GridAxis axis);
        void InsertLine(GridAxis axis, int index);
        void RemoveLine(GridAxis axis, int index);
    }

    /// <summary>
    /// Surface-based grid that contains lines and units on two axes
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridSurface<T> : IGridSurface where T : class, IGridObject
    {
        IGridVolume IGridSurface.Volume => _volume;
        public GridVolume<T> Volume => _volume;
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

        IGridUnit IGridSurface.this[int i, int j] => _lines[Normal.ToHeightAxis()][i]._units[j];

        IGridUnit IGridSurface.this[(int, int) coords] => _lines[Normal.ToHeightAxis()][coords.Item1]._units[coords.Item2];

        public GridUnit<T> this[int i, int j] => _lines[Normal.ToHeightAxis()][i]._units[j];

        public GridUnit<T> this[(int, int) coords] => _lines[Normal.ToHeightAxis()][coords.Item1]._units[coords.Item2];

        public IEnumerator<IGridUnit> GetEnumerator() => _units.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IGridLine IGridSurface.GetLine(GridAxis axis, int index) => _lines[axis][index];

        public GridLine<T> GetLine(GridAxis axis, int index) => _lines[axis][index];

        IGridUnit IGridSurface.GetUnit(int i, int j) => _lines[Normal.ToHeightAxis()][i]._units[j];

        IGridUnit IGridSurface.GetUnit((int, int) coords) => _lines[Normal.ToHeightAxis()][coords.Item1]._units[coords.Item2];

        public GridUnit<T> GetUnit(int i, int j) => _lines[Normal.ToHeightAxis()][i]._units[j];

        public GridUnit<T> GetUnit((int, int) coords) => _lines[Normal.ToHeightAxis()][coords.Item1]._units[coords.Item2];

        IGridUnit[] IGridSurface.GetUnits()
        {
            var result = new IGridUnit[Count];
            var counter = 0;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                result[counter] = _lines[Normal.ToHeightAxis()][i]._units[j];
                counter++;
            }

            return result;
        }

        IGridUnit[] IGridSurface.GetUnits((int, int) fromCoords, (int, int) toCoords)
        {
            var result = new IGridUnit[Count];
            var counter = 0;
            for (var i = fromCoords.Item1; i < toCoords.Item1; i++)
            for (var j = fromCoords.Item2; j < toCoords.Item2; j++)
            {
                result[counter] = _lines[Normal.ToHeightAxis()][i]._units[j];
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

        public GridUnit<T>[] GetUnits((int, int) fromCoords, (int, int) toCoords)
        {
            var result = new GridUnit<T>[Count];
            var counter = 0;
            for (var i = fromCoords.Item1; i < toCoords.Item1; i++)
            for (var j = fromCoords.Item2; j < toCoords.Item2; j++)
            {
                result[counter] = _lines[Normal.ToHeightAxis()][i]._units[j];
                counter++;
            }

            return result;
        }

        void IGridSurface.CreateSurface<TObj>(Func<TObj> getter)
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                _lines[Normal.ToHeightAxis()][i]._units[j].CreateUnit(_getter);
            }
        }

        public void CreateSurface(Func<T> getter)
        {
            _getter = getter;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                _lines[Normal.ToHeightAxis()][i]._units[j].CreateUnit(getter);
            }
        }

        void IGridSurface.RemoveSurface<TObj>(Action<TObj> collector)
        {
            var method = collector as Action<T>;
            var methodIsNull = method == null;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                var unit = _lines[Normal.ToHeightAxis()][i]._units[j];
                unit.DisposeUnit();
                if (!methodIsNull)
                    method.Invoke(unit.Object);
            }

            _lines.Clear();
            _units.Clear();
        }

        public void RemoveSurface(Action<T> collector)
        {
            var methodIsNull = collector == null;
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                var unit = _lines[Normal.ToHeightAxis()][i]._units[j];
                unit.DisposeUnit();
                if (!methodIsNull)
                    collector.Invoke(unit.Object);
            }

            _lines.Clear();
            _units.Clear();
        }

        public void ResizeSurface(int width, int height)
        {
            var heightAxis = Normal.ToHeightAxis();
            var widthAxis = Normal.ToWidthAxis();
            for (var i = Width - 1; i >= width; i--)
            {
                RemoveLine(heightAxis, i);
            }

            for (var i = Height - 1; i >= height; i--)
            {
                RemoveLine(widthAxis, i);
            }
        }

        public void TrimSurface((int, int) fromCoords, (int, int) toCoords)
        {
            var heightAxis = Normal.ToHeightAxis();
            var widthAxis = Normal.ToWidthAxis();
            for (var i = toCoords.Item1 - 1; i >= fromCoords.Item1; i--)
            {
                RemoveLine(heightAxis, i);
            }

            for (var i = toCoords.Item2 - 1; i >= fromCoords.Item2; i--)
            {
                RemoveLine(widthAxis, i);
            }
        }

        public void DisposeUnits()
        {
            for (var i = 0; i < Width; i++)
            for (var j = 0; j < Height; j++)
            {
                _lines[Normal.ToHeightAxis()][i]._units[j].DisposeUnit();
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

            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
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
            for (var i = 0; i < width; i++)
            for (var j = 0; j < height; j++)
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
            for (var j = other - 1; j >= index; j--)
            {
                var oldVector = GridUtilities.ConvertToVector(axis, Normal, i, j);
                var existingUnit = _units[oldVector];
                var newVector = GridUtilities.ConvertToVector(axis, Normal, i, j + 1);
                _units[newVector] = existingUnit;
                _units.Remove(oldVector);
                existingUnit.SetCoords(newVector);
                existingUnit.OnShift();
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
                return;

            for (var i = index + 1; i < other; i++)
            for (var j = 0; j < length; j++)
            {
                var oldVector = GridUtilities.ConvertToVector(axis, Normal, j, i);
                var existingUnit = _units[oldVector];
                var newVector = GridUtilities.ConvertToVector(axis, Normal, j, i - 1);
                _units[newVector] = existingUnit;
                _units.Remove(oldVector);
                existingUnit.SetCoords(newVector);
                existingUnit.OnShift();
            }
        }
    }
}
