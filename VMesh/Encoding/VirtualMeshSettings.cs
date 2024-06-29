namespace VirtualMeshCreator.VMesh.Encoding
{
    public struct VirtualMeshSettings
    {
        public enum Precision
        {
            Auto = 0,
            cm64 = 1,
            cm32 = 2,
            cm16 = 3,
            cm8 = 4,
            cm4 = 5,
            cm2 = 6,
            cm1 = 7,
        }

        public enum MinimumResidency
        {
            Minimal = 32
        }

        public Precision PositionPrecision;
        public MinimumResidency TargetMinimumResidencyInKB;
    }
}
