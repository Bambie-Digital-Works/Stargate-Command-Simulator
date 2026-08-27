namespace WormholeWorlds.Core.Transit;

public sealed class FacilityResourcePool
{
    private int _coolingFaultHold;

    public FacilityResourcePool(int powerCapacity, int coolingCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(powerCapacity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(coolingCapacity);
        PowerCapacity = powerCapacity;
        CoolingCapacity = coolingCapacity;
    }

    public int PowerCapacity { get; }

    public int CoolingCapacity { get; }

    public int ReservedPower { get; private set; }

    public int ReservedCooling { get; private set; }

    public int CoolingFaultHold => _coolingFaultHold;

    public int FreePower => PowerCapacity - ReservedPower;

    public int FreeCooling => CoolingCapacity - ReservedCooling - _coolingFaultHold;

    public bool TryReserve(int power, int cooling)
    {
        if (power <= 0 || cooling <= 0 || power > FreePower || cooling > FreeCooling)
        {
            return false;
        }

        ReservedPower += power;
        ReservedCooling += cooling;
        return true;
    }

    public void Release(int power, int cooling)
    {
        if (power < 0 || cooling < 0 || power > ReservedPower || cooling > ReservedCooling)
        {
            throw new InvalidOperationException("Cannot release resources that are not reserved.");
        }

        ReservedPower -= power;
        ReservedCooling -= cooling;
    }

    public bool TryApplyCoolingFault(int coolingUnits)
    {
        if (coolingUnits <= 0 || coolingUnits > FreeCooling)
        {
            return false;
        }

        _coolingFaultHold += coolingUnits;
        return true;
    }

    public void ClearCoolingFault()
    {
        _coolingFaultHold = 0;
    }
}
