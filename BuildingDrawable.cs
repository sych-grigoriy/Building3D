using System.Numerics;
using Microsoft.Maui.Graphics;

namespace Building3D;

/// <summary>Рисует каркас здания в перспективной проекции.</summary>
public sealed class BuildingDrawable : IDrawable
{
    private const float CameraDistance = 60f;
    private const float SceneRadius = 22f;

    private static readonly Color Green = new(0.15f, 1f, 0.35f);

    private readonly BuildingModel _model;

    public float Yaw { get; set; } = 35f;    // поворот вокруг вертикали, градусы
    public float Pitch { get; set; } = 25f;  // наклон камеры, градусы (-89..89)
    public float Zoom { get; set; } = 1f;

    /// <summary>Сотрудник, чья карточка сейчас открыта.</summary>
    public Employee? Selected { get; set; }

    // Экранные позиции точек с последней отрисовки — для определения, куда нажали
    private readonly List<(Employee Employee, float X, float Y, float R)> _hitTargets = new();

    public BuildingDrawable(BuildingModel model) => _model = model;

    /// <summary>Ближайший к точке нажатия сотрудник или null.</summary>
    public Employee? HitTest(float x, float y)
    {
        Employee? best = null;
        float bestDist = float.MaxValue;
        foreach (var t in _hitTargets)
        {
            float d = MathF.Sqrt((t.X - x) * (t.X - x) + (t.Y - y) * (t.Y - y));
            float limit = MathF.Max(t.R * 2.2f, 14f);
            if (d <= limit && d < bestDist) { best = t.Employee; bestDist = d; }
        }
        return best;
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(rect);

        float yaw = Yaw * MathF.PI / 180f, pitch = Pitch * MathF.PI / 180f;
        float cosY = MathF.Cos(yaw), sinY = MathF.Sin(yaw);
        float cosP = MathF.Cos(pitch), sinP = MathF.Sin(pitch);
        float focal = MathF.Min(rect.Width, rect.Height) * 1.1f * Zoom;
        float ox = rect.Center.X, oy = rect.Center.Y;
        var center = _model.Center;

        // Возвращает экранные X, Y и глубину (Z)
        Vector3 Project(Vector3 p)
        {
            var v = p - center;
            float x1 = v.X * cosY - v.Z * sinY;
            float z1 = v.X * sinY + v.Z * cosY;
            float y2 = v.Y * cosP + z1 * sinP;
            float z2 = -v.Y * sinP + z1 * cosP;
            float depth = z2 + CameraDistance;
            float s = focal / depth;
            return new Vector3(ox + x1 * s, oy - y2 * s, depth);
        }

        // ---- Линии ----
        canvas.StrokeLineCap = LineCap.Round;
        foreach (var e in _model.Edges)
        {
            var a = Project(e.A);
            var b = Project(e.B);

            // Дальние линии тусклее — так легче воспринимать объём
            float t = Math.Clamp(((a.Z + b.Z) / 2 - (CameraDistance - SceneRadius)) / (2 * SceneRadius), 0f, 1f);
            float fade = 1f - 0.65f * t;

            (float alpha, float width) = e.Kind switch
            {
                EdgeKind.Main => (1f, 1.8f),
                EdgeKind.Detail => (0.55f, 1f),
                _ => (0.13f, 1f)
            };

            canvas.StrokeColor = Green.WithAlpha(alpha * fade);
            canvas.StrokeSize = width;
            canvas.DrawLine(a.X, a.Y, b.X, b.Y);
        }

        // ---- Номера этажей ----
        canvas.FontSize = 12;
        canvas.FontColor = Green.WithAlpha(0.8f);
        for (int f = 0; f < BuildingModel.Floors; f++)
        {
            var p = Project(new Vector3(-1.5f, f * BuildingModel.FloorH + BuildingModel.FloorH / 2, 0));
            canvas.DrawString($"{f + 1}", p.X, p.Y, HorizontalAlignment.Center);
        }

        // ---- Сотрудники (от дальних к ближним) ----
        var people = _model.Employees
            .Select(e => (Employee: e, P: Project(e.Position)))
            .OrderByDescending(t => t.P.Z);

        _hitTargets.Clear();
        foreach (var (employee, p) in people)
        {
            float r = Math.Clamp(focal * 0.22f / p.Z, 2f, 9f);
            canvas.FillColor = new Color(1f, 0.1f, 0.1f, 0.25f);
            canvas.FillCircle(p.X, p.Y, r * 2.2f);   // свечение
            canvas.FillColor = new Color(1f, 0.15f, 0.15f);
            canvas.FillCircle(p.X, p.Y, r);

            _hitTargets.Add((employee, p.X, p.Y, r));

            if (employee == Selected)
            {
                // Прицел вокруг выбранного сотрудника и его фамилия
                float ring = r * 3f + 4f;
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = 1.5f;
                canvas.DrawCircle(p.X, p.Y, ring);
                canvas.DrawLine(p.X - ring - 6, p.Y, p.X - ring + 3, p.Y);
                canvas.DrawLine(p.X + ring - 3, p.Y, p.X + ring + 6, p.Y);
                canvas.DrawLine(p.X, p.Y - ring - 6, p.X, p.Y - ring + 3);
                canvas.DrawLine(p.X, p.Y + ring - 3, p.X, p.Y + ring + 6);

                canvas.FontSize = 12;
                canvas.FontColor = Colors.White;
                canvas.DrawString(employee.Profile.LastName, p.X + ring + 8, p.Y - ring, HorizontalAlignment.Left);
            }
        }

        // ---- Информация ----
        float yawNorm = ((Yaw % 360f) + 360f) % 360f;
        canvas.FontSize = 14;
        canvas.FontColor = Green;
        canvas.DrawString($"Этажей: {BuildingModel.Floors}   Сотрудников: {_model.Employees.Count}",
                          rect.Left + 12, rect.Top + 22, HorizontalAlignment.Left);
        canvas.DrawString($"Поворот: {yawNorm:0}°   Наклон: {Pitch:0}°   Масштаб: {Zoom:0.0}×",
                          rect.Left + 12, rect.Top + 42, HorizontalAlignment.Left);
        canvas.FontSize = 12;
        canvas.FontColor = Green.WithAlpha(0.6f);
        canvas.DrawString("Нажмите на красную точку, чтобы открыть карточку сотрудника",
                          rect.Left + 12, rect.Top + 62, HorizontalAlignment.Left);
    }
}
