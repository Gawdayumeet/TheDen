// SPDX-FileCopyrightText: 2025 Skubman <ba.fallaria@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components;

/// <summary>
///   Component that can be used on body parts to add fire stacks and trigger ignition
///   when the body part is exposed to a type of gas, unless wearing ignition immunity.
/// </summary>
[RegisterComponent]
public sealed partial class IgniteFromGasPartComponent : Component
{
    /// <summary>
    ///   What type of gas triggers ignition.
    /// </summary>
    [DataField(required: true)]
    public Gas Gas;

    /// <summary>
    ///   How many fire stacks this body part applies when exposed.
    /// </summary>
    [DataField]
    public float FireStacks = 0.02f;
}
