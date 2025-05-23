﻿using dotenv.net;

namespace SpotifyPool.Infrastructure.EnvironmentVariable
{
    public static class EnvironmentVariableLoader
    {
        public static void LoadEnvironmentVariable()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            // Xây dựng đường dẫn đầy đủ tới file .env nằm trong thư mục "4. Application"

            string envFilePath = Path.Combine(Path.GetDirectoryName(currentDirectory), ".env");
            Console.WriteLine($"Loading environment variables from: {envFilePath}");
            // Tải file .env từ đường dẫn cụ thể bằng DotEnvOptions
            DotEnvOptions options = new(
                // Truyền đường dẫn tới file .env
                envFilePaths: [envFilePath],
                // Không cần thăm dò .env vì đang chỉ định đường dẫn
                probeForEnv: false
            );

            // Load file .env
            DotEnv.Load(options);
        }
    }
}
