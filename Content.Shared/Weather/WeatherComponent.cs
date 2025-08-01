// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Weather;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherComponent : Component
{
    /// <summary>
    /// Currently running weathers
    /// </summary>
    [ViewVariables, DataField("weather", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<WeatherData, WeatherPrototype>))]
    public Dictionary<string, WeatherData> Weather = new();

    public static readonly TimeSpan StartupTime = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan ShutdownTime = TimeSpan.FromSeconds(15);
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WeatherData
{
    // Client audio stream.
    [NonSerialized]
    public EntityUid? Stream;

    /// <summary>
    /// When the weather started if relevant.
    /// </summary>
    [ViewVariables, DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// When the applied weather will end.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? EndTime;

    [ViewVariables]
    public TimeSpan Duration => EndTime == null ? TimeSpan.MaxValue : EndTime.Value - StartTime;

    [DataField("state")]
    public WeatherState State = WeatherState.Invalid;
}

public enum WeatherState : byte
{
    Invalid = 0,
    Starting,
    Running,
    Ending,
}
