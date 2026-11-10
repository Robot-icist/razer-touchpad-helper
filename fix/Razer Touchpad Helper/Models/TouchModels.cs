namespace RazerTouchpadHelper.Models
{
    public readonly record struct Contact(int Id, int X, int Y)
    {
        public double NormalizedX => (double)X / TouchConstants.MaxX;
        public double NormalizedY => (double)Y / TouchConstants.MaxY;
    }

    public readonly record struct TouchPoint(int Id, float X, float Y, float Life = 1f);

    public static class TouchConstants
    {
        public const int MaxX = 4613;
        public const int MaxY = 2918;
        public const int MaxContacts = 5;
    }
}