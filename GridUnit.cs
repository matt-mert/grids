// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci | https://github.com/matt-mert
// ========================================================================================

using System;

namespace GridSystem
{
    /// <summary>
    /// Base interface for units in the grid
    /// </summary>
    public interface IGridUnit
    {
        IGridObject Object { get; }
        IGridVolume Volume { get; }
        IGridSurface Surface { get; }
        IGridLine Line { get; }
        (int, int, int) Coords { get; }
        int Dimension { get; }
        void SetObject<T>(T obj) where T : class, IGridObject;
        void CreateUnit<T>(Func<T> getter) where T : class, IGridObject;
        void DisposeUnit();
    }

    /// <summary>
    /// Units to be placed in lines, surfaces, or volumes that carry objects
    /// </summary>
    /// <typeparam name="T">Type of the object to be placed in the grid units</typeparam>
    public class GridUnit<T> : IGridUnit where T : class, IGridObject
    {
        IGridObject IGridUnit.Object => _object;
        IGridVolume IGridUnit.Volume => _volume;
        IGridSurface IGridUnit.Surface => _surface;
        IGridLine IGridUnit.Line => _line;
        public T Object => _object;
        public GridVolume<T> Volume => _volume;
        public GridSurface<T> Surface => _surface;
        public GridLine<T> Line => _line;
        public (int, int, int) Coords => _coords;
        public int Dimension { get; }

        private (int, int, int) _coords;
        private readonly GridVolume<T> _volume;
        private readonly GridSurface<T> _surface;
        private readonly GridLine<T> _line;
        private Func<T> _getter;
        private bool _hasObject;
        private T _object;

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

        void IGridUnit.SetObject<TObj>(TObj obj)
        {
            SetField(obj);
            _hasObject = true;
            _object = obj as T;
        }

        public void SetObject(T obj)
        {
            if (_hasObject) _object.OnDispose();
            _hasObject = true;
            _object = obj;
            SetField(obj);
        }

        void IGridUnit.CreateUnit<TObj>(Func<TObj> getter)
        {
            _getter = getter as Func<T>;
            OnCreate();
        }

        public void CreateUnit(Func<T> getter)
        {
            _getter = getter;
            OnCreate();
        }

        public void DisposeUnit()
        {
            OnDispose();
        }

        private void OnCreate()
        {
            if (_getter == null) return;
            _object = _getter.Invoke();
            SetField(_object);
            _hasObject = true;
            _object.OnCreate();
        }

        private void OnDispose()
        {
            if (_hasObject)
                _object.OnDispose();
        }

        internal void OnShift()
        {
            if (_hasObject)
                _object.OnShift();
        }

        internal void SetCoords((int, int, int) coords)
        {
            _coords = coords;
        }

        private void SetField(IGridObject obj)
        {
            obj.GridUnit = this;
        }
    }
}
