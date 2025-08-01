// SPDX-FileCopyrightText: 2020 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 Acruid <shatter66@gmail.com>
// SPDX-FileCopyrightText: 2021 Vera Aguilera Puerto <gradientvera@outlook.com>
// SPDX-FileCopyrightText: 2021 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <metalgearsloth@gmail.com>
// SPDX-FileCopyrightText: 2022 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Jezithyr <Jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2022 mirrorcult <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Debug <49997488+DebugOk@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Jezithyr <jezithyr@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2024 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2024 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 sleepyyapril <flyingkarii@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Content.Server.Administration;
using Content.Server.Body.Systems;
using Content.Shared.Administration;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Console;

namespace Content.Server.Body.Commands
{
    [AdminCommand(AdminFlags.Fun)]
    public sealed class AttachBodyPartCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        public string Command => "attachbodypart";
        public string Description => "Attaches a body part to you or someone else.";
        public string Help => $"{Command} <partEntityUid> / {Command} <entityUid> <partEntityUid>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;

            EntityUid bodyId;
            EntityUid? partUid;

            switch (args.Length)
            {
                case 1:
                    if (player == null)
                    {
                        shell.WriteLine($"You need to specify an entity to attach the part to if you aren't a player.\n{Help}");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.WriteLine($"You need to specify an entity to attach the part to if you aren't attached to an entity.\n{Help}");
                        return;
                    }

                    if (!NetEntity.TryParse(args[0], out var partNet) || !_entManager.TryGetEntity(partNet, out partUid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    bodyId = player.AttachedEntity.Value;

                    break;
                case 2:
                    if (!NetEntity.TryParse(args[0], out var entityNet) || !_entManager.TryGetEntity(entityNet, out var entityUid))
                    {
                        shell.WriteLine($"{args[0]} is not a valid entity uid.");
                        return;
                    }

                    if (!NetEntity.TryParse(args[1], out partNet) || !_entManager.TryGetEntity(partNet, out partUid))
                    {
                        shell.WriteLine($"{args[1]} is not a valid entity uid.");
                        return;
                    }

                    if (!_entManager.EntityExists(entityUid))
                    {
                        shell.WriteLine($"{entityUid} is not a valid entity.");
                        return;
                    }

                    bodyId = entityUid.Value;
                    break;
                default:
                    shell.WriteLine(Help);
                    return;
            }

            if (!_entManager.TryGetComponent(bodyId, out BodyComponent? body))
            {
                shell.WriteLine($"Entity {_entManager.GetComponent<MetaDataComponent>(bodyId).EntityName} with uid {bodyId} does not have a {nameof(BodyComponent)}.");
                return;
            }

            if (!_entManager.EntityExists(partUid))
            {
                shell.WriteLine($"{partUid} is not a valid entity.");
                return;
            }

            if (!_entManager.TryGetComponent(partUid, out BodyPartComponent? part))
            {
                shell.WriteLine($"Entity {_entManager.GetComponent<MetaDataComponent>(partUid.Value).EntityName} with uid {args[0]} does not have a {nameof(BodyPartComponent)}.");
                return;
            }

            var bodySystem = _entManager.System<BodySystem>();
            if (bodySystem.BodyHasChild(bodyId, partUid.Value, body, part))
            {
                shell.WriteLine($"Body part {_entManager.GetComponent<MetaDataComponent>(partUid.Value).EntityName} with uid {partUid} is already attached to entity {_entManager.GetComponent<MetaDataComponent>(bodyId).EntityName} with uid {bodyId}");
                return;
            }

            // Shitmed Change Start
            var slotId = "";
            if (part.Symmetry != BodyPartSymmetry.None)
                slotId = $"{part.Symmetry.ToString().ToLower()} {part.GetHashCode().ToString()}";
            else
                slotId = $"{part.GetHashCode().ToString()}";

            part.SlotId = part.GetHashCode().ToString();
            // Shitmed Change End
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (body.RootContainer.ContainedEntity != null)
            {
                bodySystem.AttachPartToRoot(bodyId, partUid.Value, body, part);
            }
            else
            {
                var (rootPartId, rootPart) = bodySystem.GetRootPartOrNull(bodyId, body)!.Value;
                if (!bodySystem.TryCreatePartSlotAndAttach(rootPartId, slotId, partUid.Value, part.PartType, rootPart, part))
                {
                    shell.WriteError($"Could not create slot {slotId} on entity {_entManager.ToPrettyString(bodyId)}");
                    return;
                }
            }

            shell.WriteLine($"Attached part {_entManager.ToPrettyString(partUid.Value)} to {_entManager.ToPrettyString(bodyId)}");
        }
    }
}
