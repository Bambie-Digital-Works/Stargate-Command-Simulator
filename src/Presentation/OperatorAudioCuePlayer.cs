using Godot;

namespace WormholeWorlds.Presentation;

public partial class OperatorAudioCuePlayer : Node
{
    private AudioStreamPlayer _interfacePlayer = null!;
    private AudioStreamPlayer _alarmPlayer = null!;

    public event Action<string>? CaptionRequested;

    public override void _Ready()
    {
        _interfacePlayer = new AudioStreamPlayer
        {
            Bus = "Interface",
            Stream = CreateTone(660f, 0.045f, 0.15f),
        };
        _alarmPlayer = new AudioStreamPlayer
        {
            Bus = "Alarm",
            Stream = CreateTone(330f, 0.18f, 0.28f),
        };
        AddChild(_interfacePlayer);
        AddChild(_alarmPlayer);
    }

    public void PlayInterfaceCue() => _interfacePlayer.Play();

    public void PlayAlarmCue(string caption)
    {
        _alarmPlayer.Play();
        CaptionRequested?.Invoke(caption);
    }

    private static AudioStreamWav CreateTone(float frequency, float durationSeconds, float amplitude)
    {
        const int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * durationSeconds);
        byte[] data = new byte[sampleCount * 2];
        for (int sample = 0; sample < sampleCount; sample++)
        {
            float fade = 1f - sample / (float)sampleCount;
            short value = (short)(MathF.Sin(2f * MathF.PI * frequency * sample / sampleRate)
                * short.MaxValue
                * amplitude
                * fade);
            data[sample * 2] = (byte)(value & 0xff);
            data[(sample * 2) + 1] = (byte)((value >> 8) & 0xff);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
            Data = data,
        };
    }
}
