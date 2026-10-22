namespace TramChanh.Audio
{
    /// <summary>Where mixer values are written. The Unity layer wraps AudioMixer.SetFloat; tests use a fake.</summary>
    public interface IMixerFloatSink
    {
        /// <summary>False when the parameter is not exposed (or there is no mixer), like AudioMixer.SetFloat.</summary>
        bool SetFloat(string parameter, float value);
    }

    /// <summary>
    /// Pure logic: turns the three 0..1 volumes into dB and sends them to the exposed mixer parameters.
    /// Group paths and parameter names must match the asset made by Tram Chanh/Audio/Build Audio Mixer.
    /// </summary>
    public sealed class AudioMixerController
    {
        public const string AssetResourcePath = "Audio/AM_TramChanh";
        public const string MasterParameter = "MasterVolume";
        public const string SfxParameter = "SfxVolume";
        public const string AmbientParameter = "AmbientVolume";
        public const string MasterGroupPath = "Master";
        public const string SfxGroupPath = "Master/Sfx";
        public const string AmbientGroupPath = "Master/Ambient";

        private readonly IMixerFloatSink _sink;

        public AudioMixerController(IMixerFloatSink sink) { _sink = sink; }

        public bool HasMixer => _sink != null;

        public static string ParameterFor(AudioBus bus) => bus == AudioBus.Ambient ? AmbientParameter : SfxParameter;

        public static string GroupPathFor(AudioBus bus) => bus == AudioBus.Ambient ? AmbientGroupPath : SfxGroupPath;

        public bool SetMaster(float linear) => Send(MasterParameter, linear);

        public bool SetBus(AudioBus bus, float linear) => Send(ParameterFor(bus), linear);

        /// <summary>
        /// Writes all three volumes. Returns true only when the mixer accepted every one, so the caller knows it may
        /// stop using the old AudioListener.volume path. Any miss (no mixer, parameter not exposed) returns false.
        /// </summary>
        public bool Apply(float master, float sfx, float ambient)
        {
            bool a = SetMaster(master);
            bool b = SetBus(AudioBus.Sfx, sfx);
            bool c = SetBus(AudioBus.Ambient, ambient);
            return a && b && c;
        }

        private bool Send(string parameter, float linear)
        {
            return _sink != null && _sink.SetFloat(parameter, AudioDecibel.LinearToDb(linear));
        }
    }
}
