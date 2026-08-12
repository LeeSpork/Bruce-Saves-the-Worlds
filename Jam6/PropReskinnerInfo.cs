namespace Jam6
{
    public enum PropReskinnerStyles
    {
        Default = 0,
        PreCrashNomai,
        BrittleHollow,
    }

    public enum PaintedDetailsMode
    {
        Keep,
        Clean,
        AltMaterial,
        // Restored,
    }

    public class PropReskinnerInfo
    {
        /// <summary>
        /// Determines what materials & textures the props get reskinned to.
        /// </summary>
        public PropReskinnerStyles style;

        /// <summary>
        /// How are texture details such as paint and carved shapes handled?
        /// `Keep` : keeps original paint details.
        /// `Clean` : washes the paint off.
        /// `AltMaterial` : replaces painted surfaces with detailed metal, similar to the props from Mod Jam 3 & 5's Starship Community and Central Station.
        /// </summary>
        public PaintedDetailsMode details;

        /// <summary>
        /// Path to diffuse texture to use for suited Nomai charaters (e.g. Solanum, corpses)
        /// </summary>
        public string nomaiCharacterSuit;

        public string nomaiMaskTrim;

        /// <summary>
        /// Paths to props that you want to get reskinned. Will reskin any children as well.
        /// </summary>
        public string[] props;
    }
}