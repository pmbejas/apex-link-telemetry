namespace Telemetria.Infraestructura.MemoriaCompartida.Estructuras;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public unsafe struct EstructuraEstaticaAC
{
    public fixed char SmVersion[15];
    public fixed char AcVersion[15];
    public int NumberOfSessions;
    public int NumCars;
    public fixed char CarModel[33];
    public fixed char Track[33];
    public fixed char PlayerName[33];
    public fixed char PlayerSurname[33];
    public fixed char PlayerNick[33];
    public int SectorCount;
    public float MaxTorque;
    public float MaxPower;
    public int MaxRpm;
    public float MaxFuel;
    public fixed float SuspensionMaxTravel[4];
    public fixed float TyreRadius[4];
    public float MaxTurboBoost;
    public float Deprecated_1;
    public float Deprecated_2;
    public int PenaltiesEnabled;
    public float AidFuelRate;
    public float AidTireRate;
    public float AidMechanicalDamage;
    public int AidAllowTyreBlankets;
    public float AidStability;
    public int AidAutoClutch;
    public int AidAutoBlip;
    public int HasDRS;
    public int HasERS;
    public int HasKERS;
    public float KersMaxJ;
    public int EngineBrakeSettingsCount;
    public int ErsPowerControllerCount;
    public float TrackSPlineLength; // Longitud real del circuito en metros
    public fixed char TrackConfiguration[33];
    public float ErsMaxJ;
    public int IsTimedRace;
    public int HasExtraLap;
    public fixed char CarSkin[33];
    public int ReversedGridPositions;
    public int PitWindowStart;
    public int PitWindowEnd;
    public int IsOnline;
}
