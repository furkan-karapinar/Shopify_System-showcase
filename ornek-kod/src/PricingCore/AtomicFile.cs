using System.Text;

namespace PricingCore
{
    /// <summary>
    /// Ayar dosyaları için atomik yazma ve engelsiz okuma. Yazma önce aynı klasördeki geçici dosyaya yapılır, sonra hedefin üstüne
    /// taşınır: güç kesilse ya da biri aynı anda okusa dosya hiçbir zaman yarım/boş görünmez (<c>File.WriteAllText</c> hedefi önce kırpar).
    /// </summary>
    public static class AtomicFile
    {
        // Okuyucular dosyayı çok kısa süre açık tutar; sık ve çok deneme, birkaç uzun beklemeden daha çabuk boşluk yakalar.
        // Sabit 25 ms bekleme Windows zamanlayıcı çözünürlüğü yüzünden gerçekte ~0,5-0,6 sn'ye kadar uzayabilir.
        private const int Attempts = 20;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(25);
        private const int ReadAttempts = 5;
        private static readonly TimeSpan ReadRetryDelay = TimeSpan.FromMilliseconds(20);
        private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

        /// <exception cref="IOException">Hedef yeniden denemelerden sonra da değiştirilemedi; hedefin eski içeriği korunur.</exception>
        /// <exception cref="UnauthorizedAccessException">Windows bazı kilitli hedeflerde IOException yerine bunu verir.</exception>
        public static void WriteAllText(string path, string content)
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
            Directory.CreateDirectory(directory);
            var temp = Path.Combine(directory, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, Utf8NoBom))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                // File.Move(overwrite) Windows'ta MoveFileEx(REPLACE_EXISTING) kullanır: okuyucu için hedef hiçbir an yok olmaz
                // (File.Replace kısa bir "dosya yok" penceresi bırakır). Hedef başka bir yerde açıkken Windows IOException
                // (paylaşım ihlali) ya da UnauthorizedAccessException (erişim reddedildi) verir; okuma çok kısa sürdüğü için
                // artan beklemeyle yeniden denenir.
                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        File.Move(temp, path, overwrite: true);
                        return;
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        if (attempt >= Attempts)
                            throw;
                        Thread.Sleep(RetryDelay);
                    }
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(temp))
                        File.Delete(temp);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Geçici dosya silinemedi; asıl hatayı gizlemesin. Bir sonraki yazma kendi adıyla yenisini üretir.
                }
            }
        }

        /// <summary>
        /// Yazan sürecin taşıma/silmesini engellemeyen okuma (<c>FileShare.ReadWrite | Delete</c>). Antivirüs/yedekleme aracı dosyayı
        /// kısa süre kilitlerse (paylaşım ihlali) kısa süre yeniden dener; dosya yoksa hemen <see cref="FileNotFoundException"/> fırlatır.
        /// </summary>
        public static string ReadAllText(string path)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                    return reader.ReadToEnd();
                }
                catch (Exception ex) when (attempt < ReadAttempts && IsTransientRead(ex))
                {
                    Thread.Sleep(ReadRetryDelay);
                }
            }
        }

        private static bool IsTransientRead(Exception ex) =>
            ex is UnauthorizedAccessException ||
            (ex is IOException && ex is not FileNotFoundException && ex is not DirectoryNotFoundException);
    }
}
