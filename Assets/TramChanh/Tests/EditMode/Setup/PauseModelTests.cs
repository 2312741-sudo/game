using NUnit.Framework;
using TramChanh.UI.Menu;

namespace TramChanh.Tests.EditMode.Setup
{
    public sealed class PauseModelTests
    {
        [Test]
        public void Starts_Playing()
        {
            Assert.That(new PauseModel().State, Is.EqualTo(PauseState.Playing));
        }

        [Test]
        public void UserPause_Enters_AndRemembersTimeScale()
        {
            var m = new PauseModel();
            Assert.That(m.Sync(true, false, 18f), Is.EqualTo(PauseTransition.Entered));
            Assert.That(m.IsPaused, Is.True);
            Assert.That(m.SavedTimeScale, Is.EqualTo(18f));
        }

        [Test]
        public void Resume_Exits_AndKeepsSavedScale()
        {
            var m = new PauseModel();
            m.Sync(true, false, 18f);
            Assert.That(m.Sync(true, false, 0f), Is.EqualTo(PauseTransition.None));
            Assert.That(m.Sync(false, false, 0f), Is.EqualTo(PauseTransition.Exited));
            Assert.That(m.SavedTimeScale, Is.EqualTo(18f));
            Assert.That(m.IsPaused, Is.False);
        }

        [Test]
        public void ModalOpen_RefusesPause()
        {
            var m = new PauseModel();
            Assert.That(m.Sync(true, true, 1f), Is.EqualTo(PauseTransition.None));
            Assert.That(m.IsPaused, Is.False);
        }

        [Test]
        public void ModalAppearing_EndsPause()
        {
            var m = new PauseModel();
            m.Sync(true, false, 1f);
            Assert.That(m.Sync(true, true, 0f), Is.EqualTo(PauseTransition.Exited));
        }

        [Test]
        public void ZeroTimeScale_FallsBackToOne()
        {
            var m = new PauseModel();
            m.Sync(true, false, 0f);
            Assert.That(m.SavedTimeScale, Is.EqualTo(1f));
        }

        [Test]
        public void Release_LeavesPaused_AndReturnsSavedScale()
        {
            var m = new PauseModel();
            m.Sync(true, false, 4f);
            Assert.That(m.Release(), Is.EqualTo(4f));
            Assert.That(m.IsPaused, Is.False);
        }

        [Test]
        public void SecondPause_SavesScaleAgain()
        {
            var m = new PauseModel();
            m.Sync(true, false, 3f);
            m.Sync(false, false, 0f);
            m.Sync(true, false, 5f);
            Assert.That(m.SavedTimeScale, Is.EqualTo(5f));
        }
    }
}
