namespace VirtualMeshCreator.Math
{
    public static class Mathf
    {
        // Divides two integers and rounds up
        public static uint DivideAndRoundUp(uint Dividend, uint Divisor)
        {
            return (Dividend + Divisor - 1) / Divisor;
        }

        // Divides two integers and rounds down
        public static uint DivideAndRoundDown(uint Dividend, uint Divisor)
        {
            return Dividend / Divisor;
        }

        // Divides two intergers and rounds to nearest
        public static uint DivideAndRoundNearest(uint Dividend, uint Divisor)
        {
            return (Dividend >= 0) ? (Dividend + Divisor / 2) / Divisor : (Dividend - Divisor / 2 + 1) / Divisor;
        }

        public static int Max3(int a, int b, int c)
        {
            return System.Math.Max(System.Math.Max(a, b), c);
        }

        public static float Max3(float a, float b, float c)
        {
            return System.Math.Max(System.Math.Max(a, b), c);
        }

        public static float Square(float val)
        {
            return val * val;
        }
    }
}
