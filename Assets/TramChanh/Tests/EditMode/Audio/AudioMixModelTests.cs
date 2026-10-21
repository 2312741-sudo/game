using NUnit.Framework;
using TramChanh.Audio;

namespace TramChanh.Tests.EditMode.Audio
{
    public sealed class AudioMixModelTests
    {
        [Test]
        public void EffectiveVolume_MultipliesMasterBusAndCue()
        {
            var m = new AudioMixModel(4);
            m.SetProfile(AudioCue.Pour, new AudioCueProfile(0.5f, 0f, 1, false, AudioBus.Sfx));
            m.MasterVolume = 0.5f;
            m.SetBusVolume(AudioBus.Sfx, 0.5f);
            Assert.AreEqual(0.125f, m.EffectiveVolume(AudioCue.Pour), 1e-5f);
        }

        [Test]
        public void Volumes_AreClampedAndNaNFallsBack()
        {
            var m = new AudioMixModel(4);
            m.MasterVolume = 5f;
            Assert.AreEqual(1f, m.MasterVolume);
            m.MasterVolume = -1f;
            Assert.AreEqual(0f, m.MasterVolume);
            m.MasterVolume = float.NaN;
            Assert.AreEqual(1f, m.MasterVolume);
        }

        [Test]
        public void MutedMaster_GivesZeroVolumeButStillPlays()
        {
            var m = new AudioMixModel(2);
            m.MasterVolume = 0f;
            AudioPlayDecision d = m.Request(AudioCue.UiClick, 0, 0.1);
            Assert.IsTrue(d.Play);
            Assert.AreEqual(0f, d.Volume);
        }

        [Test]
        public void Cooldown_BlocksRepeatUntilElapsed()
        {
            var m = new AudioMixModel(4);
            m.SetProfile(AudioCue.UiClick, new AudioCueProfile(1f, 1f, 1, false, AudioBus.Sfx));
            Assert.IsTrue(m.Request(AudioCue.UiClick, 10, 0.1).Play);
            AudioPlayDecision blocked = m.Request(AudioCue.UiClick, 10.5, 0.1);
            Assert.IsFalse(blocked.Play);
            Assert.AreEqual(AudioRejectReason.Cooldown, blocked.Reason);
            Assert.IsTrue(m.Request(AudioCue.UiClick, 11.0, 0.1).Play);
        }

        [Test]
        public void RejectedRequest_DoesNotRestartCooldown()
        {
            var m = new AudioMixModel(4);
            m.SetProfile(AudioCue.UiClick, new AudioCueProfile(1f, 1f, 1, false, AudioBus.Sfx));
            m.Request(AudioCue.UiClick, 0, 0.1);
            m.Request(AudioCue.UiClick, 0.9, 0.1);
            Assert.IsTrue(m.Request(AudioCue.UiClick, 1.0, 0.1).Play);
        }

        [Test]
        public void UnknownCue_IsRejected()
        {
            var m = new AudioMixModel(2);
            Assert.AreEqual(AudioRejectReason.UnknownCue, m.Request(AudioCue.None, 0, 1).Reason);
            Assert.AreEqual(AudioRejectReason.UnknownCue, m.Request((AudioCue)999, 0, 1).Reason);
        }

        [Test]
        public void FullVoices_RejectEqualOrLowerPriority()
        {
            var m = new AudioMixModel(1);
            m.SetProfile(AudioCue.Pour, new AudioCueProfile(1f, 0f, 5, false, AudioBus.Sfx));
            m.SetProfile(AudioCue.Ice, new AudioCueProfile(1f, 0f, 5, false, AudioBus.Sfx));
            m.SetProfile(AudioCue.UiClick, new AudioCueProfile(1f, 0f, 1, false, AudioBus.Sfx));
            Assert.IsTrue(m.Request(AudioCue.Pour, 0, 5).Play);
            Assert.AreEqual(AudioRejectReason.VoicesFull, m.Request(AudioCue.Ice, 1, 5).Reason);
            Assert.AreEqual(AudioRejectReason.VoicesFull, m.Request(AudioCue.UiClick, 1, 5).Reason);
        }

        [Test]
        public void FullVoices_HigherPriorityStealsLowest()
        {
            var m = new AudioMixModel(2);
            m.SetProfile(AudioCue.UiClick, new AudioCueProfile(1f, 0f, 1, false, AudioBus.Sfx));
            m.SetProfile(AudioCue.Pour, new AudioCueProfile(1f, 0f, 3, false, AudioBus.Sfx));
            m.SetProfile(AudioCue.TicketBell, new AudioCueProfile(1f, 0f, 9, false, AudioBus.Sfx));
            AudioPlayDecision pour = m.Request(AudioCue.Pour, 0, 5);
            AudioPlayDecision click = m.Request(AudioCue.UiClick, 0.1, 5);
            Assert.AreNotEqual(pour.Slot, click.Slot);
            AudioPlayDecision bell = m.Request(AudioCue.TicketBell, 0.2, 5);
            Assert.IsTrue(bell.Play);
            Assert.IsTrue(bell.StoleSlot);
            Assert.AreEqual(click.Slot, bell.Slot);
            Assert.IsTrue(m.IsActive(AudioCue.Pour, 0.3));
            Assert.IsFalse(m.IsActive(AudioCue.UiClick, 0.3));
        }

        [Test]
        public void MaxConcurrent_IsNeverExceeded()
        {
            var m = new AudioMixModel(3);
            for (int i = 0; i < 20; i++) { m.Request(AudioCue.TicketBell, i * 0.001, 10); m.Request(AudioCue.OrderServed, i * 0.001, 10); m.Request(AudioCue.Pour, i * 0.001, 10); }
            Assert.LessOrEqual(m.ActiveCount(0.1), 3);
        }

        [Test]
        public void FinishedClip_FreesItsVoice()
        {
            var m = new AudioMixModel(1);
            m.SetProfile(AudioCue.Pour, new AudioCueProfile(1f, 0f, 5, false, AudioBus.Sfx));
            Assert.IsTrue(m.Request(AudioCue.Pour, 0, 1).Play);
            Assert.IsFalse(m.Request(AudioCue.Pour, 0.5, 1).Play);
            Assert.IsTrue(m.Request(AudioCue.Pour, 1.0, 1).Play);
        }

        [Test]
        public void Loop_PlaysOnceUntilStopped()
        {
            var m = new AudioMixModel(2);
            Assert.IsTrue(m.Request(AudioCue.GrillSizzleLoop, 0, 2).Play);
            Assert.AreEqual(AudioRejectReason.AlreadyLooping, m.Request(AudioCue.GrillSizzleLoop, 1000, 2).Reason);
            Assert.IsTrue(m.StopLoop(AudioCue.GrillSizzleLoop, out int slot));
            Assert.GreaterOrEqual(slot, 0);
            Assert.IsFalse(m.StopLoop(AudioCue.GrillSizzleLoop, out int none));
            Assert.AreEqual(-1, none);
            Assert.IsTrue(m.Request(AudioCue.GrillSizzleLoop, 1001, 2).Play);
        }

        [Test]
        public void AmbientBus_ScalesIndependently()
        {
            var m = new AudioMixModel(2);
            m.SetBusVolume(AudioBus.Ambient, 0f);
            Assert.AreEqual(0f, m.EffectiveVolume(AudioCue.StreetAmbientLoop));
            Assert.Greater(m.EffectiveVolume(AudioCue.UiClick), 0f);
        }

        [Test]
        public void Constructor_RejectsZeroVoices()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new AudioMixModel(0));
        }
    }
}
