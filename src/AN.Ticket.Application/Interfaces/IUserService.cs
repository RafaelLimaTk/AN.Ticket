using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces.Base;
using AN.Ticket.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace AN.Ticket.Application.Interfaces;

public interface IUserService : IService<UserDto, User>
{
    Task<bool> CreateUserAsync(UserDto userDto, IFormFile profilePicture);
    Task<bool> DeleteUserAsync(Guid id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<PagedResult<UserDto>> GetPaginatedUsersAsync(int pageNumber, int pageSize, string searchTerm = "");
    Task<bool> UpdateUserAsync(UserDto userDto, IFormFile profilePicture);
}
