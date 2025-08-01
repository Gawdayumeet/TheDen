// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Rane <60792108+Elijahrane@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 Visne <39844191+Visne@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

#nullable enable
using Content.Server.Stack;
using Content.Shared.Stacks;
using Content.Shared.Materials;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Materials
{
    /// <summary>
    /// Materials and stacks have some odd relationships to entities,
    /// so we need some test coverage for them.
    /// </summary>
    [TestFixture]
    [TestOf(typeof(StackSystem))]
    [TestOf(typeof(MaterialPrototype))]
    public sealed class MaterialPrototypeSpawnsStackMaterialTest
    {
        [Test]
        public async Task MaterialPrototypeSpawnsStackMaterial()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            await server.WaitIdleAsync();

            var mapManager = server.ResolveDependency<IMapManager>();
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();

            var testMap = await pair.CreateTestMap();

            await server.WaitAssertion(() =>
            {
                var allMaterialProtos = prototypeManager.EnumeratePrototypes<MaterialPrototype>();
                var coords = testMap.GridCoords;

                Assert.Multiple(() =>
                {
                    foreach (var proto in allMaterialProtos)
                    {
                        if (proto.StackEntity == null)
                            continue;

                        var spawned = entityManager.SpawnEntity(proto.StackEntity, coords);

                        Assert.That(entityManager.TryGetComponent<StackComponent>(spawned, out var stack),
                            $"{proto.ID} 'stack entity' {proto.StackEntity} does not have the stack component");

                        Assert.That(entityManager.HasComponent<MaterialComponent>(spawned),
                            $"{proto.ID} 'material stack' {proto.StackEntity} does not have the material component");

                        StackPrototype? stackProto = null;
                        Assert.That(stack?.StackTypeId != null && prototypeManager.TryIndex(stack.StackTypeId, out stackProto),
                            $"{proto.ID} material has no stack prototype");

                        if (stackProto != null)
                            Assert.That(proto.StackEntity, Is.EqualTo(stackProto.Spawn));
                    }
                });

                mapManager.DeleteMap(testMap.MapId);
            });

            await pair.CleanReturnAsync();
        }
    }
}
