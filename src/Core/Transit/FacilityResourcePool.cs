namespace FacilityCommand.Core.Transit;

public sealed class FacilityResourcePool
{
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

    public bool TryReserve(int power, int cooling)
    {
        if (power <= 0 || cooling <= 0 || power > PowerCapacity - ReservedPower || cooling > CoolingCapacity - ReservedCooling)
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
}
