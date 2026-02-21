using FixedMathSharp;
using NUnit.Framework;
using System.Collections;
using UnityEngine;

public class BattleUnitMoveTest
{
    public struct TestData
    {
        public Fixed64 Distance;
        public Fixed64 Time;

        public TestData(Fixed64 distance, Fixed64 time) : this()
        {
            Distance = distance;
            Time = time;
        }
    }

    public static IEnumerable GetMoveTestSources()
    {
        for (var i = 0; i < 10; ++i)
        {
            yield return new TestData(new Fixed64(Random.Range(1.0f, 10.0f)), new Fixed64(Random.Range(1.0f, 10.0f)));
        }
    }

    [Test]
    [TestCaseSource(nameof(GetMoveTestSources))]
    public static void MoveTest(TestData value)
    {
        var direction = new Vector3d(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)).Normalize();
        var unitMove = new BattleUnitDashMoveController(new BattleWorld(new BattleWorldManager()));
        var distance = value.Distance;
        unitMove.Initialize(direction, distance, value.Time);

        var expectPosition = direction * distance;
        var count = 90;
        var deltaTime = value.Time / (Fixed64)count;

        var movedAmount = Vector3d.Zero;
        for (var i = 0; i < count; ++i)
        {
            movedAmount += unitMove.AdvanceTime(deltaTime);
        }

        Assert.True((movedAmount - expectPosition).Magnitude.Abs() < new Fixed64(0.001d), $"Fail, Delta: {movedAmount}, Expect: {distance}");
    }


    [Test]
    [TestCaseSource(nameof(GetMoveTestSources))]
    public static void JumpMoveTest(TestData value)
    {
        var unitMove = new BattleUnitJumpMove(new BattleWorld(new BattleWorldManager()));
        var distance = value.Distance;
        unitMove.Initialize(distance, value.Time);

        var count = 90;
        var deltaTime = value.Time / (Fixed64)count;

        var movedAmount = Vector3d.Zero;
        for (var i = 0; i < count; ++i)
        {
            movedAmount += unitMove.AdvanceTime(deltaTime);
        }

        Assert.True((movedAmount.Magnitude).Abs() < new Fixed64(0.001f), $"Fail, Delta: {movedAmount}, Expect: {distance}");
    }
}
