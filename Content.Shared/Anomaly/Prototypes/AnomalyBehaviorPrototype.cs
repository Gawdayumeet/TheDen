// SPDX-FileCopyrightText: 2024 Ed <96445749+TheShuEd@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Prototypes;

namespace Content.Shared.Anomaly.Prototypes;

[Prototype]
public sealed partial class AnomalyBehaviorPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// Description for anomaly scanner
    /// </summary>
    [DataField]
    public string Description = string.Empty;

    /// <summary>
    /// modification of the number of points earned from an anomaly
    /// </summary>
    [DataField]
    public float EarnPointModifier = 1f;

    /// <summary>
    /// deceleration or acceleration of the pulsation frequency of the anomaly
    /// </summary>
    [DataField]
    public float PulseFrequencyModifier = 1f;

    /// <summary>
    /// pulse and supercrit power modifier
    /// </summary>
    [DataField]
    public float PulsePowerModifier = 1f;

    /// <summary>
    /// how much the particles will affect the anomaly
    /// </summary>
    [DataField]
    public float ParticleSensivity = 1f;

    /// <summary>
    /// Components that are added to the anomaly when this behavior is selected, and removed when another behavior is selected.
    /// </summary>
    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();
}
