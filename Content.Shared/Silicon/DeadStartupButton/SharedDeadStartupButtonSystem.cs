// SPDX-FileCopyrightText: 2024 Timemaster99 <57200767+Timemaster99@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2024 sleepyyapril <flyingkarii@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Silicon.DeadStartupButton;

/// <summary>
///     This creates a Button that can be activated after an entity, usually a silicon or an IPC, died.
///     This will activate a doAfter and then revive the entity, playing a custom afterward sound.
/// </summary>
public abstract partial class SharedDeadStartupButtonSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeadStartupButtonComponent, GetVerbsEvent<AlternativeVerb>>(AddTurnOnVerb);
    }

    private void AddTurnOnVerb(EntityUid uid, DeadStartupButtonComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobState)
            || !_mobState.IsDead(uid, mobState)
            || !args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () => TryStartup(args.User, uid, component),
            Text = Loc.GetString(component.VerbText),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Priority = component.VerbPriority
        });
    }

    private void TryStartup(EntityUid user, EntityUid target, DeadStartupButtonComponent comp)
    {
        if (!_net.IsServer)
            return;

        _audio.PlayPvs(comp.ButtonSound, target);
        var args = new DoAfterArgs(EntityManager, user, comp.DoAfterInterval, new OnDoAfterButtonPressedEvent(), target, target:target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };
        _doAfterSystem.TryStartDoAfter(args);
    }

    [Serializable, NetSerializable]
    public sealed partial class OnDoAfterButtonPressedEvent : SimpleDoAfterEvent { }
}
