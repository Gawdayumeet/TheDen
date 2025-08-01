// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <6766154+Zumorica@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Paul Ritter <ritter.paul1@googlemail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Utility;

namespace Content.Shared.Tools
{
    [Prototype("tool")]
    public sealed partial class ToolQualityPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Human-readable name for this tool quality e.g. "Anchoring"
        /// </summary>
        /// <remarks>This is a localization string ID.</remarks>
        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        ///     Human-readable name for a tool of this type e.g. "Wrench"
        /// </summary>
        /// <remarks>This is a localization string ID.</remarks>
        [DataField("toolName")]
        public string ToolName { get; private set; } = string.Empty;

        /// <summary>
        ///     An icon that will be used to represent this tool type.
        /// </summary>
        [DataField("icon")]
        public SpriteSpecifier? Icon { get; private set; } = null;

        /// <summary>
        ///     The default entity prototype for this tool type.
        /// </summary>
        [DataField("spawn", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string Spawn { get; private set; } = string.Empty;
    }
}
