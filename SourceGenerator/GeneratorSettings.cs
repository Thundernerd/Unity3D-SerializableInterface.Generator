namespace TNRD.SerializableInterface.SourceGenerator
{
    /// <summary>
    /// Settings for the source generator
    /// </summary>
    public static class GeneratorSettings
    {
        /// <summary>
        /// The prefix that will be used for the class name
        /// <example>
        /// If your interface is called IFoo, then with the default prefix (S) the class will be called SIFoo
        /// </example>
        /// </summary>
        public static string ClassPrefix = "S";

        /// <summary>
        /// The suffix that will be added after the class name and before the file extension
        /// <example>
        /// If your interface is called IFoo, then with the default prefix (S) and the default suffix (.g) the filename will be called SIFoo.g.cs</example>
        /// </summary>
        public static string FilenameSuffix = ".g";
    }
}
