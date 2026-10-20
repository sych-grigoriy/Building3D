using System.Numerics;

namespace Building3D;

public enum EdgeKind { Main, Detail, Ground }

public readonly record struct Edge(Vector3 A, Vector3 B, EdgeKind Kind);

/// <summary>Сотрудник: красная точка, которая неторопливо перемещается внутри своей комнаты.</summary>
public sealed class Employee
{
    private readonly Vector3 _min, _max;
    private Vector3 _target;
    private float _pause;

    public Vector3 Position { get; private set; }
    public EmployeeProfile Profile { get; }
    public int Floor { get; }
    public string RoomNumber { get; }

    public Employee(Vector3 roomMin, Vector3 roomMax, int floor, string roomNumber, EmployeeProfile profile)
    {
        _min = roomMin;
        _max = roomMax;
        Floor = floor;
        RoomNumber = roomNumber;
        Profile = profile;
        Position = RandomPoint();
        _target = RandomPoint();
        _pause = Random.Shared.NextSingle() * 3f;
    }

    private Vector3 RandomPoint() => new(
        _min.X + Random.Shared.NextSingle() * (_max.X - _min.X),
        _min.Y,
        _min.Z + Random.Shared.NextSingle() * (_max.Z - _min.Z));

    public void Update(float dt)
    {
        if (_pause > 0) { _pause -= dt; return; }

        var delta = _target - Position;
        float len = delta.Length();
        float step = 1.2f * dt; // скорость ходьбы, м/с

        if (len <= step)
        {
            Position = _target;
            _target = RandomPoint();
            _pause = 1f + Random.Shared.NextSingle() * 4f; // «работает» за столом
        }
        else
        {
            Position += delta / len * step;
        }
    }
}

/// <summary>5-этажное офисное здание: 4×2 комнаты на этаже.</summary>
public sealed class BuildingModel
{
    public const int Floors = 5;
    public const int RoomsX = 4;
    public const int RoomsZ = 2;
    public const float RoomW = 8f;      // ширина комнаты, м
    public const float RoomD = 7f;      // глубина комнаты, м
    public const float FloorH = 3.5f;   // высота этажа, м

    public const float Width = RoomsX * RoomW;
    public const float Depth = RoomsZ * RoomD;
    public const float Height = Floors * FloorH;

    public Vector3 Center => new(Width / 2, Height / 2, Depth / 2);

    public List<Edge> Edges { get; } = new();
    public List<Employee> Employees { get; } = new();

    public BuildingModel()
    {
        BuildGround();
        BuildStructure();
        PopulateEmployees();
    }

    public void Update(float dt)
    {
        foreach (var e in Employees) e.Update(dt);
    }

    /// <summary>Заселяет случайные комнаты сотрудниками.</summary>
    public void PopulateEmployees(int occupiedRooms = 7)
    {
        Employees.Clear();

        var rooms =
            (from f in Enumerable.Range(0, Floors)
             from i in Enumerable.Range(0, RoomsX)
             from j in Enumerable.Range(0, RoomsZ)
             select (f, i, j))
            .OrderBy(_ => Random.Shared.Next())
            .Take(occupiedRooms);

        foreach (var (f, i, j) in rooms)
        {
            const float margin = 0.8f;
            float y = f * FloorH + 1.2f; // высота «корпуса» человека над полом
            var min = new Vector3(i * RoomW + margin, y, j * RoomD + margin);
            var max = new Vector3((i + 1) * RoomW - margin, y, (j + 1) * RoomD - margin);

            string roomNumber = $"{f + 1}{i * RoomsZ + j + 1:00}"; // например, 305

            int count = Random.Shared.Next(1, 5);
            for (int k = 0; k < count; k++)
                Employees.Add(new Employee(min, max, f + 1, roomNumber, ProfileGenerator.Create()));
        }
    }

    // ---------- Геометрия ----------

    private void Line(Vector3 a, Vector3 b, EdgeKind kind) => Edges.Add(new Edge(a, b, kind));

    private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, EdgeKind kind)
    {
        Line(a, b, kind); Line(b, c, kind); Line(c, d, kind); Line(d, a, kind);
    }

    private void Box(Vector3 min, Vector3 max, EdgeKind kind)
    {
        var p = new Vector3[8];
        for (int n = 0; n < 8; n++)
            p[n] = new Vector3((n & 1) == 0 ? min.X : max.X,
                               (n & 2) == 0 ? min.Y : max.Y,
                               (n & 4) == 0 ? min.Z : max.Z);

        int[,] idx = { {0,1},{2,3},{4,5},{6,7}, {0,2},{1,3},{4,6},{5,7}, {0,4},{1,5},{2,6},{3,7} };
        for (int n = 0; n < 12; n++) Line(p[idx[n, 0]], p[idx[n, 1]], kind);
    }

    private void BuildGround()
    {
        const float pad = 10f, step = 5f;
        for (float x = -pad; x <= Width + pad + 0.01f; x += step)
            Line(new(x, 0, -pad), new(x, 0, Depth + pad), EdgeKind.Ground);
        for (float z = -pad; z <= Depth + pad + 0.01f; z += step)
            Line(new(-pad, 0, z), new(Width + pad, 0, z), EdgeKind.Ground);
    }

    private void BuildStructure()
    {
        // Перекрытия этажей с линиями внутренних стен
        for (int f = 0; f <= Floors; f++)
        {
            float y = f * FloorH;
            for (int i = 0; i <= RoomsX; i++)
            {
                bool outer = i == 0 || i == RoomsX;
                Line(new(i * RoomW, y, 0), new(i * RoomW, y, Depth), outer ? EdgeKind.Main : EdgeKind.Detail);
            }
            for (int j = 0; j <= RoomsZ; j++)
            {
                bool outer = j == 0 || j == RoomsZ;
                Line(new(0, y, j * RoomD), new(Width, y, j * RoomD), outer ? EdgeKind.Main : EdgeKind.Detail);
            }
        }

        // Вертикальные стойки (углы — основные линии, стыки стен — детали)
        for (int i = 0; i <= RoomsX; i++)
        for (int j = 0; j <= RoomsZ; j++)
        {
            bool corner = (i == 0 || i == RoomsX) && (j == 0 || j == RoomsZ);
            Line(new(i * RoomW, 0, j * RoomD), new(i * RoomW, Height, j * RoomD),
                 corner ? EdgeKind.Main : EdgeKind.Detail);
        }

        // Окна на фасадах
        const float winW = 3f, winH = 1.6f, sill = 1.0f, sideWinW = 2.5f;
        for (int f = 0; f < Floors; f++)
        {
            float y0 = f * FloorH + sill, y1 = y0 + winH;

            for (int i = 0; i < RoomsX; i++)
            {
                float x0 = i * RoomW + (RoomW - winW) / 2, x1 = x0 + winW;

                bool doorHere = f == 0 && i == 1;
                if (!doorHere)
                    Quad(new(x0, y0, 0), new(x1, y0, 0), new(x1, y1, 0), new(x0, y1, 0), EdgeKind.Detail);

                Quad(new(x0, y0, Depth), new(x1, y0, Depth), new(x1, y1, Depth), new(x0, y1, Depth), EdgeKind.Detail);
            }

            for (int j = 0; j < RoomsZ; j++)
            {
                float z0 = j * RoomD + (RoomD - sideWinW) / 2, z1 = z0 + sideWinW;
                Quad(new(0, y0, z0), new(0, y0, z1), new(0, y1, z1), new(0, y1, z0), EdgeKind.Detail);
                Quad(new(Width, y0, z0), new(Width, y0, z1), new(Width, y1, z1), new(Width, y1, z0), EdgeKind.Detail);
            }
        }

        // Входная дверь (двустворчатая) на первом этаже
        float dx0 = RoomW + RoomW / 2 - 1.2f, dx1 = dx0 + 2.4f, dh = 2.6f;
        Quad(new(dx0, 0, 0), new(dx1, 0, 0), new(dx1, dh, 0), new(dx0, dh, 0), EdgeKind.Main);
        Line(new((dx0 + dx1) / 2, 0, 0), new((dx0 + dx1) / 2, dh, 0), EdgeKind.Detail);

        // Выход на крышу и антенна
        Box(new(13f, Height, 4f), new(19f, Height + 3f, 10f), EdgeKind.Main);
        Line(new(16f, Height + 3f, 7f), new(16f, Height + 8f, 7f), EdgeKind.Detail);
    }
}
