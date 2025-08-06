using System.IO;
using System.Text.Json;
using Pet.TaskDevourer.Models;

namespace Pet.TaskDevourer.Helpers
{
    public static class JsonStorage
    {
        private static readonly string FilePath = "tasks.json";

        public static void Save(List<TaskItem> tasks)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(tasks, options);
            File.WriteAllText(FilePath, json);
        }

        public static List<TaskItem> Load()
        {
            if (!File.Exists(FilePath))
                return new List<TaskItem>();

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }
    }
}
