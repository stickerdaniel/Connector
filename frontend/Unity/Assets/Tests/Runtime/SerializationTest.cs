using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class SerializationTest
{
    [Test]  // Normal unit test
    public void SimpleTest()
    {
        Assert.AreEqual(2 + 2, 4);
    }

    [UnityTest]  // Coroutine test (for Play Mode)
    public IEnumerator WaitForSecondsTest()
    {
        yield return new WaitForSeconds(1);
        Assert.Pass();
    }
}
