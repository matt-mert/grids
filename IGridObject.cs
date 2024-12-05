// ========================================================================================
// Grids - A dynamic and data-oriented grid system
// ========================================================================================
// 2024, Mert Kucukakinci | https://github.com/matt-mert
// ========================================================================================

namespace GridSystem
{
    public interface IGridObject
    {
        IGridUnit GridUnit { get; set; }
        void OnCreate();
        void OnDispose();
        void OnShift();
    }
}
