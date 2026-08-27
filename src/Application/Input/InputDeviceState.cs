namespace WormholeWorlds.Application.Input;

public sealed class InputDeviceState
{
    public InputDeviceKind ActiveDevice { get; private set; } = InputDeviceKind.KeyboardMouse;

    public bool SetActive(InputDeviceKind device)
    {
        if (device == ActiveDevice)
        {
            return false;
        }

        ActiveDevice = device;
        return true;
    }

    public bool ControllerDisconnected() => SetActive(InputDeviceKind.KeyboardMouse);
}
