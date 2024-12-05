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
    /// Base interface for 1d line-based grids
    /// </summary>
    public interface IGridLine : IEnumerable<IGridUnit>
    {
        IGridVolume Volume { get; }
        IGridSurface Surface { get; }
        GridAxis Axis { get; }
        int Count { get; }
        int Length { get; }
        int Dimension { get; }
        IGridUnit this[int i] { get; }
        IGridUnit GetUnit(int i);
        IGridUnit[] GetUnits();
        IGridUnit[] GetUnits(int from, int to);
        void CreateLine<T>(Func<T> getter) where T : class, IGridObject;
        void RemoveLine<T>(Action<T> collector) where T : class, IGridObject;
        void ResizeLine(int length);
        void TrimLine(int from, int to);
        void DisposeUnits();
        void AddUnit();
        void InsertUnit(int index);
        void RemoveUnit(int index);
    }

    /// <summary>
    /// Line-based grid that contains units on a single axis
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridLine<T> : IGridLine where T : class, IGridObject
    {
        IGridVolume IGridLine.Volume => _volume;
        IGridSurface IGridLine.Surface => _surface;
        public GridVolume<T> Volume => _volume;
        public GridSurface<T> Surface => _surface;
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

        IGridUnit IGridLine.this[int i] => _units[i];

        public GridUnit<T> this[int i] => _units[i];

        public IEnumerator<IGridUnit> GetEnumerator() => _units.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IGridUnit IGridLine.GetUnit(int index) => _units[index];

        public GridUnit<T> GetUnit(int index) => _units[index];

        IGridUnit[] IGridLine.GetUnits()
        {
            var result = new IGridUnit[Count];
            for (var i = 0; i < Count; i++)
            {
                result[i] = _units[i];
            }

            return result;
        }

        IGridUnit[] IGridLine.GetUnits(int from, int to)
        {
            var result = new IGridUnit[Count];
            for (var i = from; i < to; i++)
            {
                result[i] = _units[i];
            }

            return result;
        }

        public GridUnit<T>[] GetUnits() => _units.ToArray();

        public GridUnit<T>[] GetUnits(int from, int to)
        {
            var result = new GridUnit<T>[Count];
            for (var i = from; i < to; i++)
            {
                result[i] = _units[i];
            }

            return result;
        }

        void IGridLine.CreateLine<TObj>(Func<TObj> getter)
        {
            _getter = getter as Func<T>;
            for (var i = 0; i < Length; i++)
            {
                _units[i].CreateUnit(_getter);
            }
        }

        public void CreateLine(Func<T> getter)
        {
            _getter = getter;
            for (var i = 0; i < Length; i++)
            {
                _units[i].CreateUnit(getter);
            }
        }

        void IGridLine.RemoveLine<TObj>(Action<TObj> collector)
        {
            var method = collector as Action<T>;
            var methodIsNull = method == null;
            for (var i = 0; i < Length; i++)
            {
                var unit = _units[i];
                unit.DisposeUnit();
                if (!methodIsNull) method.Invoke(unit.Object);
            }
            
            _units.Clear();
        }

        public void RemoveLine(Action<T> collector)
        {
            var methodIsNull = collector == null;
            for (var i = 0; i < Length; i++)
            {
                var unit = _units[i];
                unit.DisposeUnit();
                if (!methodIsNull) collector.Invoke(unit.Object);
            }
            
            _units.Clear();
        }

        public void ResizeLine(int length)
        {
            for (var i = Length - 1; i >= length; i--)
            {
                RemoveUnit(i);
            }
        }

        public void TrimLine(int from, int to)
        {
            for (var i = to - 1; i >= from; i--)
            {
                RemoveUnit(i);
            }
        }

        public void DisposeUnits()
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
                _units[i].SetCoords(Axis.ToCoords(i));
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
                _units[i].SetCoords(Axis.ToCoords(i));
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
}
