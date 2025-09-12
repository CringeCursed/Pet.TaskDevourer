using Pet.TaskDevourer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pet.TaskDevourer.Services
{
    public interface ITaskService
    {
        Task<List<TaskItem>> LoadAllAsync();
        Task SaveAllAsync(List<TaskItem> tasks);
    }
}
