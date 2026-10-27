namespace Telemetria.Infraestructura.MemoriaCompartida.Estructuras;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public unsafe struct EstructuraGraficaAC
{
    public int PacketId;
    public int Status; // 0=Off, 1=Replay, 2=Live, 3=Pause
    public int Session;
    public fixed char CurrentTime[15];
    public fixed char LastTime[15];
    public fixed char BestTime[15];
    public fixed char Split[15];
    public int CompletedLaps;
    public int Position;
    public int ICurrentTime;
    public int ILastTime;
    public int IBestTime;
    public float SessionTimeLeft;
    public float DistanceTraveled;
    public int IsInPit;
    public int CurrentSectorIndex;
    public int LastSectorTime;
    public int NumberOfLaps;
    public fixed char TyreCompound[33];
    public float ReplayTimeMultiplier;
    public float NormalizedCarPosition;
    public int ActiveCars;
    public fixed float CarCoordinates[60 * 3]; // Posición mundo de hasta 60 autos en pista (X, Y, Z)
    public fixed int CarID[60];
    public int PlayerCarID;
    public float PenaltyTime;
    public int Flag; // 0=None, 1=Blue, 2=Yellow, 3=Black, 4=White, 5=Checkered, 6=Penalty
    public int Penalty;
    public int IdealLineOn;
    public int IsInPitLane;
    public float SurfaceGrip;
    public int MandatoryPitDone;
    public float WindSpeed;
    public float WindDirection;
}
