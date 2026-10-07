namespace RadiantOrchard
{
    public enum EmotionState
    {
        Neutral,
        Happy,
        Sad,
        Worried,
        Angry,
        Lonely,
        Confused,
        Grateful,
        Excited,
        Peaceful
    }

    public enum FruitType
    {
        Strawberry,
        Pineapple,
        Watermelon,
        Lemon,
        Grapes,
        Apple,
        Peach,
        Banana,
        Cherry
    }

    public enum FruitGrowthStage
    {
        Seed,
        Growing,
        Healthy,
        Vibrant,
        Harvested
    }

    public enum ObjectiveType
    {
        InteractWithNPC,
        HelpNPC,
        RestoreFruitZone,
        RestoreEnvironmentalObject,
        ReachVibrancyThreshold,
        CompleteVirtueChallenge
    }

    public enum IslandState
    {
        Initial,
        PartiallyRestored,
        Vibrant,
        Completed
    }

    public enum TreeState
    {
        Awakening,
        Growing,
        Vibrant,
        FullyAwakened
    }

    // Player gesture used to harvest/brew a fruit into a tonic. Matches
    // GestureManager's recognized gestures exactly — it only ever fires
    // OnDoubleTap/OnLongPress/OnFastSwipe, so this can't have more cases than
    // that without leaving a fruit permanently unharvestable.
    public enum GestureType
    {
        DoubleTap,
        LongPress,
        FastSwipe
    }

    // Proposal Virtue-Health Matrix body systems — drives Stickman symptom
    // bubble copy and per-fruit heal VFX (heart sparks, breath mist, etc.).
    public enum HealthStat
    {
        HeartCirculation,
        ImmunityEnergy,
        NervousSystem,
        DetoxDigestion,
        RespiratoryBreath,
        BrainFocus,
        SkinGlow,
        MuscleStrength,
        BoneStability
    }

    // Cheap stand-in shape for a fruit's world visual before real mesh/VFX art
    // is ready — swap FruitData.placeholderShape usage for a real prefab later.
    public enum PlaceholderShape
    {
        Cube,
        Sphere
    }

    // Generic, extensible NPC behavior state — separate from StickmanController's
    // own StickmanState (which stays exactly as-is: Idle_Sad/Receiving/Celebrate/
    // Roaming/Leaving drive the player-triggered rescue loop and must not change,
    // OnboardingManager/GestureTestRunner read it directly). This enum describes
    // the ambient "living NPC" layer (see NPCSpiritComponent/StickmanController's
    // ambient sub-loop) so new behaviors can be added without touching the
    // existing rescue state machine.
    public enum NPCBehaviorState
    {
        Idle,
        Walking,
        Observing,
        Searching,
        Approaching,
        Interacting,
        ReceivingTonic,
        Healing,
        Celebrating,
        Working,
        Talking,
        Injured
    }

    // How an NPC's Spirit is doing right now. Independent from NPCBehaviorState —
    // the same Walking state looks different under each condition (see
    // NPCSpiritComponent).
    public enum NPCCondition
    {
        Healthy,
        LowSpirit,
        Sick,
        Injured
    }

    // What an NPC currently needs, derived from its NPCCondition. Independent
    // from both state and condition so "what should I look for" stays a single
    // place to query (NPCSpiritComponent.CurrentNeed).
    public enum NPCNeed
    {
        None,
        Healing,
        SpiritRestore,
        Social,
        Rest
    }
}
