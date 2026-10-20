namespace Building3D;

public partial class MainPage : ContentPage
{
    private const float DragSensitivity = 0.4f; // градусов на пиксель

    private readonly BuildingModel _model = new();
    private readonly BuildingDrawable _drawable;
    private readonly AvatarDrawable _avatar = new();
    private int _photoRequest;

    private static readonly Color CardGreen = Color.FromArgb("#26FF59");
    private static readonly Color CardRed = Color.FromArgb("#FF3B3B");
    private readonly IDispatcherTimer _timer;
    private DateTime _lastTick = DateTime.UtcNow;

    // Состояние жестов
    private float _panStartYaw, _panStartPitch;
    private bool _pointerDown;
    private Point _lastPointer;

    public MainPage()
    {
        InitializeComponent();

        _drawable = new BuildingDrawable(_model);
        Scene.Drawable = _drawable;
        AvatarView.Drawable = _avatar;

        // Нажатие (мышью или пальцем) на красную точку открывает карточку
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnSceneTapped;
        Scene.GestureRecognizers.Add(tap);

        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
        {
            // Windows / macOS: вращение перетаскиванием мышью
            var pointer = new PointerGestureRecognizer();
            pointer.PointerPressed += (_, e) =>
            {
                _pointerDown = true;
                _lastPointer = e.GetPosition(Scene) ?? default;
            };
            pointer.PointerMoved += OnPointerMoved;
            pointer.PointerReleased += (_, _) => _pointerDown = false;
            pointer.PointerExited += (_, _) => _pointerDown = false;
            Scene.GestureRecognizers.Add(pointer);
        }
        else
        {
            // Телефон / планшет: вращение пальцем
            var pan = new PanGestureRecognizer();
            pan.PanUpdated += OnPanUpdated;
            Scene.GestureRecognizers.Add(pan);
        }

        var pinch = new PinchGestureRecognizer();
        pinch.PinchUpdated += OnPinchUpdated;
        Scene.GestureRecognizers.Add(pinch);

#if WINDOWS
        // MAUI не умеет обрабатывать колесо мыши, поэтому подписываемся на событие WinUI напрямую
        Scene.HandlerChanged += (_, _) =>
        {
            if (Scene.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement element)
                element.PointerWheelChanged += OnMouseWheel;
        };
#endif

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 FPS
        _timer.Tick += OnTick;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _lastTick = DateTime.UtcNow;
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        _timer.Stop();
        base.OnDisappearing();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        float dt = MathF.Min((float)(now - _lastTick).TotalSeconds, 0.1f);
        _lastTick = now;

        _model.Update(dt);
        if (AutoRotate.IsToggled)
            _drawable.Yaw += 12f * dt;

        Scene.Invalidate();
    }

    // ---------- Вращение ----------

    private void Rotate(float dx, float dy)
    {
        _drawable.Yaw += dx * DragSensitivity;
        _drawable.Pitch = Math.Clamp(_drawable.Pitch + dy * DragSensitivity, -89f, 89f);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_pointerDown || e.GetPosition(Scene) is not Point p) return;
        Rotate((float)(p.X - _lastPointer.X), (float)(p.Y - _lastPointer.Y));
        _lastPointer = p;
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panStartYaw = _drawable.Yaw;
                _panStartPitch = _drawable.Pitch;
                break;
            case GestureStatus.Running:
                _drawable.Yaw = _panStartYaw + (float)e.TotalX * DragSensitivity;
                _drawable.Pitch = Math.Clamp(_panStartPitch + (float)e.TotalY * DragSensitivity, -89f, 89f);
                break;
        }
    }

    // ---------- Масштаб ----------

    private void OnPinchUpdated(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status == GestureStatus.Running)
            ZoomSlider.Value = Math.Clamp(ZoomSlider.Value * e.Scale, ZoomSlider.Minimum, ZoomSlider.Maximum);
    }

#if WINDOWS
    private void OnMouseWheel(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // Один «щелчок» колеса = 120 единиц; каждый щелчок меняет масштаб на 10 %
        int delta = e.GetCurrentPoint((Microsoft.UI.Xaml.UIElement)sender).Properties.MouseWheelDelta;
        double factor = Math.Pow(1.1, delta / 120.0);

        ZoomSlider.Value = Math.Clamp(ZoomSlider.Value * factor, ZoomSlider.Minimum, ZoomSlider.Maximum);
        e.Handled = true;
    }
#endif

    private void OnZoomChanged(object? sender, ValueChangedEventArgs e) => _drawable.Zoom = (float)e.NewValue;

    // ---------- Готовые ракурсы ----------

    private void SetView(float yaw, float pitch)
    {
        AutoRotate.IsToggled = false;
        _drawable.Yaw = yaw;
        _drawable.Pitch = pitch;
    }

    private void OnFrontClicked(object? sender, EventArgs e) => SetView(0, 5);
    private void OnSideClicked(object? sender, EventArgs e) => SetView(90, 5);
    private void OnBackClicked(object? sender, EventArgs e) => SetView(180, 5);
    private void OnTopClicked(object? sender, EventArgs e) => SetView(0, 89);
    private void OnIsoClicked(object? sender, EventArgs e) => SetView(35, 25);

    private void OnShuffleClicked(object? sender, EventArgs e)
    {
        HideCard();
        _model.PopulateEmployees();
    }

    // ---------- Карточка сотрудника ----------

    private void OnSceneTapped(object? sender, TappedEventArgs e)
    {
        if (e.GetPosition(Scene) is not Point p) return;

        var employee = _drawable.HitTest((float)p.X, (float)p.Y);
        if (employee is null) HideCard();   // нажатие на пустое место закрывает карточку
        else ShowCard(employee);
    }

    private void OnCloseCardClicked(object? sender, EventArgs e) => HideCard();

    private void ShowCard(Employee employee)
    {
        var p = employee.Profile;
        _drawable.Selected = employee;

        _avatar.Profile = p;
        _avatar.PhotoLoaded = false;
        _avatar.Loading = true;
        PhotoImage.IsVisible = false;
        AvatarView.Invalidate();
        _ = LoadPhotoAsync(p, ++_photoRequest);

        LastNameLabel.Text = p.LastName.ToUpperInvariant();
        FirstNameLabel.Text = $"{p.FirstName} {p.MiddleName}";
        PositionLabel.Text = p.Position;
        TabLabel.Text = $"{p.TabNumber} · стаж {p.Experience} {YearsWord(p.Experience)}";
        LocationLabel.Text = $"Этаж {employee.Floor}, помещение {employee.RoomNumber}";

        ToolsList.Children.Clear();
        foreach (var tool in p.Tools)
            ToolsList.Children.Add(new Label { Text = $"▸ {tool}", FontSize = 14 });

        PpeList.Children.Clear();
        foreach (var ppe in p.RequiredPpe)
        {
            bool issued = p.Has(ppe);
            PpeList.Children.Add(new Label
            {
                Text = issued ? $"✓ {ppe}" : $"✗ {ppe} — отсутствует",
                TextColor = issued ? CardGreen : CardRed,
                FontSize = 14
            });
        }
        PpeCountLabel.Text = $"{p.IssuedPpe.Count}/{p.RequiredPpe.Count}";
        PpeCountLabel.TextColor = p.IsAdmitted ? CardGreen : CardRed;

        if (p.IsAdmitted)
        {
            StatusLabel.Text = "● ДОПУЩЕН К РАБОТЕ";
            StatusLabel.TextColor = CardGreen;
            StatusBox.Stroke = CardGreen;
        }
        else
        {
            StatusLabel.Text = "● НЕ ДОПУЩЕН: НЕПОЛНЫЙ КОМПЛЕКТ СИЗ";
            StatusLabel.TextColor = CardRed;
            StatusBox.Stroke = CardRed;
        }

        if (!Card.IsVisible)
        {
            Card.Opacity = 0;
            Card.IsVisible = true;
            _ = Card.FadeTo(1, 180);
        }
    }

    private async Task LoadPhotoAsync(EmployeeProfile profile, int request)
    {
        var bytes = await PhotoService.GetPhotoAsync(profile);

        // Пока фото грузилось, могли открыть карточку другого сотрудника
        if (request != _photoRequest) return;

        _avatar.Loading = false;
        if (bytes is not null)
        {
            PhotoImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            PhotoImage.IsVisible = true;
            _avatar.PhotoLoaded = true;
        }
        // Если загрузить не удалось, остаётся нарисованный портрет
        AvatarView.Invalidate();
    }

    private void HideCard()
    {
        _photoRequest++;
        _drawable.Selected = null;
        Card.IsVisible = false;
    }

    private static string YearsWord(int n)
    {
        int mod100 = n % 100, mod10 = n % 10;
        if (mod100 is >= 11 and <= 14) return "лет";
        return mod10 switch { 1 => "год", 2 or 3 or 4 => "года", _ => "лет" };
    }
}
