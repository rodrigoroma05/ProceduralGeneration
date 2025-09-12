public class Cell
{
    private bool _isWater;
    public bool IsWater { get => _isWater; set => _isWater = value; }

    public Cell(bool isWater) 
    { 
        this._isWater = isWater;
    }
}
