using Pet.TaskDevourer.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Pet.TaskDevourer.Services
{
    public class ApiTaskService : ITaskService
    {
        private readonly HttpClient _http;
        public ApiTaskService(string baseAddress)
        {
            _http = new HttpClient { BaseAddress = new System.Uri(baseAddress) };
            _http.Timeout = System.TimeSpan.FromSeconds(2);
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<TaskItem>> LoadAllAsync()
        {
            var result = await _http.GetFromJsonAsync<List<TaskItem>>("api/tasks").ConfigureAwait(false);
            return result ?? new List<TaskItem>();
        }

        public async Task SaveAllAsync(List<TaskItem> tasks)
        {
            var resp = await _http.PutAsJsonAsync("api/tasks/sync  ", tasks).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();
        }

        public async Task<TaskItem?> CreateTaskAsync(TaskItem task)
        {
            var resp = await _http.PostAsJsonAsync("api/tasks", task).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            var created = await resp.Content.ReadFromJsonAsync<TaskItem>().ConfigureAwait(false);
            return created;
        }

        public async Task<bool> UpdateTaskAsync(TaskItem task)
        {
            if (task.Id <= 0) return false;
            var resp = await _http.PutAsJsonAsync($"api/tasks/{task.Id}", task).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            var resp = await _http.DeleteAsync($"api/tasks/{id}").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public async Task<int?> AddSubTaskAsync(int taskId, string title, bool isCompleted = false)
        {
            var resp = await _http.PostAsJsonAsync($"api/tasks/{taskId}/subtasks", new { Id = 0, Title = title, IsCompleted = isCompleted }).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            var dto = await resp.Content.ReadFromJsonAsync<SubTaskDto>().ConfigureAwait(false);
            return dto?.Id;
        }

        public Task<bool> UpdateSubTaskAsync(int taskId, int subId, string title, bool isCompleted)
            => SendOkAsync($"api/tasks/{taskId}/subtasks/{subId}", new { Id = subId, Title = title, IsCompleted = isCompleted });

        public async Task<bool> DeleteSubTaskAsync(int taskId, int subId)
        {
            var resp = await _http.DeleteAsync($"api/tasks/{taskId}/subtasks/{subId}").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public Task<bool> AddTagAsync(int taskId, string value)
            => SendOkAsync($"api/tasks/{taskId}/tags", value);

        public async Task<bool> DeleteTagAsync(int taskId, int tagId)
        {
            var resp = await _http.DeleteAsync($"api/tasks/{taskId}/tags/{tagId}").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        public async Task<int?> AddAttachmentAsync(int taskId, string fileName, string? filePath, long sizeBytes)
        {
            var resp = await _http.PostAsJsonAsync($"api/tasks/{taskId}/attachments", new { Id = 0, FileName = fileName, FilePath = filePath, SizeBytes = sizeBytes }).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return null;
            var dto = await resp.Content.ReadFromJsonAsync<AttachmentDto>().ConfigureAwait(false);
            return dto?.Id;
        }

        public async Task<bool> DeleteAttachmentAsync(int taskId, int attId)
        {
            var resp = await _http.DeleteAsync($"api/tasks/{taskId}/attachments/{attId}").ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        private async Task<bool> SendOkAsync<T>(string url, T body)
        {
            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
            {
                Content = JsonContent.Create(body)
            };
            var resp = await _http.SendAsync(req).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }

        private record SubTaskDto(int Id, string Title, bool IsCompleted);
        private record AttachmentDto(int Id, string FileName, string? FilePath, long SizeBytes);
    }
}
