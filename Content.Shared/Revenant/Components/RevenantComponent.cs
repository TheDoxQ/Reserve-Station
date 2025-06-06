// SPDX-FileCopyrightText: 2023 Chief-Engineer <119664036+Chief-Engineer@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <drsmugleaf@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2023 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2024 DisposableCrewmember42 <disposablecrewmember42@proton.me>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 OnsenCapy <101037138+OnsenCapy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Alert;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.Audio; // Imp
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RevenantComponent : Component
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Essence = 75;

    [DataField("stolenEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField("spawnOnDeathPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "Ectoplasm";

    [DataField("stasisTime"), ViewVariables(VVAccess.ReadWrite)] // Begin Imp Changes
    public TimeSpan StasisTime = TimeSpan.FromSeconds(60);

    /// <summary>
    /// If true, only bible users can exorcise this revenant
    /// with a bible.
    ///
    /// If false, anyone who tries to exorcise a revenant with
    /// a bible will be able to.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ExorcismRequiresBibleUser = true;

    /// <summary>
    /// If true, grinding a revenant's ectoplasm will require
    /// putting salt in the reagent grinder. Otherwise, the
    /// grinder will explode.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool GrindingRequiresSalt = true;  // End Imp Changes

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 100;

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 0.75f;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public FixedPoint2 EssencePerSecond = 1f;

    [ViewVariables]
    public float Accumulator = 0;

    // Here's the gist of the harvest ability:
    // Step 1: The revenant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the revenant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the revenant essence.
    #region Harvest Ability
    /// <summary>
    /// The duration of the soul search
    /// </summary>
    [DataField("soulSearchDuration")]
    public float SoulSearchDuration = 2.5f;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = new(5, 5);

    /// <summary>
    /// The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 10;
    #endregion

    // Begin Imp Changes
    // When used, the revenant reveals itself temporarily and gains stolen essence and a boost in
    // essence regeneration for each crewmate that witnesses it
    #region Haunt Ability

    [DataField("hauntDebuffs"), ViewVariables(VVAccess.ReadWrite)]
    public Vector2 HauntDebuffs = new(3, 8);

    [DataField("hauntStolenEssencePerWitness"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HauntStolenEssencePerWitness = 2.5;

    [DataField("hauntEssenceRegenPerWitness"), ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HauntEssenceRegenPerWitness = 0.5;

    [DataField("hauntEssenceRegenDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan HauntEssenceRegenDuration = TimeSpan.FromSeconds(10);

    [DataField("hauntSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? HauntSound = new SoundCollectionSpecifier("RevenantHaunt");

    [DataField("hauntFlashDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan HauntFlashDuration = TimeSpan.FromSeconds(3.5); //Reserve edit

    #endregion
    // End Imp Changes

    //In the nearby radius, causes various objects to be thrown, messed with, and containers opened
    //Generally just causes a mess
    #region Defile Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileCost")]
    public FixedPoint2 DefileCost = 30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("defileDebuffs")]
    public Vector2 DefileDebuffs = new(1, 4);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileRadius")]
    public float DefileRadius = 3.5f;

    /// <summary>
    /// The amount of tiles that are uprooted by the ability
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileTilePryAmount")]
    public int DefileTilePryAmount = 15;

    /// <summary>
    /// The chance that an individual entity will have any of the effects
    /// happen to it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileEffectChance")]
    public float DefileEffectChance = 0.5f;
    #endregion

    #region Overload Lights Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadCost")]
    public FixedPoint2 OverloadCost = 40;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("overloadDebuffs")]
    public Vector2 OverloadDebuffs = new(3, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadRadius")]
    public float OverloadRadius = 5f;

    /// <summary>
    /// How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadZapRadius")]
    public float OverloadZapRadius = 2f;
    #endregion

    #region Blight Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightCost")]
    public float BlightCost = 50;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("blightDebuffs")]
    public Vector2 BlightDebuffs = new(2, 5);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightRadius")]
    public float BlightRadius = 3.5f;
    #endregion

    #region Malfunction Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionCost")]
    public FixedPoint2 MalfunctionCost = 60;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("malfunctionDebuffs")]
    public Vector2 MalfunctionDebuffs = new(2, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionRadius")]
    public float MalfunctionRadius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can be emagged by malfunction.
    /// Used to prevent ultra gamer things like ghost emagging chem or instantly launching the shuttle.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhitelist;

    /// <summary>
    /// Whitelist for entities that can never be emagged by malfunction.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionBlacklist;
    #endregion

    // Begin Imp Changes
    #region Blood Writing
    [ViewVariables(VVAccess.ReadWrite), DataField("bloodWritingCost")]
    public FixedPoint2 BloodWritingCost = 2;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public EntityUid? BloodCrayon;

    #endregion

    #region Animate
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public FixedPoint2 AnimateCost = 50;

    /// <summary>
    /// How long an item should be animated for
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan AnimateTime = TimeSpan.FromSeconds(15);

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Vector2 AnimateDebuffs = new(3, 8);

    public const float DefaultAnimateWalkSpeed = 1.5f;
    public const float DefaultAnimateSprintSpeed = 3.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float AnimateWalkSpeed = DefaultAnimateWalkSpeed;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float AnimateSprintSpeed = DefaultAnimateSprintSpeed;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool AnimateCanBoltGuns = true;
    #endregion
    // End Imp Changes

    [DataField]
    public ProtoId<AlertPrototype> EssenceAlert = "Essence";

    #region Visualizer
    [DataField("state")]
    public string State = "idle";
    [DataField("corporealState")]
    public string CorporealState = "active";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    [DataField("harvestingState")]
    public string HarvestingState = "harvesting";
    #endregion

    [DataField] public EntityUid? ShopAction; // Imp
    [DataField] public EntityUid? HauntAction; // Imp
}
