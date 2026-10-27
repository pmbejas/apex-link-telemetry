namespace Telemetria.Infraestructura.MemoriaCompartida.Estructuras;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public unsafe struct EstructuraFisicaAC
{
    public int PacketId;
    public float Gas;
    public float Brake;
    public float Fuel;
    public int Gear;
    public int Rpms;
    public float SteerAngle;
    public float SpeedKmh;
    public fixed float Velocity[3];
    public fixed float AccG[3];
    public fixed float WheelSlip[4];
    public fixed float WheelLoad[4];
    public fixed float WheelsPressure[4];
    public fixed float WheelAngularSpeed[4];
    public fixed float TyreWear[4];
    public fixed float TyreDirtyLevel[4];
    public fixed float TyreCoreTemperature[4];
    public fixed float CamberRAD[4];
    public fixed float SuspensionTravel[4];
    public float Drs;
    public float Tc;
    public float Heading;
    public float Pitch;
    public float Roll;
    public float CgHeight;
    public fixed float CarDamage[5];
    public int NumberOfTyresOut;
    public int PitLimiterOn;
    public float Abs;
    public float KersCharge;
    public float KersInput;
    public int AutoShifterOn;
    public fixed float RideHeight[2];
    public float TurboBoost;
    public float Ballast;
    public float AirDensity;
    public float AirTemp;
    public float RoadTemp;
    public fixed float LocalAngularVel[3];
    public float FinalFF;
    public float PerformanceMeter;
    public int EngineBrake;
    public int ErsRecoveryLevel;
    public int ErsPowerLevel;
    public int ErsHeatCharging;
    public int ErsIsCharging;
    public float KersCurrentKJ;
    public int DrsAvailable;
    public int DrsEnabled;
    public fixed float BrakeTemp[4];
    public float Clutch;
    public fixed float TyreTempI[4];
    public fixed float TyreTempM[4];
    public fixed float TyreTempO[4];
    public int IsAIControlled;
    public fixed float TyreContactPoint[12];   // 4 ruedas x 3 coordenadas
    public fixed float TyreContactNormal[12];  // 4 ruedas x 3 coordenadas
    public fixed float TyreContactHeading[12]; // 4 ruedas x 3 coordenadas
    public float BrakeBias;
    public fixed float LocalVelocity[3];
}
