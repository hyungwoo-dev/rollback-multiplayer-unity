using NUnit.Framework;
using System.Collections;
using UnityEngine;

public class BattleUnitMoveTest
{
    public struct TestData
    {
        public float Distance;
        public float Time;

        public TestData(float distance, float time) : this()
        {
            Distance = distance;
            Time = time;
        }
    }

    public static IEnumerable GetMoveTestSources()
    {
        for (var i = 0; i < 10; ++i)
        {
            yield return new TestData(Random.Range(1.0f, 10.0f), Random.Range(1.0f, 10.0f));
        }
    }

    [Test]
    [TestCaseSource(nameof(GetMoveTestSources))]
    public static void MoveTest(TestData value)
    {
        var direction = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f)).normalized;
        var unitMove = new BattleUnitMove(new BattleWorld(new BattleWorldManager()));
        var distance = value.Distance;
        unitMove.Initialize(direction, distance, value.Time);

        var expectPosition = direction * distance;
        var count = 90;
        var deltaTime = value.Time / (float)count;

        var movedAmount = Vector3.zero;
        for (var i = 0; i < count; ++i)
        {
            movedAmount += unitMove.AdvanceTime(deltaTime);
        }

        Assert.True(Mathf.Abs((movedAmount - expectPosition).magnitude) < 0.001f, $"Fail, Delta: {movedAmount}, Expect: {distance}");
    }


    [Test]
    [TestCaseSource(nameof(GetMoveTestSources))]
    public static void JumpMoveTest(TestData value)
    {
        var unitMove = new BattleUnitJumpMove(new BattleWorld(new BattleWorldManager()));
        var distance = value.Distance;
        unitMove.Initialize(distance, value.Time);

        var count = 90;
        var deltaTime = value.Time / (float)count;

        var movedAmount = Vector3.zero;
        for (var i = 0; i < count; ++i)
        {
            movedAmount += unitMove.AdvanceTime(deltaTime);
        }

        Assert.True(Mathf.Abs(movedAmount.magnitude) < 0.001f, $"Fail, Delta: {movedAmount}, Expect: {distance}");
    }
}
