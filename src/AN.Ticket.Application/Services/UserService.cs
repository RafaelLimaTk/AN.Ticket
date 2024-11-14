using AN.Ticket.Application.DTOs.User;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;
using AutoMapper;
using Microsoft.AspNetCore.Http;

namespace AN.Ticket.Application.Services;

public class UserService : Service<UserDto, User>, IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IFileService _fileService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(
        IRepository<User> repository,
        IUserRepository userRepository,
        IFileService fileService,
        IUnitOfWork unitOfWork,
        IMapper mapper
    )
        : base(repository)
    {
        _userRepository = userRepository;
        _fileService = fileService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> GetPaginatedUsersAsync(int pageNumber, int pageSize, string searchTerm = "")
    {
        var (users, totalItems) = await _userRepository.GetPaginatedUsersAsync(pageNumber, pageSize, searchTerm);

        var userDTOs = users.Select(u => new UserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            ProfilePicture = u.ProfilePicture,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList();

        return new PagedResult<UserDto>
        {
            Items = userDTOs,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await GetAllAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<bool> CreateUserAsync(UserDto userDto, IFormFile profilePicture)
    {
        var user = new User(userDto.Id, userDto.FullName, userDto.Email, userDto.Role);

        if (profilePicture is not null)
        {
            var filePath = await _fileService.SaveFileAsync(profilePicture);
            user.UpdateProfilePicture(filePath);
        }

        try
        {
            await _userRepository.SaveAsync(user);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> UpdateUserAsync(UserDto userDto, IFormFile profilePicture)
    {
        var user = await GetByIdAsync(userDto.Id);
        if (user is null)
            return false;

        user.UpdateFullName(userDto.FullName);
        user.UpdateEmail(userDto.Email);
        user.UpdateRole(userDto.Role);

        if (profilePicture is not null)
        {
            if (!string.IsNullOrEmpty(user.ProfilePicture))
                await _fileService.DeleteFileAsync(user.ProfilePicture);

            var filePath = await _fileService.SaveFileAsync(profilePicture);
            user.UpdateProfilePicture(filePath);
        }

        try
        {
            _userRepository.Update(user);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await GetByIdAsync(id);
        if (user is null)
            return false;

        _userRepository.Delete(user);

        await _unitOfWork.CommitAsync();
        return true;
    }
}
