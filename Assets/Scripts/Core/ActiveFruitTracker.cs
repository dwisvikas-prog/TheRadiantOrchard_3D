namespace RadiantOrchard
{
    /// <summary>
    /// Which fruit the current visitor needs, right now — so the bottom tray
    /// can blink that one icon and the player instantly knows whose turn it is.
    /// </summary>
    public static class ActiveFruitTracker
    {
        public static bool HasActive { get; private set; }
        public static FruitType Current { get; private set; }

        public static void Set(FruitType fruit)
        {
            Current = fruit;
            HasActive = true;
        }

        public static void Clear()
        {
            HasActive = false;
        }
    }
}
