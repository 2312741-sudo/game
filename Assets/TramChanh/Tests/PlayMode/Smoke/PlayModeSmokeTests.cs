using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TramChanh.Tests.PlayMode.Smoke
{
    public sealed class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator Smoke_PlayMode_Runs()
        {
            int startFrame = Time.frameCount;
            yield return null;
            Assert.That(Time.frameCount, Is.GreaterThan(startFrame));
        }
    }
}
