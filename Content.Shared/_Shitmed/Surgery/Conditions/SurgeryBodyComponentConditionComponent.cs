// SPDX-FileCopyrightText: 2024 Skubman <ba.fallaria@gmail.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Medical.Surgery.Conditions;

// <summary>
//   What components are necessary in the body for the surgery to be valid.
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryBodyComponentConditionComponent : Component
{
    // <summary>
    //   The components to check for.
    // </summary>
    [DataField(required: true)]
    public ComponentRegistry Components;

    // <summary>
    //   If true, the lack of these components will instead make the surgery valid.
    // </summary>
    [DataField]
    public bool Inverse = false;
}