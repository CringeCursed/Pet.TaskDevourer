using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Pet.TaskDevourer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TasksController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetAll()
        {
            var items = await _db.Tasks
                .Include(t => t.SubTasks)
                .Include(t => t.Attachments)
                .Include(t => t.Tags)
                .AsNoTracking()
                .ToListAsync();
            return Ok(items.Select(t => TaskDto.FromEntity(t)));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskDto>> GetById(int id)
        {
            var t = await _db.Tasks
                .Include(x => x.SubTasks)
                .Include(x => x.Attachments)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            return Ok(TaskDto.FromEntity(t));
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> Create([FromBody] TaskDto dto)
        {
            var entity = dto.ToEntity();
            entity.Id = 0; 
            _db.Tasks.Add(entity);
            await _db.SaveChangesAsync();
            var result = TaskDto.FromEntity(entity);
            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] TaskDto dto)
        {
            var entity = await _db.Tasks
                .Include(x => x.SubTasks)
                .Include(x => x.Attachments)
                .Include(x => x.Tags)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound();

            entity.Title = dto.Title;
            entity.Description = dto.Description;
            entity.DueDate = dto.DueDate;
            entity.IsCompleted = dto.IsCompleted;

            _db.SubTasks.RemoveRange(entity.SubTasks);
            _db.Attachments.RemoveRange(entity.Attachments);
            _db.Tags.RemoveRange(entity.Tags);
            await _db.SaveChangesAsync();

            entity.SubTasks = dto.SubTasks.Select(s => new SubTaskEntity { Title = s.Title, IsCompleted = s.IsCompleted }).ToList();
            entity.Attachments = dto.Attachments.Select(a => new AttachmentEntity { FileName = a.FileName, FilePath = a.FilePath, SizeBytes = a.SizeBytes }).ToList();
            entity.Tags = dto.Tags.Select(v => new TagEntity { Value = v }).ToList();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var entity = await _db.Tasks.FindAsync(id);
            if (entity == null) return NotFound();
            _db.Tasks.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{taskId:int}/subtasks")]
        public async Task<ActionResult<SubTaskDto>> AddSubTask(int taskId, [FromBody] SubTaskDto dto)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return NotFound();
            var entity = new SubTaskEntity { Title = dto.Title, IsCompleted = dto.IsCompleted, TaskId = taskId };
            _db.SubTasks.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = taskId }, new SubTaskDto(entity.Id, entity.Title, entity.IsCompleted));
        }

        [HttpPatch("{taskId:int}/subtasks/{subId:int}")]
        public async Task<ActionResult> UpdateSubTask(int taskId, int subId, [FromBody] SubTaskDto dto)
        {
            var st = await _db.SubTasks.FirstOrDefaultAsync(s => s.Id == subId && s.TaskId == taskId);
            if (st == null) return NotFound();
            st.Title = dto.Title;
            st.IsCompleted = dto.IsCompleted;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{taskId:int}/subtasks/{subId:int}")]
        public async Task<ActionResult> DeleteSubTask(int taskId, int subId)
        {
            var st = await _db.SubTasks.FirstOrDefaultAsync(s => s.Id == subId && s.TaskId == taskId);
            if (st == null) return NotFound();
            _db.SubTasks.Remove(st);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{taskId:int}/tags")]
        public async Task<ActionResult> AddTag(int taskId, [FromBody] string value)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return NotFound();
            _db.Tags.Add(new TagEntity { TaskId = taskId, Value = value });
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{taskId:int}/tags/{tagId:int}")]
        public async Task<ActionResult> DeleteTag(int taskId, int tagId)
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId && t.TaskId == taskId);
            if (tag == null) return NotFound();
            _db.Tags.Remove(tag);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{taskId:int}/attachments")]
        public async Task<ActionResult<AttachmentDto>> AddAttachment(int taskId, [FromBody] AttachmentDto dto)
        {
            var task = await _db.Tasks.FindAsync(taskId);
            if (task == null) return NotFound();
            var entity = new AttachmentEntity { TaskId = taskId, FileName = dto.FileName, FilePath = dto.FilePath, SizeBytes = dto.SizeBytes };
            _db.Attachments.Add(entity);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = taskId }, new AttachmentDto(entity.Id, entity.FileName, entity.FilePath, entity.SizeBytes));
        }

        [HttpDelete("{taskId:int}/attachments/{attId:int}")]
        public async Task<ActionResult> DeleteAttachment(int taskId, int attId)
        {
            var att = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == attId && a.TaskId == taskId);
            if (att == null) return NotFound();
            _db.Attachments.Remove(att);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("sync")]
        public async Task<ActionResult> Sync([FromBody] IEnumerable<TaskDto> tasks)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            _db.SubTasks.RemoveRange(_db.SubTasks);
            _db.Attachments.RemoveRange(_db.Attachments);
            _db.Tags.RemoveRange(_db.Tags);
            _db.Tasks.RemoveRange(_db.Tasks);
            await _db.SaveChangesAsync();

            foreach (var dto in tasks)
            {
                var entity = dto.ToEntity();
                _db.Tasks.Add(entity);
            }
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }
    }

    public record TaskDto
    (
        int Id,
        string Title,
        string Description,
        DateTime DueDate,
        bool IsCompleted,
        List<string> Tags,
        List<SubTaskDto> SubTasks,
        List<AttachmentDto> Attachments
    )
    {
        public static TaskDto FromEntity(TaskEntity e) => new(
            e.Id, e.Title, e.Description, e.DueDate, e.IsCompleted,
            e.Tags.Select(t => t.Value).ToList(),
            e.SubTasks.Select(s => new SubTaskDto(s.Id, s.Title, s.IsCompleted)).ToList(),
            e.Attachments.Select(a => new AttachmentDto(a.Id, a.FileName, a.FilePath, a.SizeBytes)).ToList()
        );

        public TaskEntity ToEntity()
        {
            var entity = new TaskEntity
            {
                Id = Id,
                Title = Title,
                Description = Description,
                DueDate = DueDate,
                IsCompleted = IsCompleted
            };
            entity.Tags = Tags.Select(v => new TagEntity { Value = v }).ToList();
            entity.SubTasks = SubTasks.Select(s => new SubTaskEntity { Title = s.Title, IsCompleted = s.IsCompleted }).ToList();
            entity.Attachments = Attachments.Select(a => new AttachmentEntity { FileName = a.FileName, FilePath = a.FilePath, SizeBytes = a.SizeBytes }).ToList();
            return entity;
        }
    }

    public record SubTaskDto(int Id, string Title, bool IsCompleted);
    public record AttachmentDto(int Id, string FileName, string? FilePath, long SizeBytes);
}
