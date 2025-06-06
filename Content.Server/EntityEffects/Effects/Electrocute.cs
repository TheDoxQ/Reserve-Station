// SPDX-FileCopyrightText: 2024 SlamBamActionman <83650252+SlamBamActionman@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ReserveBot <211949879+ReserveBot@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Svarshik <96281939+lexaSvarshik@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 nazrin <tikufaev@outlook.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Electrocution;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class Electrocute : EntityEffect
{
    [DataField] public int ElectrocuteTime = 2;

    [DataField] public int ElectrocuteDamageScale = 5;

    /// <remarks>
    ///     true - refresh electrocute time,  false - accumulate electrocute time
    /// </remarks>
    [DataField] public bool Refresh = true;

    [DataField] public float ElectrocutionChance = 1f; // Reserve edit - ElectrocutionChance

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-electrocute", ("chance", Probability), ("time", ElectrocuteTime));

    public override bool ShouldLog => true;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
        {
            reagentArgs.EntityManager.System<ElectrocutionSystem>().TryDoElectrocution(reagentArgs.TargetEntity, null,
                Math.Max((reagentArgs.Quantity * ElectrocuteDamageScale).Int(), 1), TimeSpan.FromSeconds(ElectrocuteTime), Refresh, electrocutionChance: ElectrocutionChance, ignoreInsulation: true); //Reserve edit - electrocutionChance

            if (reagentArgs.Reagent != null)
                reagentArgs.Source?.RemoveReagent(reagentArgs.Reagent.ID, reagentArgs.Quantity);
        } else
        {
            args.EntityManager.System<ElectrocutionSystem>().TryDoElectrocution(args.TargetEntity, null,
                Math.Max(ElectrocuteDamageScale, 1), TimeSpan.FromSeconds(ElectrocuteTime), Refresh, ignoreInsulation: true);
        }
    }
}
