using AN.Ticket.Application.DTOs.Department;
using AN.Ticket.Application.Helpers.Pagination;
using AN.Ticket.Application.Interfaces;
using AN.Ticket.Application.Services.Base;
using AN.Ticket.Domain.Entities;
using AN.Ticket.Domain.Interfaces;
using AN.Ticket.Domain.Interfaces.Base;

namespace AN.Ticket.Application.Services;
public class DepartmentService : Service<DepartmentDto, Department>, IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentMemberRepository _departmentMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(
        IRepository<Department> repository,
        IDepartmentRepository departmentRepository,
        IDepartmentMemberRepository departmentMemberRepository,
        IUnitOfWork unitOfWork
    )
        : base(repository)
    {
        _departmentRepository = departmentRepository;
        _departmentMemberRepository = departmentMemberRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<DepartmentDto>> GetPaginatedDepartmentsAsync(int pageNumber, int pageSize, string searchTerm = "")
    {
        var (departments, totalItems) = await _departmentRepository.GetPaginatedDepartmentsAsync(pageNumber, pageSize, searchTerm);

        var departmentDtos = new List<DepartmentDto>();

        foreach (var department in departments)
        {
            var members = await _departmentMemberRepository.GetMembersByDepartmentIdAsync(department.Id);
            var memberDtos = members.Select(m => new DepartmentMemberDto
            {
                Id = m.Id,
                FullName = m.FullName,
                ProfilePictureUrl = m.ProfilePictureUrl
            }).ToList();

            var departmentDto = new DepartmentDto
            {
                Id = department.Id,
                Name = department.Name,
                Code = department.Code,
                Description = department.Description,
                Status = department.Status,
                MembersCount = department.Members.Count,
                Members = memberDtos
            };

            departmentDtos.Add(departmentDto);
        }

        return new PagedResult<DepartmentDto>
        {
            Items = departmentDtos,
            TotalItems = totalItems,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
