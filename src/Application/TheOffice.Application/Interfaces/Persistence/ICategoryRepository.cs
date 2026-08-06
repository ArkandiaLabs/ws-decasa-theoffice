using TheOffice.Domain.Entities;

namespace TheOffice.Application.Interfaces.Persistence;

public interface ICategoryRepository
{
  Task<IReadOnlyList<Category>> GetAll();
  Task<Category?> GetBySlug(string slug);
}
