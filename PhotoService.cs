namespace Building3D;

/// <summary>
/// Загружает фейковые фотографии сотрудников из интернета и кеширует их,
/// чтобы у одного сотрудника всегда было одно и то же лицо.
/// </summary>
public static class PhotoService
{
    public enum Source
    {
        /// <summary>Лица, сгенерированные нейросетью. Пол случайный — может не совпасть с ФИО.</summary>
        ThisPersonDoesNotExist,

        /// <summary>Стоковые портреты randomuser.me. Пол совпадает с ФИО.</summary>
        RandomUserMe
    }

    /// <summary>Откуда брать фото. Поменяйте, если нужно совпадение пола с ФИО.</summary>
    public static Source Current { get; set; } = Source.RandomUserMe;

    private static readonly HttpClient Http = CreateClient();
    private static readonly Dictionary<EmployeeProfile, Task<byte[]?>> Cache = new();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // Некоторые сайты не отдают картинки запросам без User-Agent
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Building3D/1.0");
        return client;
    }

    /// <summary>Возвращает байты фото или null, если загрузить не удалось.</summary>
    public static Task<byte[]?> GetPhotoAsync(EmployeeProfile profile)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(profile, out var task))
            {
                task = DownloadAsync(profile);
                Cache[profile] = task;
            }
            return task;
        }
    }

    private static async Task<byte[]?> DownloadAsync(EmployeeProfile profile)
    {
        try
        {
            string url = Current switch
            {
                Source.RandomUserMe =>
                    $"https://randomuser.me/api/portraits/{(profile.IsFemale ? "women" : "men")}/{(uint)profile.Seed % 100}.jpg",
                _ => "https://thispersondoesnotexist.com/" // каждый запрос — новое лицо
            };
            return await Http.GetByteArrayAsync(url);
        }
        catch
        {
            // Нет интернета или сайт недоступен — при следующем открытии карточки попробуем снова
            lock (Cache) Cache.Remove(profile);
            return null;
        }
    }
}
