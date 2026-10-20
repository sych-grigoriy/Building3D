using Microsoft.Maui.Graphics;

namespace Building3D;

/// <summary>
/// Рисует вымышленный портрет сотрудника линиями. На портрете видны выданные СИЗ:
/// каска, очки, респиратор, беруши, сварочная маска, жилет, страховочная привязь.
/// </summary>
public sealed class AvatarDrawable : IDrawable
{
    private static readonly Color Green = new(0.15f, 1f, 0.35f);
    private static readonly Color Red = new(1f, 0.2f, 0.2f);

    public EmployeeProfile? Profile { get; set; }

    /// <summary>Фото загружено: рисуем только рамку и развёртку поверх него.</summary>
    public bool PhotoLoaded { get; set; }

    /// <summary>Фото загружается: показываем нарисованный портрет с надписью.</summary>
    public bool Loading { get; set; }

    public void Draw(ICanvas canvas, RectF r)
    {
        if (PhotoLoaded)
        {
            DrawPhotoOverlay(canvas, r);
            return;
        }

        canvas.FillColor = Colors.Black;
        canvas.FillRectangle(r);

        // Строки развёртки, как на мониторе
        canvas.StrokeSize = 1;
        canvas.StrokeColor = Green.WithAlpha(0.07f);
        for (float y = r.Top; y < r.Bottom; y += 3)
            canvas.DrawLine(r.Left, y, r.Right, y);

        DrawCorners(canvas, r);

        if (Profile is not { } p) return;
        var rnd = new Random(p.Seed);

        // Рисуем в условных координатах 100×120 и масштабируем под размер
        float s = MathF.Min(r.Width / 100f, r.Height / 120f);
        canvas.SaveState();
        canvas.Translate(r.Center.X - 50 * s, r.Bottom - 120 * s);
        canvas.Scale(s, s);

        canvas.StrokeColor = Green;
        canvas.StrokeSize = 1.4f;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        bool helmet = p.Has("Каска");
        bool goggles = p.Has("Защитные очки");
        bool respirator = p.Has("Респиратор");
        bool weldMask = p.Has("Сварочная маска");

        // Плечи и шея
        var shoulders = new PathF();
        shoulders.MoveTo(6, 120);
        shoulders.CurveTo(8, 100, 24, 94, 38, 92);
        shoulders.LineTo(62, 92);
        shoulders.CurveTo(76, 94, 92, 100, 94, 120);
        canvas.DrawPath(shoulders);
        canvas.DrawLine(43, 79, 42, 92);
        canvas.DrawLine(57, 79, 58, 92);

        // Одежда и снаряжение
        if (p.Has("Сигнальный жилет"))
        {
            canvas.DrawLine(38, 92, 45, 120);
            canvas.DrawLine(62, 92, 55, 120);
            canvas.StrokeColor = Green.WithAlpha(0.9f);
            canvas.StrokeSize = 2.4f;
            canvas.DrawLine(14, 108, 42, 108);
            canvas.DrawLine(58, 108, 86, 108);
            canvas.StrokeSize = 1.4f;
            canvas.StrokeColor = Green;
        }
        if (p.Has("Страховочная привязь"))
        {
            canvas.DrawLine(34, 94, 38, 120);
            canvas.DrawLine(66, 94, 62, 120);
            canvas.DrawLine(35, 103, 65, 103);
            canvas.DrawCircle(50, 103, 2.5f);
        }
        if (p.Has("Огнестойкая роба"))
        {
            canvas.DrawLine(42, 92, 50, 100);
            canvas.DrawLine(58, 92, 50, 100);
            canvas.DrawLine(50, 100, 50, 120);
        }

        // Уши (+ беруши)
        canvas.DrawEllipse(27, 49, 5, 11);
        canvas.DrawEllipse(68, 49, 5, 11);
        if (p.Has("Беруши"))
        {
            canvas.FillColor = Green;
            canvas.FillCircle(29.5f, 55, 1.6f);
            canvas.FillCircle(70.5f, 55, 1.6f);
        }

        // Голова
        canvas.DrawEllipse(30, 30, 40, 50);

        // Причёска (под каской почти не видна)
        int hairStyle = rnd.Next(3);
        var hair = new PathF();
        if (p.IsFemale)
        {
            hair.MoveTo(30, 50);
            hair.CurveTo(26, 22, 74, 22, 70, 50);
            canvas.DrawPath(hair);
            if (!helmet)
            {
                canvas.DrawLine(30, 46, 26, 88);
                canvas.DrawLine(70, 46, 74, 88);
                canvas.DrawLine(50, 27, 40, 42);
            }
        }
        else if (!helmet)
        {
            switch (hairStyle)
            {
                case 0: // короткая
                    hair.MoveTo(30, 46);
                    hair.CurveTo(29, 24, 71, 24, 70, 46);
                    canvas.DrawPath(hair);
                    canvas.DrawLine(36, 34, 44, 38);
                    canvas.DrawLine(46, 31, 54, 36);
                    break;
                case 1: // боковой пробор
                    hair.MoveTo(30, 48);
                    hair.CurveTo(28, 22, 72, 22, 70, 48);
                    canvas.DrawPath(hair);
                    canvas.DrawLine(42, 28, 64, 38);
                    break;
                default: // залысины
                    canvas.DrawArc(28, 36, 10, 20, 90, 200, false, false);
                    canvas.DrawArc(62, 36, 10, 20, 340, 90, false, false);
                    break;
            }
        }

        // Борода у некоторых мужчин (если лицо не закрыто респиратором)
        if (!p.IsFemale && !respirator && rnd.NextDouble() < 0.3)
        {
            var beard = new PathF();
            beard.MoveTo(32, 62);
            beard.CurveTo(34, 88, 66, 88, 68, 62);
            canvas.DrawPath(beard);
        }

        // Брови
        canvas.DrawLine(37, 46, 46, 45);
        canvas.DrawLine(54, 45, 63, 46);

        // Глаза или очки
        canvas.DrawEllipse(39, 50, 7, 4);
        canvas.DrawEllipse(54, 50, 7, 4);
        canvas.FillColor = Green;
        canvas.FillCircle(42.5f, 52, 1.2f);
        canvas.FillCircle(57.5f, 52, 1.2f);
        if (goggles)
        {
            canvas.DrawRoundedRectangle(33, 46, 34, 12, 4);
            canvas.DrawLine(50, 46, 50, 49);
            canvas.DrawLine(33, 52, 29, 51);
            canvas.DrawLine(67, 52, 71, 51);
        }

        // Нос
        var nose = new PathF();
        nose.MoveTo(50, 55);
        nose.LineTo(47.5f, 63);
        nose.LineTo(51, 64);
        canvas.DrawPath(nose);

        // Рот или респиратор
        if (respirator)
        {
            var mask = new PathF();
            mask.MoveTo(37, 62);
            mask.CurveTo(38, 80, 62, 80, 63, 62);
            mask.Close();
            canvas.DrawPath(mask);
            canvas.DrawCircle(42, 71, 3);
            canvas.DrawCircle(58, 71, 3);
            canvas.DrawLine(37, 63, 29, 58);
            canvas.DrawLine(63, 63, 71, 58);
        }
        else
        {
            var mouth = new PathF();
            mouth.MoveTo(44, 70);
            mouth.QuadTo(50, 73, 56, 70);
            canvas.DrawPath(mouth);
        }

        // Каска
        if (helmet)
        {
            var dome = new PathF();
            dome.MoveTo(27, 43);
            dome.CurveTo(26, 13, 74, 13, 73, 43);
            canvas.FillColor = Green.WithAlpha(0.15f);
            canvas.FillPath(dome);
            canvas.DrawPath(dome);
            canvas.DrawLine(21, 43, 79, 43);
            canvas.DrawLine(50, 17, 50, 43);
        }
        // Сварочная маска (поднята на лоб)
        else if (weldMask)
        {
            canvas.DrawRoundedRectangle(30, 16, 40, 20, 3);
            canvas.FillColor = Green.WithAlpha(0.3f);
            canvas.FillRectangle(38, 21, 24, 7);
            canvas.DrawRectangle(38, 21, 24, 7);
            canvas.DrawLine(30, 30, 28, 44);
            canvas.DrawLine(70, 30, 72, 44);
        }

        canvas.RestoreState();

        if (Loading)
        {
            canvas.FillColor = Colors.Black.WithAlpha(0.7f);
            canvas.FillRectangle(r.Left, r.Bottom - 22, r.Width, 22);
            canvas.FontSize = 9;
            canvas.FontColor = Green;
            canvas.DrawString("ЗАГРУЗКА ФОТО…", r.Center.X, r.Bottom - 8, HorizontalAlignment.Center);
        }

        DrawLabels(canvas, r, p);
    }

    private void DrawPhotoOverlay(ICanvas canvas, RectF r)
    {
        // Лёгкая развёртка поверх фото, чтобы оно вписывалось в стиль «монитора»
        canvas.StrokeSize = 1;
        for (float y = r.Top; y < r.Bottom; y += 3)
        {
            canvas.StrokeColor = Colors.Black.WithAlpha(0.22f);
            canvas.DrawLine(r.Left, y, r.Right, y);
        }
        canvas.FillColor = Green.WithAlpha(0.06f);
        canvas.FillRectangle(r);

        DrawCorners(canvas, r);
        if (Profile is { } p) DrawLabels(canvas, r, p);
    }

    private static void DrawLabels(ICanvas canvas, RectF r, EmployeeProfile p)
    {
        canvas.FillColor = Colors.Black.WithAlpha(0.6f);
        canvas.FillRectangle(r.Left + 4, r.Top + 4, 62, 13);
        canvas.FontSize = 9;
        canvas.FontColor = Green;
        canvas.DrawString(p.TabNumber, r.Left + 7, r.Top + 13, HorizontalAlignment.Left);
        canvas.FillColor = Red;
        canvas.FillCircle(r.Right - 11, r.Top + 10, 3);
    }

    private static void DrawCorners(ICanvas canvas, RectF r)
    {
        const float len = 12, m = 3;
        canvas.StrokeColor = Green;
        canvas.StrokeSize = 1.5f;
        canvas.DrawLine(r.Left + m, r.Top + m, r.Left + m + len, r.Top + m);
        canvas.DrawLine(r.Left + m, r.Top + m, r.Left + m, r.Top + m + len);
        canvas.DrawLine(r.Right - m, r.Top + m, r.Right - m - len, r.Top + m);
        canvas.DrawLine(r.Right - m, r.Top + m, r.Right - m, r.Top + m + len);
        canvas.DrawLine(r.Left + m, r.Bottom - m, r.Left + m + len, r.Bottom - m);
        canvas.DrawLine(r.Left + m, r.Bottom - m, r.Left + m, r.Bottom - m - len);
        canvas.DrawLine(r.Right - m, r.Bottom - m, r.Right - m - len, r.Bottom - m);
        canvas.DrawLine(r.Right - m, r.Bottom - m, r.Right - m, r.Bottom - m - len);
    }
}
