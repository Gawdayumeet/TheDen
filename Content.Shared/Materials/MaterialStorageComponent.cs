// SPDX-FileCopyrightText: 2023 Debug <49997488+DebugOk@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Hannah Giovanna Dawson <karakkaraz@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Eris <eris@erisws.com>
// SPDX-FileCopyrightText: 2025 Spatison <137375981+Spatison@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Materials;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MaterialStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Storage { get; set; } = new();

    /// <summary>
    /// Whether or not interacting with the materialstorage inserts the material in hand.
    /// </summary>
    [DataField]
    public bool InsertOnInteract = true;

    /// <summary>
    ///     How much material the storage can store in total.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int? StorageLimit;

    /// <summary>
    /// Whitelist for specifying the kind of items that can be insert into this entity.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Whether or not to drop contained materials when deconstructed.
    /// </summary>
    [DataField]
    public bool DropOnDeconstruct = true;

    /// <summary>
    /// Whitelist generated on runtime for what specific materials can be inserted into this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<MaterialPrototype>>? MaterialWhiteList;

    /// <summary>
    /// Whether or not the visualization for the insertion animation
    /// should ignore the color of the material being inserted.
    /// </summary>
    [DataField]
    public bool IgnoreColor;

    /// <summary>
    /// The sound that plays when inserting an item into the storage
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertingSound;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [DataField]
    public TimeSpan InsertionTime = TimeSpan.FromSeconds(0.79f); // 0.01 off for animation timing

    /// <summary>
    /// Whether the storage can eject the materials stored within it
    /// </summary>
    [DataField]
    public bool CanEjectStoredMaterials = true;
}

[Serializable, NetSerializable]
public enum MaterialStorageVisuals : byte
{
    Inserting
}

/// <summary>
/// Lavaland Change: Event raised on the materialStorage when a material entity is inserted into it.
/// </summary>
[ByRefEvent]
public readonly record struct MaterialEntityInsertedEvent(EntityUid User, EntityUid Inserted, MaterialComponent MaterialComp, int Count)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Inserted = Inserted;
    public readonly MaterialComponent MaterialComp = MaterialComp;
    public readonly int Count = Count;
}

/// <summary>
/// Event raised when a material amount is changed
/// </summary>
[ByRefEvent]
public readonly record struct MaterialAmountChangedEvent;

/// <summary>
/// Event raised to get all the materials that the
/// </summary>
[ByRefEvent]
public record struct GetMaterialWhitelistEvent(EntityUid Storage)
{
    public readonly EntityUid Storage = Storage;

    public List<ProtoId<MaterialPrototype>> Whitelist = new();
}

/// <summary>
/// Message sent to try and eject a material from a storage
/// </summary>
[Serializable, NetSerializable]
public sealed class EjectMaterialMessage : EntityEventArgs
{
    public NetEntity Entity;
    public string Material;
    public int SheetsToExtract;

    public EjectMaterialMessage(NetEntity entity, string material, int sheetsToExtract)
    {
        Entity = entity;
        Material = material;
        SheetsToExtract = sheetsToExtract;
    }
}

