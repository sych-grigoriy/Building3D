namespace Building3D;

/// <summary>Данные для карточки сотрудника.</summary>
public sealed class EmployeeProfile
{
    public required string LastName { get; init; }
    public required string FirstName { get; init; }
    public required string MiddleName { get; init; }
    public required bool IsFemale { get; init; }
    public required string Position { get; init; }
    public required string TabNumber { get; init; }
    public required int Experience { get; init; }
    public required IReadOnlyList<string> Tools { get; init; }
    public required IReadOnlyList<string> RequiredPpe { get; init; }
    public required IReadOnlyList<string> IssuedPpe { get; init; }
    public required int Seed { get; init; } // определяет черты лица на «фото»

    public bool Has(string ppe) => IssuedPpe.Contains(ppe);
    public IEnumerable<string> MissingPpe => RequiredPpe.Except(IssuedPpe);
    public bool IsAdmitted => !MissingPpe.Any();
}

/// <summary>Генерирует правдоподобные вымышленные профили.</summary>
public static class ProfileGenerator
{
    private static readonly string[] Surnames =
    {
        "Иванов", "Смирнов", "Кузнецов", "Попов", "Соколов", "Лебедев", "Козлов", "Новиков",
        "Морозов", "Волков", "Соловьёв", "Васильев", "Зайцев", "Павлов", "Семёнов", "Голубев",
        "Виноградов", "Богданов", "Воробьёв", "Фёдоров", "Михайлов", "Беляев", "Тарасов", "Белов",
        "Комаров", "Орлов", "Киселёв", "Макаров", "Андреев", "Ковалёв", "Ильин", "Гусев",
        "Титов", "Кудрявцев", "Баранов", "Куликов", "Алексеев", "Степанов", "Яковлев", "Сорокин"
    };

    private static readonly string[] MaleNames =
    {
        "Александр", "Сергей", "Дмитрий", "Андрей", "Алексей", "Максим", "Евгений", "Иван",
        "Михаил", "Николай", "Владимир", "Павел", "Артём", "Олег", "Виктор", "Роман", "Игорь"
    };

    private static readonly string[] FemaleNames =
    {
        "Анна", "Елена", "Ольга", "Наталья", "Татьяна", "Ирина", "Мария", "Светлана",
        "Екатерина", "Юлия", "Марина", "Дарья", "Анастасия", "Ксения"
    };

    private static readonly (string Male, string Female)[] Patronymics =
    {
        ("Александрович", "Александровна"), ("Сергеевич", "Сергеевна"), ("Иванович", "Ивановна"),
        ("Дмитриевич", "Дмитриевна"), ("Андреевич", "Андреевна"), ("Николаевич", "Николаевна"),
        ("Владимирович", "Владимировна"), ("Петрович", "Петровна"), ("Михайлович", "Михайловна"),
        ("Олегович", "Олеговна"), ("Викторович", "Викторовна"), ("Юрьевич", "Юрьевна")
    };

    private static readonly (string Title, string[] Tools, string[] Ppe)[] Roles =
    {
        ("Инженер-электрик",
            new[] { "Мультиметр Fluke 117", "Диэлектрические отвёртки (набор)", "Указатель напряжения УНН-1", "Токоизмерительные клещи" },
            new[] { "Каска", "Диэлектрические перчатки", "Защитные очки", "Спецобувь" }),
        ("Слесарь-ремонтник",
            new[] { "Набор ключей 8–32 мм", "Динамометрический ключ", "Слесарный молоток", "Штангенциркуль ШЦ-I" },
            new[] { "Каска", "Перчатки", "Защитные очки", "Спецобувь" }),
        ("Электрогазосварщик",
            new[] { "Сварочный инвертор", "Угловая шлифмашина", "Электрододержатель", "Молоток-шлакоотбойник" },
            new[] { "Сварочная маска", "Краги", "Огнестойкая роба", "Респиратор", "Спецобувь" }),
        ("Монтажник-высотник",
            new[] { "Перфоратор", "Лазерный уровень", "Набор крепежа", "Шуруповёрт" },
            new[] { "Каска", "Страховочная привязь", "Перчатки", "Сигнальный жилет", "Спецобувь" }),
        ("Оператор станка с ЧПУ",
            new[] { "Микрометр МК-25", "Набор фрез", "Щуп измерительный", "Индикатор часового типа" },
            new[] { "Защитные очки", "Беруши", "Спецобувь" }),
        ("Инженер по охране труда",
            new[] { "Планшет с чек-листами", "Газоанализатор", "Шумомер", "Рация" },
            new[] { "Каска", "Сигнальный жилет", "Спецобувь" }),
        ("Кладовщик",
            new[] { "Сканер штрихкодов", "Рулетка 5 м", "Гидравлическая тележка" },
            new[] { "Перчатки", "Сигнальный жилет", "Спецобувь" }),
        ("Мастер участка",
            new[] { "Планшет", "Рация", "Рулетка 10 м", "Фонарь налобный" },
            new[] { "Каска", "Сигнальный жилет", "Защитные очки", "Спецобувь" }),
        ("Маляр-пескоструйщик",
            new[] { "Краскопульт", "Пескоструйный аппарат", "Толщиномер покрытия" },
            new[] { "Респиратор", "Защитные очки", "Перчатки", "Беруши", "Спецобувь" }),
    };

    public static EmployeeProfile Create()
    {
        var rnd = Random.Shared;
        var role = Roles[rnd.Next(Roles.Length)];
        bool female = rnd.NextDouble() < 0.3;
        var patronymic = Patronymics[rnd.Next(Patronymics.Length)];
        string surname = Surnames[rnd.Next(Surnames.Length)];

        var tools = role.Tools
            .OrderBy(_ => rnd.Next())
            .Take(rnd.Next(2, role.Tools.Length + 1))
            .ToList();

        // Примерно 15 % СИЗ «не выдано» — чтобы на карточках были нарушения
        var issued = role.Ppe.Where(_ => rnd.NextDouble() < 0.85).ToList();

        return new EmployeeProfile
        {
            LastName = female ? surname + "а" : surname,
            FirstName = female ? FemaleNames[rnd.Next(FemaleNames.Length)] : MaleNames[rnd.Next(MaleNames.Length)],
            MiddleName = female ? patronymic.Female : patronymic.Male,
            IsFemale = female,
            Position = role.Title,
            TabNumber = $"ТН-{rnd.Next(10000, 99999)}",
            Experience = rnd.Next(1, 26),
            Tools = tools,
            RequiredPpe = role.Ppe,
            IssuedPpe = issued,
            Seed = rnd.Next()
        };
    }
}
