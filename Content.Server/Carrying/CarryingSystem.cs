// SPDX-FileCopyrightText: 2023 Adrian16199
// SPDX-FileCopyrightText: 2024 DEATHB4DEFEAT
// SPDX-FileCopyrightText: 2024 Debug
// SPDX-FileCopyrightText: 2024 SimpleStation14
// SPDX-FileCopyrightText: 2024 VMSolidus
// SPDX-FileCopyrightText: 2024 cynical
// SPDX-FileCopyrightText: 2025 Eagle-0
// SPDX-FileCopyrightText: 2025 FoxxoTrystan
// SPDX-FileCopyrightText: 2025 M3739
// SPDX-FileCopyrightText: 2025 Mnemotechnican
// SPDX-FileCopyrightText: 2025 RedFoxIV
// SPDX-FileCopyrightText: 2025 sleepyyapril
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Numerics;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Resist;
using Content.Server.Popups;
using Content.Server.Inventory;
using Content.Server.Nyanotrasen.Item.PseudoItem;
using Content.Shared.Mobs;
using Content.Shared.DoAfter;
using Content.Shared.Buckle.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Stunnable;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Climbing.Events;
using Content.Shared.Carrying;
using Content.Shared.Contests;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.ActionBlocker;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Throwing;
using Content.Shared.Mind.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nyanotrasen.Item.PseudoItem;
using Content.Shared.Storage;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Server.GameObjects;
using System.Numerics;
using Content.Shared._DV.Polymorph;

namespace Content.Server.Carrying
{
    public sealed class CarryingSystem : EntitySystem
    {
        [Dependency] private readonly VirtualItemSystem _virtualItemSystem = default!;
        [Dependency] private readonly CarryingSlowdownSystem _slowdown = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StandingStateSystem _standingState = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly PullingSystem _pullingSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly EscapeInventorySystem _escapeInventorySystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly PseudoItemSystem _pseudoItem = default!;
        [Dependency] private readonly ContestsSystem _contests = default!;
        [Dependency] private readonly TransformSystem _transform = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CarriableComponent, GetVerbsEvent<AlternativeVerb>>(AddCarryVerb);
            SubscribeLocalEvent<CarryingComponent, GetVerbsEvent<InnateVerb>>(AddInsertCarriedVerb);
            SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
            SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnThrow);
            SubscribeLocalEvent<CarryingComponent, EntParentChangedMessage>(OnParentChanged);
            SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<CarryingComponent, BeforePolymorphedEvent>(OnBeforePolymorphed);
            SubscribeLocalEvent<BeingCarriedComponent, InteractionAttemptEvent>(OnInteractionAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BeingCarriedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StandAttemptEvent>(OnStandAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, GettingInteractedWithAttemptEvent>(OnInteractedWith);
            SubscribeLocalEvent<BeingCarriedComponent, PullAttemptEvent>(OnPullAttempt);
            SubscribeLocalEvent<BeingCarriedComponent, StartClimbEvent>(OnStartClimb);
            SubscribeLocalEvent<BeingCarriedComponent, BuckledEvent>(OnBuckled);
            SubscribeLocalEvent<CarriableComponent, CarryDoAfterEvent>(OnDoAfter);
        }

        private void AddCarryVerb(EntityUid uid, CarriableComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess || !_mobStateSystem.IsAlive(args.User)
                || !CanCarry(args.User, uid, component)
                || HasComp<CarryingComponent>(args.User)
                || HasComp<BeingCarriedComponent>(args.User) || HasComp<BeingCarriedComponent>(args.Target)
                || args.User == args.Target)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartCarryDoAfter(args.User, uid, component);
                },
                Text = Loc.GetString("carry-verb"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void AddInsertCarriedVerb(EntityUid uid, CarryingComponent component, GetVerbsEvent<InnateVerb> args)
        {
            // If the person is carrying someone, and the carried person is a pseudo-item, and the target entity is a storage,
            // then add an action to insert the carried entity into the target
            var toInsert = args.Using;
            if (toInsert is not { Valid: true } || !args.CanAccess
                || !TryComp<PseudoItemComponent>(toInsert, out var pseudoItem)
                || !TryComp<StorageComponent>(args.Target, out var storageComp)
                || !_pseudoItem.CheckItemFits((toInsert.Value, pseudoItem), (args.Target, storageComp)))
                return;

            InnateVerb verb = new()
            {
                Act = () =>
                {
                    DropCarried(uid, toInsert.Value);
                    _pseudoItem.TryInsert(args.Target, toInsert.Value, pseudoItem, storageComp);
                },
                Text = Loc.GetString("action-name-insert-other", ("target", toInsert)),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        /// <summary>
        /// Since the carried entity is stored as 2 virtual items, when deleted we want to drop them.
        /// </summary>
        private void OnVirtualItemDeleted(EntityUid uid, CarryingComponent component, VirtualItemDeletedEvent args)
        {
            if (!HasComp<CarriableComponent>(args.BlockingEntity))
                return;

            DropCarried(uid, args.BlockingEntity);
        }

        /// <summary>
        /// Basically using virtual item passthrough to throw the carried person. A new age!
        /// Maybe other things besides throwing should use virt items like this...
        /// </summary>
        private void OnThrow(EntityUid uid, CarryingComponent component, ref BeforeThrowEvent args)
        {
            if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtItem)
                || !HasComp<CarriableComponent>(virtItem.BlockingEntity))
                return;

            args.ItemUid = virtItem.BlockingEntity;

            args.ThrowSpeed *= _contests.MassContest(uid, virtItem.BlockingEntity, false, 2f)
                            * _contests.StaminaContest(uid, virtItem.BlockingEntity);
        }

        private void OnParentChanged(EntityUid uid, CarryingComponent component, ref EntParentChangedMessage args)
        {
            var xform = Transform(uid);
            if (xform.MapUid != args.OldMapId || xform.ParentUid == xform.GridUid)
                return;

            DropCarried(uid, component.Carried);
        }

        private void OnMobStateChanged(EntityUid uid, CarryingComponent component, MobStateChangedEvent args)
        {
            DropCarried(uid, component.Carried);
        }

        private void OnBeforePolymorphed(Entity<CarryingComponent> ent, ref BeforePolymorphedEvent args)
        {
            if (HasComp<MindContainerComponent>(ent.Comp.Carried))
                DropCarried(ent, ent.Comp.Carried);
        }

        /// <summary>
        /// Only let the person being carried interact with their carrier and things on their person.
        /// </summary>
        private void OnInteractionAttempt(EntityUid uid, BeingCarriedComponent component, InteractionAttemptEvent args)
        {
            // Floofstation - function body reviewed
            Predicate<TransformComponent> isChildOfCarrier = null!; // C# doesn't have local functions eugh
            isChildOfCarrier = (childXForm) => childXForm.ParentUid == component.Carrier
                             || (childXForm.ParentUid is { Valid: true } parent && isChildOfCarrier(Transform(parent)));

            if (args.Target == null // Allow self-interacts
                || isChildOfCarrier(Transform(args.Target.Value))) // Allow interacting with everything on the carriee
                return;

            // Also check if the interacted-with entity is on the carrier and cancel the event if not
            var targetParent = Transform(args.Target.Value).ParentUid;
            if (args.Target.Value != component.Carrier && targetParent != component.Carrier && targetParent != uid)
                args.Cancelled = true;
        }

        /// <summary>
        /// Try to escape via the escape inventory system.
        /// </summary>
        private void OnMoveInput(EntityUid uid, BeingCarriedComponent component, ref MoveInputEvent args)
        {
            if (!TryComp<CanEscapeInventoryComponent>(uid, out var escape)
                || !args.HasDirectionalMovement)
                return;

            // Check if the victim is in any way incapacitated, and if not make an escape attempt.
            // Escape time scales with the inverse of a mass contest. Being lighter makes escape harder.
            if (_actionBlockerSystem.CanInteract(uid, component.Carrier))
                _escapeInventorySystem.AttemptEscape(uid, component.Carrier, escape, _contests.MassContest(uid, component.Carrier, false, 2f));
        }

        private void OnMoveAttempt(EntityUid uid, BeingCarriedComponent component, UpdateCanMoveEvent args)
        {
            args.Cancel();
        }

        private void OnStandAttempt(EntityUid uid, BeingCarriedComponent component, StandAttemptEvent args)
        {
            args.Cancel();
        }

        private void OnInteractedWith(EntityUid uid, BeingCarriedComponent component, GettingInteractedWithAttemptEvent args)
        {
            // Floofstation - why.
            // if (args.Uid != component.Carrier)
            //     args.Cancel();
        }

        private void OnPullAttempt(EntityUid uid, BeingCarriedComponent component, PullAttemptEvent args)
        {
            args.Cancelled = true;
        }

        private void OnStartClimb(EntityUid uid, BeingCarriedComponent component, ref StartClimbEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnBuckled(EntityUid uid, BeingCarriedComponent component, ref BuckledEvent args)
        {
            DropCarried(component.Carrier, uid);
        }

        private void OnDoAfter(EntityUid uid, CarriableComponent component, CarryDoAfterEvent args)
        {
            component.CancelToken = null;
            if (args.Handled || args.Cancelled
                || !CanCarry(args.Args.User, uid, component))
                return;

            Carry(args.Args.User, uid);
            args.Handled = true;
        }
        private void StartCarryDoAfter(EntityUid carrier, EntityUid carried, CarriableComponent component)
        {
            if (!TryComp<PhysicsComponent>(carrier, out var carrierPhysics)
                || !TryComp<PhysicsComponent>(carried, out var carriedPhysics)
                || carriedPhysics.Mass > carrierPhysics.Mass * 2f)
            {
                _popupSystem.PopupEntity(Loc.GetString("carry-too-heavy"), carried, carrier, Shared.Popups.PopupType.SmallCaution);
                return;
            }

            var length = TimeSpan.FromSeconds(component.PickupDuration
                        * _contests.MassContest(carriedPhysics, carrierPhysics, false, 4f)
                        * _contests.StaminaContest(carrier, carried)
                        * (_standingState.IsDown(carried) ? 0.5f : 1));

            component.CancelToken = new CancellationTokenSource();

            var ev = new CarryDoAfterEvent();
            var args = new DoAfterArgs(EntityManager, carrier, length, ev, carried, target: carried)
            {
                BreakOnMove = true,
                NeedHand = true
            };

            _doAfterSystem.TryStartDoAfter(args);

            // Show a popup to the person getting picked up
            _popupSystem.PopupEntity(Loc.GetString("carry-started", ("carrier", carrier)), carried, carried);
        }

        private void Carry(EntityUid carrier, EntityUid carried)
        {
            if (TryComp<PullableComponent>(carried, out var pullable))
                _pullingSystem.TryStopPull(carried, pullable);

            EnsureComp<KnockedDownComponent>(carried); // Floof - moved this statement up because some systems can break carrying in response to knockdown
            _transform.AttachToGridOrMap(carrier);
            _transform.AttachToGridOrMap(carried);
            _transform.SetCoordinates(carried, Transform(carrier).Coordinates);
            _transform.SetParent(carried, carrier);
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            _virtualItemSystem.TrySpawnVirtualItemInHand(carried, carrier);
            var carryingComp = EnsureComp<CarryingComponent>(carrier);
            ApplyCarrySlowdown(carrier, carried);
            var carriedComp = EnsureComp<BeingCarriedComponent>(carried);

            carryingComp.Carried = carried;
            carriedComp.Carrier = carrier;

            _actionBlockerSystem.UpdateCanMove(carried);
        }

        public bool TryCarry(EntityUid carrier, EntityUid toCarry, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(toCarry, ref carriedComp, false)
                || !CanCarry(carrier, toCarry, carriedComp)
                || HasComp<BeingCarriedComponent>(carrier)
                || HasComp<ItemComponent>(carrier)
                || TryComp<PhysicsComponent>(carrier, out var carrierPhysics)
                && TryComp<PhysicsComponent>(toCarry, out var toCarryPhysics)
                && carrierPhysics.Mass * 2f < toCarryPhysics.Mass)
                return false;

            Carry(carrier, toCarry);

            return true;
        }

        public void DropCarried(EntityUid carrier, EntityUid carried)
        {
            RemComp<CarryingComponent>(carrier); // get rid of this first so we don't recursively fire that event
            RemComp<CarryingSlowdownComponent>(carrier);
            RemComp<BeingCarriedComponent>(carried);
            RemComp<KnockedDownComponent>(carried);
            _actionBlockerSystem.UpdateCanMove(carried);
            _virtualItemSystem.DeleteInHandsMatching(carrier, carried);
            _transform.AttachToGridOrMap(carried);
            _standingState.Stand(carried);
            _movementSpeed.RefreshMovementSpeedModifiers(carrier);
        }

        private void ApplyCarrySlowdown(EntityUid carrier, EntityUid carried)
        {
            var massRatio = _contests.MassContest(carrier, carried, true);
            var massRatioSq = MathF.Pow(massRatio, 2);
            var modifier = 1 - 0.15f / massRatioSq;
            modifier = Math.Max(0.1f, modifier);

            var slowdownComp = EnsureComp<CarryingSlowdownComponent>(carrier);
            _slowdown.SetModifier(carrier, modifier, modifier, slowdownComp);
        }

        public bool CanCarry(EntityUid carrier, EntityUid carried, CarriableComponent? carriedComp = null)
        {
            if (!Resolve(carried, ref carriedComp, false)
                || carriedComp.CancelToken != null
                || !HasComp<MapGridComponent>(Transform(carrier).ParentUid)
                || HasComp<BeingCarriedComponent>(carrier)
                || HasComp<BeingCarriedComponent>(carried)
                || !TryComp<HandsComponent>(carrier, out var hands)
                || hands.CountFreeHands() < carriedComp.FreeHandsRequired)
                return false;
            return true;
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<BeingCarriedComponent>();
            while (query.MoveNext(out var carried, out var comp))
            {
                var carrier = comp.Carrier;
                if (carrier is not { Valid: true } || carried is not { Valid: true })
                    continue;

                // SOMETIMES - when an entity is inserted into disposals, or a cryosleep chamber - it can get re-parented without a proper reparent event
                // when this happens, it needs to be dropped because it leads to weird behavior
                if (Transform(carried).ParentUid != carrier)
                {
                    DropCarried(carrier, carried);
                    continue;
                }

                // Make sure the carried entity is always centered relative to the carrier, as gravity pulls can offset it otherwise
                var xform = Transform(carried);
                if (!xform.LocalPosition.Equals(Vector2.Zero))
                {
                    xform.LocalPosition = Vector2.Zero;
                }
            }
            query.Dispose();
        }
    }
}
